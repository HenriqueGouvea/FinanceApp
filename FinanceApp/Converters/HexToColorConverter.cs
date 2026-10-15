using System.Globalization;

namespace FinanceApp.Converters;

public class HexToColorConverter : IValueConverter
{
    public static readonly HexToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string hex && !string.IsNullOrWhiteSpace(hex) ? Color.FromArgb(hex) : Colors.Gray;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
