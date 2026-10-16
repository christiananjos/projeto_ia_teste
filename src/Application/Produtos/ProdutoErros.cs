using Catalogo.Application.Comum;
using Catalogo.Domain.Excecoes;

namespace Catalogo.Application.Produtos;

public static class ProdutoErros
{
    /// <summary>Nome do índice único de SKU; usado para distinguir o 409 de SKU do 409 de nome.</summary>
    public const string IndiceSku = "UX_Produtos_Sku";

    public static readonly Erro NaoEncontrado =
        Erro.NaoEncontrado(CodigosErro.ProdutoNaoEncontrado, "Produto não encontrado.");

    public static readonly Erro SkuDuplicado =
        Erro.Conflito(CodigosErro.ProdutoSkuDuplicado, "Já existe um produto com este SKU.");

    public static readonly Erro NomeDuplicado =
        Erro.Conflito(CodigosErro.ProdutoNomeDuplicado, "Já existe um produto com este nome nesta categoria.");

    public static readonly Erro CategoriaInvalida =
        Erro.RegraNegocio(CodigosErro.ProdutoCategoriaInvalida, "A categoria informada não existe ou está inativa.");

    /// <summary>Converte um conflito detectado pelo banco (corrida) no erro de negócio correspondente.</summary>
    public static Erro DeConflito(ConflitoPersistenciaException conflito)
    {
        ArgumentNullException.ThrowIfNull(conflito);

        return conflito.Tipo switch
        {
            TipoConflitoPersistencia.Referencia => CategoriaInvalida,
            _ when string.Equals(conflito.Restricao, IndiceSku, StringComparison.OrdinalIgnoreCase) => SkuDuplicado,
            _ => NomeDuplicado
        };
    }
}
