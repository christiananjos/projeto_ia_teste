using System.Net;
using System.Net.Http.Json;
using Catalogo.Application.Categorias.Dtos;
using Catalogo.Application.Comum;
using Catalogo.IntegrationTests.Infra;

namespace Catalogo.IntegrationTests.Api;

/// <summary>ADR-010: GET exige autenticação; escrita exige role Admin; nenhum endpoint é anônimo.</summary>
public sealed class AutenticacaoTests : IClassFixture<ApiComSubstitutosFactory>
{
    private readonly ApiComSubstitutosFactory _api;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public AutenticacaoTests(ApiComSubstitutosFactory api)
    {
        _api = api;
        _api.ReiniciarSubstitutos();
        _api.Mocks.ConsultasCategoria.ListarAsync(Arg.Any<ListarCategoriasQuery>(), Arg.Any<CancellationToken>())
            .Returns(PaginaResponse<CategoriaResponse>.Criar([], 1, 20, 0));
    }

    private const string IdQualquer = "0199f0a1-7c3e-7d2a-9b41-3f5e8c2d1a01";

    public static TheoryData<string, string> Endpoints => new()
    {
        { "GET", "/api/v1/categorias" },
        { "GET", $"/api/v1/categorias/{IdQualquer}" },
        { "POST", "/api/v1/categorias" },
        { "PUT", $"/api/v1/categorias/{IdQualquer}" },
        { "DELETE", $"/api/v1/categorias/{IdQualquer}" },
        { "GET", "/api/v1/produtos" },
        { "GET", $"/api/v1/produtos/{IdQualquer}" },
        { "POST", "/api/v1/produtos" },
        { "PUT", $"/api/v1/produtos/{IdQualquer}" },
        { "PATCH", $"/api/v1/produtos/{IdQualquer}/status" },
        { "DELETE", $"/api/v1/produtos/{IdQualquer}" }
    };

    public static TheoryData<string, string> EndpointsDeEscrita => new()
    {
        { "POST", "/api/v1/categorias" },
        { "PUT", $"/api/v1/categorias/{IdQualquer}" },
        { "DELETE", $"/api/v1/categorias/{IdQualquer}" },
        { "POST", "/api/v1/produtos" },
        { "PUT", $"/api/v1/produtos/{IdQualquer}" },
        { "PATCH", $"/api/v1/produtos/{IdQualquer}/status" },
        { "DELETE", $"/api/v1/produtos/{IdQualquer}" }
    };

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task SemToken_Retorna401ProblemDetails(string metodo, string rota)
    {
        using var cliente = _api.ClienteAnonimo();

        var resposta = await cliente.SendAsync(Requisicao(metodo, rota), _ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.Unauthorized, "nao_autenticado");
    }

    [Theory]
    [MemberData(nameof(EndpointsDeEscrita))]
    public async Task EscritaComTokenSemRoleAdmin_Retorna403(string metodo, string rota)
    {
        using var cliente = _api.ClienteLeitor();

        var resposta = await cliente.SendAsync(Requisicao(metodo, rota), _ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.Forbidden, "acesso_negado");
    }

    [Fact]
    public async Task LeituraComTokenSemRole_Retorna200()
    {
        using var cliente = _api.ClienteLeitor();

        var resposta = await cliente.GetAsync("/api/v1/categorias", _ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("expirado")]
    [InlineData("issuer-desconhecido")]
    [InlineData("outra-chave")]
    [InlineData("malformado")]
    public async Task TokenInvalido_Retorna401(string caso)
    {
        // O token é gerado na execução (e não na descoberta) porque depende do relógio.
        var token = caso switch
        {
            "expirado" => TokenJwt.Expirado(),
            "issuer-desconhecido" => TokenJwt.ComIssuer("https://emissor-desconhecido.test"),
            "outra-chave" => TokenJwt.AssinadoComOutraChave(),
            _ => "nao-eh-um-jwt"
        };
        using var cliente = _api.ClienteCom(token);

        var resposta = await cliente.GetAsync("/api/v1/categorias", _ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.Unauthorized, "nao_autenticado");
    }

    private static HttpRequestMessage Requisicao(string metodo, string rota)
    {
        var requisicao = new HttpRequestMessage(new HttpMethod(metodo), rota);
        if (metodo is "POST" or "PUT" or "PATCH")
            requisicao.Content = JsonContent.Create(new { });
        return requisicao;
    }
}

/// <summary>ADR-011: janela fixa por IP, aplicada antes da autenticação.</summary>
public sealed class RateLimitTests : IClassFixture<ApiComLimiteBaixoFactory>
{
    private readonly ApiComLimiteBaixoFactory _api;

    public RateLimitTests(ApiComLimiteBaixoFactory api)
    {
        _api = api;
        _api.ReiniciarSubstitutos();
    }

    [Fact]
    public async Task AcimaDoLimite_Retorna429ComRetryAfterMesmoSemToken()
    {
        var ct = TestContext.Current.CancellationToken;
        using var cliente = _api.ClienteAnonimo();

        for (var i = 0; i < ApiComLimiteBaixoFactory.Limite; i++)
            (await cliente.GetAsync("/api/v1/produtos", ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var resposta = await cliente.GetAsync("/api/v1/produtos", ct);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.TooManyRequests, "limite_requisicoes");
        resposta.Headers.RetryAfter.ShouldNotBeNull();
        resposta.Headers.RetryAfter!.Delta!.Value.TotalSeconds.ShouldBeGreaterThanOrEqualTo(1);
    }
}
