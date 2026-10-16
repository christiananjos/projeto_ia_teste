using Catalogo.Application.Categorias.Dtos;
using Catalogo.Application.Comum;
using Catalogo.Domain.Regras;
using FluentValidation;

namespace Catalogo.Application.Categorias.Validators;

public sealed class AtualizarCategoriaRequestValidator : AbstractValidator<AtualizarCategoriaRequest>
{
    public AtualizarCategoriaRequestValidator()
    {
        RuleFor(x => x.Nome).NomeValido(LimitesCampos.CategoriaNomeMin, LimitesCampos.CategoriaNomeMax);
        RuleFor(x => x.Descricao).DescricaoValida(LimitesCampos.CategoriaDescricaoMax);
        RuleFor(x => x.Ativa).NotNull().WithMessage("O campo ativa é obrigatório.");
    }
}
