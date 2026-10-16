using Catalogo.Application.Comum;
using Catalogo.Domain.Abstracoes;
using Catalogo.Domain.Produtos;

namespace Catalogo.Application.Produtos.UseCases;

/// <summary>BR-005: exclusão lógica. Produto já removido não é encontrado e responde 404.</summary>
public sealed class RemoverProdutoUseCase(
    IProdutoRepository produtos,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Resultado> ExecutarAsync(Guid id, CancellationToken ct)
    {
        var produto = await produtos.ObterPorIdAsync(id, ct);
        if (produto is null)
            return ProdutoErros.NaoEncontrado;

        produto.Remover(timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SalvarAlteracoesAsync(ct);

        return Resultado.Ok();
    }
}
