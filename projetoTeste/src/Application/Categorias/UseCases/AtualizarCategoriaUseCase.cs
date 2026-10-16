using Catalogo.Application.Categorias.Dtos;
using Catalogo.Application.Comum;
using Catalogo.Domain.Abstracoes;
using Catalogo.Domain.Categorias;
using Catalogo.Domain.Excecoes;
using FluentValidation;

namespace Catalogo.Application.Categorias.UseCases;

public sealed class AtualizarCategoriaUseCase(
    IValidator<AtualizarCategoriaRequest> validator,
    ICategoriaRepository categorias,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Resultado<CategoriaResponse>> ExecutarAsync(Guid id, AtualizarCategoriaRequest request, CancellationToken ct)
    {
        var erroValidacao = await validator.ValidarAsync(request, ct);
        if (erroValidacao is not null)
            return erroValidacao;

        var categoria = await categorias.ObterPorIdAsync(id, ct);
        if (categoria is null)
            return CategoriaErros.NaoEncontrada;

        if (await categorias.ExisteNomeAsync(request.Nome, id, ct))
            return CategoriaErros.NomeDuplicado;

        categoria.Atualizar(request.Nome, request.Descricao, request.Ativa!.Value, timeProvider.GetUtcNow().UtcDateTime);

        try
        {
            await unitOfWork.SalvarAlteracoesAsync(ct);
        }
        catch (ConflitoPersistenciaException ex) when (ex.Tipo == TipoConflitoPersistencia.Unicidade)
        {
            return CategoriaErros.NomeDuplicado;
        }

        return Resultado<CategoriaResponse>.Ok(CategoriaResponse.De(categoria));
    }
}
