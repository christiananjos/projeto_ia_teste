using Catalogo.Application.Categorias;
using Catalogo.Application.Categorias.Dtos;
using Catalogo.Application.Categorias.UseCases;
using Catalogo.Application.Categorias.Validators;
using Catalogo.Application.Comum;
using Catalogo.Domain.Abstracoes;
using Catalogo.Domain.Categorias;
using Catalogo.Domain.Excecoes;
using Catalogo.UnitTests.Fixtures;
using NSubstitute.ExceptionExtensions;

namespace Catalogo.UnitTests.Application.UseCases;

public sealed class CriarCategoriaUseCaseTests
{
    private readonly ICategoriaRepository _categorias = Substitute.For<ICategoriaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CriarCategoriaUseCase _useCase;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public CriarCategoriaUseCaseTests()
    {
        _useCase = new CriarCategoriaUseCase(new CriarCategoriaRequestValidator(), _categorias, _unitOfWork, Relogio.CriarFake());
    }

    [Fact]
    public async Task ComNomeLivre_CriaAtivaPersisteERetornaOResponse()
    {
        var resultado = await _useCase.ExecutarAsync(Requisicoes.CriarCategoria(), _ct);

        resultado.Sucesso.ShouldBeTrue();
        var categoria = resultado.Valor!;
        categoria.Nome.ShouldBe("Periféricos");
        categoria.Descricao.ShouldBe("Teclados, mouses e headsets");
        categoria.Ativa.ShouldBeTrue();
        categoria.DataCriacao.ShouldBe(Relogio.AgoraUtc);
        categoria.DataAtualizacao.ShouldBe(Relogio.AgoraUtc);
        _categorias.Received(1).Adicionar(Arg.Is<Categoria>(c => c.Id == categoria.Id && c.Nome == "Periféricos"));
        await _unitOfWork.Received(1).SalvarAlteracoesAsync(_ct);
    }

    [Fact]
    public async Task ComRequestInvalido_Retorna400SemConsultarOBanco()
    {
        var resultado = await _useCase.ExecutarAsync(Requisicoes.CriarCategoria() with { Nome = " Periféricos" }, _ct);

        resultado.Erro!.Tipo.ShouldBe(TipoErro.Validacao);
        resultado.Erro.Campos!.Keys.ShouldContain("nome");
        await _categorias.DidNotReceiveWithAnyArgs().ExisteNomeAsync(default!, default, _ct);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SalvarAlteracoesAsync(_ct);
    }

    // REQ-001: nome único sem diferenciar maiúsculas (a comparação CI é da collation; aqui o repositório responde)
    [Fact]
    public async Task ComNomeJaUsado_Retorna409SemAdicionar()
    {
        _categorias.ExisteNomeAsync("Periféricos", null, _ct).Returns(true);

        var resultado = await _useCase.ExecutarAsync(Requisicoes.CriarCategoria(), _ct);

        resultado.Erro.ShouldBe(CategoriaErros.NomeDuplicado);
        _categorias.DidNotReceiveWithAnyArgs().Adicionar(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SalvarAlteracoesAsync(_ct);
    }

    [Fact]
    public async Task ComCorridaDeUnicidadeNoBanco_Retorna409()
    {
        _unitOfWork.SalvarAlteracoesAsync(_ct)
            .ThrowsAsync(new ConflitoPersistenciaException(TipoConflitoPersistencia.Unicidade, "UX_Categorias_Nome"));

        var resultado = await _useCase.ExecutarAsync(Requisicoes.CriarCategoria(), _ct);

        resultado.Erro.ShouldBe(CategoriaErros.NomeDuplicado);
    }

    [Fact]
    public async Task ComConflitoDeReferencia_NaoEngoleAExcecao()
    {
        _unitOfWork.SalvarAlteracoesAsync(_ct)
            .ThrowsAsync(new ConflitoPersistenciaException(TipoConflitoPersistencia.Referencia, null));

        await Should.ThrowAsync<ConflitoPersistenciaException>(() => _useCase.ExecutarAsync(Requisicoes.CriarCategoria(), _ct));
    }
}

public sealed class AtualizarCategoriaUseCaseTests
{
    private readonly ICategoriaRepository _categorias = Substitute.For<ICategoriaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Microsoft.Extensions.Time.Testing.FakeTimeProvider _relogio = Relogio.CriarFake();
    private readonly AtualizarCategoriaUseCase _useCase;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;
    private readonly Categoria _existente = new CategoriaBuilder().Build();

