using Catalogo.Application.Categorias.Dtos;
using Catalogo.Application.Categorias.Validators;
using Catalogo.UnitTests.Fixtures;
using FluentValidation.TestHelper;

namespace Catalogo.UnitTests.Application.Validators;

public sealed class CriarCategoriaRequestValidatorTests
{
    private readonly CriarCategoriaRequestValidator _validator = new();

    [Fact]
    public void RequestValido_NaoTemErros()
    {
        _validator.TestValidate(Requisicoes.CriarCategoria()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null, "O nome é obrigatório.")]
    [InlineData("", "O nome é obrigatório.")]
    [InlineData("ab", "O nome deve ter entre 3 e 100 caracteres.")]
    [InlineData(" Periféricos", "O nome não pode começar nem terminar com espaços.")]
    [InlineData("Periféricos ", "O nome não pode começar nem terminar com espaços.")]
    public void NomeInvalido_TemErroNoNome(string? nome, string mensagem)
    {
        var resultado = _validator.TestValidate(Requisicoes.CriarCategoria() with { Nome = nome! });

        resultado.ShouldHaveValidationErrorFor(x => x.Nome).WithErrorMessage(mensagem);
        resultado.Errors.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void Nome_RespeitaOsLimitesExatos(int tamanho, bool valido)
    {
        var resultado = _validator.TestValidate(Requisicoes.CriarCategoria() with { Nome = Requisicoes.Texto(tamanho) });
        resultado.IsValid.ShouldBe(valido);
    }

    [Fact]
    public void NomeSoComEspacos_TemErroNoNome()
    {
        _validator.TestValidate(Requisicoes.CriarCategoria() with { Nome = "     " })
            .ShouldHaveValidationErrorFor(x => x.Nome);
    }

    [Theory]
    [InlineData(500, true)]
    [InlineData(501, false)]
    public void Descricao_TemNoMaximo500(int tamanho, bool valido)
    {
        _validator.TestValidate(Requisicoes.CriarCategoria() with { Descricao = Requisicoes.Texto(tamanho) })
            .IsValid.ShouldBe(valido);
    }
}

public sealed class AtualizarCategoriaRequestValidatorTests
{
    private readonly AtualizarCategoriaRequestValidator _validator = new();

    [Fact]
    public void RequestValido_NaoTemErros()
    {
        _validator.TestValidate(Requisicoes.AtualizarCategoria()).ShouldNotHaveAnyValidationErrors();
    }

    // ADR-005: PUT é substituição completa, "ativa" é obrigatório
    [Fact]
    public void SemAtiva_TemErroEmAtiva()
    {
        _validator.TestValidate(Requisicoes.AtualizarCategoria() with { Ativa = null })
            .ShouldHaveValidationErrorFor(x => x.Ativa)
            .WithErrorMessage("O campo ativa é obrigatório.");
    }

    [Fact]
    public void AtivaFalse_EhValido()
    {
        _validator.TestValidate(Requisicoes.AtualizarCategoria() with { Ativa = false }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void NomeComEspacoNasPontasEDescricaoLonga_TemOsDoisErros()
    {
        var resultado = _validator.TestValidate(
            Requisicoes.AtualizarCategoria() with { Nome = "Periféricos ", Descricao = Requisicoes.Texto(501) });

        resultado.ShouldHaveValidationErrorFor(x => x.Nome);
        resultado.ShouldHaveValidationErrorFor(x => x.Descricao);
    }
}

public sealed class ListarCategoriasQueryValidatorTests
{
    private readonly ListarCategoriasQueryValidator _validator = new();

    [Fact]
    public void QueryPadrao_EhValida()
    {
        _validator.TestValidate(new ListarCategoriasQuery()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PageMenorQue1_TemErro(int page)
    {
        _validator.TestValidate(new ListarCategoriasQuery(Page: page))
            .ShouldHaveValidationErrorFor(x => x.Page)
            .WithErrorMessage("A página deve ser maior ou igual a 1.");
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void PageSize_EntreUmECem(int pageSize, bool valido)
    {
        _validator.TestValidate(new ListarCategoriasQuery(PageSize: pageSize)).IsValid.ShouldBe(valido);
    }

    [Fact]
    public void FiltroDeNomeAcimaDe100_TemErro()
    {
        _validator.TestValidate(new ListarCategoriasQuery(Nome: Requisicoes.Texto(101)))
            .ShouldHaveValidationErrorFor(x => x.Nome);
    }
}
