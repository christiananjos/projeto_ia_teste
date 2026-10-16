using System.Net;
using System.Net.Http.Json;
using Catalogo.Application.Comum;
using Catalogo.Application.Produtos.Dtos;
using Catalogo.Domain.Categorias;
using Catalogo.Domain.Excecoes;
using Catalogo.Domain.Produtos;
using Catalogo.IntegrationTests.Infra;
using NSubstitute.ExceptionExtensions;

namespace Catalogo.IntegrationTests.Api;

/// <summary>REQ-002 a REQ-004 pelo pipeline HTTP, com a persistência substituída.</summary>
public sealed class ProdutoEndpointsTests : IClassFixture<ApiComSubstitutosFactory>, IDisposable
{
    private const string Rota = "/api/v1/produtos";

    private readonly ApiComSubstitutosFactory _api;
    private readonly HttpClient _admin;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;
    private readonly Categoria _categoria = Categoria.Criar("Periféricos", null, DateTime.UtcNow);

    public ProdutoEndpointsTests(ApiComSubstitutosFactory api)
    {
        _api = api;
        _api.ReiniciarSubstitutos();
        _admin = _api.ClienteAdmin();
        Mocks.Categorias.ObterPorIdAsync(_categoria.Id, Arg.Any<CancellationToken>()).Returns(_categoria);
    }

    public void Dispose() => _admin.Dispose();

    private Substitutos Mocks => _api.Mocks;

    private object CorpoDeCriacao(string sku = "TEC-001") =>
        new { categoriaId = _categoria.Id, nome = "Teclado Mecânico", sku, descricao = "ABNT2", preco = 349.90m };

    private Produto ProdutoExistente(bool ativo = true)
    {
        var produto = Produto.Criar(_categoria.Id, "Teclado Mecânico", Sku.Criar("TEC-001"), "ABNT2", 349.90m, DateTime.UtcNow);
        if (!ativo)
            produto.DefinirStatus(false, DateTime.UtcNow);
        Mocks.Produtos.ObterPorIdAsync(produto.Id, Arg.Any<CancellationToken>()).Returns(produto);
        return produto;
    }

    [Fact]
    public async Task Post_ComDadosValidos_Retorna201ComNomeDaCategoria()
    {
        var resposta = await _admin.PostAsJsonAsync(Rota, CorpoDeCriacao(), _ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var produto = await resposta.LerAsync<ProdutoResponse>();
        resposta.Headers.Location!.ToString().ShouldBe($"{Rota}/{produto.Id}");
        produto.CategoriaNome.ShouldBe("Periféricos");
        produto.Sku.ShouldBe("TEC-001");
        produto.Preco.ShouldBe(349.90m);
        produto.Ativo.ShouldBeTrue();
        // ADR-006: dataRemocao nunca é exposta
        (await resposta.LerJsonAsync()).TryGetProperty("dataRemocao", out _).ShouldBeFalse();
    }

    /// <summary>Regressão de [[SKU aceita quebra de linha no final]] no contrato HTTP.</summary>
    [Theory]
    [InlineData("TEC-001\n")]
    [InlineData("AB\n")]
    public async Task Post_ComSkuTerminandoEmQuebraDeLinha_Retorna400(string sku)
    {
        var resposta = await _admin.PostAsJsonAsync(Rota, CorpoDeCriacao(sku), _ct);

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "validacao");
        problema.ChavesDeErros().ShouldBe(new[] { "sku" });
        Mocks.Produtos.DidNotReceiveWithAnyArgs().Adicionar(default!);
    }

