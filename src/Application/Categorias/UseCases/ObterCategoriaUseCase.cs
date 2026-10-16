using Catalogo.Application.Categorias.Dtos;
using Catalogo.Application.Comum;

namespace Catalogo.Application.Categorias.UseCases;

public sealed class ObterCategoriaUseCase(ICategoriaConsultas consultas)
{
    public async Task<Resultado<CategoriaResponse>> ExecutarAsync(Guid id, CancellationToken ct)
    {
        var categoria = await consultas.ObterPorIdAsync(id, ct);
        return categoria is null
            ? CategoriaErros.NaoEncontrada
            : Resultado<CategoriaResponse>.Ok(categoria);
    }
}
