using Catalogo.Application.Comum;
using Catalogo.Application.Produtos.Dtos;
using Catalogo.Domain.Regras;
using FluentValidation;

namespace Catalogo.Application.Produtos.Validators;

public sealed class CriarProdutoRequestValidator : AbstractValidator<CriarProdutoRequest>
{
    public CriarProdutoRequestValidator()
    {
        RuleFor(x => x.CategoriaId).CategoriaIdValido();
        RuleFor(x => x.Nome).NomeValido(LimitesCampos.ProdutoNomeMin, LimitesCampos.ProdutoNomeMax);
        RuleFor(x => x.Sku)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("O SKU é obrigatório.")
            .Length(LimitesCampos.SkuMin, LimitesCampos.SkuMax)
            .WithMessage($"O SKU deve ter entre {LimitesCampos.SkuMin} e {LimitesCampos.SkuMax} caracteres.")
            .Matches(LimitesCampos.SkuRegex).WithMessage("O SKU deve conter apenas letras, números e hífen.");
        RuleFor(x => x.Descricao).DescricaoValida(LimitesCampos.ProdutoDescricaoMax);
        RuleFor(x => x.Preco).PrecoValido();
    }
}
