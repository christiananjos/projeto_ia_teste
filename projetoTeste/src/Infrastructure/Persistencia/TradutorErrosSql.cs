using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Catalogo.Domain.Excecoes;
using Microsoft.Data.SqlClient;

namespace Catalogo.Infrastructure.Persistencia;

/// <summary>
/// Traduz erros do SQL Server de unicidade (2601/2627) e de FK (547) para
/// <see cref="ConflitoPersistenciaException"/>, para que a Application trate corridas sem depender do EF.
/// </summary>
internal static partial class TradutorErrosSql
{
    private const int ErroIndiceUnicoDuplicado = 2601;
    private const int ErroRestricaoUnicaViolada = 2627;
    private const int ErroConflitoReferencia = 547;

    public static bool TentarTraduzir(Exception excecao, [NotNullWhen(true)] out ConflitoPersistenciaException? conflito)
    {
        conflito = null;

        var sqlException = EncontrarSqlException(excecao);
        if (sqlException is null)
            return false;

        TipoConflitoPersistencia? tipo = sqlException.Number switch
        {
            ErroIndiceUnicoDuplicado or ErroRestricaoUnicaViolada => TipoConflitoPersistencia.Unicidade,
            ErroConflitoReferencia => TipoConflitoPersistencia.Referencia,
            _ => null
        };

        if (tipo is null)
            return false;

        conflito = new ConflitoPersistenciaException(tipo.Value, ExtrairRestricao(sqlException.Message), excecao);
        return true;
    }

    private static SqlException? EncontrarSqlException(Exception? excecao)
    {
        for (var atual = excecao; atual is not null; atual = atual.InnerException)
        {
            if (atual is SqlException sqlException)
                return sqlException;
        }

        return null;
    }

    private static string? ExtrairRestricao(string mensagem)
    {
        // Os nomes conhecidos são procurados primeiro: funciona mesmo com mensagens localizadas pelo servidor.
        foreach (var restricao in NomesBanco.Restricoes)
        {
            if (mensagem.Contains(restricao, StringComparison.OrdinalIgnoreCase))
                return restricao;
        }

        var correspondencia = NomeEntreAspas().Match(mensagem);
        return correspondencia.Success ? correspondencia.Groups["nome"].Value : null;
    }

    [GeneratedRegex("""(?:index|constraint)\s+['"](?<nome>[^'"]+)['"]""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NomeEntreAspas();
}
