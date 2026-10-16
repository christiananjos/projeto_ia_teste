namespace Catalogo.Application.Comum;

/// <summary>Códigos estáveis devolvidos na extensão <c>code</c> do ProblemDetails.</summary>
public static class CodigosErro
{
    public const string Validacao = "validacao";

    public const string CategoriaNaoEncontrada = "categoria.nao_encontrada";
    public const string CategoriaNomeDuplicado = "categoria.nome_duplicado";
    public const string CategoriaPossuiProdutos = "categoria.possui_produtos";

    public const string ProdutoNaoEncontrado = "produto.nao_encontrado";
    public const string ProdutoSkuDuplicado = "produto.sku_duplicado";
    public const string ProdutoNomeDuplicado = "produto.nome_duplicado";
    public const string ProdutoCategoriaInvalida = "produto.categoria_invalida";
}
