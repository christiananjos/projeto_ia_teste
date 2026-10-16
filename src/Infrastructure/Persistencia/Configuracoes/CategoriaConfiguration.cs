using Catalogo.Domain.Categorias;
using Catalogo.Domain.Regras;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalogo.Infrastructure.Persistencia.Configuracoes;

internal sealed class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable(NomesBanco.TabelaCategorias);

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Nome)
            .HasMaxLength(LimitesCampos.CategoriaNomeMax)
            .UseCollation(NomesBanco.Collation)
            .IsRequired();

        builder.Property(c => c.Descricao)
            .HasMaxLength(LimitesCampos.CategoriaDescricaoMax);

        // Sentinel true: o valor sempre é enviado quando false; quando true, o default do banco produz o mesmo valor.
        builder.Property(c => c.Ativa)
            .HasDefaultValue(true)
            .HasSentinel(true)
            .IsRequired();

        builder.Property(c => c.DataCriacao).HasColumnType("datetime2(7)").IsRequired();
        builder.Property(c => c.DataAtualizacao).HasColumnType("datetime2(7)").IsRequired();

        builder.HasIndex(c => c.Nome)
            .IsUnique()
            .HasDatabaseName(NomesBanco.IndiceCategoriasNome);
    }
}
