using Catalogo.Application.Comum;
using Catalogo.Application.Produtos.Dtos;

namespace Catalogo.Application.Produtos;

/// <summary>Consultas de leitura de produto (CQRS leve, ADR-003), sem rastreamento e sem produtos removidos.</summary>
public interface IProdutoConsultas
{
    /// <summary>Inclui produtos inativos (ADR-006); removidos retornam nulo.</summary>
    Task<ProdutoResponse?> ObterPorIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Filtros: ativo (<c>Ativo ?? true</c>), categoriaId (igualdade), nome (contém, sem diferenciar maiúsculas).
    /// Ordenação: <c>OrdenarPor ?? nome</c>, <c>Direcao ?? asc</c>, desempate por Id asc (ADR-009).
    /// </summary>
    Task<PaginaResponse<ProdutoResponse>> ListarAsync(ListarProdutosQuery query, CancellationToken ct);
}
