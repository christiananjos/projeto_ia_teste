using Catalogo.Domain.Categorias;
using Catalogo.Domain.Excecoes;
using Catalogo.Domain.Produtos;
using Catalogo.Infrastructure.Persistencia;
using Catalogo.Infrastructure.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.IntegrationTests.Persistencia;

/// <summary>
/// Corridas que só o banco detecta: a tradução dos erros SQL 2601/2627/547 para
/// <see cref="ConflitoPersistenciaException"/> com o nome da restrição (spec técnica §5.3).
/// </summary>
public sealed class RepositoriosSqlTests(SqlServerFixture banco) : TesteComSqlServer(banco)
{
    private static DateTime Agora => DateTime.UtcNow;

    private async Task<Categoria> SalvarCategoriaAsync(string nome)
    {
        var categoria = Categoria.Criar(nome, null, Agora);
        await ComContextoAsync(async db =>
        {
            db.Categorias.Add(categoria);
            await db.SalvarAlteracoesAsync(Ct);
            return categoria;
        });
        return categoria;
    }

    private Task<ConflitoPersistenciaException> SalvarEsperandoConflitoAsync(Action<CatalogoDbContext> alterar) =>
        ComContextoAsync(async db =>
        {
            alterar(db);
            return await Should.ThrowAsync<ConflitoPersistenciaException>(() => db.SalvarAlteracoesAsync(Ct));
        });

    [Fact]
    public async Task SkuDuplicadoComOutraCaixa_ViraConflitoDeUnicidadeNoIndiceDeSku()
    {
        using var _ = await PrepararAsync();
        var categoria = await SalvarCategoriaAsync("Periféricos");
        await ComContextoAsync(async db =>
        {
            db.Produtos.Add(Produto.Criar(categoria.Id, "Teclado Mecânico", Sku.Criar("TEC-001"), null, 10m, Agora));
            await db.SalvarAlteracoesAsync(Ct);
            return true;
        });

        var conflito = await SalvarEsperandoConflitoAsync(db =>
            db.Produtos.Add(Produto.Criar(categoria.Id, "Outro Teclado", Sku.Criar("tec-001"), null, 10m, Agora)));

        conflito.Tipo.ShouldBe(TipoConflitoPersistencia.Unicidade);
        conflito.Restricao.ShouldBe("UX_Produtos_Sku");
    }

    [Fact]
    public async Task NomeDuplicadoNaCategoria_ViraConflitoNoIndiceComposto()
    {
        using var _ = await PrepararAsync();
        var categoria = await SalvarCategoriaAsync("Periféricos");
        await ComContextoAsync(async db =>
        {
            db.Produtos.Add(Produto.Criar(categoria.Id, "Teclado Mecânico", Sku.Criar("TEC-001"), null, 10m, Agora));
            await db.SalvarAlteracoesAsync(Ct);
            return true;
        });

        var conflito = await SalvarEsperandoConflitoAsync(db =>
            db.Produtos.Add(Produto.Criar(categoria.Id, "teclado mecânico", Sku.Criar("TEC-002"), null, 10m, Agora)));

        conflito.Tipo.ShouldBe(TipoConflitoPersistencia.Unicidade);
        conflito.Restricao.ShouldBe("UX_Produtos_CategoriaId_Nome");
    }

    [Fact]
    public async Task ProdutoComCategoriaInexistente_ViraConflitoDeReferencia()
    {
        using var _ = await PrepararAsync();

        var conflito = await SalvarEsperandoConflitoAsync(db =>
            db.Produtos.Add(Produto.Criar(Guid.CreateVersion7(), "Teclado Mecânico", Sku.Criar("TEC-001"), null, 10m, Agora)));

        conflito.Tipo.ShouldBe(TipoConflitoPersistencia.Referencia);
        conflito.Restricao.ShouldBe("FK_Produtos_Categorias_CategoriaId");
    }

    [Fact]
    public async Task NomeDeCategoriaDuplicado_ViraConflitoNoIndiceDeNome()
    {
        using var _ = await PrepararAsync();
        await SalvarCategoriaAsync("Periféricos");

        var conflito = await SalvarEsperandoConflitoAsync(db => db.Categorias.Add(Categoria.Criar("periféricos", null, Agora)));

        conflito.Tipo.ShouldBe(TipoConflitoPersistencia.Unicidade);
        conflito.Restricao.ShouldBe("UX_Categorias_Nome");
    }

    // ADR-001: produto não removido criado depois da checagem → FK 547 → Referencia, com rollback
    [Fact]
    public async Task ExcluirCategoriaComProdutoNaoRemovido_LancaReferenciaEMantemTudo()
    {
        using var _ = await PrepararAsync();
        var categoria = await SalvarCategoriaAsync("Periféricos");
        var removido = Produto.Criar(categoria.Id, "Mouse Antigo", Sku.Criar("MOU-000"), null, 10m, Agora);
        removido.Remover(Agora);
        await ComContextoAsync(async db =>
        {
            db.Produtos.Add(removido);
            db.Produtos.Add(Produto.Criar(categoria.Id, "Teclado Mecânico", Sku.Criar("TEC-001"), null, 10m, Agora));
            await db.SalvarAlteracoesAsync(Ct);
            return true;
        });

        var conflito = await ComContextoAsync(db =>
            Should.ThrowAsync<ConflitoPersistenciaException>(() =>
                new CategoriaRepository(db).ExcluirComProdutosRemovidosAsync(categoria.Id, Ct)));

        conflito.Tipo.ShouldBe(TipoConflitoPersistencia.Referencia);
        (await ComContextoAsync(db => db.Produtos.IgnoreQueryFilters().CountAsync(Ct))).ShouldBe(2);
        (await ComContextoAsync(db => db.Categorias.CountAsync(Ct))).ShouldBe(1);
    }

    [Fact]
    public async Task Repositorios_IgnoramProdutosRemovidosNasChecagens()
    {
        using var _ = await PrepararAsync();
        var categoria = await SalvarCategoriaAsync("Periféricos");
        var produto = Produto.Criar(categoria.Id, "Teclado Mecânico", Sku.Criar("TEC-001"), null, 10m, Agora);
        produto.Remover(Agora);
        await ComContextoAsync(async db =>
        {
            db.Produtos.Add(produto);
            await db.SalvarAlteracoesAsync(Ct);
            return true;
        });

        await ComContextoAsync(async db =>
        {
            var produtos = new ProdutoRepository(db);
            var categorias = new CategoriaRepository(db);

            (await produtos.ObterPorIdAsync(produto.Id, Ct)).ShouldBeNull();
            (await produtos.ExisteSkuAsync(Sku.Criar("tec-001"), Ct)).ShouldBeFalse();
            (await produtos.ExisteNomeNaCategoriaAsync(categoria.Id, "Teclado Mecânico", null, Ct)).ShouldBeFalse();
            (await categorias.PossuiProdutosNaoRemovidosAsync(categoria.Id, Ct)).ShouldBeFalse();
            (await categorias.ExisteNomeAsync("PERIFÉRICOS", null, Ct)).ShouldBeTrue();
            (await categorias.ExisteNomeAsync("PERIFÉRICOS", categoria.Id, Ct)).ShouldBeFalse();
            return true;
        });
    }
}
