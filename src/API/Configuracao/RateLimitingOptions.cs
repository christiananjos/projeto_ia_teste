using System.ComponentModel.DataAnnotations;

namespace Catalogo.Api.Configuracao;

/// <summary>Seção <c>RateLimiting</c> (ADR-011): janela fixa por IP.</summary>
public sealed class RateLimitingOptions
{
    public const string Secao = "RateLimiting";

    [Range(1, int.MaxValue, ErrorMessage = "RateLimiting:LimitePorJanela deve ser maior que zero.")]
    public int LimitePorJanela { get; set; } = 100;

    [Range(1, 86400, ErrorMessage = "RateLimiting:JanelaSegundos deve estar entre 1 e 86400.")]
    public int JanelaSegundos { get; set; } = 60;
}
