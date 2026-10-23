using FinanceApp.Converters;
using FinanceApp.Models;
using FluentAssertions;
using System.Globalization;
using Xunit;

namespace FinanceApp.Tests.Converters;

public class EntryTypeStatusLabelConverterTests
{
    [Theory]
    [InlineData(FinancialEntryType.Outcome, "Pago")]
    [InlineData(FinancialEntryType.Income, "Recebido")]
    public void PaidLabel_Convert_EntryType_ReturnsPortugueseLabel(FinancialEntryType entryType, string expected)
    {
        EntryTypeToPaidLabelConverter.Instance.Convert(entryType, typeof(string), null, CultureInfo.InvariantCulture)
            .Should().Be(expected);
    }

    [Theory]
    [InlineData(FinancialEntryType.Outcome, "Pagar")]
    [InlineData(FinancialEntryType.Income, "Receber")]
    public void MarkPaidLabel_Convert_EntryType_ReturnsPortugueseAction(FinancialEntryType entryType, string expected)
    {
        EntryTypeToMarkPaidLabelConverter.Instance.Convert(entryType, typeof(string), null, CultureInfo.InvariantCulture)
            .Should().Be(expected);
    }
}
