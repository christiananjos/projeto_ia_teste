using Catalogo.Application;
using Catalogo.Application.Categorias;
using Catalogo.Application.Categorias.UseCases;
using Catalogo.Application.Produtos;
using Catalogo.Application.Produtos.Dtos;
using Catalogo.Application.Produtos.UseCases;
using Catalogo.Domain.Abstracoes;
using Catalogo.Domain.Categorias;
using Catalogo.Domain.Produtos;
using Catalogo.Infrastructure;
using Catalogo.Infrastructure.Persistencia;
using Catalogo.UnitTests.Fixtures;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalogo.UnitTests.Infrastructure;

public sealed class DependencyInjectionTests
{
    public static TheoryData<Type> CasosDeUso =>
    [
        typeof(CriarCategoriaUseCase), typeof(AtualizarCategoriaUseCase), typeof(RemoverCategoriaUseCase),
        typeof(ObterCategoriaUseCase), typeof(ListarCategoriasUseCase), typeof(CriarProdutoUseCase),
        typeof(AtualizarProdutoUseCase), typeof(AlterarStatusProdutoUseCase), typeof(RemoverProdutoUseCase),
        typeof(ObterProdutoUseCase), typeof(ListarProdutosUseCase)
    ];

    [Theory]
    [MemberData(nameof(CasosDeUso))]
    public void AddApplication_RegistraOsOnzeCasosDeUsoComoScoped(Type tipo)
    {
        using var provider = CriarProvider();
        using var escopo = provider.CreateScope();

        escopo.ServiceProvider.GetRequiredService(tipo).ShouldNotBeNull();
        CriarServicos().Single(d => d.ServiceType == tipo).Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddApplication_RegistraOsValidators()
    {
        using var provider = CriarProvider();
        using var escopo = provider.CreateScope();

        escopo.ServiceProvider.GetRequiredService<IValidator<CriarProdutoRequest>>().ShouldNotBeNull();
        escopo.ServiceProvider.GetRequiredService<IValidator<ListarProdutosQuery>>().ShouldNotBeNull();
    }

    [Fact]
    public void AddApplication_NaoSobrescreveTimeProviderJaRegistrado()
    {
        var fake = Relogio.CriarFake();
        var servicos = new ServiceCollection().AddSingleton<TimeProvider>(fake).AddApplication();

        using var provider = servicos.BuildServiceProvider();

        provider.GetRequiredService<TimeProvider>().ShouldBeSameAs(fake);
    }

    [Fact]
    public void AddApplication_SemTimeProvider_UsaOSistema()
    {
        using var provider = new ServiceCollection().AddApplication().BuildServiceProvider();

        provider.GetRequiredService<TimeProvider>().ShouldBeSameAs(TimeProvider.System);
    }

    [Fact]
    public void AddInfrastructure_RegistraRepositoriosConsultasEUnitOfWorkNoMesmoContexto()
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Catalogo"] = "Server=nao-usado;Database=Catalogo;TrustServerCertificate=True"
            })
            .Build();
        var servicos = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuracao)
            .AddInfrastructure(configuracao);

        using var provider = servicos.BuildServiceProvider();
        using var escopo = provider.CreateScope();
        var sp = escopo.ServiceProvider;

        sp.GetRequiredService<IUnitOfWork>().ShouldBeSameAs(sp.GetRequiredService<CatalogoDbContext>());
        sp.GetRequiredService<ICategoriaRepository>().ShouldNotBeNull();
        sp.GetRequiredService<IProdutoRepository>().ShouldNotBeNull();
        sp.GetRequiredService<ICategoriaConsultas>().ShouldNotBeNull();
        sp.GetRequiredService<IProdutoConsultas>().ShouldNotBeNull();
    }

    [Fact]
    public void AddInfrastructure_SemConnectionString_FalhaComMensagemClara()
    {
        var configuracao = new ConfigurationBuilder().Build();
        var servicos = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuracao)
            .AddInfrastructure(configuracao);

        using var provider = servicos.BuildServiceProvider();
        using var escopo = provider.CreateScope();

        Should.Throw<InvalidOperationException>(() => escopo.ServiceProvider.GetRequiredService<CatalogoDbContext>())
            .Message.ShouldContain("ConnectionStrings__Catalogo");
    }

    private static IServiceCollection CriarServicos() =>
        new ServiceCollection()
            .AddSingleton(Substitute.For<ICategoriaRepository>())
            .AddSingleton(Substitute.For<IProdutoRepository>())
            .AddSingleton(Substitute.For<IUnitOfWork>())
            .AddSingleton(Substitute.For<ICategoriaConsultas>())
            .AddSingleton(Substitute.For<IProdutoConsultas>())
            .AddApplication();

    private static ServiceProvider CriarProvider() =>
        CriarServicos().BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
}
