using Catalogo.Api.Configuracao;
using Catalogo.Api.Http;
using Catalogo.Application.Categorias.Dtos;
using Catalogo.Application.Categorias.UseCases;
using Catalogo.Application.Comum;
using Microsoft.AspNetCore.Mvc;

namespace Catalogo.Api.Endpoints;

/// <summary>REQ-001: CRUD de categorias em <c>/api/v1/categorias</c>.</summary>
internal static class CategoriaEndpoints
{
    public const string Rota = "/api/v1/categorias";

    public static IEndpointRouteBuilder MapCategoriaEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup(Rota).WithTags("Categorias");

        grupo.MapGet("/", ListarAsync)
            .RequireAuthorization(Politicas.Leitura)
            .WithName("ListarCategorias")
            .Produces<PaginaResponse<CategoriaResponse>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        grupo.MapGet("/{id:guid}", ObterAsync)
            .RequireAuthorization(Politicas.Leitura)
            .WithName("ObterCategoria")
            .Produces<CategoriaResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        grupo.MapPost("/", CriarAsync)
            .RequireAuthorization(Politicas.Escrita)
            .WithName("CriarCategoria")
            .Produces<CategoriaResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        grupo.MapPut("/{id:guid}", AtualizarAsync)
            .RequireAuthorization(Politicas.Escrita)
            .WithName("AtualizarCategoria")
            .Produces<CategoriaResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        grupo.MapDelete("/{id:guid}", RemoverAsync)
            .RequireAuthorization(Politicas.Escrita)
            .WithName("RemoverCategoria")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return app;
    }

    private static async Task<IResult> ListarAsync(
        [FromQuery(Name = "page")] int? page,
        [FromQuery(Name = "pageSize")] int? pageSize,
        [FromQuery(Name = "nome")] string? nome,
        [FromQuery(Name = "ativa")] bool? ativa,
        [FromServices] ListarCategoriasUseCase useCase,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var padrao = new ListarCategoriasQuery();
        var query = new ListarCategoriasQuery(page ?? padrao.Page, pageSize ?? padrao.PageSize, nome, ativa);

        var resultado = await useCase.ExecutarAsync(query, ct);
        return resultado.ParaHttp(httpContext, pagina => TypedResults.Ok(pagina));
    }

    private static async Task<IResult> ObterAsync(
        Guid id,
        [FromServices] ObterCategoriaUseCase useCase,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var resultado = await useCase.ExecutarAsync(id, ct);
        return resultado.ParaHttp(httpContext, categoria => TypedResults.Ok(categoria));
    }

    private static async Task<IResult> CriarAsync(
        CriarCategoriaRequest request,
        [FromServices] CriarCategoriaUseCase useCase,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var resultado = await useCase.ExecutarAsync(request, ct);
        return resultado.ParaHttp(httpContext, categoria => TypedResults.Created($"{Rota}/{categoria.Id}", categoria));
    }

    private static async Task<IResult> AtualizarAsync(
        Guid id,
        AtualizarCategoriaRequest request,
        [FromServices] AtualizarCategoriaUseCase useCase,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var resultado = await useCase.ExecutarAsync(id, request, ct);
        return resultado.ParaHttp(httpContext, categoria => TypedResults.Ok(categoria));
    }

    private static async Task<IResult> RemoverAsync(
        Guid id,
        [FromServices] RemoverCategoriaUseCase useCase,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var resultado = await useCase.ExecutarAsync(id, ct);
        return resultado.ParaHttp(httpContext, () => TypedResults.NoContent());
    }
}
