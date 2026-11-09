using System.Globalization;
using FinanceApp.Converters;
using FluentAssertions;
using Xunit;

namespace FinanceApp.Tests.Converters;

public class AnnualTaxConverterTests
{
    [Theory]
    [InlineData(1, "Janeiro")]
    [InlineData(3, "Março")]
    [InlineData(12, "Dezembro")]
    [InlineData(0, "")]
    public void MonthNumberToName_NamesTheMonth(int month, string expected)
    {
        MonthNumberToNameConverter.Instance.Convert(month, typeof(string), null, CultureInfo.InvariantCulture).Should().Be(expected);
    }

    [Theory]
    [InlineData(1, "Cota única")]
    [InlineData(3, "3 parcelas")]
    [InlineData(10, "10 parcelas")]
    public void AnnualInstallmentsToLabel_LabelsSinglePaymentAndInstallments(int count, string expected)
    {
        AnnualInstallmentsToLabelConverter.Instance.Convert(count, typeof(string), null, CultureInfo.InvariantCulture).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, "Todo mês")]
    [InlineData(1, "À vista")]
    [InlineData(10, "10x")]
    public void YearlyPaymentToLabel_LabelsMonthlySinglePaymentAndInstallments(int installments, string expected)
    {
        YearlyPaymentToLabelConverter.Instance.Convert(installments, typeof(string), null, CultureInfo.InvariantCulture).Should().Be(expected);
    }
}
