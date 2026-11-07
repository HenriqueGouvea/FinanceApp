using System.Globalization;

namespace FinanceApp.Converters;

/// <summary>
/// Names a month number (1–12) in pt-BR, capitalized: 2 is "Fevereiro".
/// </summary>
public class MonthNumberToNameConverter : IValueConverter
{
    public static readonly MonthNumberToNameConverter Instance = new();

    private static readonly CultureInfo PtBr = new("pt-BR");

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int month || month is < 1 or > 12)
            return string.Empty;

        var name = PtBr.DateTimeFormat.GetMonthName(month);
        return char.ToUpper(name[0], PtBr) + name[1..];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
