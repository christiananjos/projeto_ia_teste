namespace Catalogo.Application.Produtos.Dtos;

/// <summary>
/// Substituição completa dos campos editáveis (ADR-006). <c>sku</c> e <c>ativo</c> não fazem parte do contrato;
/// se vierem no JSON, são ignorados pelo System.Text.Json.
/// </summary>
public sealed record AtualizarProdutoRequest(
    Guid? CategoriaId,
    string Nome,
    string? Descricao,
    decimal? Preco);
