using Catalogo.Domain.Categorias;
using Catalogo.Domain.Excecoes;
using Catalogo.UnitTests.Fixtures;

namespace Catalogo.UnitTests.Domain;

public sealed class CategoriaTests
{
    [Fact]
    public void Criar_ComDadosValidos_NasceAtivaComDatasUtcIguais()
    {
        var categoria = Categoria.Criar("Periféricos", "Teclados e mouses", Relogio.AgoraUtc);

        categoria.Id.ShouldNotBe(Guid.Empty);
        categoria.Id.Version.ShouldBe(7);
        categoria.Nome.ShouldBe("Periféricos");
        categoria.Descricao.ShouldBe("Teclados e mouses");
        categoria.Ativa.ShouldBeTrue();
        categoria.DataCriacao.ShouldBe(Relogio.AgoraUtc);
        categoria.DataAtualizacao.ShouldBe(categoria.DataCriacao);
        categoria.DataCriacao.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void Criar_SemDescricao_Aceita()
    {
        Categoria.Criar("Periféricos", null, Relogio.AgoraUtc).Descricao.ShouldBeNull();
    }

    [Theory]
    [InlineData(3)]
    [InlineData(100)]
    public void Criar_ComNomeNoLimite_Aceita(int tamanho)
    {
        Categoria.Criar(Requisicoes.Texto(tamanho), null, Relogio.AgoraUtc).Nome.Length.ShouldBe(tamanho);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(101)]
    public void Criar_ComNomeForaDoLimite_LancaDomainException(int tamanho)
    {
        Should.Throw<DomainException>(() => Categoria.Criar(Requisicoes.Texto(tamanho), null, Relogio.AgoraUtc))
            .Message.ShouldBe("O nome da categoria deve ter entre 3 e 100 caracteres.");
    }

    [Fact]
    public void Criar_ComNomeNulo_LancaDomainException()
    {
        Should.Throw<DomainException>(() => Categoria.Criar(null!, null, Relogio.AgoraUtc))
            .Message.ShouldBe("O nome da categoria é obrigatório.");
    }

    [Theory]
    [InlineData(" Periféricos")]
    [InlineData("Periféricos ")]
    [InlineData("Periféricos\t")]
    public void Criar_ComEspacoNasPontas_LancaDomainException(string nome)
    {
        Should.Throw<DomainException>(() => Categoria.Criar(nome, null, Relogio.AgoraUtc))
            .Message.ShouldBe("O nome da categoria não pode começar nem terminar com espaços.");
    }

    [Fact]
    public void Criar_ComDescricaoAcimaDe500_LancaDomainException()
    {
        Should.Throw<DomainException>(() => Categoria.Criar("Periféricos", Requisicoes.Texto(501), Relogio.AgoraUtc));
        Categoria.Criar("Periféricos", Requisicoes.Texto(500), Relogio.AgoraUtc).Descricao!.Length.ShouldBe(500);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Criar_ComDataQueNaoEhUtc_LancaDomainException(DateTimeKind kind)
    {
        var data = DateTime.SpecifyKind(Relogio.AgoraUtc, kind);

        Should.Throw<DomainException>(() => Categoria.Criar("Periféricos", null, data))
            .Message.ShouldBe("A data informada deve estar em UTC.");
    }

    [Fact]
    public void Atualizar_SubstituiCamposEAtualizaSoADataDeAtualizacao()
    {
        var categoria = new CategoriaBuilder().Build();
        var id = categoria.Id;
        var depois = Relogio.Depois(TimeSpan.FromHours(1));

        categoria.Atualizar("Periféricos Gamer", null, ativa: false, depois);

        categoria.Id.ShouldBe(id);
        categoria.Nome.ShouldBe("Periféricos Gamer");
        categoria.Descricao.ShouldBeNull();
        categoria.Ativa.ShouldBeFalse();
        categoria.DataCriacao.ShouldBe(Relogio.AgoraUtc);
        categoria.DataAtualizacao.ShouldBe(depois);
    }

    [Fact]
    public void Atualizar_ComDadosInvalidos_NaoAlteraNada()
    {
        var categoria = new CategoriaBuilder().Build();

        Should.Throw<DomainException>(() => categoria.Atualizar("ab", null, false, Relogio.Depois(TimeSpan.FromHours(1))));

        categoria.Nome.ShouldBe("Periféricos");
        categoria.Ativa.ShouldBeTrue();
        categoria.DataAtualizacao.ShouldBe(Relogio.AgoraUtc);
    }
}
