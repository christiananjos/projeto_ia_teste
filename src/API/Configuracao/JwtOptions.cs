using System.ComponentModel.DataAnnotations;

namespace Catalogo.Api.Configuracao;

/// <summary>
/// Seção <c>Autenticacao:Jwt</c> (ADR-010). <see cref="ChaveAssinatura"/> vem de user-secrets ou da variável de
/// ambiente <c>Autenticacao__Jwt__ChaveAssinatura</c>, nunca de appsettings versionado.
/// </summary>
public sealed class JwtOptions
{
    public const string Secao = "Autenticacao:Jwt";
    public const int TamanhoMinimoChaveBytes = 32;

    [Required(ErrorMessage = "Autenticacao:Jwt:Issuer é obrigatório.")]
    public string Issuer { get; set; } = string.Empty;

    [Required(ErrorMessage = "Autenticacao:Jwt:Audience é obrigatório.")]
    public string Audience { get; set; } = string.Empty;

    [Required(ErrorMessage = "Autenticacao:Jwt:ChaveAssinatura é obrigatória.")]
    public string ChaveAssinatura { get; set; } = string.Empty;
}
