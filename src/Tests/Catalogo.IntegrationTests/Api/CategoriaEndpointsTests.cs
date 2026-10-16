using System.Net;
using System.Net.Http.Json;
using Catalogo.Application.Categorias.Dtos;
using Catalogo.Application.Comum;
using Catalogo.Domain.Categorias;
using Catalogo.Domain.Excecoes;
using Catalogo.IntegrationTests.Infra;
using NSubstitute.ExceptionExtensions;

namespace Catalogo.IntegrationTests.Api;

/// <summary>REQ-001 pelo pipeline HTTP, com a persistência substituída.</summary>
public sealed class CategoriaEndpointsTests : IClassFixture<ApiComSubstitutosFactory>, IDisposable
{
    private const string Rota = "/api/v1/categorias";

    private readonly ApiComSubstitutosFactory _api;
    private readonly HttpClient _admin;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public CategoriaEndpointsTests(ApiComSubstitutosFactory api)
    {
        _api = api;
        _api.ReiniciarSubstitutos();
        _admin = _api.ClienteAdmin();
    }

    public void Dispose() => _admin.Dispose();

    private Substitutos Mocks => _api.Mocks;

    [Fact]
    public async Task Post_ComDadosValidos_Retorna201ComLocationECorpo()
    {
        var resposta = await _admin.PostAsJsonAsync(Rota, new { nome = "Periféricos", descricao = "Teclados e mouses" }, _ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var corpo = await resposta.LerJsonAsync();
        var id = corpo.GetProperty("id").GetGuid();
        resposta.Headers.Location!.ToString().ShouldBe($"{Rota}/{id}");
        corpo.GetProperty("nome").GetString().ShouldBe("Periféricos");
        corpo.GetProperty("ativa").GetBoolean().ShouldBeTrue();
        // BR-007: datas do servidor, em UTC, serializadas com sufixo Z
        corpo.GetProperty("dataCriacao").GetString()!.ShouldEndWith("Z");
        corpo.GetProperty("dataAtualizacao").GetString().ShouldBe(corpo.GetProperty("dataCriacao").GetString());
        await Mocks.UnitOfWork.Received(1).SalvarAlteracoesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Post_ComDatasNoCorpo_IgnoraOsValoresDoCliente()
    {
        var resposta = await _admin.PostAsJsonAsync(Rota, new { nome = "Periféricos", dataCriacao = "2000-01-01T00:00:00Z" }, _ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await resposta.LerJsonAsync()).GetProperty("dataCriacao").GetString()!.ShouldNotStartWith("2000-01-01");
    }

    [Fact]
    public async Task Post_ComNomeComEspacoNasPontas_Retorna400ComErroPorCampo()
    {
        var resposta = await _admin.PostAsJsonAsync(Rota, new { nome = " Periféricos" }, _ct);

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "validacao");
        problema.ChavesDeErros().ShouldBe(new[] { "nome" });
        problema.GetProperty("errors").GetProperty("nome")[0].GetString()
            .ShouldBe("O nome não pode começar nem terminar com espaços.");
        await Mocks.Categorias.DidNotReceiveWithAnyArgs().ExisteNomeAsync(default!, default, _ct);
    }

    [Fact]
    public async Task Post_ComJsonMalformado_Retorna400()
    {
        var resposta = await _admin.PostAsync(Rota, HttpExtensions.JsonBruto("{ \"nome\": "), _ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "requisicao_invalida");
    }

    [Fact]
    public async Task Post_ComNomeDuplicado_Retorna409()
    {
        Mocks.Categorias.ExisteNomeAsync("Periféricos", null, Arg.Any<CancellationToken>()).Returns(true);

        var resposta = await _admin.PostAsJsonAsync(Rota, new { nome = "Periféricos" }, _ct);

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.Conflict, "categoria.nome_duplicado");
        problema.GetProperty("detail").GetString().ShouldBe("Já existe uma categoria com este nome.");
        problema.GetProperty("instance").GetString().ShouldBe(Rota);
    }

    [Fact]
    public async Task GetPorId_Existente_Retorna200()
    {
        var categoria = CategoriaResponse.De(Categoria.Criar("Periféricos", null, DateTime.UtcNow));
        Mocks.ConsultasCategoria.ObterPorIdAsync(categoria.Id, Arg.Any<CancellationToken>()).Returns(categoria);

        var resposta = await _admin.GetAsync($"{Rota}/{categoria.Id}", _ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await resposta.LerAsync<CategoriaResponse>()).ShouldBe(categoria);
    }

    [Fact]
    public async Task GetPorId_Inexistente_Retorna404ComCodigoDeNegocio()
    {
        var resposta = await _admin.GetAsync($"{Rota}/{Guid.CreateVersion7()}", _ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.NotFound, "categoria.nao_encontrada");
    }

    // Spec técnica §6.2: id malformado não casa com {id:guid}
    [Fact]
    public async Task GetPorId_ComIdQueNaoEhGuid_Retorna404DeRota()
    {
        var resposta = await _admin.GetAsync($"{Rota}/abc", _ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.NotFound, "nao_encontrado");
    }

