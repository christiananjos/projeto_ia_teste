using Catalogo.Application.Categorias.Dtos;
using Catalogo.Application.Comum;
using FluentValidation;

namespace Catalogo.Application.Categorias.UseCases;

public sealed class ListarCategoriasUseCase(
    IValidator<ListarCategoriasQuery> validator,
    ICategoriaConsultas consultas)
{
    public async Task<Resultado<PaginaResponse<CategoriaResponse>>> ExecutarAsync(ListarCategoriasQuery query, CancellationToken ct)
    {
        var erroValidacao = await validator.ValidarAsync(query, ct);
        if (erroValidacao is not null)
            return erroValidacao;

        var pagina = await consultas.ListarAsync(query, ct);
        return Resultado<PaginaResponse<CategoriaResponse>>.Ok(pagina);
    }
}
