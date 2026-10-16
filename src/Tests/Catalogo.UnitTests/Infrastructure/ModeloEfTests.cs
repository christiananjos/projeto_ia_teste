using Catalogo.Domain.Categorias;
using Catalogo.Domain.Produtos;
using Catalogo.Infrastructure.Persistencia;
using Catalogo.Infrastructure.Persistencia.Conversores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Catalogo.UnitTests.Infrastructure;

/// <summary>
/// Confere o modelo EF sem abrir conexão. Os nomes de índice e FK são lidos pelo <c>TradutorErrosSql</c> para
/// decidir entre 409 de SKU, 409 de nome e 422, então mudar um deles quebraria o mapeamento de corridas.
/// </summary>
public sealed class ModeloEfTests : IDisposable
{
    private const string ConexaoNaoUsada = "Server=nao-usado;Database=Catalogo;Trusted_Connection=True;TrustServerCertificate=True";

    private readonly CatalogoDbContext _db = new(
        new DbContextOptionsBuilder<CatalogoDbContext>().UseSqlServer(ConexaoNaoUsada).Options);

    public void Dispose() => _db.Dispose();

    private IEntityType EntidadeProduto => ModeloDesignTime.FindEntityType(typeof(Produto))!;
    private IEntityType EntidadeCategoria => ModeloDesignTime.FindEntityType(typeof(Categoria))!;

    // O modelo de design-time guarda as anotações de collation, que o modelo de runtime descarta.
    private IModel ModeloDesignTime => _db.GetService<IDesignTimeModel>().Model;

    [Fact]
    public void Tabelas_FicamNoSchemaDbo()
    {
        EntidadeProduto.GetTableName().ShouldBe("Produtos");
        EntidadeProduto.GetSchema().ShouldBe("dbo");
        EntidadeCategoria.GetTableName().ShouldBe("Categorias");
    }

    // BR-003 / BR-005: SKU único entre não removidos
    [Fact]
    public void IndiceDeSku_EhUnicoEFiltradoPorNaoRemovidos()
    {
        var indice = EntidadeProduto.GetIndexes().Single(i => i.GetDatabaseName() == "UX_Produtos_Sku");

        indice.IsUnique.ShouldBeTrue();
        indice.GetFilter().ShouldBe("[DataRemocao] IS NULL");
        indice.Properties.Select(p => p.Name).ShouldBe(new[] { "Sku" });
    }

    // BR-001 / BR-005: nome único na categoria entre não removidos
    [Fact]
    public void IndiceDeNomeNaCategoria_EhUnicoEFiltradoPorNaoRemovidos()
    {
        var indice = EntidadeProduto.GetIndexes().Single(i => i.GetDatabaseName() == "UX_Produtos_CategoriaId_Nome");

        indice.IsUnique.ShouldBeTrue();
        indice.GetFilter().ShouldBe("[DataRemocao] IS NULL");
        indice.Properties.Select(p => p.Name).ShouldBe(new[] { "CategoriaId", "Nome" });
    }

    // REQ-001: nome de categoria único (remoção de categoria é física, então o índice não é filtrado)
    [Fact]
    public void IndiceDeNomeDaCategoria_EhUnicoSemFiltro()
    {
        var indice = EntidadeCategoria.GetIndexes().Single(i => i.GetDatabaseName() == "UX_Categorias_Nome");

        indice.IsUnique.ShouldBeTrue();
        indice.GetFilter().ShouldBeNull();
    }

    // ADR-008: unicidade sem diferenciar maiúsculas pela collation
    [Fact]
    public void NomesESku_UsamCollationCaseInsensitive()
    {
        EntidadeProduto.FindProperty(nameof(Produto.Sku))!.GetCollation().ShouldBe("Latin1_General_100_CI_AS");
        EntidadeProduto.FindProperty(nameof(Produto.Nome))!.GetCollation().ShouldBe("Latin1_General_100_CI_AS");
        EntidadeCategoria.FindProperty(nameof(Categoria.Nome))!.GetCollation().ShouldBe("Latin1_General_100_CI_AS");
    }

