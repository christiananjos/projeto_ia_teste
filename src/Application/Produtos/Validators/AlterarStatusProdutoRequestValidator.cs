using Catalogo.Application.Produtos.Dtos;
using FluentValidation;

namespace Catalogo.Application.Produtos.Validators;

public sealed class AlterarStatusProdutoRequestValidator : AbstractValidator<AlterarStatusProdutoRequest>
{
    public AlterarStatusProdutoRequestValidator()
    {
        RuleFor(x => x.Ativo).NotNull().WithMessage("O campo ativo é obrigatório.");
    }
}
