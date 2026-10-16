using System.Net;
using Catalogo.Application.Categorias.Dtos;
using Catalogo.Domain.Excecoes;
using Catalogo.IntegrationTests.Infra;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace Catalogo.IntegrationTests.Api;

/// <summary>
/// Handler global (spec técnica §6.3): <see cref="DomainException"/> vira 400 <c>dominio</c>, outras exceções
/// viram 500 genérico, e o cancelamento pelo cliente não é registrado como erro do servidor.
/// </summary>
public sealed class TratamentoDeExcecoesTests : IClassFixture<ApiComSubstitutosFactory>, IDisposable
{
    private const string Rota = "/api/v1/categorias";

    private readonly ApiComSubstitutosFactory _api;
    private readonly HttpClient _admin;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public TratamentoDeExcecoesTests(ApiComSubstitutosFactory api)
    {
        _api = api;
        _api.ReiniciarSubstitutos();
        _admin = _api.ClienteAdmin();
    }

    public void Dispose() => _admin.Dispose();

    [Fact]
    public async Task DomainException_Retorna400ComCodigoDominioELogWarning()
    {
        var id = Guid.CreateVersion7();
        _api.Mocks.ConsultasCategoria.ObterPorIdAsync(id, Arg.Any<CancellationToken>())
            .ThrowsAsync(new DomainException("O SKU deve conter apenas letras, números e hífen."));

        var resposta = await _admin.GetAsync($"{Rota}/{id}", _ct);

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "dominio");
        problema.GetProperty("detail").GetString().ShouldBe("O SKU deve conter apenas letras, números e hífen.");
        LogsDoHandler().ShouldContain(l => l.Nivel == LogLevel.Warning);
        LogsDoHandler().ShouldNotContain(l => l.Nivel == LogLevel.Error);
    }

    [Fact]
    public async Task ExcecaoInesperada_Retorna500SemVazarDetalhesELogaError()
    {
        var id = Guid.CreateVersion7();
        _api.Mocks.ConsultasCategoria.ObterPorIdAsync(id, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Falha ao abrir Server=sql-prod;Password=segredo"));

        var resposta = await _admin.GetAsync($"{Rota}/{id}", _ct);

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.InternalServerError, "erro_interno");
        problema.GetProperty("detail").GetString().ShouldBe("Ocorreu um erro inesperado ao processar a requisição.");
        problema.GetRawText().ShouldNotContain("segredo");
        problema.GetRawText().ShouldNotContain("InvalidOperationException");
        LogsDoHandler().ShouldContain(l => l.Nivel == LogLevel.Error && l.Excecao is InvalidOperationException);
    }

    /// <summary>
    /// Pedido do 04-reviewer: cliente que desiste da requisição resulta em 499, sem log <c>Error</c>.
    /// </summary>
    [Fact]
    public async Task RequisicaoCanceladaPeloCliente_Responde499SemLogDeErro()
    {
        var id = Guid.CreateVersion7();
        var caminho = $"{Rota}/{id}";
        var consultaIniciada = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _api.Mocks.ConsultasCategoria.ObterPorIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(async chamada =>
            {
                consultaIniciada.TrySetResult();
                await Task.Delay(Timeout.Infinite, chamada.ArgAt<CancellationToken>(1));
                return (CategoriaResponse?)null;
            });

        using var cancelamento = CancellationTokenSource.CreateLinkedTokenSource(_ct);
        var requisicao = _admin.GetAsync(caminho, cancelamento.Token);
        await consultaIniciada.Task.WaitAsync(TimeSpan.FromSeconds(10), _ct);

        await cancelamento.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => requisicao);
        var status = await _api.Respostas.AguardarStatusAsync(caminho).WaitAsync(TimeSpan.FromSeconds(10), _ct);
        status.ShouldBe(499);
        _api.Logs.Entradas.ShouldNotContain(l => l.Nivel >= LogLevel.Error);
    }

    private IEnumerable<EntradaDeLog> LogsDoHandler() =>
        _api.Logs.Entradas.Where(l => l.Categoria.EndsWith("ExcecaoGlobalHandler", StringComparison.Ordinal));
}
