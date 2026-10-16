using Catalogo.Application.Categorias.Dtos;
using Catalogo.Application.Produtos.Dtos;

namespace Catalogo.UnitTests.Fixtures;

/// <summary>Requests válidos por padrão; cada teste altera só o campo que interessa com <c>with</c>.</summary>
internal static class Requisicoes
{
    public static CriarCategoriaRequest CriarCategoria() =>
        new("Periféricos", "Teclados, mouses e headsets");

    public static AtualizarCategoriaRequest AtualizarCategoria() =>
        new("Periféricos Gamer", "Linha gamer", Ativa: true);

    public static CriarProdutoRequest CriarProduto(Guid? categoriaId = null) =>
        new(categoriaId ?? Guid.CreateVersion7(), "Teclado Mecânico", "TEC-001", "ABNT2", 349.90m);

    public static AtualizarProdutoRequest AtualizarProduto(Guid? categoriaId = null) =>
        new(categoriaId ?? Guid.CreateVersion7(), "Teclado Mecânico RGB", "ABNT2 com iluminação", 399.90m);

    public static string Texto(int tamanho, char caractere = 'a') => new(caractere, tamanho);
}
