using Catalogo.Domain.Excecoes;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Catalogo.Api.Http;

/// <summary>
/// Trata exceções não capturadas: <see cref="DomainException"/> vira 400 com <c>code = "dominio"</c>; qualquer
/// outra vira 500 genérico, sem stack trace nem mensagem interna. Cancelamento causado pelo cliente que
/// desconectou não é erro do servidor: é logado em <c>Information</c> e a resposta não recebe corpo.
/// </summary>
internal sealed partial class ExcecaoGlobalHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ExcecaoGlobalHandler> logger) : IExceptionHandler
{
    private const string CodigoDominio = "dominio";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var metodo = httpContext.Request.Method;
        var caminho = httpContext.Request.Path.Value;

        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            LogRequisicaoCancelada(logger, metodo, caminho);
            if (!httpContext.Response.HasStarted)
                httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }

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

        if (status >= StatusCodes.Status500InternalServerError)
            LogExcecao(logger, exception, metodo, caminho);
        else
            LogRequisicaoRejeitada(logger, exception, status, metodo, caminho);

        httpContext.Response.StatusCode = status;

        var problema = new ProblemDetails
        {
            Status = status,
            Type = ProblemDetailsPadrao.Tipo(status),
            Title = ProblemDetailsPadrao.Titulo(status),
            Detail = detalhe,
            Instance = caminho
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Requisição {Metodo} {Caminho} rejeitada com {Status}.")]
    private static partial void LogRequisicaoRejeitada(ILogger logger, Exception exception, int status, string metodo, string? caminho);

    [LoggerMessage(Level = LogLevel.Information, Message = "Requisição {Metodo} {Caminho} cancelada pelo cliente.")]
    private static partial void LogRequisicaoCancelada(ILogger logger, string metodo, string? caminho);
}
