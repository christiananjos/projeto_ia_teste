using Catalogo.Domain.Excecoes;
using Catalogo.Domain.Produtos;
using Catalogo.UnitTests.Fixtures;

namespace Catalogo.UnitTests.Domain;

/// <summary>BR-003: SKU de 3 a 40 caracteres, só letras ASCII, números e hífen.</summary>
public sealed class SkuTests
{
    [Theory]
    [InlineData("TEC-001")]
    [InlineData("abc")]
    [InlineData("A-1")]
    [InlineData("1234567890")]
    [InlineData("---")]
    public void Criar_ComValorValido_GuardaOValorExatamenteComoRecebido(string valor)
    {
        var sku = Sku.Criar(valor);

        sku.Valor.ShouldBe(valor);
        sku.ToString().ShouldBe(valor);
    }

    [Fact]
    public void Criar_NaoNormalizaMaiusculas_PorqueAComparacaoEhPelaCollation()
    {
        Sku.Criar("tec-001").Valor.ShouldBe("tec-001");
    }

    [Fact]
    public void Criar_ComLimitesExatos_Aceita()
    {
        Sku.Criar(Requisicoes.Texto(3, 'A')).Valor.Length.ShouldBe(3);
        Sku.Criar(Requisicoes.Texto(40, 'A')).Valor.Length.ShouldBe(40);
    }

    [Fact]
    public void Criar_ComNulo_LancaDomainException()
    {
        var excecao = Should.Throw<DomainException>(() => Sku.Criar(null!));
        excecao.Message.ShouldBe("O SKU é obrigatório.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(41)]
    public void Criar_ForaDoTamanho_LancaDomainException(int tamanho)
    {
        var excecao = Should.Throw<DomainException>(() => Sku.Criar(Requisicoes.Texto(tamanho, 'A')));
        excecao.Message.ShouldBe("O SKU deve ter entre 3 e 40 caracteres.");
    }

    [Theory]
    [InlineData("TEC 001")]
    [InlineData("TEC_001")]
    [InlineData("TEC.001")]
    [InlineData("TÉC-001")]
    [InlineData("TEC/001")]
    [InlineData(" TEC-001")]
    public void Criar_ComCaractereInvalido_LancaDomainException(string valor)
    {
        var excecao = Should.Throw<DomainException>(() => Sku.Criar(valor));
        excecao.Message.ShouldBe("O SKU deve conter apenas letras, números e hífen.");
    }

    /// <summary>
    /// Regressão de [[SKU aceita quebra de linha no final]]: com <c>$</c> a regex casava antes do <c>\n</c> final.
    /// "AB\n" tem 3 caracteres, então passa no tamanho e só a regex o barra.
    /// </summary>
    [Theory]
    [InlineData("TEC-001\n")]
    [InlineData("AB\n")]
    [InlineData("TEC-001\r\n")]
    [InlineData("\nTEC-001")]
    public void Criar_ComQuebraDeLinha_LancaDomainException(string valor)
    {
        var excecao = Should.Throw<DomainException>(() => Sku.Criar(valor));
        excecao.Message.ShouldBe("O SKU deve conter apenas letras, números e hífen.");
    }

    [Fact]
    public void Igualdade_ComparaPeloValor()
    {
        Sku.Criar("TEC-001").ShouldBe(Sku.Criar("TEC-001"));
        Sku.Criar("TEC-001").ShouldNotBe(Sku.Criar("tec-001"));
    }
}
