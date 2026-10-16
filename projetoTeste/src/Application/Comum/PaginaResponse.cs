namespace Catalogo.Application.Comum;

public sealed record PaginaResponse<T>(
    IReadOnlyList<T> Itens,
    int Page,
    int PageSize,
    int TotalItens,
    int TotalPaginas)
{
    public static PaginaResponse<T> Criar(IReadOnlyList<T> itens, int page, int pageSize, int totalItens) =>
        new(itens, page, pageSize, totalItens, CalcularTotalPaginas(totalItens, pageSize));

    public static int CalcularTotalPaginas(int totalItens, int pageSize) =>
        totalItens == 0 ? 0 : (int)Math.Ceiling(totalItens / (double)pageSize);

    /// <summary>Quantidade de itens a pular; nulo quando a página está além do total (evita overflow).</summary>
    public static int? CalcularDeslocamento(int page, int pageSize, int totalItens)
    {
        var deslocamento = (long)(page - 1) * pageSize;
        return deslocamento >= totalItens ? null : (int)deslocamento;
    }
}
