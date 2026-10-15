using FinanceApp.Converters;
using FinanceApp.Models;
using FluentAssertions;
using System.Globalization;
using Xunit;

namespace FinanceApp.Tests.Converters;

public class RecurrenceTypeToLabelConverterTests
{
    [Theory]
    [InlineData(RecurrenceType.OneTime, "Única")]
    [InlineData(RecurrenceType.Recurrent, "Recorrente")]
    [InlineData(RecurrenceType.Installments, "Parcelada")]
    public void Convert_RecurrenceType_ReturnsPortugueseLabel(RecurrenceType recurrence, string expected)
    {
        Convert(recurrence).Should().Be(expected);
    }

    [Fact]
    public void Convert_NonRecurrenceValue_ReturnsEmpty()
    {
        Convert(null).Should().Be(string.Empty);
    }

    private static object? Convert(object? value) =>
        RecurrenceTypeToLabelConverter.Instance.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);
}
