using Catalogo.Application.Comum;
using Catalogo.Application.Produtos;
using Catalogo.Application.Produtos.Dtos;
using Catalogo.Application.Produtos.UseCases;
using Catalogo.Application.Produtos.Validators;
using Catalogo.Domain.Abstracoes;
using Catalogo.Domain.Categorias;
using Catalogo.Domain.Excecoes;
using Catalogo.Domain.Produtos;
using Catalogo.UnitTests.Fixtures;
using Microsoft.Extensions.Time.Testing;
using NSubstitute.ExceptionExtensions;

namespace Catalogo.UnitTests.Application.UseCases;

/// <summary>Ordem das checagens: 400 → 422 (BR-004) → 409 SKU (BR-003) → 409 nome (BR-001).</summary>
public sealed class CriarProdutoUseCaseTests
{
    private readonly ICategoriaRepository _categorias = Substitute.For<ICategoriaRepository>();
    private readonly IProdutoRepository _produtos = Substitute.For<IProdutoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CriarProdutoUseCase _useCase;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;
    private readonly Categoria _categoria = new CategoriaBuilder().Build();
    private readonly CriarProdutoRequest _request;

    public CriarProdutoUseCaseTests()
    {
        _useCase = new CriarProdutoUseCase(new CriarProdutoRequestValidator(), _categorias, _produtos, _unitOfWork, Relogio.CriarFake());
        _categorias.ObterPorIdAsync(_categoria.Id, _ct).Returns(_categoria);
        _request = Requisicoes.CriarProduto(_categoria.Id);
    }

    [Fact]
    public async Task ComDadosValidos_CriaAtivoComONomeDaCategoria()
    {
        var resultado = await _useCase.ExecutarAsync(_request, _ct);

        resultado.Sucesso.ShouldBeTrue();
        var produto = resultado.Valor!;
        produto.CategoriaId.ShouldBe(_categoria.Id);
        produto.CategoriaNome.ShouldBe("Periféricos");
        produto.Nome.ShouldBe("Teclado Mecânico");
        produto.Sku.ShouldBe("TEC-001");
        produto.Preco.ShouldBe(349.90m);
        produto.Ativo.ShouldBeTrue();
        produto.DataCriacao.ShouldBe(Relogio.AgoraUtc);
        _produtos.Received(1).Adicionar(Arg.Is<Produto>(p => p.Id == produto.Id && p.Sku == Sku.Criar("TEC-001")));
        await _unitOfWork.Received(1).SalvarAlteracoesAsync(_ct);
    }

    [Fact]
    public async Task VerificaOSkuComOValueObjectEONomeNaCategoriaSemIgnorarId()
    {
        await _useCase.ExecutarAsync(_request, _ct);

        await _produtos.Received(1).ExisteSkuAsync(Sku.Criar("TEC-001"), _ct);
        await _produtos.Received(1).ExisteNomeNaCategoriaAsync(_categoria.Id, "Teclado Mecânico", null, _ct);
    }

    [Fact]
    public async Task ComRequestInvalido_Retorna400SemConsultarOBanco()
    {
        var resultado = await _useCase.ExecutarAsync(_request with { Preco = 0m, Nome = "ab" }, _ct);

        resultado.Erro!.Tipo.ShouldBe(TipoErro.Validacao);
        resultado.Erro.Campos!.Keys.ShouldBe(new[] { "nome", "preco" }, ignoreOrder: true);
        await _categorias.DidNotReceiveWithAnyArgs().ObterPorIdAsync(default, _ct);
        await _produtos.DidNotReceiveWithAnyArgs().ExisteSkuAsync(default, _ct);
    }

    /// <summary>Regressão de [[SKU aceita quebra de linha no final]]: 400 antes de qualquer acesso ao banco.</summary>
    [Theory]
    [InlineData("TEC-001\n")]
    [InlineData("AB\n")]
    public async Task ComSkuTerminandoEmQuebraDeLinha_Retorna400(string sku)
    {
        var resultado = await _useCase.ExecutarAsync(_request with { Sku = sku }, _ct);

        resultado.Erro!.Tipo.ShouldBe(TipoErro.Validacao);
        resultado.Erro.Campos!["sku"].ShouldBe(new[] { "O SKU deve conter apenas letras, números e hífen." });
        await _categorias.DidNotReceiveWithAnyArgs().ObterPorIdAsync(default, _ct);
        _produtos.DidNotReceiveWithAnyArgs().Adicionar(default!);
    }

