using Catalogo.Domain.Abstracoes;
using Catalogo.Domain.Categorias;
using Catalogo.Domain.Produtos;
using Catalogo.Infrastructure.Persistencia.Conversores;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Infrastructure.Persistencia;

public sealed class CatalogoDbContext(DbContextOptions<CatalogoDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Produto> Produtos => Set<Produto>();

    public async Task SalvarAlteracoesAsync(CancellationToken ct)
    {
        try
        {
            await SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (TradutorErrosSql.TentarTraduzir(ex, out var conflito))
        {
            throw conflito;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("dbo");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogoDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }
}
