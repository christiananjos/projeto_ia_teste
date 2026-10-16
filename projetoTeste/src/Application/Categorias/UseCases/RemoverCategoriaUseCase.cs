using Catalogo.Application.Comum;
using Catalogo.Domain.Categorias;
using Catalogo.Domain.Excecoes;

namespace Catalogo.Application.Categorias.UseCases;

/// <summary>BR-006 + ADR-001: só remove se não houver produto não removido; apaga junto os removidos logicamente.</summary>
public sealed class RemoverCategoriaUseCase(ICategoriaRepository categorias)
{
    public async Task<Resultado> ExecutarAsync(Guid id, CancellationToken ct)
    {
        var categoria = await categorias.ObterPorIdAsync(id, ct);
        if (categoria is null)
            return CategoriaErros.NaoEncontrada;

        if (await categorias.PossuiProdutosNaoRemovidosAsync(id, ct))
            return CategoriaErros.PossuiProdutos;

        try
        {
            await categorias.ExcluirComProdutosRemovidosAsync(id, ct);
        }
        catch (ConflitoPersistenciaException ex) when (ex.Tipo == TipoConflitoPersistencia.Referencia)
        {
            return CategoriaErros.PossuiProdutos;
        }

        return Resultado.Ok();
    }
}
