namespace Catalogo.Application.Categorias.Dtos;

public sealed record ListarCategoriasQuery(
    int Page = 1,
    int PageSize = 20,
    string? Nome = null,
    bool? Ativa = null);
