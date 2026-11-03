using FinanceApp.Converters;
using FinanceApp.Models;
using FluentAssertions;
using System.Globalization;
using Xunit;

namespace FinanceApp.Tests.Converters;

public class EntryTypeToColorConverterTests
{
    [Theory]
    [InlineData(FinancialEntryType.Income, "#FF1F8F63")]
    [InlineData(FinancialEntryType.Outcome, "#FFC2414B")]
    public void Convert_EntryType_ReturnsBrandColor(FinancialEntryType type, string expected)
    {
        var color = (Color)EntryTypeToColorConverter.Instance.Convert(type, typeof(Color), null, CultureInfo.InvariantCulture)!;

        color.ToArgbHex(includeAlpha: true).Should().Be(expected);
    }
}
