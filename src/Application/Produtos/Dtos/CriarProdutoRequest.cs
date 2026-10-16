namespace Catalogo.Application.Produtos.Dtos;

/// <summary>Campos obrigatórios são anuláveis só para o validator detectar a ausência e responder 400.</summary>
public sealed record CriarProdutoRequest(
    Guid? CategoriaId,
    string Nome,
    string Sku,
    string? Descricao,
    decimal? Preco);
