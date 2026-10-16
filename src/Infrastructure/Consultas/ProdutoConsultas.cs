using Catalogo.Application.Comum;
using Catalogo.Application.Produtos;
using Catalogo.Application.Produtos.Dtos;
using Catalogo.Domain.Produtos;
using Catalogo.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Infrastructure.Consultas;

internal sealed class ProdutoConsultas(CatalogoDbContext db) : IProdutoConsultas
{
    public async Task<ProdutoResponse?> ObterPorIdAsync(Guid id, CancellationToken ct)
    {
        var linha = await ProjetarComCategoria(db.Produtos.AsNoTracking().Where(p => p.Id == id))
            .FirstOrDefaultAsync(ct);

        return linha?.ParaResponse();
    }

    public async Task<PaginaResponse<ProdutoResponse>> ListarAsync(ListarProdutosQuery query, CancellationToken ct)
    {
        var ativo = query.Ativo ?? true;
        var produtos = db.Produtos.AsNoTracking().Where(p => p.Ativo == ativo);

        if (query.CategoriaId is { } categoriaId)
            produtos = produtos.Where(p => p.CategoriaId == categoriaId);

        if (!string.IsNullOrWhiteSpace(query.Nome))
        {
            var nome = query.Nome;
            produtos = produtos.Where(p => p.Nome.Contains(nome));
        }

        var totalItens = await produtos.CountAsync(ct);
        var deslocamento = PaginaResponse<ProdutoResponse>.CalcularDeslocamento(query.Page, query.PageSize, totalItens);
        if (deslocamento is null)
            return PaginaResponse<ProdutoResponse>.Criar([], query.Page, query.PageSize, totalItens);

        var linhas = ProjetarComCategoria(produtos);
        var ordenadas = (query.OrdenaPorPreco, query.OrdemDescendente) switch
        {
            (true, false) => linhas.OrderBy(l => l.Preco),
            (true, true) => linhas.OrderByDescending(l => l.Preco),
            (false, true) => linhas.OrderByDescending(l => l.Nome),
            _ => linhas.OrderBy(l => l.Nome)
        };

        var pagina = await ordenadas
            .ThenBy(l => l.Id)
            .Skip(deslocamento.Value)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var itens = pagina.Select(l => l.ParaResponse()).ToList();
        return PaginaResponse<ProdutoResponse>.Criar(itens, query.Page, query.PageSize, totalItens);
    }

    private IQueryable<ProdutoLinha> ProjetarComCategoria(IQueryable<Produto> produtos) =>
        from p in produtos
        join c in db.Categorias.AsNoTracking() on p.CategoriaId equals c.Id
        select new ProdutoLinha
        {
            Id = p.Id,
            CategoriaId = p.CategoriaId,
            CategoriaNome = c.Nome,
            Nome = p.Nome,
            Sku = p.Sku,
            Descricao = p.Descricao,
            Preco = p.Preco,
            Ativo = p.Ativo,
            DataCriacao = p.DataCriacao,
            DataAtualizacao = p.DataAtualizacao
        };

    /// <summary>
    /// Projeção intermediária: o SKU é um value object com conversor, então é materializado como <see cref="Sku"/>
    /// e convertido para string em memória.
    /// </summary>
    private sealed class ProdutoLinha
    {
        public Guid Id { get; init; }
        public Guid CategoriaId { get; init; }
        public required string CategoriaNome { get; init; }
        public required string Nome { get; init; }
        public Sku Sku { get; init; }
        public string? Descricao { get; init; }
        public decimal Preco { get; init; }
        public bool Ativo { get; init; }
        public DateTime DataCriacao { get; init; }
        public DateTime DataAtualizacao { get; init; }

        public ProdutoResponse ParaResponse() =>
            new(Id, CategoriaId, CategoriaNome, Nome, Sku.Valor, Descricao, Preco, Ativo, DataCriacao, DataAtualizacao);
    }
}
