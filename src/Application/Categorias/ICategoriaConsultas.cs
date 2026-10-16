using Catalogo.Application.Categorias.Dtos;
using Catalogo.Application.Comum;

namespace Catalogo.Application.Categorias;

/// <summary>Consultas de leitura de categoria (CQRS leve, ADR-003), sem rastreamento.</summary>
public interface ICategoriaConsultas
{
    Task<CategoriaResponse?> ObterPorIdAsync(Guid id, CancellationToken ct);

    /// <summary>Filtros: Nome (contém, sem diferenciar maiúsculas) e Ativa (nulo = todas). Ordem: Nome asc, Id asc.</summary>
    Task<PaginaResponse<CategoriaResponse>> ListarAsync(ListarCategoriasQuery query, CancellationToken ct);
}
