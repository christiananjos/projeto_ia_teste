namespace Catalogo.Domain.Excecoes;

/// <summary>
/// Conflito detectado pelo banco (unicidade ou referência), traduzido pela Infrastructure para que a
/// Application trate corridas sem depender do EF Core.
/// </summary>
public class ConflitoPersistenciaException : Exception
{
    public ConflitoPersistenciaException(TipoConflitoPersistencia tipo, string? restricao, Exception? innerException = null)
        : base(CriarMensagem(tipo, restricao), innerException)
    {
        Tipo = tipo;
        Restricao = restricao;
    }

    public TipoConflitoPersistencia Tipo { get; }

    /// <summary>Nome do índice ou da FK que falhou, quando identificado.</summary>
    public string? Restricao { get; }

    private static string CriarMensagem(TipoConflitoPersistencia tipo, string? restricao) =>
        restricao is null
            ? $"Conflito de persistência ({tipo})."
            : $"Conflito de persistência ({tipo}) na restrição '{restricao}'.";
}
