using Catalogo.Infrastructure.Persistencia;
using Catalogo.IntegrationTests.Infra;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace Catalogo.IntegrationTests.Persistencia;

/// <summary>API real (EF Core + SQL Server do container), sem substitutos.</summary>
public sealed class ApiComSqlServerFactory(string connectionString) : CatalogoApiFactory
{
    protected override string ConnectionString => connectionString;
}

/// <summary>
/// Um container SQL Server por coleção (ADR-013), com a migration <c>Inicial</c> aplicada. Sem Docker, os testes
/// da coleção são marcados como ignorados com o motivo; com a variável <c>CATALOGO_EXIGIR_DOCKER=true</c>
/// (uso em CI), eles falham em vez de serem ignorados.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    public const string VariavelExigirDocker = "CATALOGO_EXIGIR_DOCKER";
    private const string Imagem = "mcr.microsoft.com/mssql/server:2022-latest";

    private MsSqlContainer? _container;
    private ApiComSqlServerFactory? _api;

    public string? MotivoIndisponivel { get; private set; }

    public ApiComSqlServerFactory Api =>
        _api ?? throw new InvalidOperationException("O SQL Server de teste não foi iniciado. Chame ExigirDocker() antes.");

    public async ValueTask InitializeAsync()
    {
        try
        {
            _container = new MsSqlBuilder(Imagem).Build();
            await _container.StartAsync(TestContext.Current.CancellationToken);
        }
        catch (Exception ex)
        {
            MotivoIndisponivel =
                $"Docker indisponível para Testcontainers ({ex.GetType().Name}: {ex.Message}). " +
                "Inicie o Docker para rodar os testes de integração com SQL Server.";
            return;
        }

        var connectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = "Catalogo",
            TrustServerCertificate = true
        }.ConnectionString;

        _api = new ApiComSqlServerFactory(connectionString);
        await using var escopo = _api.Services.CreateAsyncScope();
        await escopo.ServiceProvider.GetRequiredService<CatalogoDbContext>().Database.MigrateAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>Ignora o teste (ou falha, em CI) quando não há Docker.</summary>
    public void ExigirDocker()
    {
        if (MotivoIndisponivel is null)
            return;

        if (string.Equals(Environment.GetEnvironmentVariable(VariavelExigirDocker), "true", StringComparison.OrdinalIgnoreCase))
            Assert.Fail(MotivoIndisponivel);

        Assert.Skip(MotivoIndisponivel);
    }

    /// <summary>Limpa as tabelas entre testes: Produtos (inclusive removidos) e depois Categorias.</summary>
    public async Task LimparAsync()
    {
        await using var escopo = Api.Services.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<CatalogoDbContext>();
        await db.Produtos.IgnoreQueryFilters().ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await db.Categorias.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_api is not null)
            await _api.DisposeAsync();
        if (_container is not null)
            await _container.DisposeAsync();
    }
}

[CollectionDefinition(Nome)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Nome = "SqlServer";
}

/// <summary>Base dos testes com banco real: exige Docker, limpa o banco e entrega um cliente Admin.</summary>
[Collection(SqlServerCollection.Nome)]
public abstract class TesteComSqlServer(SqlServerFixture banco)
{
    protected SqlServerFixture Banco { get; } = banco;
    protected CancellationToken Ct { get; } = TestContext.Current.CancellationToken;

    protected async Task<HttpClient> PrepararAsync()
    {
        Banco.ExigirDocker();
        await Banco.LimparAsync();
        return Banco.Api.ClienteAdmin();
    }

    protected async Task<T> ComContextoAsync<T>(Func<CatalogoDbContext, Task<T>> acao)
    {
        await using var escopo = Banco.Api.Services.CreateAsyncScope();
        return await acao(escopo.ServiceProvider.GetRequiredService<CatalogoDbContext>());
    }
}