    [Fact]
    public async Task Listar_SemParametros_UsaPagina1Tamanho20()
    {
        var pagina = PaginaResponse<CategoriaResponse>.Criar([], 1, 20, 0);
        Mocks.ConsultasCategoria.ListarAsync(new ListarCategoriasQuery(), Arg.Any<CancellationToken>()).Returns(pagina);

        var resposta = await _admin.GetAsync(Rota, _ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var corpo = await resposta.LerJsonAsync();
        corpo.GetProperty("itens").GetArrayLength().ShouldBe(0);
        corpo.GetProperty("page").GetInt32().ShouldBe(1);
        corpo.GetProperty("pageSize").GetInt32().ShouldBe(20);
        corpo.GetProperty("totalItens").GetInt32().ShouldBe(0);
        corpo.GetProperty("totalPaginas").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task Listar_RepassaFiltrosDaQueryString()
    {
        Mocks.ConsultasCategoria.ListarAsync(default!, _ct).ReturnsForAnyArgs(PaginaResponse<CategoriaResponse>.Criar([], 2, 5, 0));

        var resposta = await _admin.GetAsync($"{Rota}?page=2&pageSize=5&nome=peri&ativa=false", _ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        await Mocks.ConsultasCategoria.Received(1).ListarAsync(new ListarCategoriasQuery(2, 5, "peri", false), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("page=0", "page")]
    [InlineData("pageSize=101", "pageSize")]
    public async Task Listar_ComPaginacaoForaDosLimites_Retorna400(string query, string campo)
    {
        var resposta = await _admin.GetAsync($"{Rota}?{query}", _ct);

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "validacao");
        problema.ChavesDeErros().ShouldBe(new[] { campo });
    }

    [Theory]
    [InlineData("page=abc")]
    [InlineData("ativa=talvez")]
    public async Task Listar_ComTipoInvalidoNaQueryString_Retorna400(string query)
    {
        var resposta = await _admin.GetAsync($"{Rota}?{query}", _ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "requisicao_invalida");
    }

    [Fact]
    public async Task Put_ComDadosValidos_Retorna200ComOsNovosValores()
    {
        var categoria = Categoria.Criar("Periféricos", null, DateTime.UtcNow);
        Mocks.Categorias.ObterPorIdAsync(categoria.Id, Arg.Any<CancellationToken>()).Returns(categoria);

        var resposta = await _admin.PutAsJsonAsync($"{Rota}/{categoria.Id}",
            new { nome = "Periféricos Gamer", descricao = "Linha gamer", ativa = false }, _ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var corpo = await resposta.LerAsync<CategoriaResponse>();
        corpo.Nome.ShouldBe("Periféricos Gamer");
        corpo.Ativa.ShouldBeFalse();
    }

    // ADR-005: "ativa" é obrigatório no PUT
    [Fact]
    public async Task Put_SemAtiva_Retorna400()
    {
        var resposta = await _admin.PutAsJsonAsync($"{Rota}/{Guid.CreateVersion7()}", new { nome = "Periféricos" }, _ct);

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "validacao");
        problema.ChavesDeErros().ShouldBe(new[] { "ativa" });
    }

    [Fact]
    public async Task Put_Inexistente_Retorna404()
    {
        var resposta = await _admin.PutAsJsonAsync($"{Rota}/{Guid.CreateVersion7()}", new { nome = "Periféricos", ativa = true }, _ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.NotFound, "categoria.nao_encontrada");
    }

    [Fact]
    public async Task Put_ComNomeDeOutraCategoria_Retorna409()
    {
        var categoria = Categoria.Criar("Periféricos", null, DateTime.UtcNow);
        Mocks.Categorias.ObterPorIdAsync(categoria.Id, Arg.Any<CancellationToken>()).Returns(categoria);
        Mocks.Categorias.ExisteNomeAsync("Acessórios", categoria.Id, Arg.Any<CancellationToken>()).Returns(true);

        var resposta = await _admin.PutAsJsonAsync($"{Rota}/{categoria.Id}", new { nome = "Acessórios", ativa = true }, _ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.Conflict, "categoria.nome_duplicado");
    }

    [Fact]
    public async Task Delete_SemProdutos_Retorna204()
    {
        var categoria = Categoria.Criar("Periféricos", null, DateTime.UtcNow);
        Mocks.Categorias.ObterPorIdAsync(categoria.Id, Arg.Any<CancellationToken>()).Returns(categoria);

        var resposta = await _admin.DeleteAsync($"{Rota}/{categoria.Id}", _ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await Mocks.Categorias.Received(1).ExcluirComProdutosRemovidosAsync(categoria.Id, Arg.Any<CancellationToken>());
    }

    // BR-006
    [Fact]
    public async Task Delete_ComProdutoNaoRemovido_Retorna409()
    {
        var categoria = Categoria.Criar("Periféricos", null, DateTime.UtcNow);
        Mocks.Categorias.ObterPorIdAsync(categoria.Id, Arg.Any<CancellationToken>()).Returns(categoria);
        Mocks.Categorias.PossuiProdutosNaoRemovidosAsync(categoria.Id, Arg.Any<CancellationToken>()).Returns(true);

        var resposta = await _admin.DeleteAsync($"{Rota}/{categoria.Id}", _ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.Conflict, "categoria.possui_produtos");
    }

    // ADR-001: corrida com produto criado depois da checagem
    [Fact]
    public async Task Delete_ComCorridaDeFk_Retorna409()
    {
        var categoria = Categoria.Criar("Periféricos", null, DateTime.UtcNow);
        Mocks.Categorias.ObterPorIdAsync(categoria.Id, Arg.Any<CancellationToken>()).Returns(categoria);
        Mocks.Categorias.ExcluirComProdutosRemovidosAsync(categoria.Id, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflitoPersistenciaException(TipoConflitoPersistencia.Referencia, "FK_Produtos_Categorias_CategoriaId"));

        var resposta = await _admin.DeleteAsync($"{Rota}/{categoria.Id}", _ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.Conflict, "categoria.possui_produtos");
    }

    [Fact]
    public async Task Delete_Inexistente_Retorna404()
    {
        var resposta = await _admin.DeleteAsync($"{Rota}/{Guid.CreateVersion7()}", _ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.NotFound, "categoria.nao_encontrada");
    }
}
