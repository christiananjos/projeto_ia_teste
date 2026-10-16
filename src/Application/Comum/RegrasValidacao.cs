using Catalogo.Domain.Regras;
using FluentValidation;

namespace Catalogo.Application.Comum;

/// <summary>Regras de validação reutilizadas pelos validators de categoria e produto.</summary>
internal static class RegrasValidacao
{
    public const string MensagemEspacosNasPontas = "O nome não pode começar nem terminar com espaços.";

    public static IRuleBuilderOptions<T, string> NomeValido<T>(this IRuleBuilderInitial<T, string> regra, int minimo, int maximo) =>
        regra
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .Length(minimo, maximo).WithMessage($"O nome deve ter entre {minimo} e {maximo} caracteres.")
            .Must(LimitesCampos.SemEspacosNasPontas).WithMessage(MensagemEspacosNasPontas);

    public static IRuleBuilderOptions<T, string?> DescricaoValida<T>(this IRuleBuilder<T, string?> regra, int maximo) =>
        regra
            .MaximumLength(maximo).WithMessage($"A descrição deve ter no máximo {maximo} caracteres.");

    public static IRuleBuilderOptions<T, decimal?> PrecoValido<T>(this IRuleBuilderInitial<T, decimal?> regra) =>
        regra
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("O preço é obrigatório.")
            .GreaterThan(0m).WithMessage("O preço deve ser maior que zero.")
            .LessThanOrEqualTo(LimitesCampos.PrecoMaximo).WithMessage($"O preço deve ser menor ou igual a {LimitesCampos.PrecoMaximo}.")
            .Must(p => p is null || LimitesCampos.TemNoMaximoCasasDecimais(p.Value))
            .WithMessage($"O preço deve ter no máximo {LimitesCampos.PrecoCasasDecimais} casas decimais.");

    public static IRuleBuilderOptions<T, Guid?> CategoriaIdValido<T>(this IRuleBuilderInitial<T, Guid?> regra) =>
        regra
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("A categoria é obrigatória.")
            .NotEqual(Guid.Empty).WithMessage("A categoria é obrigatória.");

    public const int PageSizeMaximo = 100;

    public static IRuleBuilderOptions<T, int> PageValida<T>(this IRuleBuilder<T, int> regra) =>
        regra.GreaterThanOrEqualTo(1).WithMessage("A página deve ser maior ou igual a 1.");

    public static IRuleBuilderOptions<T, int> PageSizeValido<T>(this IRuleBuilder<T, int> regra) =>
        regra.InclusiveBetween(1, PageSizeMaximo).WithMessage($"O tamanho da página deve estar entre 1 e {PageSizeMaximo}.");
}
