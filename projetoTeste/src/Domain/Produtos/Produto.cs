using Catalogo.Domain.Excecoes;
using Catalogo.Domain.Regras;

namespace Catalogo.Domain.Produtos;

public sealed class Produto
{
    private const string CampoNome = "O nome do produto";
    private const string CampoDescricao = "A descrição do produto";
    private const string CampoCategoria = "A categoria do produto";

    private Produto()
    {
        Nome = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid CategoriaId { get; private set; }
    public string Nome { get; private set; }

    /// <summary>Imutável depois da criação (BR-003): não há método que o altere.</summary>
    public Sku Sku { get; private set; }

    public string? Descricao { get; private set; }
    public decimal Preco { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime DataCriacao { get; private set; }
    public DateTime DataAtualizacao { get; private set; }

    /// <summary>Preenchida na exclusão lógica (BR-005).</summary>
    public DateTime? DataRemocao { get; private set; }

    public bool EstaRemovido => DataRemocao is not null;

    public static Produto Criar(Guid categoriaId, string nome, Sku sku, string? descricao, decimal preco, DateTime agoraUtc)
    {
        Validar(categoriaId, nome, descricao, preco, agoraUtc);
        if (sku.Valor is null)
            throw new DomainException("O SKU é obrigatório.");

        return new Produto
        {
            Id = Guid.CreateVersion7(),
            CategoriaId = categoriaId,
            Nome = nome,
            Sku = sku,
            Descricao = descricao,
            Preco = preco,
            Ativo = true,
            DataCriacao = agoraUtc,
            DataAtualizacao = agoraUtc
        };
    }

    public void Atualizar(Guid categoriaId, string nome, string? descricao, decimal preco, DateTime agoraUtc)
    {
        Validar(categoriaId, nome, descricao, preco, agoraUtc);

        CategoriaId = categoriaId;
        Nome = nome;
        Descricao = descricao;
        Preco = preco;
        DataAtualizacao = agoraUtc;
    }

    /// <summary>Idempotente: com o mesmo valor atual, não altera nada (ADR-006).</summary>
    public void DefinirStatus(bool ativo, DateTime agoraUtc)
    {
        Guarda.Utc(agoraUtc);
        if (Ativo == ativo)
            return;

        Ativo = ativo;
        DataAtualizacao = agoraUtc;
    }

    public void Remover(DateTime agoraUtc)
    {
        Guarda.Utc(agoraUtc);
        if (EstaRemovido)
            throw new DomainException("O produto já foi removido.");

        DataRemocao = agoraUtc;
        DataAtualizacao = agoraUtc;
    }

    private static void Validar(Guid categoriaId, string nome, string? descricao, decimal preco, DateTime agoraUtc)
    {
        Guarda.IdObrigatorio(categoriaId, CampoCategoria);
        Guarda.Nome(nome, LimitesCampos.ProdutoNomeMin, LimitesCampos.ProdutoNomeMax, CampoNome);
        Guarda.TamanhoMaximo(descricao, LimitesCampos.ProdutoDescricaoMax, CampoDescricao);
        Guarda.Preco(preco);
        Guarda.Utc(agoraUtc);
    }
}
