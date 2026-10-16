namespace Catalogo.Application.Comum;

/// <summary>
/// Erro esperado de um caso de uso. <see cref="Campos"/> só é preenchido em erros de validação e usa chaves
/// em camelCase iguais às propriedades JSON.
/// </summary>
public sealed record Erro(
    TipoErro Tipo,
    string Codigo,
    string Mensagem,
    IReadOnlyDictionary<string, string[]>? Campos = null)
{
    public static Erro Validacao(IReadOnlyDictionary<string, string[]> campos) =>
        new(TipoErro.Validacao, CodigosErro.Validacao, "Um ou mais campos são inválidos.", campos);

    public static Erro NaoEncontrado(string codigo, string mensagem) =>
        new(TipoErro.NaoEncontrado, codigo, mensagem);

    public static Erro Conflito(string codigo, string mensagem) =>
        new(TipoErro.Conflito, codigo, mensagem);

    public static Erro RegraNegocio(string codigo, string mensagem) =>
        new(TipoErro.RegraNegocio, codigo, mensagem);
}
