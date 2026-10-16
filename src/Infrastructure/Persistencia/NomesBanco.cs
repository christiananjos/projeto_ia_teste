namespace Catalogo.Infrastructure.Persistencia;

/// <summary>
/// Nomes fixos de tabelas, índices e FKs. São usados pelo <see cref="TradutorErrosSql"/> para identificar a
/// restrição violada e não podem mudar sem ajustar a tradução.
/// </summary>
internal static class NomesBanco
{
    public const string Collation = "Latin1_General_100_CI_AS";

    public const string TabelaCategorias = "Categorias";
    public const string TabelaProdutos = "Produtos";

    public const string IndiceCategoriasNome = "UX_Categorias_Nome";
    public const string IndiceProdutosSku = "UX_Produtos_Sku";
    public const string IndiceProdutosCategoriaIdNome = "UX_Produtos_CategoriaId_Nome";
    public const string IndiceProdutosCategoriaId = "IX_Produtos_CategoriaId";
    public const string FkProdutosCategorias = "FK_Produtos_Categorias_CategoriaId";

    public const string FiltroNaoRemovido = "[DataRemocao] IS NULL";

    public static readonly string[] Restricoes =
    [
        IndiceCategoriasNome,
        IndiceProdutosSku,
        IndiceProdutosCategoriaIdNome,
        FkProdutosCategorias
    ];
}
