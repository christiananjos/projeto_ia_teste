using Catalogo.Application.Categorias.Dtos;
using Catalogo.Application.Comum;
using Catalogo.Application.Produtos;
using Catalogo.Application.Produtos.Dtos;
using Catalogo.Domain.Excecoes;
using Catalogo.UnitTests.Fixtures;
using FluentValidation;
using FluentValidation.Results;

namespace Catalogo.UnitTests.Application.Comum;

public sealed class ResultadoTests
{
    [Fact]
    public void ConversaoImplicitaDeErro_GeraFalha()
    {
        Resultado resultado = CodigosErroFixos.Erro;
        Resultado<int> resultadoComValor = CodigosErroFixos.Erro;

        resultado.Sucesso.ShouldBeFalse();
        resultado.Erro.ShouldBe(CodigosErroFixos.Erro);
        resultadoComValor.Sucesso.ShouldBeFalse();
        resultadoComValor.Valor.ShouldBe(default);
        resultadoComValor.Erro.ShouldBe(CodigosErroFixos.Erro);
    }

    [Fact]
    public void Ok_NaoTemErro()
    {
        Resultado.Ok().Sucesso.ShouldBeTrue();
        Resultado.Ok().Erro.ShouldBeNull();
        Resultado<string>.Ok("valor").Valor.ShouldBe("valor");
    }

    [Fact]
    public void Falha_ComErroNulo_LancaArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => Resultado.Falha(null!));
        Should.Throw<ArgumentNullException>(() => Resultado<int>.Falha(null!));
    }

    private static class CodigosErroFixos
    {
        public static readonly Erro Erro = Erro.Conflito("teste.conflito", "Conflito de teste.");
    }
}

public sealed class PaginaResponseTests
{
    // REQ-003: totalPaginas = ceil(totalItens / pageSize), 0 sem itens
    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(250, 100, 3)]
    public void CalcularTotalPaginas(int totalItens, int pageSize, int esperado)
    {
        PaginaResponse<int>.CalcularTotalPaginas(totalItens, pageSize).ShouldBe(esperado);
    }

    [Theory]
    [InlineData(1, 20, 25, 0)]
    [InlineData(2, 20, 25, 20)]
    [InlineData(3, 20, 25, null)]
    [InlineData(1, 20, 0, null)]
    public void CalcularDeslocamento_RetornaNuloAlemDaUltimaPagina(int page, int pageSize, int totalItens, int? esperado)
    {
        PaginaResponse<int>.CalcularDeslocamento(page, pageSize, totalItens).ShouldBe(esperado);
    }

    [Fact]
    public void CalcularDeslocamento_ComPaginaMaxima_NaoEstoura()
    {
        PaginaResponse<int>.CalcularDeslocamento(int.MaxValue, 100, 10).ShouldBeNull();
    }

    [Fact]
    public void Criar_PreencheOsTotais()
    {
        var pagina = PaginaResponse<string>.Criar(["a", "b"], page: 1, pageSize: 2, totalItens: 5);

        pagina.Itens.Count.ShouldBe(2);
        pagina.Page.ShouldBe(1);
        pagina.PageSize.ShouldBe(2);
        pagina.TotalItens.ShouldBe(5);
        pagina.TotalPaginas.ShouldBe(3);
    }
}

public sealed class ValidacaoExtensionsTests
{
    [Fact]
    public void ParaErro_UsaChavesEmCamelCaseEAgrupaMensagensSemRepetir()
    {
        var resultado = new ValidationResult(
        [
            new ValidationFailure("CategoriaId", "A categoria é obrigatória."),
            new ValidationFailure("Preco", "O preço é obrigatório."),
            new ValidationFailure("Preco", "O preço é obrigatório."),
            new ValidationFailure("Preco", "Outra mensagem."),
            new ValidationFailure("Endereco.NomeRua", "Inválido.")
        ]);

        var erro = resultado.ParaErro();

        erro.Tipo.ShouldBe(TipoErro.Validacao);
        erro.Codigo.ShouldBe(CodigosErro.Validacao);
        erro.Mensagem.ShouldBe("Um ou mais campos são inválidos.");
        erro.Campos.ShouldNotBeNull();
        erro.Campos.Keys.ShouldBe(new[] { "categoriaId", "preco", "endereco.nomeRua" }, ignoreOrder: true);
        erro.Campos["preco"].ShouldBe(new[] { "O preço é obrigatório.", "Outra mensagem." });
    }

    [Fact]
    public void ParaErro_ComNomeDePropriedadeVazio_MantemAChaveVazia()
    {
        var erro = new ValidationResult([new ValidationFailure(string.Empty, "Objeto inválido.")]).ParaErro();

        erro.Campos!.ContainsKey(string.Empty).ShouldBeTrue();
    }

