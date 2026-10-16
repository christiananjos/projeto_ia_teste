using Catalogo.Domain.Excecoes;
using Catalogo.Domain.Produtos;
using Catalogo.Domain.Regras;
using Catalogo.UnitTests.Fixtures;

namespace Catalogo.UnitTests.Domain;

public sealed class ProdutoTests
{
    private static readonly Guid CategoriaId = Guid.CreateVersion7();

    private static Produto Criar(
        string nome = "Teclado Mecânico",
        string? descricao = "ABNT2",
        decimal preco = 349.90m,
        Guid? categoriaId = null,
        DateTime? agora = null) =>
        Produto.Criar(categoriaId ?? CategoriaId, nome, Sku.Criar("TEC-001"), descricao, preco, agora ?? Relogio.AgoraUtc);

    [Fact]
    public void Criar_ComDadosValidos_NasceAtivoENaoRemovido()
    {
        var produto = Criar();

        produto.Id.Version.ShouldBe(7);
        produto.CategoriaId.ShouldBe(CategoriaId);
        produto.Nome.ShouldBe("Teclado Mecânico");
        produto.Sku.Valor.ShouldBe("TEC-001");
        produto.Descricao.ShouldBe("ABNT2");
        produto.Preco.ShouldBe(349.90m);
        produto.Ativo.ShouldBeTrue();
        produto.DataRemocao.ShouldBeNull();
        produto.EstaRemovido.ShouldBeFalse();
    }

