using Catalogo.Application.Categorias.Dtos;
using Catalogo.Application.Comum;
using Catalogo.Domain.Regras;
using FluentValidation;

namespace Catalogo.Application.Categorias.Validators;

public sealed class CriarCategoriaRequestValidator : AbstractValidator<CriarCategoriaRequest>
{
    public CriarCategoriaRequestValidator()
    {
        RuleFor(x => x.Nome).NomeValido(LimitesCampos.CategoriaNomeMin, LimitesCampos.CategoriaNomeMax);
        RuleFor(x => x.Descricao).DescricaoValida(LimitesCampos.CategoriaDescricaoMax);
    }
}
