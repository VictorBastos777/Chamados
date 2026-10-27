using Chamados.Domain;

namespace Chamados.Domain.Tests;

public class ChamadoTests
{
    [Fact]
    public void NovoChamado_NasceComStatusAberto()
    {
        var chamado = new Chamado("Trocar lâmpada do corredor");

        Assert.Equal(StatusChamado.Aberto, chamado.Status);
    }

    [Fact]
    public void NovoChamado_RemoveEspacosDoTitulo()
    {
        var chamado = new Chamado("  Vazamento na copa  ");

        Assert.Equal("Vazamento na copa", chamado.Titulo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NovoChamado_TituloEmBranco_LancaArgumentException(string titulo)
    {
        Assert.Throws<ArgumentException>(() => new Chamado(titulo));
    }

    [Fact]
    public void NovoChamado_TituloNulo_LancaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Chamado(null!));
    }
}
