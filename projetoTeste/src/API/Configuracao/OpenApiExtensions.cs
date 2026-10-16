using Microsoft.OpenApi;

namespace Catalogo.Api.Configuracao;

internal static class OpenApiExtensions
{
    private const string EsquemaBearer = "Bearer";

    /// <summary>Documento OpenAPI com o esquema Bearer (JWT). Só é exposto em Development (Program.cs).</summary>
    public static IServiceCollection AddDocumentacaoOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(opcoes =>
        {
            opcoes.AddDocumentTransformer((documento, _, _) =>
            {
                documento.Info.Title = "API de Catálogo de Produtos";
                documento.Info.Version = "v1";

                documento.Components ??= new OpenApiComponents();
                documento.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                documento.Components.SecuritySchemes[EsquemaBearer] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Token JWT no header Authorization: Bearer {token}."
                };

                documento.Security ??= [];
                documento.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(EsquemaBearer, documento)] = []
                });

                return Task.CompletedTask;
            });
        });

        return services;
    }
}
