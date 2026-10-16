using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Catalogo.Api.Configuracao;

internal static class AutenticacaoExtensions
{
    private const string ClaimRole = "role";
    private const string ClaimNome = "sub";

    /// <summary>
    /// JWT HMAC-SHA256 validado pela própria API (ADR-010). As opções são validadas no startup: sem chave de
    /// pelo menos 32 bytes, issuer e audience, a aplicação não sobe.
    /// </summary>
    public static IServiceCollection AddAutenticacaoJwt(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.Secao))
            .ValidateDataAnnotations()
            .Validate(
                o => o.ChaveAssinatura is not null && Encoding.UTF8.GetByteCount(o.ChaveAssinatura) >= JwtOptions.TamanhoMinimoChaveBytes,
                $"Autenticacao:Jwt:ChaveAssinatura deve ter pelo menos {JwtOptions.TamanhoMinimoChaveBytes} bytes.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, opcoesJwt) =>
            {
                var jwt = opcoesJwt.Value;

                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.ChaveAssinatura)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromSeconds(30),
                    RoleClaimType = ClaimRole,
                    NameClaimType = ClaimNome
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Politicas.Leitura, policy => policy.RequireAuthenticatedUser())
            .AddPolicy(Politicas.Escrita, policy => policy.RequireAuthenticatedUser().RequireRole(Politicas.RoleAdmin))
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }
}
