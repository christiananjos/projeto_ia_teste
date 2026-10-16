using Catalogo.Application.Comum;
using Catalogo.Application.Produtos.Dtos;
using Catalogo.Domain.Abstracoes;
using Catalogo.Domain.Categorias;
using Catalogo.Domain.Excecoes;
using Catalogo.Domain.Produtos;
using FluentValidation;

namespace Catalogo.Application.Produtos.UseCases;

/// <summary>Ordem das checagens: 400 → 422 (categoria) → 409 (SKU) → 409 (nome) (ADR-006).</summary>
public sealed class CriarProdutoUseCase(
    IValidator<CriarProdutoRequest> validator,
    ICategoriaRepository categorias,
    IProdutoRepository produtos,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Resultado<ProdutoResponse>> ExecutarAsync(CriarProdutoRequest request, CancellationToken ct)
    {
        var erroValidacao = await validator.ValidarAsync(request, ct);
        if (erroValidacao is not null)
            return erroValidacao;

        var categoriaId = request.CategoriaId!.Value;
        var categoria = await categorias.ObterPorIdAsync(categoriaId, ct);
        if (categoria is null || !categoria.Ativa)
            return ProdutoErros.CategoriaInvalida;

        if (await produtos.ExisteSkuAsync(request.Sku, ct))
            return ProdutoErros.SkuDuplicado;

        if (await produtos.ExisteNomeNaCategoriaAsync(categoriaId, request.Nome, null, ct))
            return ProdutoErros.NomeDuplicado;

        var produto = Produto.Criar(
            categoriaId,
            request.Nome,
            Sku.Criar(request.Sku),
            request.Descricao,
            request.Preco!.Value,
            timeProvider.GetUtcNow().UtcDateTime);
        produtos.Adicionar(produto);

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
