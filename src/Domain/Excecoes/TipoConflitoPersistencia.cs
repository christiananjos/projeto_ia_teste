namespace Catalogo.Domain.Excecoes;

public enum TipoConflitoPersistencia
{
    /// <summary>Violação de índice único (SQL 2601/2627).</summary>
    Unicidade,

    /// <summary>Violação de chave estrangeira (SQL 547).</summary>
    Referencia
}
