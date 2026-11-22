using FinanceApp.ViewModels;
using FluentAssertions;
using Xunit;

namespace FinanceApp.Tests.ViewModels;

public class DescriptionMatcherTests
{
    [Theory]
    [InlineData("Seguro", "Seguro do carro", true)]
    [InlineData("seguro carro", "Seguro do carro", true)]
    [InlineData("AGUA", "Água", true)]
    [InlineData("Plano saude", "Plano de saúde", true)]
    [InlineData("Seguro do carro", "Seguro", false)]
    [InlineData("Celular novo", "Celular", false)]
    [InlineData("Net", "Internet", false)]
    [InlineData("Seguro auto", "Seguro do carro", false)]
    [InlineData("", "Energia", false)]
    [InlineData("de", "Plano de saúde", false)]
    [InlineData("  -  ", "Energia", false)]
    public void IsPartOf_EveryWordOfThePartInTheWholeIgnoringCaseAccentsAndLinkingWords(string part, string whole, bool expected)
    {
        DescriptionMatcher.IsPartOf(part, whole).Should().Be(expected);
    }
}
