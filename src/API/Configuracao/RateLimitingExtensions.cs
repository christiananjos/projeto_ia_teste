using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Catalogo.Api.Configuracao;

internal static class RateLimitingExtensions
{
    private const string ParticaoSemIp = "ip-desconhecido";

    /// <summary>Limiter global de janela fixa particionado por IP de origem, sem fila (ADR-011).</summary>
    public static IServiceCollection AddRateLimitingPorIp(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RateLimitingOptions>()
            .Bind(configuration.GetSection(RateLimitingOptions.Secao))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(_ => { });

        services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<RateLimitingOptions>>((opcoes, limites) =>
            {
                var permitidas = limites.Value.LimitePorJanela;
                var janela = TimeSpan.FromSeconds(limites.Value.JanelaSegundos);

                opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                opcoes.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? ParticaoSemIp,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = permitidas,
                            Window = janela,
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }));

                opcoes.OnRejected = async (contexto, cancellationToken) =>
                {
                    var httpContext = contexto.HttpContext;
                    var esperar = contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                        ? retryAfter
                        : janela;

                    httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    httpContext.Response.Headers.RetryAfter =
                        Math.Max(1, (int)Math.Ceiling(esperar.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

                    var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                    await problemDetailsService.WriteAsync(new ProblemDetailsContext
                    {
                        HttpContext = httpContext,
                        ProblemDetails = { Status = StatusCodes.Status429TooManyRequests }
                    });
                };
            });

        return services;
    }
}
