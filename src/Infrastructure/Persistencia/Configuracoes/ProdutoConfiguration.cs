using Catalogo.Domain.Categorias;
using Catalogo.Domain.Produtos;
using Catalogo.Domain.Regras;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalogo.Infrastructure.Persistencia.Configuracoes;

internal sealed class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable(NomesBanco.TabelaProdutos);

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.CategoriaId).IsRequired();

        builder.Property(p => p.Nome)
            .HasMaxLength(LimitesCampos.ProdutoNomeMax)
            .UseCollation(NomesBanco.Collation)
            .IsRequired();

        builder.Property(p => p.Sku)
            .HasConversion(s => s.Valor, v => Sku.Criar(v))
            .HasMaxLength(LimitesCampos.SkuMax)
            .UseCollation(NomesBanco.Collation)
            .IsRequired();

        builder.Property(p => p.Descricao)
            .HasMaxLength(LimitesCampos.ProdutoDescricaoMax);

        builder.Property(p => p.Preco)
            .HasPrecision(18, LimitesCampos.PrecoCasasDecimais)
            .IsRequired();

        // Sentinel true: o valor sempre é enviado quando false; quando true, o default do banco produz o mesmo valor.
        builder.Property(p => p.Ativo)
            .HasDefaultValue(true)
            .HasSentinel(true)
            .IsRequired();

        builder.Property(p => p.DataCriacao).HasColumnType("datetime2(7)").IsRequired();
        builder.Property(p => p.DataAtualizacao).HasColumnType("datetime2(7)").IsRequired();
        builder.Property(p => p.DataRemocao).HasColumnType("datetime2(7)");

        builder.Ignore(p => p.EstaRemovido);

        builder.HasOne<Categoria>()
            .WithMany()
            .HasForeignKey(p => p.CategoriaId)
            .HasConstraintName(NomesBanco.FkProdutosCategorias)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.CategoriaId)
            .HasDatabaseName(NomesBanco.IndiceProdutosCategoriaId);

        builder.HasIndex(p => p.Sku)
            .IsUnique()
            .HasFilter(NomesBanco.FiltroNaoRemovido)
            .HasDatabaseName(NomesBanco.IndiceProdutosSku);

        builder.HasIndex(p => new { p.CategoriaId, p.Nome })
            .IsUnique()
            .HasFilter(NomesBanco.FiltroNaoRemovido)
            .HasDatabaseName(NomesBanco.IndiceProdutosCategoriaIdNome);

        // ADR-015: produtos removidos logicamente somem de todas as consultas.
        builder.HasQueryFilter(p => p.DataRemocao == null);
    }
}
