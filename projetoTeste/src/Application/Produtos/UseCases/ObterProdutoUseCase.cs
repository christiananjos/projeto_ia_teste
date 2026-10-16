using Catalogo.Application.Comum;
using Catalogo.Application.Produtos.Dtos;

namespace Catalogo.Application.Produtos.UseCases;

public sealed class ObterProdutoUseCase(IProdutoConsultas consultas)
{
    public async Task<Resultado<ProdutoResponse>> ExecutarAsync(Guid id, CancellationToken ct)
    {
        var produto = await consultas.ObterPorIdAsync(id, ct);
        return produto is null
            ? ProdutoErros.NaoEncontrado
            : Resultado<ProdutoResponse>.Ok(produto);
    }
}
