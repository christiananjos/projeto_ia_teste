namespace Catalogo.Domain.Abstracoes;

public interface IUnitOfWork
{
    /// <summary>
    /// Persiste as alterações rastreadas.
    /// Lança <see cref="Excecoes.ConflitoPersistenciaException"/> em violação de unicidade ou de FK.
    /// </summary>
    Task SalvarAlteracoesAsync(CancellationToken ct);
}
