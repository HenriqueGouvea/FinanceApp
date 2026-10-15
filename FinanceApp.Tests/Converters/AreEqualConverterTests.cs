using FinanceApp.Converters;
using FluentAssertions;
using System.Globalization;
using Xunit;

namespace FinanceApp.Tests.Converters;

public class AreEqualConverterTests
{
    [Fact]
    public void Convert_EqualValues_ReturnsTrue()
    {
        Convert(7, (int?)7).Should().Be(true);
    }

    [Fact]
    public void Convert_DifferentValues_ReturnsFalse()
    {
        Convert(7, (int?)8).Should().Be(false);
    }

    [Fact]
    public void Convert_ValueAndNull_ReturnsFalse()
    {
        Convert(7, null).Should().Be(false);
    }

    [Fact]
    public void Convert_BothNull_ReturnsFalse()
    {
        Convert(null, null).Should().Be(false);
    }

    private static object Convert(object? first, object? second) =>
        AreEqualConverter.Instance.Convert([first!, second!], typeof(bool), null, CultureInfo.InvariantCulture);
}
