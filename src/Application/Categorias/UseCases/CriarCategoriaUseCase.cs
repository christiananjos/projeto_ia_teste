using Catalogo.Application.Categorias.Dtos;
using Catalogo.Application.Comum;
using Catalogo.Domain.Abstracoes;
using Catalogo.Domain.Categorias;
using Catalogo.Domain.Excecoes;
using FluentValidation;

namespace Catalogo.Application.Categorias.UseCases;

public sealed class CriarCategoriaUseCase(
    IValidator<CriarCategoriaRequest> validator,
    ICategoriaRepository categorias,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Resultado<CategoriaResponse>> ExecutarAsync(CriarCategoriaRequest request, CancellationToken ct)
    {
        var erroValidacao = await validator.ValidarAsync(request, ct);
        if (erroValidacao is not null)
            return erroValidacao;

        if (await categorias.ExisteNomeAsync(request.Nome, null, ct))
            return CategoriaErros.NomeDuplicado;

        var categoria = Categoria.Criar(request.Nome, request.Descricao, timeProvider.GetUtcNow().UtcDateTime);
        categorias.Adicionar(categoria);

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
