using Catalogo.Api.Configuracao;
using Catalogo.Api.Http;
using Catalogo.Application.Comum;
using Catalogo.Application.Produtos.Dtos;
using Catalogo.Application.Produtos.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Catalogo.Api.Endpoints;

/// <summary>REQ-002, REQ-003 e REQ-004: produtos em <c>/api/v1/produtos</c>.</summary>
internal static class ProdutoEndpoints
{
    public const string Rota = "/api/v1/produtos";

    public static IEndpointRouteBuilder MapProdutoEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup(Rota).WithTags("Produtos");

        grupo.MapGet("/", ListarAsync)
            .RequireAuthorization(Politicas.Leitura)
            .WithName("ListarProdutos")
            .Produces<PaginaResponse<ProdutoResponse>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        grupo.MapGet("/{id:guid}", ObterAsync)
            .RequireAuthorization(Politicas.Leitura)
            .WithName("ObterProduto")
            .Produces<ProdutoResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        grupo.MapPost("/", CriarAsync)
            .RequireAuthorization(Politicas.Escrita)
            .WithName("CriarProduto")
            .Produces<ProdutoResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        grupo.MapPut("/{id:guid}", AtualizarAsync)
            .RequireAuthorization(Politicas.Escrita)
            .WithName("AtualizarProduto")
            .Produces<ProdutoResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        grupo.MapPatch("/{id:guid}/status", AlterarStatusAsync)
            .RequireAuthorization(Politicas.Escrita)
            .WithName("AlterarStatusProduto")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        grupo.MapDelete("/{id:guid}", RemoverAsync)
            .RequireAuthorization(Politicas.Escrita)
            .WithName("RemoverProduto")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return app;
    }

    private static async Task<IResult> ListarAsync(
        [FromQuery(Name = "page")] int? page,
        [FromQuery(Name = "pageSize")] int? pageSize,
        [FromQuery(Name = "categoriaId")] Guid? categoriaId,
        [FromQuery(Name = "nome")] string? nome,
        [FromQuery(Name = "ativo")] bool? ativo,
        [FromQuery(Name = "ordenarPor")] string? ordenarPor,
        [FromQuery(Name = "direcao")] string? direcao,
        [FromServices] ListarProdutosUseCase useCase,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var padrao = new ListarProdutosQuery();
        var query = new ListarProdutosQuery(
            page ?? padrao.Page,
            pageSize ?? padrao.PageSize,
            categoriaId,
            nome,
            ativo,
            ordenarPor,
            direcao);

        var resultado = await useCase.ExecutarAsync(query, ct);
        return resultado.ParaHttp(httpContext, pagina => TypedResults.Ok(pagina));
    }

    private static async Task<IResult> ObterAsync(
        Guid id,
        [FromServices] ObterProdutoUseCase useCase,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var resultado = await useCase.ExecutarAsync(id, ct);
        return resultado.ParaHttp(httpContext, produto => TypedResults.Ok(produto));
    }

    private static async Task<IResult> CriarAsync(
        CriarProdutoRequest request,
        [FromServices] CriarProdutoUseCase useCase,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var resultado = await useCase.ExecutarAsync(request, ct);
        return resultado.ParaHttp(httpContext, produto => TypedResults.Created($"{Rota}/{produto.Id}", produto));
    }

    private static async Task<IResult> AtualizarAsync(
        Guid id,
        AtualizarProdutoRequest request,
        [FromServices] AtualizarProdutoUseCase useCase,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var resultado = await useCase.ExecutarAsync(id, request, ct);
        return resultado.ParaHttp(httpContext, produto => TypedResults.Ok(produto));
    }

    private static async Task<IResult> AlterarStatusAsync(
        Guid id,
        AlterarStatusProdutoRequest request,
        [FromServices] AlterarStatusProdutoUseCase useCase,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var resultado = await useCase.ExecutarAsync(id, request, ct);
        return resultado.ParaHttp(httpContext, () => TypedResults.NoContent());
    }

    private static async Task<IResult> RemoverAsync(
        Guid id,
        [FromServices] RemoverProdutoUseCase useCase,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var resultado = await useCase.ExecutarAsync(id, ct);
        return resultado.ParaHttp(httpContext, () => TypedResults.NoContent());
    }
}
