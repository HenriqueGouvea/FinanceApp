using System.Globalization;

namespace FinanceApp.Converters;

public class IsNullConverter : IValueConverter
{
    public static readonly IsNullConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
