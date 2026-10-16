using Catalogo.Domain.Excecoes;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Catalogo.Api.Http;

/// <summary>
/// Trata exceções não capturadas: <see cref="DomainException"/> vira 400 com <c>code = "dominio"</c>; qualquer
/// outra vira 500 genérico, sem stack trace nem mensagem interna.
/// </summary>
internal sealed partial class ExcecaoGlobalHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ExcecaoGlobalHandler> logger) : IExceptionHandler
{
    private const string CodigoDominio = "dominio";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        LogExcecao(logger, exception, httpContext.Request.Method, httpContext.Request.Path.Value);

        var (status, codigo, detalhe) = exception switch
        {
            DomainException dominio => (StatusCodes.Status400BadRequest, CodigoDominio, dominio.Message),
            BadHttpRequestException requisicaoInvalida => (
                requisicaoInvalida.StatusCode,
                ProblemDetailsPadrao.CodigoPadrao(requisicaoInvalida.StatusCode),
                ProblemDetailsPadrao.DetalhePadrao(requisicaoInvalida.StatusCode)),
            _ => (
                StatusCodes.Status500InternalServerError,
                ProblemDetailsPadrao.CodigoPadrao(StatusCodes.Status500InternalServerError),
                ProblemDetailsPadrao.DetalhePadrao(StatusCodes.Status500InternalServerError))
        };

        httpContext.Response.StatusCode = status;

        var problema = new ProblemDetails
        {
            Status = status,
            Type = ProblemDetailsPadrao.Tipo(status),
            Title = ProblemDetailsPadrao.Titulo(status),
            Detail = detalhe,
            Instance = httpContext.Request.Path.Value
        };
        problema.Extensions[ProblemDetailsPadrao.ExtensaoCodigo] = codigo;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problema,
            Exception = exception
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Exceção não tratada ao processar {Metodo} {Caminho}.")]
    private static partial void LogExcecao(ILogger logger, Exception exception, string metodo, string? caminho);
}
