namespace Catalogo.Application.Categorias.Dtos;

/// <summary>
/// Substituição completa da categoria (ADR-005). <see cref="Ativa"/> é obrigatório; o tipo é anulável só para
/// que o validator detecte a ausência e responda 400.
/// </summary>
public sealed record AtualizarCategoriaRequest(string Nome, string? Descricao, bool? Ativa);
