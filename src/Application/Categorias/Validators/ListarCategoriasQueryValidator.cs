using Catalogo.Application.Categorias.Dtos;
using Catalogo.Application.Comum;
using Catalogo.Domain.Regras;
using FluentValidation;

namespace Catalogo.Application.Categorias.Validators;

public sealed class ListarCategoriasQueryValidator : AbstractValidator<ListarCategoriasQuery>
{
    public ListarCategoriasQueryValidator()
    {
        RuleFor(x => x.Page).PageValida();
        RuleFor(x => x.PageSize).PageSizeValido();
        RuleFor(x => x.Nome)
            .MaximumLength(LimitesCampos.CategoriaNomeMax)
            .WithMessage($"O filtro de nome deve ter no máximo {LimitesCampos.CategoriaNomeMax} caracteres.");
    }
}