    public AtualizarCategoriaUseCaseTests()
    {
        _useCase = new AtualizarCategoriaUseCase(new AtualizarCategoriaRequestValidator(), _categorias, _unitOfWork, _relogio);
        _categorias.ObterPorIdAsync(_existente.Id, _ct).Returns(_existente);
        _relogio.Advance(TimeSpan.FromHours(2));
    }

    [Fact]
    public async Task ComDadosValidos_SubstituiTudoEAtualizaADataDeAtualizacao()
    {
        var resultado = await _useCase.ExecutarAsync(
            _existente.Id, new AtualizarCategoriaRequest("Periféricos Gamer", null, false), _ct);

        resultado.Sucesso.ShouldBeTrue();
        resultado.Valor!.Nome.ShouldBe("Periféricos Gamer");
        resultado.Valor.Descricao.ShouldBeNull();
        resultado.Valor.Ativa.ShouldBeFalse();
        resultado.Valor.DataCriacao.ShouldBe(Relogio.AgoraUtc);
        resultado.Valor.DataAtualizacao.ShouldBe(Relogio.Depois(TimeSpan.FromHours(2)));
        await _unitOfWork.Received(1).SalvarAlteracoesAsync(_ct);
    }

    [Fact]
    public async Task VerificaNomeDuplicadoIgnorandoAPropriaCategoria()
    {
        await _useCase.ExecutarAsync(_existente.Id, Requisicoes.AtualizarCategoria(), _ct);

        await _categorias.Received(1).ExisteNomeAsync("Periféricos Gamer", _existente.Id, _ct);
    }

    // ADR-005
    [Fact]
    public async Task SemAtiva_Retorna400SemConsultarOBanco()
    {
        var resultado = await _useCase.ExecutarAsync(_existente.Id, Requisicoes.AtualizarCategoria() with { Ativa = null }, _ct);

        resultado.Erro!.Tipo.ShouldBe(TipoErro.Validacao);
        resultado.Erro.Campos!.Keys.ShouldContain("ativa");
        await _categorias.DidNotReceiveWithAnyArgs().ObterPorIdAsync(default, _ct);
    }

    [Fact]
    public async Task CategoriaInexistente_Retorna404()
    {
        var resultado = await _useCase.ExecutarAsync(Guid.CreateVersion7(), Requisicoes.AtualizarCategoria(), _ct);

        resultado.Erro.ShouldBe(CategoriaErros.NaoEncontrada);
        await _categorias.DidNotReceiveWithAnyArgs().ExisteNomeAsync(default!, default, _ct);
    }

    [Fact]
    public async Task NomeDeOutraCategoria_Retorna409SemSalvar()
    {
        _categorias.ExisteNomeAsync("Periféricos Gamer", _existente.Id, _ct).Returns(true);

        var resultado = await _useCase.ExecutarAsync(_existente.Id, Requisicoes.AtualizarCategoria(), _ct);

        resultado.Erro.ShouldBe(CategoriaErros.NomeDuplicado);
        _existente.Nome.ShouldBe("Periféricos");
        await _unitOfWork.DidNotReceiveWithAnyArgs().SalvarAlteracoesAsync(_ct);
    }

    [Fact]
    public async Task ComCorridaDeUnicidadeNoBanco_Retorna409()
    {
        _unitOfWork.SalvarAlteracoesAsync(_ct)
            .ThrowsAsync(new ConflitoPersistenciaException(TipoConflitoPersistencia.Unicidade, "UX_Categorias_Nome"));

        var resultado = await _useCase.ExecutarAsync(_existente.Id, Requisicoes.AtualizarCategoria(), _ct);

        resultado.Erro.ShouldBe(CategoriaErros.NomeDuplicado);
    }
}

/// <summary>BR-006 e ADR-001.</summary>
public sealed class RemoverCategoriaUseCaseTests
{
    private readonly ICategoriaRepository _categorias = Substitute.For<ICategoriaRepository>();
    private readonly RemoverCategoriaUseCase _useCase;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;
    private readonly Categoria _existente = new CategoriaBuilder().Build();

    public RemoverCategoriaUseCaseTests()
    {
        _useCase = new RemoverCategoriaUseCase(_categorias);
        _categorias.ObterPorIdAsync(_existente.Id, _ct).Returns(_existente);
    }

