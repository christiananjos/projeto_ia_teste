namespace Catalogo.Application.Comum;

public enum TipoErro
{
    /// <summary>Entrada inválida (400).</summary>
    Validacao,

    /// <summary>Recurso inexistente (404).</summary>
    NaoEncontrado,

    /// <summary>Conflito de unicidade ou de vínculo (409).</summary>
    Conflito,

    /// <summary>Regra de negócio violada (422).</summary>
    RegraNegocio
}
