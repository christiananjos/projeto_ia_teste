using Catalogo.Application.Comum;
using Catalogo.Application.Produtos.Dtos;
using Catalogo.Domain.Regras;
using FluentValidation;

namespace Catalogo.Application.Produtos.Validators;

public sealed class AtualizarProdutoRequestValidator : AbstractValidator<AtualizarProdutoRequest>
{
    public AtualizarProdutoRequestValidator()
    {
        RuleFor(x => x.CategoriaId).CategoriaIdValido();
        RuleFor(x => x.Nome).NomeValido(LimitesCampos.ProdutoNomeMin, LimitesCampos.ProdutoNomeMax);
        RuleFor(x => x.Descricao).DescricaoValida(LimitesCampos.ProdutoDescricaoMax);
        RuleFor(x => x.Preco).PrecoValido();
    }
}
