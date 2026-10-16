using Catalogo.Application.Comum;

namespace Catalogo.Application.Categorias;

public static class CategoriaErros
{
    public static readonly Erro NaoEncontrada =
        Erro.NaoEncontrado(CodigosErro.CategoriaNaoEncontrada, "Categoria não encontrada.");

    public static readonly Erro NomeDuplicado =
        Erro.Conflito(CodigosErro.CategoriaNomeDuplicado, "Já existe uma categoria com este nome.");

    public static readonly Erro PossuiProdutos =
        Erro.Conflito(CodigosErro.CategoriaPossuiProdutos, "A categoria possui produtos vinculados e não pode ser removida.");
}
