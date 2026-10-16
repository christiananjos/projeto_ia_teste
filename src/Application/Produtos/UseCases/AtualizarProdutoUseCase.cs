using Catalogo.Application.Comum;
using Catalogo.Application.Produtos.Dtos;
using Catalogo.Domain.Abstracoes;
using Catalogo.Domain.Categorias;
using Catalogo.Domain.Excecoes;
using Catalogo.Domain.Produtos;
using FluentValidation;

namespace Catalogo.Application.Produtos.UseCases;

/// <summary>
/// Atualiza os campos editáveis. Mover para outra categoria exige que ela exista e esteja ativa (BR-004);
/// manter a categoria atual é aceito mesmo que ela esteja inativa (ADR-005).
/// </summary>
public sealed class AtualizarProdutoUseCase(
    IValidator<AtualizarProdutoRequest> validator,
    ICategoriaRepository categorias,
    IProdutoRepository produtos,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Resultado<ProdutoResponse>> ExecutarAsync(Guid id, AtualizarProdutoRequest request, CancellationToken ct)
    {
        var erroValidacao = await validator.ValidarAsync(request, ct);
        if (erroValidacao is not null)
            return erroValidacao;

        var produto = await produtos.ObterPorIdAsync(id, ct);
        if (produto is null)
            return ProdutoErros.NaoEncontrado;

        var categoriaId = request.CategoriaId!.Value;
        var categoria = await categorias.ObterPorIdAsync(categoriaId, ct);
        var moveuDeCategoria = categoriaId != produto.CategoriaId;
        if (categoria is null || (moveuDeCategoria && !categoria.Ativa))
            return ProdutoErros.CategoriaInvalida;

        if (await produtos.ExisteNomeNaCategoriaAsync(categoriaId, request.Nome, id, ct))
            return ProdutoErros.NomeDuplicado;

        produto.Atualizar(
            categoriaId,
            request.Nome,
            request.Descricao,
            request.Preco!.Value,
            timeProvider.GetUtcNow().UtcDateTime);

        try
        {
            await unitOfWork.SalvarAlteracoesAsync(ct);
        }
        catch (ConflitoPersistenciaException ex)
        {
            return ProdutoErros.DeConflito(ex);
        }

        return Resultado<ProdutoResponse>.Ok(ProdutoResponse.De(produto, categoria.Nome));
    }
}
