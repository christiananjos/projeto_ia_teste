using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Catalogo.IntegrationTests.Infra;

public sealed record EntradaDeLog(string Categoria, LogLevel Nivel, string Mensagem, Exception? Excecao);

/// <summary>Logger em memória para verificar o nível com que a API registra cada classe de erro.</summary>
public sealed class LogsCapturados : ILoggerProvider
{
    private readonly ConcurrentQueue<EntradaDeLog> _entradas = new();

    public IReadOnlyList<EntradaDeLog> Entradas => [.. _entradas];

    public void Limpar() => _entradas.Clear();

    public ILogger CreateLogger(string categoryName) => new Logger(categoryName, _entradas);

    public void Dispose()
    {
    }

    private sealed class Logger(string categoria, ConcurrentQueue<EntradaDeLog> entradas) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            entradas.Enqueue(new EntradaDeLog(categoria, logLevel, formatter(state, exception), exception));
    }
}
