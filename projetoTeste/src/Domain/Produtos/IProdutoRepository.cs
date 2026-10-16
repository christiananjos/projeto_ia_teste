namespace Catalogo.Domain.Produtos;

public interface IProdutoRepository
{
    /// <summary>Obtém o produto rastreado para alteração. Produtos removidos não são retornados.</summary>
    Task<Produto?> ObterPorIdAsync(Guid id, CancellationToken ct);

    /// <summary>Verifica SKU já usado entre produtos não removidos, sem diferenciar maiúsculas.</summary>
    Task<bool> ExisteSkuAsync(string sku, CancellationToken ct);

    /// <summary>Verifica nome já usado na categoria entre produtos não removidos, sem diferenciar maiúsculas.</summary>
    Task<bool> ExisteNomeNaCategoriaAsync(Guid categoriaId, string nome, Guid? ignorarId, CancellationToken ct);

    void Adicionar(Produto produto);
}