    [Fact]
    public async Task Post_ComVariosCamposInvalidos_Retorna400ComTodosOsCamposEmCamelCase()
    {
        var resposta = await _admin.PostAsJsonAsync(Rota, new { nome = "ab", sku = "", preco = 0.001m }, _ct);

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "validacao");
        problema.ChavesDeErros().ShouldBe(new[] { "categoriaId", "nome", "sku", "preco" }, ignoreOrder: true);
    }

    // BR-004
    [Fact]
    public async Task Post_ComCategoriaInativa_Retorna422()
    {
        _categoria.Atualizar(_categoria.Nome, null, ativa: false, DateTime.UtcNow);

        var resposta = await _admin.PostAsJsonAsync(Rota, CorpoDeCriacao(), _ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.UnprocessableEntity, "produto.categoria_invalida");
    }

    // BR-003
    [Fact]
    public async Task Post_ComSkuDuplicado_Retorna409()
    {
        Mocks.Produtos.ExisteSkuAsync(Sku.Criar("TEC-001"), Arg.Any<CancellationToken>()).Returns(true);

        var resposta = await _admin.PostAsJsonAsync(Rota, CorpoDeCriacao(), _ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.Conflict, "produto.sku_duplicado");
    }

    // BR-001
    [Fact]
    public async Task Post_ComNomeDuplicadoNaCategoria_Retorna409()
    {
        Mocks.Produtos.ExisteNomeNaCategoriaAsync(_categoria.Id, "Teclado Mecânico", null, Arg.Any<CancellationToken>()).Returns(true);

        var resposta = await _admin.PostAsJsonAsync(Rota, CorpoDeCriacao(), _ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.Conflict, "produto.nome_duplicado");
    }

    [Fact]
    public async Task Post_ComCorridaDeSkuNoBanco_Retorna409DeSku()
    {
        Mocks.UnitOfWork.SalvarAlteracoesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflitoPersistenciaException(TipoConflitoPersistencia.Unicidade, "UX_Produtos_Sku"));

        var resposta = await _admin.PostAsJsonAsync(Rota, CorpoDeCriacao(), _ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.Conflict, "produto.sku_duplicado");
    }

    [Fact]
    public async Task GetPorId_ProdutoInativo_Retorna200()
    {
        var produto = ProdutoResponse.De(ProdutoExistente(ativo: false), "Periféricos");
        Mocks.ConsultasProduto.ObterPorIdAsync(produto.Id, Arg.Any<CancellationToken>()).Returns(produto);

        var resposta = await _admin.GetAsync($"{Rota}/{produto.Id}", _ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var corpo = await resposta.LerAsync<ProdutoResponse>();
        corpo.ShouldBe(produto);
        corpo.Ativo.ShouldBeFalse();
    }

    [Fact]
    public async Task GetPorId_InexistenteOuRemovido_Retorna404()
    {
        var resposta = await _admin.GetAsync($"{Rota}/{Guid.CreateVersion7()}", _ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.NotFound, "produto.nao_encontrado");
    }

    // REQ-003 / REQ-004: sem filtros → página 1, 20 itens, ativo implícito (nulo vira true na consulta)
    [Fact]
    public async Task Listar_SemParametros_UsaOsPadroes()
    {
        Mocks.ConsultasProduto.ListarAsync(default!, _ct).ReturnsForAnyArgs(PaginaResponse<ProdutoResponse>.Criar([], 1, 20, 0));

        var resposta = await _admin.GetAsync(Rota, _ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        await Mocks.ConsultasProduto.Received(1).ListarAsync(new ListarProdutosQuery(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Listar_RepassaFiltrosEOrdenacao()
    {
        var categoriaId = _categoria.Id;
        Mocks.ConsultasProduto.ListarAsync(default!, _ct).ReturnsForAnyArgs(PaginaResponse<ProdutoResponse>.Criar([], 2, 5, 0));

        var resposta = await _admin.GetAsync(
            $"{Rota}?page=2&pageSize=5&categoriaId={categoriaId}&nome=teclado&ativo=false&ordenarPor=preco&direcao=desc", _ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        await Mocks.ConsultasProduto.Received(1).ListarAsync(
            new ListarProdutosQuery(2, 5, categoriaId, "teclado", false, "preco", "desc"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Listar_DevolveOsItensNoFormatoDaSpec()
    {
        var item = ProdutoResponse.De(ProdutoExistente(), "Periféricos");
        Mocks.ConsultasProduto.ListarAsync(default!, _ct).ReturnsForAnyArgs(PaginaResponse<ProdutoResponse>.Criar([item], 1, 20, 1));

        var corpo = await (await _admin.GetAsync(Rota, _ct)).LerJsonAsync();

        corpo.GetProperty("totalItens").GetInt32().ShouldBe(1);
        corpo.GetProperty("totalPaginas").GetInt32().ShouldBe(1);
        var primeiro = corpo.GetProperty("itens")[0];
        primeiro.GetProperty("categoriaNome").GetString().ShouldBe("Periféricos");
        primeiro.GetProperty("sku").GetString().ShouldBe("TEC-001");
        primeiro.GetProperty("dataCriacao").GetString()!.ShouldEndWith("Z");
    }

    [Theory]
    [InlineData("ordenarPor=data", "ordenarPor")]
    [InlineData("direcao=cima", "direcao")]
    [InlineData("pageSize=101", "pageSize")]
    [InlineData("page=0", "page")]
    public async Task Listar_ComParametroForaDoDominio_Retorna400(string query, string campo)
    {
        var resposta = await _admin.GetAsync($"{Rota}?{query}", _ct);

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "validacao");
        problema.ChavesDeErros().ShouldBe(new[] { campo });
    }

    [Theory]
    [InlineData("page=abc")]
    [InlineData("ativo=talvez")]
    [InlineData("categoriaId=x")]
    public async Task Listar_ComTipoInvalidoNaQueryString_Retorna400(string query)
    {
        var resposta = await _admin.GetAsync($"{Rota}?{query}", _ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "requisicao_invalida");
    }

    // BR-003 / ADR-006: sku e ativo no corpo do PUT são ignorados
    [Fact]
    public async Task Put_IgnoraSkuEAtivoEnviadosNoCorpo()
    {
        var produto = ProdutoExistente();

        var resposta = await _admin.PutAsJsonAsync($"{Rota}/{produto.Id}", new
        {
            categoriaId = _categoria.Id,
            nome = "Teclado Mecânico RGB",
            descricao = "ABNT2 com iluminação",
            preco = 399.90m,
            sku = "OUTRO-SKU",
            ativo = false
        }, _ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var corpo = await resposta.LerAsync<ProdutoResponse>();
        corpo.Nome.ShouldBe("Teclado Mecânico RGB");
        corpo.Preco.ShouldBe(399.90m);
        corpo.Sku.ShouldBe("TEC-001");
        corpo.Ativo.ShouldBeTrue();
    }

    [Fact]
    public async Task Put_Inexistente_Retorna404()
    {
        var resposta = await _admin.PutAsJsonAsync($"{Rota}/{Guid.CreateVersion7()}",
            new { categoriaId = _categoria.Id, nome = "Teclado Mecânico", preco = 10m }, _ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.NotFound, "produto.nao_encontrado");
    }

    // BR-004
    [Fact]
    public async Task Put_MovendoParaCategoriaInexistente_Retorna422()
    {
        var produto = ProdutoExistente();

        var resposta = await _admin.PutAsJsonAsync($"{Rota}/{produto.Id}",
            new { categoriaId = Guid.CreateVersion7(), nome = "Teclado Mecânico", preco = 10m }, _ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.UnprocessableEntity, "produto.categoria_invalida");
    }

    [Fact]
    public async Task Put_SemPreco_Retorna400()
    {
        var resposta = await _admin.PutAsJsonAsync($"{Rota}/{Guid.CreateVersion7()}",
            new { categoriaId = _categoria.Id, nome = "Teclado Mecânico" }, _ct);

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "validacao");
        problema.ChavesDeErros().ShouldBe(new[] { "preco" });
    }

    [Fact]
    public async Task Put_ComNomeDuplicado_Retorna409()
    {
        var produto = ProdutoExistente();
        Mocks.Produtos.ExisteNomeNaCategoriaAsync(_categoria.Id, "Mouse Gamer", produto.Id, Arg.Any<CancellationToken>()).Returns(true);

        var resposta = await _admin.PutAsJsonAsync($"{Rota}/{produto.Id}",
            new { categoriaId = _categoria.Id, nome = "Mouse Gamer", preco = 10m }, _ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.Conflict, "produto.nome_duplicado");
    }

    // REQ-004
    [Fact]
    public async Task PatchStatus_Inativar_Retorna204()
    {
        var produto = ProdutoExistente();

        var resposta = await _admin.PatchAsJsonAsync($"{Rota}/{produto.Id}/status", new { ativo = false }, _ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        produto.Ativo.ShouldBeFalse();
        await Mocks.UnitOfWork.Received(1).SalvarAlteracoesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchStatus_SemAtivo_Retorna400()
    {
        var resposta = await _admin.PatchAsJsonAsync($"{Rota}/{Guid.CreateVersion7()}/status", new { }, _ct);

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "validacao");
        problema.ChavesDeErros().ShouldBe(new[] { "ativo" });
    }

    [Fact]
    public async Task PatchStatus_Inexistente_Retorna404()
    {
        var resposta = await _admin.PatchAsJsonAsync($"{Rota}/{Guid.CreateVersion7()}/status", new { ativo = true }, _ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.NotFound, "produto.nao_encontrado");
    }

    // BR-005
    [Fact]
    public async Task Delete_ProdutoExistente_Retorna204EMarcaRemocao()
    {
        var produto = ProdutoExistente();

        var resposta = await _admin.DeleteAsync($"{Rota}/{produto.Id}", _ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        produto.EstaRemovido.ShouldBeTrue();
    }

    // BR-005: removido não é encontrado
    [Fact]
    public async Task Delete_JaRemovido_Retorna404()
    {
        var resposta = await _admin.DeleteAsync($"{Rota}/{Guid.CreateVersion7()}", _ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.NotFound, "produto.nao_encontrado");
    }
}
