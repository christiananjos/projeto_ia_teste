using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Catalogo.IntegrationTests.Infra;

public static class HttpExtensions
{
    public static async Task<JsonElement> LerJsonAsync(this HttpResponseMessage resposta)
    {
        var conteudo = await resposta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var documento = JsonDocument.Parse(conteudo);
        return documento.RootElement.Clone();
    }

    public static async Task<T> LerAsync<T>(this HttpResponseMessage resposta) =>
        (await resposta.Content.ReadFromJsonAsync<T>(JsonSerializerOptions.Web, TestContext.Current.CancellationToken))!;

    /// <summary>
    /// Confere o formato ProblemDetails (RFC 9457) exigido pela spec técnica §6.3 e devolve o corpo.
    /// </summary>
    public static async Task<JsonElement> DeveSerProblemaAsync(this HttpResponseMessage resposta, HttpStatusCode status, string codigo)
    {
        resposta.StatusCode.ShouldBe(status);
        resposta.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        var corpo = await resposta.LerJsonAsync();
        var numero = (int)status;
        corpo.GetProperty("status").GetInt32().ShouldBe(numero);
        corpo.GetProperty("type").GetString().ShouldBe($"https://httpstatuses.io/{numero}");
        corpo.GetProperty("title").GetString().ShouldNotBeNullOrWhiteSpace();
        corpo.GetProperty("instance").GetString().ShouldNotBeNullOrWhiteSpace();
        corpo.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        corpo.GetProperty("code").GetString().ShouldBe(codigo);
        return corpo;
    }

    public static StringContent JsonBruto(string json) => new(json, Encoding.UTF8, "application/json");

    public static IEnumerable<string> ChavesDeErros(this JsonElement problema) =>
        problema.GetProperty("errors").EnumerateObject().Select(p => p.Name);
}
