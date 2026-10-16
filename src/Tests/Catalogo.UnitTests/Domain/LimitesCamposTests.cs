using System.Globalization;
using Catalogo.Domain.Excecoes;
using Catalogo.Domain.Regras;

namespace Catalogo.UnitTests.Domain;

public sealed class LimitesCamposTests
{
    [Theory]
    [InlineData("10", true)]
    [InlineData("10.5", true)]
    [InlineData("10.50", true)]
    [InlineData("10.500", true)]
    [InlineData("10.001", false)]
    [InlineData("0.009", false)]
    public void TemNoMaximoCasasDecimais_ConsideraOValorENaoAEscala(string valor, bool esperado)
    {
        LimitesCampos.TemNoMaximoCasasDecimais(decimal.Parse(valor, CultureInfo.InvariantCulture)).ShouldBe(esperado);
    }

    [Theory]
    [InlineData("Teclado", true)]
    [InlineData("Teclado Mecânico", true)]
    [InlineData(" Teclado", false)]
    [InlineData("Teclado ", false)]
    [InlineData("\nTeclado", false)]
    [InlineData("Teclado ", false)]
    public void SemEspacosNasPontas_UsaADefinicaoDeEspacoDoTrim(string valor, bool esperado)
    {
        LimitesCampos.SemEspacosNasPontas(valor).ShouldBe(esperado);
    }

    [Fact]
    public void ConflitoPersistenciaException_IncluiARestricaoNaMensagemQuandoConhecida()
    {
        var comRestricao = new ConflitoPersistenciaException(TipoConflitoPersistencia.Unicidade, "UX_Produtos_Sku");
        var semRestricao = new ConflitoPersistenciaException(TipoConflitoPersistencia.Referencia, null);

        comRestricao.Message.ShouldContain("UX_Produtos_Sku");
        comRestricao.Tipo.ShouldBe(TipoConflitoPersistencia.Unicidade);
        semRestricao.Message.ShouldBe("Conflito de persistência (Referencia).");
        semRestricao.Restricao.ShouldBeNull();
    }
}
