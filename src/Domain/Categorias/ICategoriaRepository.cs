namespace Catalogo.Domain.Categorias;

public interface ICategoriaRepository
{
    /// <summary>Obtém a categoria rastreada para alteração.</summary>
    Task<Categoria?> ObterPorIdAsync(Guid id, CancellationToken ct);

    /// <summary>Verifica nome já usado (sem diferenciar maiúsculas, pela collation), ignorando opcionalmente um Id.</summary>
    Task<bool> ExisteNomeAsync(string nome, Guid? ignorarId, CancellationToken ct);

    Task<bool> PossuiProdutosNaoRemovidosAsync(Guid categoriaId, CancellationToken ct);

    void Adicionar(Categoria categoria);

    /// <summary>
    /// ADR-001: numa transação, apaga fisicamente os produtos removidos logicamente da categoria e a própria
    /// categoria. Comita sozinho. Em corrida com produto novo, lança
    /// <see cref="Excecoes.ConflitoPersistenciaException"/> do tipo Referencia.
    /// </summary>
    Task ExcluirComProdutosRemovidosAsync(Guid categoriaId, CancellationToken ct);
}
