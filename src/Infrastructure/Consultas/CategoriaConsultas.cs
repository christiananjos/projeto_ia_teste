using Catalogo.Application.Categorias;
using Catalogo.Application.Categorias.Dtos;
using Catalogo.Application.Comum;
using Catalogo.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Infrastructure.Consultas;

internal sealed class CategoriaConsultas(CatalogoDbContext db) : ICategoriaConsultas
{
    public Task<CategoriaResponse?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.Categorias
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoriaResponse(c.Id, c.Nome, c.Descricao, c.Ativa, c.DataCriacao, c.DataAtualizacao))
            .FirstOrDefaultAsync(ct);

    public async Task<PaginaResponse<CategoriaResponse>> ListarAsync(ListarCategoriasQuery query, CancellationToken ct)
    {
        var categorias = db.Categorias.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Nome))
        {
            var nome = query.Nome;
            categorias = categorias.Where(c => c.Nome.Contains(nome));
        }

        if (query.Ativa is { } ativa)
            categorias = categorias.Where(c => c.Ativa == ativa);

        var totalItens = await categorias.CountAsync(ct);
        var deslocamento = PaginaResponse<CategoriaResponse>.CalcularDeslocamento(query.Page, query.PageSize, totalItens);
        if (deslocamento is null)
            return PaginaResponse<CategoriaResponse>.Criar([], query.Page, query.PageSize, totalItens);

        var itens = await categorias
            .OrderBy(c => c.Nome)
            .ThenBy(c => c.Id)
            .Skip(deslocamento.Value)
            .Take(query.PageSize)
            .Select(c => new CategoriaResponse(c.Id, c.Nome, c.Descricao, c.Ativa, c.DataCriacao, c.DataAtualizacao))
            .ToListAsync(ct);

        return PaginaResponse<CategoriaResponse>.Criar(itens, query.Page, query.PageSize, totalItens);
    }
}
