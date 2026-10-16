using Catalogo.Domain.Regras;

namespace Catalogo.Domain.Categorias;

public sealed class Categoria
{
    private const string CampoNome = "O nome da categoria";
    private const string CampoDescricao = "A descrição da categoria";

    private Categoria()
    {
        Nome = string.Empty;
    }

    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public string? Descricao { get; private set; }
    public bool Ativa { get; private set; }
    public DateTime DataCriacao { get; private set; }
    public DateTime DataAtualizacao { get; private set; }

    public static Categoria Criar(string nome, string? descricao, DateTime agoraUtc)
    {
        Validar(nome, descricao, agoraUtc);

        return new Categoria
        {
            Id = Guid.CreateVersion7(),
            Nome = nome,
            Descricao = descricao,
            Ativa = true,
            DataCriacao = agoraUtc,
            DataAtualizacao = agoraUtc
        };
    }

    public void Atualizar(string nome, string? descricao, bool ativa, DateTime agoraUtc)
    {
        Validar(nome, descricao, agoraUtc);

        Nome = nome;
        Descricao = descricao;
        Ativa = ativa;
        DataAtualizacao = agoraUtc;
    }

    private static void Validar(string nome, string? descricao, DateTime agoraUtc)
    {
        Guarda.Nome(nome, LimitesCampos.CategoriaNomeMin, LimitesCampos.CategoriaNomeMax, CampoNome);
        Guarda.TamanhoMaximo(descricao, LimitesCampos.CategoriaDescricaoMax, CampoDescricao);
        Guarda.Utc(agoraUtc);
    }
}
