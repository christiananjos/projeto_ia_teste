namespace Catalogo.Api.Configuracao;

/// <summary>Policies de autorização (ADR-010).</summary>
public static class Politicas
{
    /// <summary>Usuário autenticado.</summary>
    public const string Leitura = "Leitura";

    /// <summary>Usuário autenticado com role <see cref="RoleAdmin"/>.</summary>
    public const string Escrita = "Escrita";

    public const string RoleAdmin = "Admin";
}
