namespace Catalogo.Domain.Regras;

/// <summary>
/// Limites de campos compartilhados pelas entidades, pelos validators da Application e pelo mapeamento EF.
/// </summary>
public static class LimitesCampos
{
    public const int CategoriaNomeMin = 3;
    public const int CategoriaNomeMax = 100;
    public const int CategoriaDescricaoMax = 500;

    public const int ProdutoNomeMin = 3;
    public const int ProdutoNomeMax = 150;
    public const int ProdutoDescricaoMax = 1000;

    public const int SkuMin = 3;
    public const int SkuMax = 40;
    /// <summary>
    /// Só alfanuméricos ASCII e hífen (BR-003). Usa <c>\z</c> em vez de <c>$</c> porque, no .NET, <c>$</c> também
    /// casa antes de um <c>\n</c> final e deixaria passar <c>"TEC-001\n"</c>.
    /// </summary>
    public const string SkuRegex = @"^[A-Za-z0-9-]+\z";

    /// <summary>Maior valor representável em decimal(18,2) (ADR-014).</summary>
    public const decimal PrecoMaximo = 9999999999999999.99m;
    public const int PrecoCasasDecimais = 2;

    public static bool TemNoMaximoCasasDecimais(decimal valor) =>
        decimal.Round(valor, PrecoCasasDecimais) == valor;

    public static bool SemEspacosNasPontas(string valor) =>
        valor.Length == valor.Trim().Length;
}