    // BR-004
    [Fact]
    public async Task ComCategoriaInexistente_Retorna422()
    {
        var resultado = await _useCase.ExecutarAsync(_request with { CategoriaId = Guid.CreateVersion7() }, _ct);

        resultado.Erro.ShouldBe(ProdutoErros.CategoriaInvalida);
        await _produtos.DidNotReceiveWithAnyArgs().ExisteSkuAsync(default, _ct);
    }

    // BR-004
    [Fact]
    public async Task ComCategoriaInativa_Retorna422()
    {
        var inativa = new CategoriaBuilder().ComNome("Descontinuados").Inativa().Build();
        _categorias.ObterPorIdAsync(inativa.Id, _ct).Returns(inativa);

        var resultado = await _useCase.ExecutarAsync(_request with { CategoriaId = inativa.Id }, _ct);

        resultado.Erro.ShouldBe(ProdutoErros.CategoriaInvalida);
        resultado.Erro!.Tipo.ShouldBe(TipoErro.RegraNegocio);
    }

    [Fact]
    public async Task CategoriaInvalidaTemPrioridadeSobreSkuDuplicado()
    {
        _produtos.ExisteSkuAsync(Arg.Any<Sku>(), _ct).Returns(true);

        var resultado = await _useCase.ExecutarAsync(_request with { CategoriaId = Guid.CreateVersion7() }, _ct);

        resultado.Erro.ShouldBe(ProdutoErros.CategoriaInvalida);
    }

    // BR-003: único globalmente
    [Fact]
    public async Task ComSkuJaUsado_Retorna409SemVerificarONome()
    {
        _produtos.ExisteSkuAsync(Sku.Criar("TEC-001"), _ct).Returns(true);
        _produtos.ExisteNomeNaCategoriaAsync(default, default!, default, _ct).ReturnsForAnyArgs(true);

        var resultado = await _useCase.ExecutarAsync(_request, _ct);

        resultado.Erro.ShouldBe(ProdutoErros.SkuDuplicado);
        await _produtos.DidNotReceiveWithAnyArgs().ExisteNomeNaCategoriaAsync(default, default!, default, _ct);
        _produtos.DidNotReceiveWithAnyArgs().Adicionar(default!);
    }

    // BR-001: único dentro da categoria
    [Fact]
    public async Task ComNomeJaUsadoNaCategoria_Retorna409SemAdicionar()
    {
        _produtos.ExisteNomeNaCategoriaAsync(_categoria.Id, "Teclado Mecânico", null, _ct).Returns(true);

        var resultado = await _useCase.ExecutarAsync(_request, _ct);

        resultado.Erro.ShouldBe(ProdutoErros.NomeDuplicado);
        _produtos.DidNotReceiveWithAnyArgs().Adicionar(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SalvarAlteracoesAsync(_ct);
    }

    [Theory]
    [InlineData(TipoConflitoPersistencia.Unicidade, "UX_Produtos_Sku", CodigosErro.ProdutoSkuDuplicado)]
    [InlineData(TipoConflitoPersistencia.Unicidade, "UX_Produtos_CategoriaId_Nome", CodigosErro.ProdutoNomeDuplicado)]
    [InlineData(TipoConflitoPersistencia.Referencia, "FK_Produtos_Categorias_CategoriaId", CodigosErro.ProdutoCategoriaInvalida)]
    public async Task ComCorridaDetectadaPeloBanco_MapeiaParaOErroDeNegocio(
        TipoConflitoPersistencia tipo, string restricao, string codigoEsperado)
    {
        _unitOfWork.SalvarAlteracoesAsync(_ct).ThrowsAsync(new ConflitoPersistenciaException(tipo, restricao));

        var resultado = await _useCase.ExecutarAsync(_request, _ct);

        resultado.Erro!.Codigo.ShouldBe(codigoEsperado);
    }
}

public sealed class AtualizarProdutoUseCaseTests
{
    private readonly ICategoriaRepository _categorias = Substitute.For<ICategoriaRepository>();
    private readonly IProdutoRepository _produtos = Substitute.For<IProdutoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _relogio = Relogio.CriarFake();
    private readonly AtualizarProdutoUseCase _useCase;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;
    private readonly Categoria _categoriaAtual = new CategoriaBuilder().Build();
    private readonly Categoria _outraCategoria = new CategoriaBuilder().ComNome("Acessórios").Build();
    private readonly Produto _produto;

