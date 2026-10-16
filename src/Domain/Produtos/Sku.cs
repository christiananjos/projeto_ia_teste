using System.Text.RegularExpressions;
using Catalogo.Domain.Excecoes;
using Catalogo.Domain.Regras;

namespace Catalogo.Domain.Produtos;

/// <summary>
/// SKU do produto (BR-003). Guarda o valor exatamente como recebido; a comparação sem diferenciar
/// maiúsculas é feita pela collation do banco (ADR-008).
/// </summary>
public readonly partial record struct Sku
{
    private Sku(string valor)
    {
        Valor = valor;
    }

    public string Valor { get; }

    public static Sku Criar(string valor)
    {
        if (valor is null)
            throw new DomainException("O SKU é obrigatório.");
        if (valor.Length < LimitesCampos.SkuMin || valor.Length > LimitesCampos.SkuMax)
            throw new DomainException($"O SKU deve ter entre {LimitesCampos.SkuMin} e {LimitesCampos.SkuMax} caracteres.");
        if (!FormatoSku().IsMatch(valor))
            throw new DomainException("O SKU deve conter apenas letras, números e hífen.");

        return new Sku(valor);
    }

    public override string ToString() => Valor;

    [GeneratedRegex(LimitesCampos.SkuRegex, RegexOptions.CultureInvariant)]
    private static partial Regex FormatoSku();
}