    // BR-007
    [Fact]
    public void Criar_DefineAsDatasPeloServidorEmUtc()
    {
        var produto = Criar();

        produto.DataCriacao.ShouldBe(Relogio.AgoraUtc);
        produto.DataAtualizacao.ShouldBe(Relogio.AgoraUtc);
        produto.DataCriacao.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void Criar_ComDataLocal_LancaDomainException()
    {
        Should.Throw<DomainException>(() => Criar(agora: DateTime.SpecifyKind(Relogio.AgoraUtc, DateTimeKind.Local)));
    }

    [Fact]
    public void Criar_SemCategoria_LancaDomainException()
    {
        Should.Throw<DomainException>(() => Criar(categoriaId: Guid.Empty))
            .Message.ShouldBe("A categoria do produto é obrigatória.");
    }

    [Fact]
    public void Criar_ComSkuNaoInicializado_LancaDomainException()
    {
        Should.Throw<DomainException>(() =>
                Produto.Criar(CategoriaId, "Teclado Mecânico", default, null, 10m, Relogio.AgoraUtc))
            .Message.ShouldBe("O SKU é obrigatório.");
    }

    // BR-001
    [Theory]
    [InlineData(3)]
    [InlineData(150)]
    public void Criar_ComNomeNoLimite_Aceita(int tamanho)
    {
        Criar(nome: Requisicoes.Texto(tamanho)).Nome.Length.ShouldBe(tamanho);
    }

    // BR-001
    [Theory]
    [InlineData(2)]
    [InlineData(151)]
    public void Criar_ComNomeForaDoLimite_LancaDomainException(int tamanho)
    {
        Should.Throw<DomainException>(() => Criar(nome: Requisicoes.Texto(tamanho)))
            .Message.ShouldBe("O nome do produto deve ter entre 3 e 150 caracteres.");
    }

    // BR-001
    [Theory]
    [InlineData(" Teclado")]
    [InlineData("Teclado ")]
    public void Criar_ComNomeComEspacoNasPontas_LancaDomainException(string nome)
    {
        Should.Throw<DomainException>(() => Criar(nome: nome))
            .Message.ShouldBe("O nome do produto não pode começar nem terminar com espaços.");
    }

    [Fact]
    public void Criar_ComDescricaoAcimaDe1000_LancaDomainException()
    {
        Should.Throw<DomainException>(() => Criar(descricao: Requisicoes.Texto(1001)));
        Criar(descricao: Requisicoes.Texto(1000)).Descricao!.Length.ShouldBe(1000);
    }

    // BR-002
    [Theory]
    [InlineData("0.01")]
    [InlineData("349.90")]
    [InlineData("1")]
    [InlineData("9999999999999999.99")]
    public void Criar_ComPrecoValido_Aceita(string preco)
    {
        var valor = decimal.Parse(preco, System.Globalization.CultureInfo.InvariantCulture);
        Criar(preco: valor).Preco.ShouldBe(valor);
    }

    // BR-002
    [Theory]
    [InlineData("0", "O preço deve ser maior que zero.")]
    [InlineData("-10", "O preço deve ser maior que zero.")]
    [InlineData("0.001", "O preço deve ter no máximo 2 casas decimais.")]
    [InlineData("10.999", "O preço deve ter no máximo 2 casas decimais.")]
    public void Criar_ComPrecoInvalido_LancaDomainException(string preco, string mensagem)
    {
        var valor = decimal.Parse(preco, System.Globalization.CultureInfo.InvariantCulture);
        Should.Throw<DomainException>(() => Criar(preco: valor)).Message.ShouldBe(mensagem);
    }

    // BR-002 / ADR-014
    [Fact]
    public void Criar_ComPrecoAcimaDoLimiteDoDecimal18_2_LancaDomainException()
    {
        Should.Throw<DomainException>(() => Criar(preco: LimitesCampos.PrecoMaximo + 0.01m))
            .Message.ShouldStartWith("O preço deve ser menor ou igual a");
    }

    // BR-003: o SKU não é alterado pela atualização
    [Fact]
    public void Atualizar_SubstituiCamposEditaveisEMantemSkuEDataDeCriacao()
    {
        var produto = Criar();
        var novaCategoria = Guid.CreateVersion7();
        var depois = Relogio.Depois(TimeSpan.FromMinutes(5));

        produto.Atualizar(novaCategoria, "Teclado Mecânico RGB", null, 399.90m, depois);

        produto.CategoriaId.ShouldBe(novaCategoria);
        produto.Nome.ShouldBe("Teclado Mecânico RGB");
        produto.Descricao.ShouldBeNull();
        produto.Preco.ShouldBe(399.90m);
        produto.Sku.Valor.ShouldBe("TEC-001");
        produto.DataCriacao.ShouldBe(Relogio.AgoraUtc);
        produto.DataAtualizacao.ShouldBe(depois);
    }

    [Fact]
    public void Atualizar_ComPrecoInvalido_NaoAlteraNada()
    {
        var produto = Criar();

        Should.Throw<DomainException>(() =>
            produto.Atualizar(CategoriaId, "Outro nome", null, 0m, Relogio.Depois(TimeSpan.FromMinutes(5))));

        produto.Nome.ShouldBe("Teclado Mecânico");
        produto.Preco.ShouldBe(349.90m);
        produto.DataAtualizacao.ShouldBe(Relogio.AgoraUtc);
    }

    // REQ-004 / ADR-006
    [Fact]
    public void DefinirStatus_ComValorDiferente_AlteraAtivoEDataDeAtualizacao()
    {
        var produto = Criar();
        var depois = Relogio.Depois(TimeSpan.FromMinutes(1));

        produto.DefinirStatus(false, depois);

        produto.Ativo.ShouldBeFalse();
        produto.DataAtualizacao.ShouldBe(depois);
    }

    // ADR-006: idempotente
    [Fact]
    public void DefinirStatus_ComMesmoValor_NaoAlteraADataDeAtualizacao()
    {
        var produto = Criar();

        produto.DefinirStatus(true, Relogio.Depois(TimeSpan.FromMinutes(1)));

        produto.Ativo.ShouldBeTrue();
        produto.DataAtualizacao.ShouldBe(Relogio.AgoraUtc);
    }

    [Fact]
    public void DefinirStatus_ComDataQueNaoEhUtc_LancaDomainException()
    {
        var produto = Criar();
        Should.Throw<DomainException>(() => produto.DefinirStatus(false, DateTime.SpecifyKind(Relogio.AgoraUtc, DateTimeKind.Unspecified)));
    }

    // BR-005
    [Fact]
    public void Remover_PreencheDataRemocaoEDataAtualizacao()
    {
        var produto = Criar();
        var depois = Relogio.Depois(TimeSpan.FromDays(1));

        produto.Remover(depois);

        produto.DataRemocao.ShouldBe(depois);
        produto.DataAtualizacao.ShouldBe(depois);
        produto.EstaRemovido.ShouldBeTrue();
    }

    // BR-005: remover duas vezes é invariante violada (o caso de uso responde 404 antes)
    [Fact]
    public void Remover_ProdutoJaRemovido_LancaDomainException()
    {
        var produto = new ProdutoBuilder().Removido().Build();

        Should.Throw<DomainException>(() => produto.Remover(Relogio.Depois(TimeSpan.FromDays(1))))
            .Message.ShouldBe("O produto já foi removido.");
    }

    [Fact]
    public void Remover_ComDataQueNaoEhUtc_LancaDomainException()
    {
        var produto = Criar();
        Should.Throw<DomainException>(() => produto.Remover(DateTime.SpecifyKind(Relogio.AgoraUtc, DateTimeKind.Local)));
        produto.EstaRemovido.ShouldBeFalse();
    }
}
