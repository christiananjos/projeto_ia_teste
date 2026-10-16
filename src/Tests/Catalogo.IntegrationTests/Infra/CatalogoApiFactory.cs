using System.Globalization;
using System.Net.Http.Headers;
using Catalogo.Application.Categorias;
using Catalogo.Application.Produtos;
using Catalogo.Domain.Abstracoes;
using Catalogo.Domain.Categorias;
using Catalogo.Domain.Produtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Catalogo.IntegrationTests.Infra;

/// <summary>
/// Base das factories: injeta JWT de teste e um rate limit alto (só o teste de 429 usa limite baixo) e captura
/// logs e o status final das respostas.
/// </summary>
public abstract class CatalogoApiFactory : WebApplicationFactory<Program>
{
    public LogsCapturados Logs { get; } = new();
    public RegistroDeRespostas Respostas { get; } = new();

    protected virtual int LimitePorJanela => 100_000;

    protected abstract string ConnectionString { get; }

    public HttpClient ClienteAdmin() => ClienteCom(TokenJwt.Admin());

    public HttpClient ClienteLeitor() => ClienteCom(TokenJwt.SemRole());

    public HttpClient ClienteAnonimo() => CreateClient();

    public HttpClient ClienteCom(string token)
    {
        var cliente = CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuracao) => configuracao.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Autenticacao:Jwt:Issuer"] = TokenJwt.Issuer,
            ["Autenticacao:Jwt:Audience"] = TokenJwt.Audience,
            ["Autenticacao:Jwt:ChaveAssinatura"] = TokenJwt.Chave,
            ["RateLimiting:LimitePorJanela"] = LimitePorJanela.ToString(CultureInfo.InvariantCulture),
            ["RateLimiting:JanelaSegundos"] = "60",
            ["ConnectionStrings:Catalogo"] = ConnectionString
        }));

        builder.ConfigureLogging(logging => logging.AddProvider(Logs).SetMinimumLevel(LogLevel.Debug));

        builder.ConfigureTestServices(servicos =>
        {
            servicos.AddSingleton(Respostas);
            servicos.AddSingleton<IStartupFilter>(new RegistroDeRespostasStartupFilter(Respostas));
            ConfigurarServicos(servicos);
        });
    }

    protected virtual void ConfigurarServicos(IServiceCollection servicos)
    {
    }
}

/// <summary>Substitutos NSubstitute da persistência, trocados a cada teste.</summary>
public sealed class Substitutos
{
    public ICategoriaRepository Categorias { get; } = Substitute.For<ICategoriaRepository>();
    public IProdutoRepository Produtos { get; } = Substitute.For<IProdutoRepository>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public ICategoriaConsultas ConsultasCategoria { get; } = Substitute.For<ICategoriaConsultas>();
    public IProdutoConsultas ConsultasProduto { get; } = Substitute.For<IProdutoConsultas>();
}

/// <summary>
/// Pipeline HTTP completo (autenticação, autorização, rate limit, validação, ProblemDetails, handler global)
/// com a persistência substituída. Não precisa de Docker nem de SQL Server.
/// </summary>
public class ApiComSubstitutosFactory : CatalogoApiFactory
{
    // Nunca é aberta: os repositórios, as consultas e o IUnitOfWork são substituídos.
    protected override string ConnectionString => "Server=nao-usado;Database=Catalogo;TrustServerCertificate=True";

    public Substitutos Mocks { get; private set; } = new();

    public void ReiniciarSubstitutos()
    {
        Mocks = new Substitutos();
        Logs.Limpar();
        Respostas.Limpar();
    }

    protected override void ConfigurarServicos(IServiceCollection servicos)
    {
        servicos.RemoveAll<ICategoriaRepository>();
        servicos.RemoveAll<IProdutoRepository>();
        servicos.RemoveAll<IUnitOfWork>();
        servicos.RemoveAll<ICategoriaConsultas>();
        servicos.RemoveAll<IProdutoConsultas>();

        servicos.AddScoped(_ => Mocks.Categorias);
        servicos.AddScoped(_ => Mocks.Produtos);
        servicos.AddScoped(_ => Mocks.UnitOfWork);
        servicos.AddScoped(_ => Mocks.ConsultasCategoria);
        servicos.AddScoped(_ => Mocks.ConsultasProduto);
    }
}

/// <summary>Mesma API com limite de 2 requisições por janela, para o teste de 429 (ADR-011).</summary>
public sealed class ApiComLimiteBaixoFactory : ApiComSubstitutosFactory
{
    public const int Limite = 2;

    protected override int LimitePorJanela => Limite;
}
