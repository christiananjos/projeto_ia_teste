using Catalogo.Application.Comum;
using Catalogo.Application.Produtos.Dtos;
using Catalogo.Domain.Regras;
using FluentValidation;

namespace Catalogo.Application.Produtos.Validators;

public sealed class ListarProdutosQueryValidator : AbstractValidator<ListarProdutosQuery>
{
    private static readonly string[] CamposOrdenacao = [ListarProdutosQuery.OrdenarPorNome, ListarProdutosQuery.OrdenarPorPreco];
    private static readonly string[] Direcoes = [ListarProdutosQuery.DirecaoAsc, ListarProdutosQuery.DirecaoDesc];

    public ListarProdutosQueryValidator()
    {
        RuleFor(x => x.Page).PageValida();
        RuleFor(x => x.PageSize).PageSizeValido();
        RuleFor(x => x.Nome)
            .MaximumLength(LimitesCampos.ProdutoNomeMax)
            .WithMessage($"O filtro de nome deve ter no máximo {LimitesCampos.ProdutoNomeMax} caracteres.");
        RuleFor(x => x.OrdenarPor)
            .Must(v => v is null || CamposOrdenacao.Contains(v, StringComparer.OrdinalIgnoreCase))
            .WithMessage("O campo ordenarPor deve ser 'nome' ou 'preco'.");
        RuleFor(x => x.Direcao)
            .Must(v => v is null || Direcoes.Contains(v, StringComparer.OrdinalIgnoreCase))
            .WithMessage("O campo direcao deve ser 'asc' ou 'desc'.");
    }
}
