using FinanceApp.Models;
using System.Globalization;

namespace FinanceApp.Converters;

public class EntryTypeToPaidLabelConverter : IValueConverter
{
    public static readonly EntryTypeToPaidLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is FinancialEntryType.Income ? "Recebido" : "Pago";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