    [Fact]
    public async Task ValidarAsync_ComEntradaValida_RetornaNulo()
    {
        var validator = new InlineValidator<string>();
        validator.RuleFor(x => x).NotEmpty();

        (await validator.ValidarAsync("ok", TestContext.Current.CancellationToken)).ShouldBeNull();
        (await validator.ValidarAsync("", TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }
}

public sealed class ProdutoErrosTests
{
    // Corridas detectadas pelo banco (spec técnica 4.6)
    [Fact]
    public void DeConflito_ViolacaoDoIndiceDeSku_ViraSkuDuplicado()
    {
        ProdutoErros.DeConflito(new ConflitoPersistenciaException(TipoConflitoPersistencia.Unicidade, "UX_Produtos_Sku"))
            .ShouldBe(ProdutoErros.SkuDuplicado);
    }

    [Fact]
    public void DeConflito_ComparaONomeDoIndiceSemDiferenciarMaiusculas()
    {
        ProdutoErros.DeConflito(new ConflitoPersistenciaException(TipoConflitoPersistencia.Unicidade, "ux_produtos_sku"))
            .ShouldBe(ProdutoErros.SkuDuplicado);
    }

    [Theory]
    [InlineData("UX_Produtos_CategoriaId_Nome")]
    [InlineData(null)]
    public void DeConflito_OutraUnicidade_ViraNomeDuplicado(string? restricao)
    {
        ProdutoErros.DeConflito(new ConflitoPersistenciaException(TipoConflitoPersistencia.Unicidade, restricao))
            .ShouldBe(ProdutoErros.NomeDuplicado);
    }

    [Fact]
    public void DeConflito_ViolacaoDeFk_ViraCategoriaInvalida()
    {
        ProdutoErros.DeConflito(new ConflitoPersistenciaException(TipoConflitoPersistencia.Referencia, "FK_Produtos_Categorias_CategoriaId"))
            .ShouldBe(ProdutoErros.CategoriaInvalida);
    }

    [Fact]
    public void DeConflito_ComNulo_LancaArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ProdutoErros.DeConflito(null!));
    }

    [Fact]
    public void Erros_TemOsCodigosETiposDaSpec()
    {
        ProdutoErros.NaoEncontrado.ShouldBe(new Erro(TipoErro.NaoEncontrado, "produto.nao_encontrado", "Produto não encontrado."));
        ProdutoErros.SkuDuplicado.Tipo.ShouldBe(TipoErro.Conflito);
        ProdutoErros.SkuDuplicado.Codigo.ShouldBe("produto.sku_duplicado");
        ProdutoErros.NomeDuplicado.Codigo.ShouldBe("produto.nome_duplicado");
        ProdutoErros.CategoriaInvalida.Tipo.ShouldBe(TipoErro.RegraNegocio);
        ProdutoErros.CategoriaInvalida.Codigo.ShouldBe("produto.categoria_invalida");
    }
}

public sealed class MapeamentoResponseTests
{
    [Fact]
    public void ProdutoResponseDe_CopiaOsCamposEUsaOValorDoSku()
    {
        var produto = new ProdutoBuilder().ComSku("tec-001").Build();

        var response = ProdutoResponse.De(produto, "Periféricos");

        response.Id.ShouldBe(produto.Id);
        response.CategoriaId.ShouldBe(produto.CategoriaId);
        response.CategoriaNome.ShouldBe("Periféricos");
        response.Nome.ShouldBe(produto.Nome);
        response.Sku.ShouldBe("tec-001");
        response.Descricao.ShouldBe(produto.Descricao);
        response.Preco.ShouldBe(produto.Preco);
        response.Ativo.ShouldBe(produto.Ativo);
        response.DataCriacao.ShouldBe(produto.DataCriacao);
        response.DataAtualizacao.ShouldBe(produto.DataAtualizacao);
    }

    // ADR-006: DataRemocao nunca é exposta
    [Fact]
    public void ProdutoResponse_NaoExpoeDataRemocao()
    {
        typeof(ProdutoResponse).GetProperty("DataRemocao").ShouldBeNull();
    }

    [Fact]
    public void ProdutoResponseDe_ComArgumentoNulo_LancaArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ProdutoResponse.De(null!, "Periféricos"));
        Should.Throw<ArgumentNullException>(() => ProdutoResponse.De(new ProdutoBuilder().Build(), null!));
    }

    [Fact]
    public void CategoriaResponseDe_CopiaOsCampos()
    {
        var categoria = new CategoriaBuilder().Inativa().Build();

        var response = CategoriaResponse.De(categoria);

        response.ShouldBe(new CategoriaResponse(
            categoria.Id, categoria.Nome, categoria.Descricao, false, categoria.DataCriacao, categoria.DataAtualizacao));
        Should.Throw<ArgumentNullException>(() => CategoriaResponse.De(null!));
    }
}
