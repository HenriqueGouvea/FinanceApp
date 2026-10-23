using FinanceApp.Converters;
using FluentAssertions;
using System.Globalization;
using Xunit;

namespace FinanceApp.Tests.Converters;

public class MoneyConverterTests
{
    [Theory]
    [InlineData(0, "R$ 0,00")]
    [InlineData(5.5, "R$ 5,50")]
    [InlineData(1000, "R$ 1.000,00")]
    [InlineData(1234567.89, "R$ 1.234.567,89")]
    [InlineData(-1500, "-R$ 1.500,00")]
    public void Format_Amount_ReturnsBrazilianCurrency(double amount, string expected)
    {
        MoneyConverter.Format((decimal)amount).Should().Be(expected);
    }

    [Theory]
    [InlineData("150000", 1500)]
    [InlineData("5", 0.05)]
    [InlineData("R$ 1.234,567", 12345.67)]
    [InlineData("R$ 0,0", 0)]
    [InlineData("0005", 0.05)]
    public void Parse_TypedText_ReadsDigitsAsCents(string text, double expected)
    {
        MoneyConverter.Parse(text).Should().Be((decimal)expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("R$ ,")]
    public void Parse_NoDigits_ReturnsZero(string? text)
    {
        MoneyConverter.Parse(text).Should().Be(0m);
    }

    [Fact]
    public void Parse_TooManyDigits_DoesNotOverflow()
    {
        var act = () => MoneyConverter.Parse(new string('9', 40));

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("1", "R$ 0,01")]
    [InlineData("R$ 0,015", "R$ 0,15")]
    [InlineData("R$ 1,50", "R$ 1,50")]
    [InlineData("R$ 1,5", "R$ 0,15")]
    [InlineData("123456", "R$ 1.234,56")]
    public void Mask_TypedText_ReformatsAsCurrency(string text, string expected)
    {
        MoneyConverter.Mask(text).Should().Be(expected);
    }

    [Fact]
    public void Mask_EmptyText_StaysEmpty()
    {
        MoneyConverter.Mask(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void Convert_Decimal_ReturnsFormattedAmount()
    {
        Convert(1500m).Should().Be("R$ 1.500,00");
    }

    [Fact]
    public void Convert_NonDecimalValue_ReturnsEmpty()
    {
        Convert(null).Should().Be(string.Empty);
    }

    private static object? Convert(object? value) =>
        MoneyConverter.Instance.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);
}
