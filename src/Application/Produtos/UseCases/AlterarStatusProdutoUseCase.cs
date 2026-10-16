using Catalogo.Application.Comum;
using Catalogo.Application.Produtos.Dtos;
using Catalogo.Domain.Abstracoes;
using Catalogo.Domain.Produtos;
using FluentValidation;

namespace Catalogo.Application.Produtos.UseCases;

/// <summary>REQ-004: ativa ou inativa o produto. Idempotente (ADR-006).</summary>
public sealed class AlterarStatusProdutoUseCase(
    IValidator<AlterarStatusProdutoRequest> validator,
    IProdutoRepository produtos,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Resultado> ExecutarAsync(Guid id, AlterarStatusProdutoRequest request, CancellationToken ct)
    {
        var erroValidacao = await validator.ValidarAsync(request, ct);
        if (erroValidacao is not null)
            return erroValidacao;

        var produto = await produtos.ObterPorIdAsync(id, ct);
        if (produto is null)
            return ProdutoErros.NaoEncontrado;

        produto.DefinirStatus(request.Ativo!.Value, timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SalvarAlteracoesAsync(ct);

        return Resultado.Ok();
    }
}
