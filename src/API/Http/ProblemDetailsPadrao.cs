using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Catalogo.Api.Http;

/// <summary>
/// Padroniza todo ProblemDetails da API (RFC 9457): <c>type</c>, <c>title</c>, <c>instance</c> e as extensões
/// <c>code</c> e <c>traceId</c>, inclusive nas respostas geradas pelo framework (401, 403, 404 de rota, 429...).
/// </summary>
internal static class ProblemDetailsPadrao
{
    public const string ExtensaoCodigo = "code";
    public const string ExtensaoTraceId = "traceId";

    public static string Tipo(int status) => $"https://httpstatuses.io/{status}";

    public static string Titulo(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "Requisição inválida",
        StatusCodes.Status401Unauthorized => "Não autenticado",
        StatusCodes.Status403Forbidden => "Acesso negado",
        StatusCodes.Status404NotFound => "Recurso não encontrado",
        StatusCodes.Status405MethodNotAllowed => "Método não permitido",
        StatusCodes.Status409Conflict => "Conflito",
        StatusCodes.Status413PayloadTooLarge => "Requisição muito grande",
        StatusCodes.Status415UnsupportedMediaType => "Tipo de mídia não suportado",
        StatusCodes.Status422UnprocessableEntity => "Regra de negócio violada",
        StatusCodes.Status429TooManyRequests => "Limite de requisições excedido",
        StatusCodes.Status500InternalServerError => "Erro interno",
        _ => "Erro"
    };

    public static string CodigoPadrao(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "requisicao_invalida",
        StatusCodes.Status401Unauthorized => "nao_autenticado",
        StatusCodes.Status403Forbidden => "acesso_negado",
        StatusCodes.Status404NotFound => "nao_encontrado",
        StatusCodes.Status405MethodNotAllowed => "metodo_nao_permitido",
        StatusCodes.Status413PayloadTooLarge => "requisicao_muito_grande",
        StatusCodes.Status415UnsupportedMediaType => "tipo_midia_nao_suportado",
        StatusCodes.Status429TooManyRequests => "limite_requisicoes",
        StatusCodes.Status500InternalServerError => "erro_interno",
        _ => "erro"
    };

    public static string? DetalhePadrao(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "A requisição contém parâmetros ou corpo inválidos.",
        StatusCodes.Status401Unauthorized => "É necessário um token de acesso válido.",
        StatusCodes.Status403Forbidden => "O usuário não tem permissão para esta operação.",
        StatusCodes.Status404NotFound => "O recurso solicitado não foi encontrado.",
        StatusCodes.Status405MethodNotAllowed => "O método HTTP não é suportado por este recurso.",
        StatusCodes.Status415UnsupportedMediaType => "O tipo de conteúdo da requisição não é suportado.",
        StatusCodes.Status429TooManyRequests => "Muitas requisições. Tente novamente mais tarde.",
        StatusCodes.Status500InternalServerError => "Ocorreu um erro inesperado ao processar a requisição.",
        _ => null
    };

    public static string TraceId(HttpContext httpContext) =>
        Activity.Current?.Id ?? httpContext.TraceIdentifier;

    /// <summary>Usado em <c>AddProblemDetails(o => o.CustomizeProblemDetails = ...)</c>.</summary>
    public static void Personalizar(ProblemDetailsContext contexto)
    {
        var problema = contexto.ProblemDetails;
        var status = problema.Status ?? contexto.HttpContext.Response.StatusCode;

        problema.Status = status;
        problema.Type = Tipo(status);
        problema.Title = Titulo(status);
        problema.Detail ??= DetalhePadrao(status);
        problema.Instance ??= ObterCaminho(contexto.HttpContext);
        problema.Extensions[ExtensaoTraceId] = TraceId(contexto.HttpContext);
        problema.Extensions.TryAdd(ExtensaoCodigo, CodigoPadrao(status));
    }

    private static string ObterCaminho(HttpContext httpContext)
    {
        // Em respostas de status code pages re-executadas, o caminho original fica na feature.
        var original = httpContext.Features.Get<IStatusCodeReExecuteFeature>();
        return original?.OriginalPath ?? httpContext.Request.Path.Value ?? string.Empty;
    }
}
