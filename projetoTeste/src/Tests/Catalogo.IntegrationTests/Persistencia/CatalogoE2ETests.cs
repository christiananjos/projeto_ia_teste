using System.Data.SqlTypes;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Catalogo.Application.Categorias.Dtos;
using Catalogo.Application.Comum;
using Catalogo.Application.Produtos.Dtos;
using Catalogo.IntegrationTests.Infra;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.IntegrationTests.Persistencia;

/// <summary>Massa via API para os testes ponta a ponta.</summary>
internal static class Massa
{
    public static async Task<Guid> CriarCategoriaAsync(this HttpClient cliente, string nome, bool ativa = true)
    {
        var ct = TestContext.Current.CancellationToken;
        var resposta = await cliente.PostAsJsonAsync("/api/v1/categorias", new { nome }, ct);
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = (await resposta.LerAsync<CategoriaResponse>()).Id;

        if (!ativa)
        {
            (await cliente.PutAsJsonAsync($"/api/v1/categorias/{id}", new { nome, ativa = false }, ct))
                .StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        return id;
    }

    public static Task<HttpResponseMessage> PostarProdutoAsync(
        this HttpClient cliente, Guid categoriaId, string nome, string sku, decimal preco = 100m) =>
        cliente.PostAsJsonAsync("/api/v1/produtos", new { categoriaId, nome, sku, preco }, TestContext.Current.CancellationToken);

    public static async Task<Guid> CriarProdutoAsync(
        this HttpClient cliente, Guid categoriaId, string nome, string sku, decimal preco = 100m, bool ativo = true)
    {
        var resposta = await cliente.PostarProdutoAsync(categoriaId, nome, sku, preco);
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = (await resposta.LerAsync<ProdutoResponse>()).Id;

        if (!ativo)
        {
            (await cliente.PatchAsJsonAsync($"/api/v1/produtos/{id}/status", new { ativo = false }, TestContext.Current.CancellationToken))
                .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        return id;
    }

    public static async Task<PaginaResponse<ProdutoResponse>> ListarProdutosAsync(this HttpClient cliente, string query = "")
    {
        var resposta = await cliente.GetAsync($"/api/v1/produtos{query}", TestContext.Current.CancellationToken);
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await resposta.LerAsync<PaginaResponse<ProdutoResponse>>();
    }
}

/// <summary>REQ-001, BR-006 e ADR-001 contra SQL Server real.</summary>
public sealed class CategoriasE2ETests(SqlServerFixture banco) : TesteComSqlServer(banco)
{
    [Fact]
    public async Task CriarEObter_PersisteComDatasUtc()
    {
        using var cliente = await PrepararAsync();
        var id = await cliente.CriarCategoriaAsync("Periféricos");

        var resposta = await cliente.GetAsync($"/api/v1/categorias/{id}", Ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var corpo = await resposta.LerJsonAsync();
        corpo.GetProperty("nome").GetString().ShouldBe("Periféricos");
        corpo.GetProperty("dataCriacao").GetString()!.ShouldEndWith("Z");
    }

    // REQ-001 / ADR-008: nome único sem diferenciar maiúsculas
    [Fact]
    public async Task NomeDuplicadoComOutraCaixa_Retorna409()
    {
        using var cliente = await PrepararAsync();
        await cliente.CriarCategoriaAsync("Periféricos");

        var resposta = await cliente.PostAsJsonAsync("/api/v1/categorias", new { nome = "PERIFÉRICOS" }, Ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.Conflict, "categoria.nome_duplicado");
    }

    [Fact]
    public async Task Atualizar_ComONomeAtual_NaoConflitaConsigoMesma()
    {
        using var cliente = await PrepararAsync();
        var id = await cliente.CriarCategoriaAsync("Periféricos");

        var resposta = await cliente.PutAsJsonAsync($"/api/v1/categorias/{id}", new { nome = "Periféricos", ativa = false }, Ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await resposta.LerAsync<CategoriaResponse>()).Ativa.ShouldBeFalse();
    }

    [Fact]
    public async Task Listar_FiltraPorNomeEAtivaEOrdenaPorNome()
    {
        using var cliente = await PrepararAsync();
        await cliente.CriarCategoriaAsync("Periféricos");
        await cliente.CriarCategoriaAsync("Acessórios");
        await cliente.CriarCategoriaAsync("Periféricos Antigos", ativa: false);

        var todas = await (await cliente.GetAsync("/api/v1/categorias", Ct)).LerAsync<PaginaResponse<CategoriaResponse>>();
        var ativasComPeri = await (await cliente.GetAsync("/api/v1/categorias?nome=PERI&ativa=true", Ct))
            .LerAsync<PaginaResponse<CategoriaResponse>>();

        todas.Itens.Select(c => c.Nome).ShouldBe(new[] { "Acessórios", "Periféricos", "Periféricos Antigos" });
        ativasComPeri.Itens.Select(c => c.Nome).ShouldBe(new[] { "Periféricos" });
    }

    [Fact]
    public async Task RemoverSemProdutos_Retorna204EApagaFisicamente()
    {
        using var cliente = await PrepararAsync();
        var id = await cliente.CriarCategoriaAsync("Periféricos");

        (await cliente.DeleteAsync($"/api/v1/categorias/{id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await cliente.GetAsync($"/api/v1/categorias/{id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ComContextoAsync(db => db.Categorias.CountAsync(Ct))).ShouldBe(0);
    }

    // BR-006: produto ativo ou inativo, não removido, bloqueia
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RemoverComProdutoNaoRemovido_Retorna409(bool produtoAtivo)
    {
        using var cliente = await PrepararAsync();
        var id = await cliente.CriarCategoriaAsync("Periféricos");
        await cliente.CriarProdutoAsync(id, "Teclado Mecânico", "TEC-001", ativo: produtoAtivo);

        var resposta = await cliente.DeleteAsync($"/api/v1/categorias/{id}", Ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.Conflict, "categoria.possui_produtos");
    }

    // ADR-001: só produtos removidos → 204 e os removidos são apagados junto
    [Fact]
    public async Task RemoverComApenasProdutosRemovidos_Retorna204EApagaOsProdutos()
    {
        using var cliente = await PrepararAsync();
        var id = await cliente.CriarCategoriaAsync("Periféricos");
        var produtoId = await cliente.CriarProdutoAsync(id, "Teclado Mecânico", "TEC-001");
        (await cliente.DeleteAsync($"/api/v1/produtos/{produtoId}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var resposta = await cliente.DeleteAsync($"/api/v1/categorias/{id}", Ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ComContextoAsync(db => db.Produtos.IgnoreQueryFilters().CountAsync(p => p.CategoriaId == id, Ct))).ShouldBe(0);
        (await ComContextoAsync(db => db.Categorias.AnyAsync(c => c.Id == id, Ct))).ShouldBeFalse();
    }
}

/// <summary>REQ-002 a REQ-004 e BR-001 a BR-005 contra SQL Server real.</summary>
public sealed class ProdutosE2ETests(SqlServerFixture banco) : TesteComSqlServer(banco)
{
    [Fact]
    public async Task CriarEObter_IncluiONomeDaCategoria()
    {
        using var cliente = await PrepararAsync();
        var categoriaId = await cliente.CriarCategoriaAsync("Periféricos");
        var id = await cliente.CriarProdutoAsync(categoriaId, "Teclado Mecânico", "TEC-001", 349.90m);

        var produto = await (await cliente.GetAsync($"/api/v1/produtos/{id}", Ct)).LerAsync<ProdutoResponse>();

        produto.CategoriaNome.ShouldBe("Periféricos");
        produto.Preco.ShouldBe(349.90m);
        produto.Sku.ShouldBe("TEC-001");
    }

    // BR-003 / ADR-008: SKU único global, sem diferenciar maiúsculas
    [Fact]
    public async Task SkuDuplicadoComOutraCaixaEmOutraCategoria_Retorna409()
    {
        using var cliente = await PrepararAsync();
        var perifericos = await cliente.CriarCategoriaAsync("Periféricos");
        var acessorios = await cliente.CriarCategoriaAsync("Acessórios");
        await cliente.CriarProdutoAsync(perifericos, "Teclado Mecânico", "TEC-001");

        var resposta = await cliente.PostarProdutoAsync(acessorios, "Outro Teclado", "tec-001");

        await resposta.DeveSerProblemaAsync(HttpStatusCode.Conflict, "produto.sku_duplicado");
    }

    // BR-001: único na categoria, sem diferenciar maiúsculas; em outra categoria pode
    [Fact]
    public async Task NomeDuplicadoNaMesmaCategoria_Retorna409EmOutraCategoriaAceita()
    {
        using var cliente = await PrepararAsync();
        var perifericos = await cliente.CriarCategoriaAsync("Periféricos");
        var acessorios = await cliente.CriarCategoriaAsync("Acessórios");
        await cliente.CriarProdutoAsync(perifericos, "Teclado Mecânico", "TEC-001");

        var mesmaCategoria = await cliente.PostarProdutoAsync(perifericos, "TECLADO MECÂNICO", "TEC-002");
        var outraCategoria = await cliente.PostarProdutoAsync(acessorios, "Teclado Mecânico", "TEC-003");

        await mesmaCategoria.DeveSerProblemaAsync(HttpStatusCode.Conflict, "produto.nome_duplicado");
        outraCategoria.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    // BR-004
    [Fact]
    public async Task CriarEmCategoriaInativa_Retorna422()
    {
        using var cliente = await PrepararAsync();
        var inativa = await cliente.CriarCategoriaAsync("Descontinuados", ativa: false);

        var resposta = await cliente.PostarProdutoAsync(inativa, "Teclado Mecânico", "TEC-001");

        await resposta.DeveSerProblemaAsync(HttpStatusCode.UnprocessableEntity, "produto.categoria_invalida");
    }

    // BR-005: remoção lógica libera SKU e nome para reuso
    [Fact]
    public async Task DepoisDeRemover_SkuENomePodemSerReusados()
    {
        using var cliente = await PrepararAsync();
        var categoriaId = await cliente.CriarCategoriaAsync("Periféricos");
        var id = await cliente.CriarProdutoAsync(categoriaId, "Teclado Mecânico", "TEC-001");
        (await cliente.DeleteAsync($"/api/v1/produtos/{id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var resposta = await cliente.PostarProdutoAsync(categoriaId, "Teclado Mecânico", "TEC-001");

        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await ComContextoAsync(db => db.Produtos.IgnoreQueryFilters().CountAsync(Ct))).ShouldBe(2);
    }

    // BR-005: removido some de todas as consultas e de toda alteração
    [Fact]
    public async Task ProdutoRemovido_Retorna404EmTodasAsOperacoes()
    {
        using var cliente = await PrepararAsync();
        var categoriaId = await cliente.CriarCategoriaAsync("Periféricos");
        var id = await cliente.CriarProdutoAsync(categoriaId, "Teclado Mecânico", "TEC-001");
        (await cliente.DeleteAsync($"/api/v1/produtos/{id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await cliente.GetAsync($"/api/v1/produtos/{id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await cliente.DeleteAsync($"/api/v1/produtos/{id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await cliente.PatchAsJsonAsync($"/api/v1/produtos/{id}/status", new { ativo = false }, Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await cliente.PutAsJsonAsync($"/api/v1/produtos/{id}", new { categoriaId, nome = "Teclado", preco = 1m }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await cliente.ListarProdutosAsync()).TotalItens.ShouldBe(0);

        var dataRemocao = await ComContextoAsync(db => db.Produtos.IgnoreQueryFilters().Where(p => p.Id == id).Select(p => p.DataRemocao).SingleAsync(Ct));
        dataRemocao.ShouldNotBeNull();
        dataRemocao.Value.Kind.ShouldBe(DateTimeKind.Utc);
    }

    // BR-003: o PUT não altera o SKU
    [Fact]
    public async Task Atualizar_NaoAlteraOSku()
    {
        using var cliente = await PrepararAsync();
        var categoriaId = await cliente.CriarCategoriaAsync("Periféricos");
        var id = await cliente.CriarProdutoAsync(categoriaId, "Teclado Mecânico", "TEC-001");

        var resposta = await cliente.PutAsJsonAsync($"/api/v1/produtos/{id}",
            new { categoriaId, nome = "Teclado Mecânico RGB", preco = 399.90m, sku = "NOVO-SKU" }, Ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var salvo = await (await cliente.GetAsync($"/api/v1/produtos/{id}", Ct)).LerAsync<ProdutoResponse>();
        salvo.Sku.ShouldBe("TEC-001");
        salvo.Nome.ShouldBe("Teclado Mecânico RGB");
        salvo.DataAtualizacao.ShouldBeGreaterThanOrEqualTo(salvo.DataCriacao);
    }

    // REQ-004 / ADR-006
    [Fact]
    public async Task Inativar_TiraDaListagemPadraoMasContinuaNoGetPorId()
    {
        using var cliente = await PrepararAsync();
        var categoriaId = await cliente.CriarCategoriaAsync("Periféricos");
        var id = await cliente.CriarProdutoAsync(categoriaId, "Teclado Mecânico", "TEC-001", ativo: false);

        (await cliente.ListarProdutosAsync()).TotalItens.ShouldBe(0);
        (await cliente.ListarProdutosAsync("?ativo=false")).Itens.Single().Id.ShouldBe(id);
        (await (await cliente.GetAsync($"/api/v1/produtos/{id}", Ct)).LerAsync<ProdutoResponse>()).Ativo.ShouldBeFalse();
    }

    // ADR-006: PATCH com o mesmo valor não muda dataAtualizacao
    [Fact]
    public async Task AlterarStatusComOMesmoValor_EhIdempotente()
    {
        using var cliente = await PrepararAsync();
        var categoriaId = await cliente.CriarCategoriaAsync("Periféricos");
        var id = await cliente.CriarProdutoAsync(categoriaId, "Teclado Mecânico", "TEC-001");
        var antes = await (await cliente.GetAsync($"/api/v1/produtos/{id}", Ct)).LerAsync<ProdutoResponse>();

        (await cliente.PatchAsJsonAsync($"/api/v1/produtos/{id}/status", new { ativo = true }, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var depois = await (await cliente.GetAsync($"/api/v1/produtos/{id}", Ct)).LerAsync<ProdutoResponse>();
        depois.DataAtualizacao.ShouldBe(antes.DataAtualizacao);
    }

    // REQ-003: filtros, ordenação, paginação e desempate por id
    [Fact]
    public async Task Listar_FiltraOrdenaEPagina()
    {
        using var cliente = await PrepararAsync();
        var perifericos = await cliente.CriarCategoriaAsync("Periféricos");
        var acessorios = await cliente.CriarCategoriaAsync("Acessórios");
        await cliente.CriarProdutoAsync(perifericos, "Teclado Mecânico", "TEC-001", 349.90m);
        await cliente.CriarProdutoAsync(perifericos, "Mouse Gamer", "MOU-001", 199.90m);
        await cliente.CriarProdutoAsync(perifericos, "Headset", "HEA-001", 499.00m);
        await cliente.CriarProdutoAsync(acessorios, "Teclado Compacto", "TEC-002", 149.90m);

        var porNome = await cliente.ListarProdutosAsync();
        var porPrecoDesc = await cliente.ListarProdutosAsync("?ordenarPor=preco&direcao=desc");
        var tecladosCi = await cliente.ListarProdutosAsync("?nome=TECLADO");
        var daCategoria = await cliente.ListarProdutosAsync($"?categoriaId={acessorios}");
        var segundaPagina = await cliente.ListarProdutosAsync("?page=2&pageSize=3");
        var alemDoFim = await cliente.ListarProdutosAsync("?page=5&pageSize=3");

        porNome.Itens.Select(p => p.Nome).ShouldBe(new[] { "Headset", "Mouse Gamer", "Teclado Compacto", "Teclado Mecânico" });
        porPrecoDesc.Itens.Select(p => p.Preco).ShouldBe(new[] { 499.00m, 349.90m, 199.90m, 149.90m });
        tecladosCi.Itens.Select(p => p.Nome).ShouldBe(new[] { "Teclado Compacto", "Teclado Mecânico" });
        daCategoria.Itens.Single().CategoriaNome.ShouldBe("Acessórios");
        segundaPagina.Itens.Select(p => p.Nome).ShouldBe(new[] { "Teclado Mecânico" });
        segundaPagina.TotalItens.ShouldBe(4);
        segundaPagina.TotalPaginas.ShouldBe(2);
        alemDoFim.Itens.ShouldBeEmpty();
        alemDoFim.TotalItens.ShouldBe(4);
    }

    // ADR-009: desempate por id asc, na ordem do SQL Server para uniqueidentifier
    [Fact]
    public async Task Listar_ComPrecosIguais_DesempataPorId()
    {
        using var cliente = await PrepararAsync();
        var categoriaId = await cliente.CriarCategoriaAsync("Periféricos");
        var ids = new List<Guid>();
        for (var i = 1; i <= 5; i++)
            ids.Add(await cliente.CriarProdutoAsync(categoriaId, $"Cabo USB {i}", $"CAB-00{i}", 19.90m));

        var pagina = await cliente.ListarProdutosAsync("?ordenarPor=preco");

        pagina.Itens.Select(p => p.Id).ShouldBe(ids.OrderBy(id => new SqlGuid(id)));
    }

    // Segurança: curingas do LIKE no filtro são tratados como texto
    [Fact]
    public async Task Listar_ComCuringasNoFiltroDeNome_TrataComoTexto()
    {
        using var cliente = await PrepararAsync();
        var categoriaId = await cliente.CriarCategoriaAsync("Periféricos");
        await cliente.CriarProdutoAsync(categoriaId, "Teclado Mecânico", "TEC-001");

        (await cliente.ListarProdutosAsync("?nome=%25")).TotalItens.ShouldBe(0);
        (await cliente.ListarProdutosAsync("?nome=_")).TotalItens.ShouldBe(0);
    }

    [Fact]
    public async Task Serializacao_UsaCamelCaseENaoExpoeDataRemocao()
    {
        using var cliente = await PrepararAsync();
        var categoriaId = await cliente.CriarCategoriaAsync("Periféricos");
        var id = await cliente.CriarProdutoAsync(categoriaId, "Teclado Mecânico", "TEC-001");

        var corpo = await (await cliente.GetAsync($"/api/v1/produtos/{id}", Ct)).LerJsonAsync();

        corpo.EnumerateObject().Select(p => p.Name).ShouldBe(new[]
        {
            "id", "categoriaId", "categoriaNome", "nome", "sku", "descricao", "preco", "ativo", "dataCriacao", "dataAtualizacao"
        });
        corpo.GetProperty("descricao").ValueKind.ShouldBe(JsonValueKind.Null);
    }
}
