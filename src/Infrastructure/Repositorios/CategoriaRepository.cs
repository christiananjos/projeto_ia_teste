using Catalogo.Domain.Categorias;
using Catalogo.Domain.Excecoes;
using Catalogo.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Infrastructure.Repositorios;

internal sealed class CategoriaRepository(CatalogoDbContext db) : ICategoriaRepository
{
    public Task<Categoria?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.Categorias.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> ExisteNomeAsync(string nome, Guid? ignorarId, CancellationToken ct) =>
        db.Categorias.AnyAsync(c => c.Nome == nome && (ignorarId == null || c.Id != ignorarId), ct);

    public Task<bool> PossuiProdutosNaoRemovidosAsync(Guid categoriaId, CancellationToken ct) =>
        db.Produtos.AnyAsync(p => p.CategoriaId == categoriaId, ct);

    public void Adicionar(Categoria categoria) => db.Categorias.Add(categoria);

    public async Task ExcluirComProdutosRemovidosAsync(Guid categoriaId, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(
            (Db: db, CategoriaId: categoriaId),
            async (_, estado, token) =>
            {
                await using var transacao = await estado.Db.Database.BeginTransactionAsync(token);
                try
                {
                    await estado.Db.Produtos
                        .IgnoreQueryFilters()
                        .Where(p => p.CategoriaId == estado.CategoriaId && p.DataRemocao != null)
                        .ExecuteDeleteAsync(token);

                    await estado.Db.Categorias
                        .Where(c => c.Id == estado.CategoriaId)
                        .ExecuteDeleteAsync(token);

                    await transacao.CommitAsync(token);
                }
                catch (Exception ex) when (TradutorErrosSql.TentarTraduzir(ex, out var conflito)
                                           && conflito.Tipo == TipoConflitoPersistencia.Referencia)
                {
                    await transacao.RollbackAsync(CancellationToken.None);
                    throw conflito;
                }

                return true;
            },
            verifySucceeded: null,
            ct);
    }
}
