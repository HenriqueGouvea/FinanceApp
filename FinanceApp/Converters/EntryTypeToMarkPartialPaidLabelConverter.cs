using FinanceApp.Models;
using System.Globalization;

namespace FinanceApp.Converters;

public class EntryTypeToMarkPartialPaidLabelConverter : IValueConverter
{
    public static readonly EntryTypeToMarkPartialPaidLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is FinancialEntryType.Income ? "Receber parcial" : "Pagar parcial";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
