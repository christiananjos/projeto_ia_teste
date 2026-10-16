using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Catalogo.IntegrationTests.Infra;

/// <summary>
/// Guarda o status final de cada requisição, visto pelo middleware mais externo. Serve para os casos em que o
/// cliente já desistiu e não recebe a resposta (cancelamento → 499).
/// </summary>
public sealed class RegistroDeRespostas
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<int>> _status = new(StringComparer.Ordinal);

    public Task<int> AguardarStatusAsync(string caminho) => Obter(caminho).Task;

    public void Limpar() => _status.Clear();

    internal void Registrar(string caminho, int status) => Obter(caminho).TrySetResult(status);

    private TaskCompletionSource<int> Obter(string caminho) =>
        _status.GetOrAdd(caminho, _ => new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously));
}

internal sealed class RegistroDeRespostasStartupFilter(RegistroDeRespostas registro) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (contexto, proximo) =>
        {
            try
            {
                await proximo(contexto);
            }
            finally
            {
                registro.Registrar(contexto.Request.Path.Value ?? string.Empty, contexto.Response.StatusCode);
            }
        });
        next(app);
    };
}
