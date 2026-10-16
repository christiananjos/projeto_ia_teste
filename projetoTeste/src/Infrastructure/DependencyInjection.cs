using Catalogo.Application.Categorias;
using Catalogo.Application.Produtos;
using Catalogo.Domain.Abstracoes;
using Catalogo.Domain.Categorias;
using Catalogo.Domain.Produtos;
using Catalogo.Infrastructure.Consultas;
using Catalogo.Infrastructure.Persistencia;
using Catalogo.Infrastructure.Repositorios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalogo.Infrastructure;

public static class DependencyInjection
{
    public const string NomeConnectionString = "Catalogo";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // A connection string é lida na criação do contexto, permitindo que os testes a substituam via configuração.
        services.AddDbContext<CatalogoDbContext>((provider, options) =>
        {
            var connectionString = provider.GetRequiredService<IConfiguration>().GetConnectionString(NomeConnectionString);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"A connection string '{NomeConnectionString}' não foi configurada. " +
                    "Use 'dotnet user-secrets' em desenvolvimento ou a variável de ambiente 'ConnectionStrings__Catalogo'.");
            }

            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
        });

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<CatalogoDbContext>());
        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<IProdutoRepository, ProdutoRepository>();
        services.AddScoped<ICategoriaConsultas, CategoriaConsultas>();
        services.AddScoped<IProdutoConsultas, ProdutoConsultas>();

        return services;
    }
}
