using FinanceApp.Converters;
using FinanceApp.ViewModels;
using FluentAssertions;
using System.Globalization;
using Xunit;

namespace FinanceApp.Tests.Converters;

public class DueDateConverterTests
{
    [Theory]
    [InlineData(1, "Dia 1")]
    [InlineData(15, "Dia 15")]
    [InlineData(31, "Dia 31")]
    public void DueDayToLabel_Day_ReturnsDayLabel(int day, string expected)
    {
        ConvertDueDay(day).Should().Be(expected);
    }

    [Fact]
    public void DueDayToLabel_NoDueDay_ReturnsNoDueDateLabel()
    {
        ConvertDueDay(AddEntryViewModel.NoDueDay).Should().Be("Sem vencimento");
    }

    [Fact]
    public void DueDateToLabel_Today_ReturnsDueToday()
    {
        ConvertDueDate(DateTime.Today).Should().Be("Vence hoje");
    }

    [Fact]
    public void DueDateToLabel_OtherDay_ReturnsDayAndMonth()
    {
        var date = new DateTime(2020, 11, 5, 0, 0, 0, DateTimeKind.Local);

        ConvertDueDate(date).Should().Be("Vence 05/11");
    }

    [Fact]
    public void DueDateToLabel_NoDate_ReturnsEmpty()
    {
        ConvertDueDate(null).Should().Be(string.Empty);
    }

    private static object? ConvertDueDay(object? value) =>
        DueDayToLabelConverter.Instance.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

    private static object? ConvertDueDate(object? value) =>
        DueDateToLabelConverter.Instance.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);
}
