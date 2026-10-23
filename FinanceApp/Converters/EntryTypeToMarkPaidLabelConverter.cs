using FinanceApp.Models;
using System.Globalization;

namespace FinanceApp.Converters;

public class EntryTypeToMarkPaidLabelConverter : IValueConverter
{
    public static readonly EntryTypeToMarkPaidLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is FinancialEntryType.Income ? "Receber" : "Pagar";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
