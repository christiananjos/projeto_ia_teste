using Catalogo.Domain.Produtos;
using Catalogo.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Infrastructure.Repositorios;

/// <summary>O global query filter (ADR-015) garante que produtos removidos não participem das consultas.</summary>
internal sealed class ProdutoRepository(CatalogoDbContext db) : IProdutoRepository
{
    public Task<Produto?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.Produtos.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> ExisteSkuAsync(string sku, CancellationToken ct)
    {
        var valor = Sku.Criar(sku);
        return db.Produtos.AnyAsync(p => p.Sku == valor, ct);
    }

    public Task<bool> ExisteNomeNaCategoriaAsync(Guid categoriaId, string nome, Guid? ignorarId, CancellationToken ct) =>
        db.Produtos.AnyAsync(
            p => p.CategoriaId == categoriaId && p.Nome == nome && (ignorarId == null || p.Id != ignorarId),
            ct);

    public void Adicionar(Produto produto) => db.Produtos.Add(produto);
}
