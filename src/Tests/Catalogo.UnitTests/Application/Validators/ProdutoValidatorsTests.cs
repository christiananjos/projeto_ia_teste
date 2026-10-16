using System.Globalization;
using Catalogo.Application.Produtos.Dtos;
using Catalogo.Application.Produtos.Validators;
using Catalogo.Domain.Regras;
using Catalogo.UnitTests.Fixtures;
using FluentValidation.TestHelper;

namespace Catalogo.UnitTests.Application.Validators;

public sealed class CriarProdutoRequestValidatorTests
{
    private readonly CriarProdutoRequestValidator _validator = new();

    [Fact]
    public void RequestValido_NaoTemErros()
    {
        _validator.TestValidate(Requisicoes.CriarProduto()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void SemCategoria_TemErro()
    {
        _validator.TestValidate(Requisicoes.CriarProduto() with { CategoriaId = null })
            .ShouldHaveValidationErrorFor(x => x.CategoriaId)
            .WithErrorMessage("A categoria é obrigatória.");
    }

    [Fact]
    public void CategoriaVazia_TemErro()
    {
        _validator.TestValidate(Requisicoes.CriarProduto() with { CategoriaId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.CategoriaId);
    }

    // BR-001
    [Theory]
    [InlineData(2, false)]
    [InlineData(3, true)]
    [InlineData(150, true)]
    [InlineData(151, false)]
    public void Nome_LimitesExatosDe3A150(int tamanho, bool valido)
    {
        _validator.TestValidate(Requisicoes.CriarProduto() with { Nome = Requisicoes.Texto(tamanho) })
            .IsValid.ShouldBe(valido);
    }

    // BR-001 / ADR-007
    [Theory]
    [InlineData(" Teclado Mecânico")]
    [InlineData("Teclado Mecânico ")]
    public void NomeComEspacoNasPontas_TemErro(string nome)
    {
        _validator.TestValidate(Requisicoes.CriarProduto() with { Nome = nome })
            .ShouldHaveValidationErrorFor(x => x.Nome)
            .WithErrorMessage("O nome não pode começar nem terminar com espaços.");
    }

    [Fact]
    public void SemNome_TemErro()
    {
        _validator.TestValidate(Requisicoes.CriarProduto() with { Nome = null! })
            .ShouldHaveValidationErrorFor(x => x.Nome)
            .WithErrorMessage("O nome é obrigatório.");
    }

    // BR-003
    [Theory]
    [InlineData(null, "O SKU é obrigatório.")]
    [InlineData("", "O SKU é obrigatório.")]
    [InlineData("AB", "O SKU deve ter entre 3 e 40 caracteres.")]
    [InlineData("TEC 001", "O SKU deve conter apenas letras, números e hífen.")]
    [InlineData("TEC_001", "O SKU deve conter apenas letras, números e hífen.")]
    [InlineData("TÉC-001", "O SKU deve conter apenas letras, números e hífen.")]
    public void SkuInvalido_TemErroNoSku(string? sku, string mensagem)
    {
        var resultado = _validator.TestValidate(Requisicoes.CriarProduto() with { Sku = sku! });

        resultado.ShouldHaveValidationErrorFor(x => x.Sku).WithErrorMessage(mensagem);
        resultado.Errors.Count.ShouldBe(1);
    }

    /// <summary>Regressão de [[SKU aceita quebra de linha no final]] (pedido do 04-reviewer).</summary>
    [Theory]
    [InlineData("TEC-001\n")]
    [InlineData("AB\n")]
    [InlineData("TEC-001\r\n")]
    public void SkuComQuebraDeLinhaNoFinal_TemErroNoSku(string sku)
    {
        _validator.TestValidate(Requisicoes.CriarProduto() with { Sku = sku })
            .ShouldHaveValidationErrorFor(x => x.Sku)
            .WithErrorMessage("O SKU deve conter apenas letras, números e hífen.");
    }

    // BR-003
    [Theory]
    [InlineData(3, true)]
    [InlineData(40, true)]
    [InlineData(41, false)]
    public void Sku_LimitesExatosDe3A40(int tamanho, bool valido)
    {
        _validator.TestValidate(Requisicoes.CriarProduto() with { Sku = Requisicoes.Texto(tamanho, 'X') })
            .IsValid.ShouldBe(valido);
    }

    [Theory]
    [InlineData(1000, true)]
    [InlineData(1001, false)]
    public void Descricao_TemNoMaximo1000(int tamanho, bool valido)
    {
        _validator.TestValidate(Requisicoes.CriarProduto() with { Descricao = Requisicoes.Texto(tamanho) })
            .IsValid.ShouldBe(valido);
    }

    // BR-002
    [Theory]
    [InlineData("0.01")]
    [InlineData("349.90")]
    [InlineData("9999999999999999.99")]
    public void PrecoValido_NaoTemErro(string preco)
    {
        _validator.TestValidate(Requisicoes.CriarProduto() with { Preco = Decimal(preco) })
            .ShouldNotHaveValidationErrorFor(x => x.Preco);
    }

    // BR-002 / ADR-014
    [Theory]
    [InlineData("0", "O preço deve ser maior que zero.")]
    [InlineData("-0.01", "O preço deve ser maior que zero.")]
    [InlineData("0.001", "O preço deve ter no máximo 2 casas decimais.")]
    [InlineData("349.999", "O preço deve ter no máximo 2 casas decimais.")]
    [InlineData("10000000000000000.00", "O preço deve ser menor ou igual a")]
    public void PrecoInvalido_TemErroNoPreco(string preco, string inicioDaMensagem)
    {
        var resultado = _validator.TestValidate(Requisicoes.CriarProduto() with { Preco = Decimal(preco) });

        // A mensagem do limite máximo formata o número na cultura corrente; por isso a comparação é pelo início.
        resultado.ShouldHaveValidationErrorFor(x => x.Preco)
            .ShouldContain(f => f.ErrorMessage.StartsWith(inicioDaMensagem, StringComparison.Ordinal));
        resultado.Errors.Count.ShouldBe(1);
    }

    [Fact]
    public void SemPreco_TemErro()
    {
        _validator.TestValidate(Requisicoes.CriarProduto() with { Preco = null })
            .ShouldHaveValidationErrorFor(x => x.Preco)
            .WithErrorMessage("O preço é obrigatório.");
    }

    [Fact]
    public void VariosCamposInvalidos_ReportaTodos()
    {
        var resultado = _validator.TestValidate(new CriarProdutoRequest(null, "", "", null, null));

        resultado.ShouldHaveValidationErrorFor(x => x.CategoriaId);
        resultado.ShouldHaveValidationErrorFor(x => x.Nome);
        resultado.ShouldHaveValidationErrorFor(x => x.Sku);
        resultado.ShouldHaveValidationErrorFor(x => x.Preco);
    }

    [Fact]
    public void ValidatorUsaAMesmaRegexDoDominio()
    {
        LimitesCampos.SkuRegex.ShouldEndWith(@"\z");
    }

    private static decimal Decimal(string valor) => decimal.Parse(valor, CultureInfo.InvariantCulture);
}

public sealed class AtualizarProdutoRequestValidatorTests
{
    private readonly AtualizarProdutoRequestValidator _validator = new();

    [Fact]
    public void RequestValido_NaoTemErros()
    {
        _validator.TestValidate(Requisicoes.AtualizarProduto()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void SemCategoria_TemErro()
    {
        _validator.TestValidate(Requisicoes.AtualizarProduto() with { CategoriaId = null })
            .ShouldHaveValidationErrorFor(x => x.CategoriaId);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(151)]
    public void NomeForaDoLimite_TemErro(int tamanho)
    {
        _validator.TestValidate(Requisicoes.AtualizarProduto() with { Nome = Requisicoes.Texto(tamanho) })
            .ShouldHaveValidationErrorFor(x => x.Nome);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("1.234")]
    public void PrecoInvalido_TemErro(string? preco)
    {
        decimal? valor = preco is null ? null : decimal.Parse(preco, CultureInfo.InvariantCulture);
        _validator.TestValidate(Requisicoes.AtualizarProduto() with { Preco = valor })
            .ShouldHaveValidationErrorFor(x => x.Preco);
    }

    [Fact]
    public void DescricaoAcimaDe1000_TemErro()
    {
        _validator.TestValidate(Requisicoes.AtualizarProduto() with { Descricao = Requisicoes.Texto(1001) })
            .ShouldHaveValidationErrorFor(x => x.Descricao);
    }

    // BR-003: o contrato de atualização não tem SKU
    [Fact]
    public void ContratoDeAtualizacao_NaoTemSkuNemAtivo()
    {
        var propriedades = typeof(AtualizarProdutoRequest).GetProperties().Select(p => p.Name).ToArray();

        propriedades.ShouldNotContain("Sku");
        propriedades.ShouldNotContain("Ativo");
    }
}

public sealed class AlterarStatusProdutoRequestValidatorTests
{
    private readonly AlterarStatusProdutoRequestValidator _validator = new();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ComAtivo_EhValido(bool ativo)
    {
        _validator.TestValidate(new AlterarStatusProdutoRequest(ativo)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void SemAtivo_TemErro()
    {
        _validator.TestValidate(new AlterarStatusProdutoRequest(null))
            .ShouldHaveValidationErrorFor(x => x.Ativo)
            .WithErrorMessage("O campo ativo é obrigatório.");
    }
}

public sealed class ListarProdutosQueryValidatorTests
{
    private readonly ListarProdutosQueryValidator _validator = new();

    [Fact]
    public void QueryPadrao_EhValida()
    {
        _validator.TestValidate(new ListarProdutosQuery()).ShouldNotHaveAnyValidationErrors();
    }

    // REQ-003: paginação 1..100
    [Theory]
    [InlineData(0, 20, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 101, false)]
    [InlineData(1, 1, true)]
    [InlineData(1, 100, true)]
    [InlineData(int.MaxValue, 100, true)]
    public void Paginacao_RespeitaOsLimites(int page, int pageSize, bool valido)
    {
        _validator.TestValidate(new ListarProdutosQuery(page, pageSize)).IsValid.ShouldBe(valido);
    }

    [Theory]
    [InlineData("nome")]
    [InlineData("preco")]
    [InlineData("PRECO")]
    [InlineData("Nome")]
    public void OrdenarPorConhecido_SemDiferenciarMaiusculas_EhValido(string ordenarPor)
    {
        _validator.TestValidate(new ListarProdutosQuery(OrdenarPor: ordenarPor)).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("data")]
    [InlineData("sku")]
    [InlineData("")]
    public void OrdenarPorDesconhecido_TemErro(string ordenarPor)
    {
        _validator.TestValidate(new ListarProdutosQuery(OrdenarPor: ordenarPor))
            .ShouldHaveValidationErrorFor(x => x.OrdenarPor)
            .WithErrorMessage("O campo ordenarPor deve ser 'nome' ou 'preco'.");
    }

    [Theory]
    [InlineData("asc", true)]
    [InlineData("DESC", true)]
    [InlineData("up", false)]
    [InlineData("ascending", false)]
    public void Direcao_AscOuDesc(string direcao, bool valido)
    {
        _validator.TestValidate(new ListarProdutosQuery(Direcao: direcao)).IsValid.ShouldBe(valido);
    }

    [Fact]
    public void FiltroDeNomeAcimaDe150_TemErro()
    {
        _validator.TestValidate(new ListarProdutosQuery(Nome: Requisicoes.Texto(151)))
            .ShouldHaveValidationErrorFor(x => x.Nome);
    }

    [Theory]
    [InlineData(null, null, false, false)]
    [InlineData("preco", "desc", true, true)]
    [InlineData("PRECO", "Asc", true, false)]
    [InlineData("nome", "DESC", false, true)]
    public void PropriedadesDeOrdenacao_InterpretamSemDiferenciarMaiusculas(
        string? ordenarPor, string? direcao, bool porPreco, bool descendente)
    {
        var query = new ListarProdutosQuery(OrdenarPor: ordenarPor, Direcao: direcao);

        query.OrdenaPorPreco.ShouldBe(porPreco);
        query.OrdemDescendente.ShouldBe(descendente);
    }
}
