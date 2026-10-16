namespace Catalogo.Application.Comum;

/// <summary>Resultado de um caso de uso sem valor de retorno (ADR-003).</summary>
public sealed class Resultado
{
    private static readonly Resultado SucessoUnico = new(true, null);

    private Resultado(bool sucesso, Erro? erro)
    {
        Sucesso = sucesso;
        Erro = erro;
    }

    public bool Sucesso { get; }
    public Erro? Erro { get; }

    public static Resultado Ok() => SucessoUnico;

    public static Resultado Falha(Erro erro)
    {
        ArgumentNullException.ThrowIfNull(erro);
        return new Resultado(false, erro);
    }

    public static implicit operator Resultado(Erro erro) => Falha(erro);
}

/// <summary>Resultado de um caso de uso com valor de retorno (ADR-003).</summary>
public sealed class Resultado<T>
{
    private Resultado(bool sucesso, T? valor, Erro? erro)
    {
        Sucesso = sucesso;
        Valor = valor;
        Erro = erro;
    }

    public bool Sucesso { get; }
    public T? Valor { get; }
    public Erro? Erro { get; }

    public static Resultado<T> Ok(T valor) => new(true, valor, null);

    public static Resultado<T> Falha(Erro erro)
    {
        ArgumentNullException.ThrowIfNull(erro);
        return new Resultado<T>(false, default, erro);
    }

    public static implicit operator Resultado<T>(Erro erro) => Falha(erro);
}
