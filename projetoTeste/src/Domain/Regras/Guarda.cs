using Catalogo.Domain.Excecoes;

namespace Catalogo.Domain.Regras;

/// <summary>Guardas de invariantes do domínio. Última linha de defesa depois dos validators.</summary>
internal static class Guarda
{
    public static void Nome(string? nome, int minimo, int maximo, string campo)
    {
        if (nome is null)
            throw new DomainException($"{campo} é obrigatório.");
        if (nome.Length < minimo || nome.Length > maximo)
            throw new DomainException($"{campo} deve ter entre {minimo} e {maximo} caracteres.");
        if (!LimitesCampos.SemEspacosNasPontas(nome))
            throw new DomainException($"{campo} não pode começar nem terminar com espaços.");
    }

    public static void TamanhoMaximo(string? valor, int maximo, string campo)
    {
        if (valor is not null && valor.Length > maximo)
            throw new DomainException($"{campo} deve ter no máximo {maximo} caracteres.");
    }

    public static void Utc(DateTime data)
    {
        if (data.Kind != DateTimeKind.Utc)
            throw new DomainException("A data informada deve estar em UTC.");
    }

    public static void IdObrigatorio(Guid id, string campo)
    {
        if (id == Guid.Empty)
            throw new DomainException($"{campo} é obrigatória.");
    }

    public static void Preco(decimal preco)
    {
        if (preco <= 0)
            throw new DomainException("O preço deve ser maior que zero.");
        if (preco > LimitesCampos.PrecoMaximo)
            throw new DomainException($"O preço deve ser menor ou igual a {LimitesCampos.PrecoMaximo}.");
        if (!LimitesCampos.TemNoMaximoCasasDecimais(preco))
            throw new DomainException($"O preço deve ter no máximo {LimitesCampos.PrecoCasasDecimais} casas decimais.");
    }
}
