using Catalogo.Domain.Produtos;

namespace Catalogo.Application.Produtos.Dtos;

/// <summary>Representação pública do produto. <c>DataRemocao</c> nunca é exposta (ADR-006).</summary>
public sealed record ProdutoResponse(
    Guid Id,
    Guid CategoriaId,
    string CategoriaNome,
    string Nome,
    string Sku,
    string? Descricao,
    decimal Preco,
    bool Ativo,
    DateTime DataCriacao,
    DateTime DataAtualizacao)
{
    public static ProdutoResponse De(Produto produto, string categoriaNome)
    {
        ArgumentNullException.ThrowIfNull(produto);
        ArgumentNullException.ThrowIfNull(categoriaNome);

        return new ProdutoResponse(
            produto.Id,
            produto.CategoriaId,
            categoriaNome,
            produto.Nome,
            produto.Sku.Valor,
            produto.Descricao,
            produto.Preco,
            produto.Ativo,
            produto.DataCriacao,
            produto.DataAtualizacao);
    }
}
