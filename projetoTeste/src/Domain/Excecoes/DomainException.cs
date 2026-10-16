namespace Catalogo.Domain.Excecoes;

/// <summary>Violação de invariante do domínio.</summary>
public class DomainException : Exception
{
    public DomainException(string mensagem) : base(mensagem)
    {
    }

    public DomainException(string mensagem, Exception innerException) : base(mensagem, innerException)
    {
    }
}
