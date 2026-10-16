using Catalogo.Domain.Categorias;

namespace Catalogo.Application.Categorias.Dtos;

public sealed record CategoriaResponse(
    Guid Id,
    string Nome,
    string? Descricao,
    bool Ativa,
    DateTime DataCriacao,
    DateTime DataAtualizacao)
{
    public static CategoriaResponse De(Categoria categoria)
    {
        ArgumentNullException.ThrowIfNull(categoria);

        return new CategoriaResponse(
            categoria.Id,
            categoria.Nome,
            categoria.Descricao,
            categoria.Ativa,
            categoria.DataCriacao,
            categoria.DataAtualizacao);
    }
}
