using Catalogo.Api.Configuracao;
using Catalogo.Api.Endpoints;
using Catalogo.Api.Http;
using Catalogo.Application;
using Catalogo.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(opcoes => opcoes.CustomizeProblemDetails = ProblemDetailsPadrao.Personalizar);
builder.Services.AddExceptionHandler<ExcecaoGlobalHandler>();

// Binding inválido (query string com tipo errado, JSON malformado) responde 400 em vez de lançar exceção;
// o corpo ProblemDetails é escrito pelo UseStatusCodePages.
builder.Services.Configure<RouteHandlerOptions>(opcoes => opcoes.ThrowOnBadRequest = false);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAutenticacaoJwt(builder.Configuration);
builder.Services.AddRateLimitingPorIp(builder.Configuration);
builder.Services.AddDocumentacaoOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// O limiter roda antes da autenticação para que requisições sem token também sejam limitadas por IP.
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.MapCategoriaEndpoints();
app.MapProdutoEndpoints();

await app.RunAsync();

/// <summary>Exposto para <c>WebApplicationFactory&lt;Program&gt;</c> nos testes de integração.</summary>
public partial class Program;
