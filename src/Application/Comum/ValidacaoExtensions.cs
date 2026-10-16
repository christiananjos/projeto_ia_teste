using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;

namespace Catalogo.Application.Comum;

public static class ValidacaoExtensions
{
    /// <summary>Converte as falhas do FluentValidation em <see cref="Erro"/> de validação com chaves camelCase.</summary>
    public static Erro ParaErro(this ValidationResult resultado)
    {
        var campos = resultado.Errors
            .GroupBy(f => ParaCamelCase(f.PropertyName), StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);

        return Erro.Validacao(campos);
    }

    /// <summary>Valida a entrada e devolve o erro de validação, ou nulo quando é válida.</summary>
    public static async Task<Erro?> ValidarAsync<T>(this IValidator<T> validator, T entrada, CancellationToken ct)
    {
        var resultado = await validator.ValidateAsync(entrada, ct);
        return resultado.IsValid ? null : resultado.ParaErro();
    }

    private static string ParaCamelCase(string nomePropriedade) =>
        string.IsNullOrEmpty(nomePropriedade)
            ? nomePropriedade
            : string.Join('.', nomePropriedade.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}
