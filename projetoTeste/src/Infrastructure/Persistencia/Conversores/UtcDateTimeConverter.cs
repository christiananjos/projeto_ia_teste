using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Catalogo.Infrastructure.Persistencia.Conversores;

/// <summary>
/// Garante que toda data seja gravada em UTC e lida com <see cref="DateTimeKind.Utc"/>, para serialização com
/// sufixo <c>Z</c> (BR-007).
/// </summary>
public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