    // BR-006: FK sem cascata, a remoção da categoria é controlada pela Application (ADR-001)
    [Fact]
    public void FkDeCategoria_TemNomeFixoESemCascata()
    {
        var fk = EntidadeProduto.GetForeignKeys().Single();

        fk.GetConstraintName().ShouldBe("FK_Produtos_Categorias_CategoriaId");
        fk.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);
        fk.PrincipalEntityType.ClrType.ShouldBe(typeof(Categoria));
    }

    // ADR-015: removidos somem de todas as consultas
    [Fact]
    public void Produto_TemQueryFilterDeNaoRemovidoEIgnoraEstaRemovido()
    {
        EntidadeProduto.GetDeclaredQueryFilters().ShouldNotBeEmpty();
        EntidadeProduto.FindProperty(nameof(Produto.EstaRemovido)).ShouldBeNull();
    }

    // BR-002 / ADR-014
    [Fact]
    public void Preco_EhDecimal18Com2Casas()
    {
        var preco = EntidadeProduto.FindProperty(nameof(Produto.Preco))!;

        preco.GetPrecision().ShouldBe(18);
        preco.GetScale().ShouldBe(2);
    }

    [Fact]
    public void TamanhosDasColunas_SeguemOsLimitesDosCampos()
    {
        EntidadeProduto.FindProperty("Nome")!.GetMaxLength().ShouldBe(150);
        EntidadeProduto.FindProperty("Sku")!.GetMaxLength().ShouldBe(40);
        EntidadeProduto.FindProperty("Descricao")!.GetMaxLength().ShouldBe(1000);
        EntidadeCategoria.FindProperty("Nome")!.GetMaxLength().ShouldBe(100);
        EntidadeCategoria.FindProperty("Descricao")!.GetMaxLength().ShouldBe(500);
    }

    [Fact]
    public void Migrations_EstaoEmDiaComOModelo()
    {
        _db.Database.HasPendingModelChanges().ShouldBeFalse();
    }

    // Segurança: o filtro "nome" vira LIKE parametrizado, sem SQL concatenado
    [Fact]
    public void FiltroPorNome_GeraLikeParametrizadoEAplicaOQueryFilter()
    {
        var nome = "100%_[teclado]'; DROP TABLE Produtos;--";

        var sql = _db.Produtos.Where(p => p.Nome.Contains(nome)).ToQueryString();

        var where = sql[sql.IndexOf("WHERE", StringComparison.Ordinal)..];
        where.ShouldContain("LIKE @");
        where.ShouldContain("[DataRemocao] IS NULL");
        where.ShouldNotContain("DROP TABLE");
        sql.ShouldContain("DECLARE @");
    }

    [Fact]
    public void ComparacaoDeSku_UsaOConversorDoValueObject()
    {
        var sku = Sku.Criar("TEC-001");

        var sql = _db.Produtos.Where(p => p.Sku == sku).ToQueryString();

        sql.ShouldContain("[p].[Sku] = @");
        sql.ShouldContain("N'TEC-001'");
    }
}

/// <summary>BR-007: datas gravadas e lidas em UTC.</summary>
public sealed class UtcDateTimeConverterTests
{
    private readonly UtcDateTimeConverter _conversor = new();

    [Fact]
    public void NaEscrita_MantemDataUtc()
    {
        var utc = new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);

        ((DateTime)_conversor.ConvertToProvider(utc)!).ShouldBe(utc);
    }

    [Fact]
    public void NaEscrita_ConverteDataLocalParaUtc()
    {
        var local = new DateTime(2026, 10, 16, 9, 0, 0, DateTimeKind.Local);

        var gravada = (DateTime)_conversor.ConvertToProvider(local)!;

        gravada.ShouldBe(local.ToUniversalTime());
    }

    [Fact]
    public void NaLeitura_MarcaComoUtcSemMudarOValor()
    {
        var doBanco = new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Unspecified);

        var lida = (DateTime)_conversor.ConvertFromProvider(doBanco)!;

        lida.Kind.ShouldBe(DateTimeKind.Utc);
        lida.Ticks.ShouldBe(doBanco.Ticks);
    }
}

public sealed class TradutorErrosSqlTests
{
    [Fact]
    public void ExcecaoSemSqlException_NaoEhTraduzida()
    {
        var excecao = new DbUpdateException("falha", new InvalidOperationException("sem SQL"));

        TradutorErrosSql.TentarTraduzir(excecao, out var conflito).ShouldBeFalse();
        conflito.ShouldBeNull();
    }
}
