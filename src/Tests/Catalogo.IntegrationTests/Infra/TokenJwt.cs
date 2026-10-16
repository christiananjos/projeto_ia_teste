using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Catalogo.IntegrationTests.Infra;

/// <summary>
/// Gera JWT HS256 com a mesma chave/issuer/audience injetados na API de teste (ADR-010), nos perfis com role
/// <c>Admin</c>, sem role e com dados inválidos.
/// </summary>
public static class TokenJwt
{
    public const string Issuer = "https://auth.catalogo.test";
    public const string Audience = "catalogo-api";
    public const string Chave = "chave-de-teste-do-catalogo-com-mais-de-32-bytes";

    public static string Admin() => Criar(role: "Admin");

    public static string SemRole() => Criar(role: null);

    public static string Expirado() =>
        Criar(role: "Admin", inicio: DateTime.UtcNow.AddHours(-2), fim: DateTime.UtcNow.AddHours(-1));

    public static string ComIssuer(string issuer) => Criar(role: "Admin", issuer: issuer);

    public static string AssinadoComOutraChave() =>
        Criar(role: "Admin", chave: "outra-chave-qualquer-com-mais-de-32-bytes-de-tamanho");

    private static string Criar(
        string? role,
        string issuer = Issuer,
        string chave = Chave,
        DateTime? inicio = null,
        DateTime? fim = null)
    {
        var agora = DateTime.UtcNow;
        var claims = new Dictionary<string, object> { ["sub"] = "usuario-de-teste" };
        if (role is not null)
            claims["role"] = role;

        var descritor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = Audience,
            IssuedAt = inicio ?? agora,
            NotBefore = inicio ?? agora,
            Expires = fim ?? agora.AddMinutes(15),
            Claims = claims,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)), SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descritor);
    }
}
