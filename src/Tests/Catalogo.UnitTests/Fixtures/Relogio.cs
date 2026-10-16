using Microsoft.Extensions.Time.Testing;

namespace Catalogo.UnitTests.Fixtures;

/// <summary>Instantes fixos usados na massa de teste (BR-007: o servidor define as datas em UTC).</summary>
internal static class Relogio
{
    public static readonly DateTimeOffset Inicio = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    public static DateTime AgoraUtc => Inicio.UtcDateTime;

    public static DateTime Depois(TimeSpan intervalo) => Inicio.Add(intervalo).UtcDateTime;

    public static FakeTimeProvider CriarFake() => new(Inicio);
}