    [Fact]
    public async Task SemProdutosNaoRemovidos_ExcluiComOsProdutosRemovidos()
    {
        var resultado = await _useCase.ExecutarAsync(_existente.Id, _ct);

        resultado.Sucesso.ShouldBeTrue();
        await _categorias.Received(1).ExcluirComProdutosRemovidosAsync(_existente.Id, _ct);
    }

    [Fact]
    public async Task CategoriaInexistente_Retorna404()
    {
        var resultado = await _useCase.ExecutarAsync(Guid.CreateVersion7(), _ct);

        resultado.Erro.ShouldBe(CategoriaErros.NaoEncontrada);
        await _categorias.DidNotReceiveWithAnyArgs().ExcluirComProdutosRemovidosAsync(default, _ct);
    }

    // BR-006: produto ativo ou inativo, desde que não removido, bloqueia
    [Fact]
    public async Task ComProdutoNaoRemovido_Retorna409SemExcluir()
    {
        _categorias.PossuiProdutosNaoRemovidosAsync(_existente.Id, _ct).Returns(true);

        var resultado = await _useCase.ExecutarAsync(_existente.Id, _ct);

        resultado.Erro.ShouldBe(CategoriaErros.PossuiProdutos);
        resultado.Erro!.Codigo.ShouldBe("categoria.possui_produtos");
        await _categorias.DidNotReceiveWithAnyArgs().ExcluirComProdutosRemovidosAsync(default, _ct);
    }

    // ADR-001: produto criado entre a checagem e o DELETE (FK 547)
    [Fact]
    public async Task ComCorridaDeFkNaExclusao_Retorna409()
    {
        _categorias.ExcluirComProdutosRemovidosAsync(_existente.Id, _ct)
            .ThrowsAsync(new ConflitoPersistenciaException(TipoConflitoPersistencia.Referencia, "FK_Produtos_Categorias_CategoriaId"));

        var resultado = await _useCase.ExecutarAsync(_existente.Id, _ct);

        resultado.Erro.ShouldBe(CategoriaErros.PossuiProdutos);
    }

    [Fact]
    public async Task ComConflitoDeUnicidade_NaoEngoleAExcecao()
    {
        _categorias.ExcluirComProdutosRemovidosAsync(_existente.Id, _ct)
            .ThrowsAsync(new ConflitoPersistenciaException(TipoConflitoPersistencia.Unicidade, null));

        await Should.ThrowAsync<ConflitoPersistenciaException>(() => _useCase.ExecutarAsync(_existente.Id, _ct));
    }
}

public sealed class ConsultasDeCategoriaUseCasesTests
{
    private readonly ICategoriaConsultas _consultas = Substitute.For<ICategoriaConsultas>();
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Obter_Existente_Retorna200()
    {
        var response = CategoriaResponse.De(new CategoriaBuilder().Build());
        _consultas.ObterPorIdAsync(response.Id, _ct).Returns(response);

        var resultado = await new ObterCategoriaUseCase(_consultas).ExecutarAsync(response.Id, _ct);

        resultado.Valor.ShouldBe(response);
    }

    [Fact]
    public async Task Obter_Inexistente_Retorna404()
    {
        var resultado = await new ObterCategoriaUseCase(_consultas).ExecutarAsync(Guid.CreateVersion7(), _ct);

        resultado.Erro.ShouldBe(CategoriaErros.NaoEncontrada);
    }

    [Fact]
    public async Task Listar_ComQueryValida_RepassaParaAConsulta()
    {
        var query = new ListarCategoriasQuery(Page: 2, PageSize: 10, Nome: "peri", Ativa: true);
        var pagina = PaginaResponse<CategoriaResponse>.Criar([], 2, 10, 3);
        _consultas.ListarAsync(query, _ct).Returns(pagina);

        var resultado = await new ListarCategoriasUseCase(new ListarCategoriasQueryValidator(), _consultas).ExecutarAsync(query, _ct);

        resultado.Valor.ShouldBeSameAs(pagina);
    }

    [Fact]
    public async Task Listar_ComPageSizeAcimaDe100_Retorna400SemConsultar()
    {
        var resultado = await new ListarCategoriasUseCase(new ListarCategoriasQueryValidator(), _consultas)
            .ExecutarAsync(new ListarCategoriasQuery(PageSize: 101), _ct);

        resultado.Erro!.Campos!.Keys.ShouldContain("pageSize");
        await _consultas.DidNotReceiveWithAnyArgs().ListarAsync(default!, _ct);
    }
}
