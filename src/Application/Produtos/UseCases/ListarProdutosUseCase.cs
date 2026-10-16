using Catalogo.Application.Comum;
using Catalogo.Application.Produtos.Dtos;
using FluentValidation;

namespace Catalogo.Application.Produtos.UseCases;

public sealed class ListarProdutosUseCase(
    IValidator<ListarProdutosQuery> validator,
    IProdutoConsultas consultas)
{
    public async Task<Resultado<PaginaResponse<ProdutoResponse>>> ExecutarAsync(ListarProdutosQuery query, CancellationToken ct)
    {
        var erroValidacao = await validator.ValidarAsync(query, ct);
        if (erroValidacao is not null)
            return erroValidacao;

        var pagina = await consultas.ListarAsync(query, ct);
        return Resultado<PaginaResponse<ProdutoResponse>>.Ok(pagina);
    }
}
