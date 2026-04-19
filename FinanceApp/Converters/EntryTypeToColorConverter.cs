using FinanceApp.Models;
using System.Globalization;

namespace FinanceApp.Converters;

public class EntryTypeToColorConverter : IValueConverter
{
    public static readonly EntryTypeToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is FinancialEntryType.Income ? Colors.Green : Colors.Red;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
