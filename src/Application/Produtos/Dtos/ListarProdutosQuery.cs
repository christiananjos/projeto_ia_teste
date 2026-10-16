namespace Catalogo.Application.Produtos.Dtos;

/// <summary>
/// Listagem paginada de produtos (ADR-009). <see cref="Ativo"/> nulo equivale a <c>true</c>;
/// <see cref="OrdenarPor"/> = <c>nome</c> | <c>preco</c> (padrão <c>nome</c>);
/// <see cref="Direcao"/> = <c>asc</c> | <c>desc</c> (padrão <c>asc</c>).
/// </summary>
public sealed record ListarProdutosQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? CategoriaId = null,
    string? Nome = null,
    bool? Ativo = null,
    string? OrdenarPor = null,
    string? Direcao = null)
{
    public const string OrdenarPorNome = "nome";
    public const string OrdenarPorPreco = "preco";
    public const string DirecaoAsc = "asc";
    public const string DirecaoDesc = "desc";

    public bool OrdenaPorPreco => string.Equals(OrdenarPor, OrdenarPorPreco, StringComparison.OrdinalIgnoreCase);

    public bool OrdemDescendente => string.Equals(Direcao, DirecaoDesc, StringComparison.OrdinalIgnoreCase);
}
