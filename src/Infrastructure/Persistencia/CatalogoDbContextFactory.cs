using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Catalogo.Infrastructure.Persistencia;

/// <summary>
/// Usado apenas pelas ferramentas do EF (<c>dotnet ef migrations</c>). Lê a connection string da variável de
/// ambiente <c>ConnectionStrings__Catalogo</c>; sem ela, usa o LocalDB, suficiente para gerar migrations.
/// </summary>
internal sealed class CatalogoDbContextFactory : IDesignTimeDbContextFactory<CatalogoDbContext>
{
    private const string ConexaoPadraoDesignTime =
        "Server=(localdb)\\MSSQLLocalDB;Database=Catalogo;Trusted_Connection=True;TrustServerCertificate=True";

    public CatalogoDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Catalogo");
        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = ConexaoPadraoDesignTime;

        var options = new DbContextOptionsBuilder<CatalogoDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new CatalogoDbContext(options);
    }
}
