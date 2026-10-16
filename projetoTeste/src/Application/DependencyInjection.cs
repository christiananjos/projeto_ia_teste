using Catalogo.Application.Categorias.UseCases;
using Catalogo.Application.Produtos.UseCases;
using Catalogo.Application.Produtos.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Catalogo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidatorsFromAssemblyContaining<CriarProdutoRequestValidator>(ServiceLifetime.Scoped);

        services.AddScoped<CriarCategoriaUseCase>();
        services.AddScoped<AtualizarCategoriaUseCase>();
        services.AddScoped<RemoverCategoriaUseCase>();
        services.AddScoped<ObterCategoriaUseCase>();
        services.AddScoped<ListarCategoriasUseCase>();

        services.AddScoped<CriarProdutoUseCase>();
        services.AddScoped<AtualizarProdutoUseCase>();
        services.AddScoped<AlterarStatusProdutoUseCase>();
        services.AddScoped<RemoverProdutoUseCase>();
        services.AddScoped<ObterProdutoUseCase>();
        services.AddScoped<ListarProdutosUseCase>();

        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
