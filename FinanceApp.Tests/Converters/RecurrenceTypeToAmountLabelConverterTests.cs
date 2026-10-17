using FinanceApp.Converters;
using FinanceApp.Models;
using FluentAssertions;
using System.Globalization;
using Xunit;

namespace FinanceApp.Tests.Converters;

public class RecurrenceTypeToAmountLabelConverterTests
{
    [Theory]
    [InlineData(RecurrenceType.OneTime, "Valor total")]
    [InlineData(RecurrenceType.Recurrent, "Valor mensal")]
    [InlineData(RecurrenceType.Installments, "Valor de cada parcela")]
    public void Convert_RecurrenceType_ReturnsAmountLabel(RecurrenceType recurrence, string expected)
    {
        Convert(recurrence).Should().Be(expected);
    }

    [Fact]
    public void Convert_NullRecurrence_ReturnsGenericLabel()
    {
        Convert(null).Should().Be("Valor");
    }

    private static object? Convert(object? value) =>
        RecurrenceTypeToAmountLabelConverter.Instance.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);
}