    public AtualizarProdutoUseCaseTests()
    {
        _useCase = new AtualizarProdutoUseCase(
            new AtualizarProdutoRequestValidator(), _categorias, _produtos, _unitOfWork, _relogio);
        _produto = new ProdutoBuilder().NaCategoria(_categoriaAtual.Id).Build();
        _produtos.ObterPorIdAsync(_produto.Id, _ct).Returns(_produto);
        _categorias.ObterPorIdAsync(_categoriaAtual.Id, _ct).Returns(_categoriaAtual);
        _categorias.ObterPorIdAsync(_outraCategoria.Id, _ct).Returns(_outraCategoria);
        _relogio.Advance(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public async Task NaMesmaCategoria_AtualizaCamposEMantemOSku()
    {
        var resultado = await _useCase.ExecutarAsync(_produto.Id, Requisicoes.AtualizarProduto(_categoriaAtual.Id), _ct);

        resultado.Sucesso.ShouldBeTrue();
        var response = resultado.Valor!;
        response.Nome.ShouldBe("Teclado Mecânico RGB");
        response.Descricao.ShouldBe("ABNT2 com iluminação");
        response.Preco.ShouldBe(399.90m);
        response.Sku.ShouldBe("TEC-001");
        response.CategoriaNome.ShouldBe("Periféricos");
        response.DataCriacao.ShouldBe(Relogio.AgoraUtc);
        response.DataAtualizacao.ShouldBe(Relogio.Depois(TimeSpan.FromMinutes(30)));
        await _unitOfWork.Received(1).SalvarAlteracoesAsync(_ct);
    }

    // BR-004: mover para categoria ativa
    [Fact]
    public async Task MovendoParaOutraCategoriaAtiva_UsaONomeDaNovaCategoria()
    {
        var resultado = await _useCase.ExecutarAsync(_produto.Id, Requisicoes.AtualizarProduto(_outraCategoria.Id), _ct);

        resultado.Valor!.CategoriaId.ShouldBe(_outraCategoria.Id);
        resultado.Valor.CategoriaNome.ShouldBe("Acessórios");
        await _produtos.Received(1).ExisteNomeNaCategoriaAsync(_outraCategoria.Id, "Teclado Mecânico RGB", _produto.Id, _ct);
    }

    [Fact]
    public async Task ComRequestInvalido_Retorna400SemConsultarOBanco()
    {
        var resultado = await _useCase.ExecutarAsync(
            _produto.Id, Requisicoes.AtualizarProduto(_categoriaAtual.Id) with { Preco = null }, _ct);

        resultado.Erro!.Tipo.ShouldBe(TipoErro.Validacao);
        resultado.Erro.Campos!.Keys.ShouldContain("preco");
        await _produtos.DidNotReceiveWithAnyArgs().ObterPorIdAsync(default, _ct);
    }

    // BR-005: removido não é encontrado pelo repositório (query filter) e responde 404
    [Fact]
    public async Task ProdutoInexistenteOuRemovido_Retorna404()
    {
        var resultado = await _useCase.ExecutarAsync(Guid.CreateVersion7(), Requisicoes.AtualizarProduto(_categoriaAtual.Id), _ct);

        resultado.Erro.ShouldBe(ProdutoErros.NaoEncontrado);
        await _categorias.DidNotReceiveWithAnyArgs().ObterPorIdAsync(default, _ct);
    }

    // BR-004
    [Fact]
    public async Task MovendoParaCategoriaInexistente_Retorna422()
    {
        var resultado = await _useCase.ExecutarAsync(_produto.Id, Requisicoes.AtualizarProduto(Guid.CreateVersion7()), _ct);

        resultado.Erro.ShouldBe(ProdutoErros.CategoriaInvalida);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SalvarAlteracoesAsync(_ct);
    }

    // BR-004
    [Fact]
    public async Task MovendoParaCategoriaInativa_Retorna422()
    {
        var inativa = new CategoriaBuilder().ComNome("Descontinuados").Inativa().Build();
        _categorias.ObterPorIdAsync(inativa.Id, _ct).Returns(inativa);

        var resultado = await _useCase.ExecutarAsync(_produto.Id, Requisicoes.AtualizarProduto(inativa.Id), _ct);

        resultado.Erro.ShouldBe(ProdutoErros.CategoriaInvalida);
        _produto.CategoriaId.ShouldBe(_categoriaAtual.Id);
    }

    // ADR-005: manter a categoria atual, mesmo inativa, é aceito
    [Fact]
    public async Task MantendoACategoriaAtualInativa_Aceita()
    {
        _categoriaAtual.Atualizar(_categoriaAtual.Nome, _categoriaAtual.Descricao, ativa: false, Relogio.AgoraUtc);

        var resultado = await _useCase.ExecutarAsync(_produto.Id, Requisicoes.AtualizarProduto(_categoriaAtual.Id), _ct);

        resultado.Sucesso.ShouldBeTrue();
        resultado.Valor!.CategoriaId.ShouldBe(_categoriaAtual.Id);
    }

    // BR-001
    [Fact]
    public async Task ComNomeDeOutroProdutoNaCategoria_Retorna409()
    {
        _produtos.ExisteNomeNaCategoriaAsync(_categoriaAtual.Id, "Teclado Mecânico RGB", _produto.Id, _ct).Returns(true);

        var resultado = await _useCase.ExecutarAsync(_produto.Id, Requisicoes.AtualizarProduto(_categoriaAtual.Id), _ct);

        resultado.Erro.ShouldBe(ProdutoErros.NomeDuplicado);
        _produto.Nome.ShouldBe("Teclado Mecânico");
        await _unitOfWork.DidNotReceiveWithAnyArgs().SalvarAlteracoesAsync(_ct);
    }

    [Theory]
    [InlineData(TipoConflitoPersistencia.Unicidade, "UX_Produtos_CategoriaId_Nome", CodigosErro.ProdutoNomeDuplicado)]
    [InlineData(TipoConflitoPersistencia.Referencia, "FK_Produtos_Categorias_CategoriaId", CodigosErro.ProdutoCategoriaInvalida)]
    public async Task ComCorridaDetectadaPeloBanco_MapeiaParaOErroDeNegocio(
        TipoConflitoPersistencia tipo, string restricao, string codigoEsperado)
    {
        _unitOfWork.SalvarAlteracoesAsync(_ct).ThrowsAsync(new ConflitoPersistenciaException(tipo, restricao));

        var resultado = await _useCase.ExecutarAsync(_produto.Id, Requisicoes.AtualizarProduto(_categoriaAtual.Id), _ct);

        resultado.Erro!.Codigo.ShouldBe(codigoEsperado);
    }
}

/// <summary>REQ-004 e ADR-006.</summary>
public sealed class AlterarStatusProdutoUseCaseTests
{
    private readonly IProdutoRepository _produtos = Substitute.For<IProdutoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _relogio = Relogio.CriarFake();
    private readonly AlterarStatusProdutoUseCase _useCase;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;
    private readonly Produto _produto = new ProdutoBuilder().Build();

    public AlterarStatusProdutoUseCaseTests()
    {
        _useCase = new AlterarStatusProdutoUseCase(new AlterarStatusProdutoRequestValidator(), _produtos, _unitOfWork, _relogio);
        _produtos.ObterPorIdAsync(_produto.Id, _ct).Returns(_produto);
        _relogio.Advance(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public async Task Inativar_AlteraStatusEPersiste()
    {
        var resultado = await _useCase.ExecutarAsync(_produto.Id, new AlterarStatusProdutoRequest(false), _ct);

        resultado.Sucesso.ShouldBeTrue();
        _produto.Ativo.ShouldBeFalse();
        _produto.DataAtualizacao.ShouldBe(Relogio.Depois(TimeSpan.FromMinutes(10)));
        await _unitOfWork.Received(1).SalvarAlteracoesAsync(_ct);
    }

    [Fact]
    public async Task Reativar_ProdutoInativo_AlteraStatus()
    {
        var inativo = new ProdutoBuilder().Inativo().Build();
        _produtos.ObterPorIdAsync(inativo.Id, _ct).Returns(inativo);

        var resultado = await _useCase.ExecutarAsync(inativo.Id, new AlterarStatusProdutoRequest(true), _ct);

        resultado.Sucesso.ShouldBeTrue();
        inativo.Ativo.ShouldBeTrue();
    }

    [Fact]
    public async Task ComOMesmoStatus_RespondeSucessoSemAlterarADataDeAtualizacao()
    {
        var resultado = await _useCase.ExecutarAsync(_produto.Id, new AlterarStatusProdutoRequest(true), _ct);

        resultado.Sucesso.ShouldBeTrue();
        _produto.DataAtualizacao.ShouldBe(Relogio.AgoraUtc);
    }

    [Fact]
    public async Task SemAtivo_Retorna400SemConsultarOBanco()
    {
        var resultado = await _useCase.ExecutarAsync(_produto.Id, new AlterarStatusProdutoRequest(null), _ct);

        resultado.Erro!.Campos!.Keys.ShouldContain("ativo");
        await _produtos.DidNotReceiveWithAnyArgs().ObterPorIdAsync(default, _ct);
    }

    [Fact]
    public async Task ProdutoInexistenteOuRemovido_Retorna404()
    {
        var resultado = await _useCase.ExecutarAsync(Guid.CreateVersion7(), new AlterarStatusProdutoRequest(false), _ct);

        resultado.Erro.ShouldBe(ProdutoErros.NaoEncontrado);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SalvarAlteracoesAsync(_ct);
    }
}

/// <summary>BR-005: exclusão lógica.</summary>
public sealed class RemoverProdutoUseCaseTests
{
    private readonly IProdutoRepository _produtos = Substitute.For<IProdutoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _relogio = Relogio.CriarFake();
    private readonly RemoverProdutoUseCase _useCase;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public RemoverProdutoUseCaseTests()
    {
        _useCase = new RemoverProdutoUseCase(_produtos, _unitOfWork, _relogio);
    }

    [Fact]
    public async Task ProdutoExistente_MarcaDataRemocaoEPersiste()
    {
        var produto = new ProdutoBuilder().Build();
        _produtos.ObterPorIdAsync(produto.Id, _ct).Returns(produto);
        _relogio.Advance(TimeSpan.FromDays(1));

        var resultado = await _useCase.ExecutarAsync(produto.Id, _ct);

        resultado.Sucesso.ShouldBeTrue();
        produto.DataRemocao.ShouldBe(Relogio.Depois(TimeSpan.FromDays(1)));
        produto.EstaRemovido.ShouldBeTrue();
        await _unitOfWork.Received(1).SalvarAlteracoesAsync(_ct);
    }

    [Fact]
    public async Task ProdutoInativo_TambemPodeSerRemovido()
    {
        var produto = new ProdutoBuilder().Inativo().Build();
        _produtos.ObterPorIdAsync(produto.Id, _ct).Returns(produto);

        (await _useCase.ExecutarAsync(produto.Id, _ct)).Sucesso.ShouldBeTrue();
        produto.EstaRemovido.ShouldBeTrue();
    }

    // BR-005: remover de novo dá 404 (o repositório não devolve removidos)
    [Fact]
    public async Task ProdutoJaRemovido_Retorna404SemSalvar()
    {
        var resultado = await _useCase.ExecutarAsync(Guid.CreateVersion7(), _ct);

        resultado.Erro.ShouldBe(ProdutoErros.NaoEncontrado);
        resultado.Erro!.Tipo.ShouldBe(TipoErro.NaoEncontrado);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SalvarAlteracoesAsync(_ct);
    }
}

public sealed class ConsultasDeProdutoUseCasesTests
{
    private readonly IProdutoConsultas _consultas = Substitute.For<IProdutoConsultas>();
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Obter_Existente_RetornaOResponse()
    {
        var response = ProdutoResponse.De(new ProdutoBuilder().Inativo().Build(), "Periféricos");
        _consultas.ObterPorIdAsync(response.Id, _ct).Returns(response);

        var resultado = await new ObterProdutoUseCase(_consultas).ExecutarAsync(response.Id, _ct);

        resultado.Valor.ShouldBe(response);
    }

    [Fact]
    public async Task Obter_InexistenteOuRemovido_Retorna404()
    {
        var resultado = await new ObterProdutoUseCase(_consultas).ExecutarAsync(Guid.CreateVersion7(), _ct);

        resultado.Erro.ShouldBe(ProdutoErros.NaoEncontrado);
    }

    [Fact]
    public async Task Listar_ComQueryValida_RepassaFiltrosEOrdenacao()
    {
        var query = new ListarProdutosQuery(1, 20, Guid.CreateVersion7(), "teclado", Ativo: false, OrdenarPor: "preco", Direcao: "desc");
        var pagina = PaginaResponse<ProdutoResponse>.Criar([], 1, 20, 0);
        _consultas.ListarAsync(query, _ct).Returns(pagina);

        var resultado = await new ListarProdutosUseCase(new ListarProdutosQueryValidator(), _consultas).ExecutarAsync(query, _ct);

        resultado.Valor.ShouldBeSameAs(pagina);
    }

    [Fact]
    public async Task Listar_ComOrdenacaoInvalida_Retorna400SemConsultar()
    {
        var resultado = await new ListarProdutosUseCase(new ListarProdutosQueryValidator(), _consultas)
            .ExecutarAsync(new ListarProdutosQuery(OrdenarPor: "data", Direcao: "cima"), _ct);

        resultado.Erro!.Campos!.Keys.ShouldBe(new[] { "ordenarPor", "direcao" }, ignoreOrder: true);
        await _consultas.DidNotReceiveWithAnyArgs().ListarAsync(default!, _ct);
    }
}
