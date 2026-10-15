using System.Globalization;

namespace FinanceApp.Converters;

public class AreEqualConverter : IMultiValueConverter
{
    public static readonly AreEqualConverter Instance = new();

    // Null never matches: bindings are still unresolved (null) while an item is being created.
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        => values.Length == 2 && values[0] is not null && Equals(values[0], values[1]);

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
