using Catalogo.Application.Comum;

namespace Catalogo.Api.Http;

/// <summary>Converte <see cref="Resultado"/> e <see cref="Erro"/> em respostas HTTP (ADR-004).</summary>
internal static class ResultadoHttpExtensions
{
    private static readonly IReadOnlyDictionary<string, string[]> SemCampos = new Dictionary<string, string[]>();

    public static IResult ParaHttp<T>(this Resultado<T> resultado, HttpContext httpContext, Func<T, IResult> aoSucesso) =>
        resultado.Sucesso
            ? aoSucesso(resultado.Valor!)
            : resultado.Erro!.ParaProblema(httpContext);

    public static IResult ParaHttp(this Resultado resultado, HttpContext httpContext, Func<IResult> aoSucesso) =>
        resultado.Sucesso
            ? aoSucesso()
            : resultado.Erro!.ParaProblema(httpContext);

    public static IResult ParaProblema(this Erro erro, HttpContext httpContext)
    {
        var status = StatusDe(erro.Tipo);
        var extensoes = new Dictionary<string, object?>
        {
            [ProblemDetailsPadrao.ExtensaoCodigo] = erro.Codigo,
            [ProblemDetailsPadrao.ExtensaoTraceId] = ProblemDetailsPadrao.TraceId(httpContext)
        };
        var instancia = httpContext.Request.Path.Value;

        if (erro.Tipo == TipoErro.Validacao)
        {
            return TypedResults.ValidationProblem(
                errors: erro.Campos ?? SemCampos,
                detail: erro.Mensagem,
                instance: instancia,
                title: ProblemDetailsPadrao.Titulo(status),
                type: ProblemDetailsPadrao.Tipo(status),
                extensions: extensoes);
        }

        return TypedResults.Problem(
            detail: erro.Mensagem,
            instance: instancia,
            statusCode: status,
            title: ProblemDetailsPadrao.Titulo(status),
            type: ProblemDetailsPadrao.Tipo(status),
            extensions: extensoes);
    }

    private static int StatusDe(TipoErro tipo) => tipo switch
    {
        TipoErro.Validacao => StatusCodes.Status400BadRequest,
        TipoErro.NaoEncontrado => StatusCodes.Status404NotFound,
        TipoErro.Conflito => StatusCodes.Status409Conflict,
        TipoErro.RegraNegocio => StatusCodes.Status422UnprocessableEntity,
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de erro sem mapeamento HTTP.")
    };
}
