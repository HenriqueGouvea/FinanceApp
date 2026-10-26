using FinanceApp.Models;
using System.Globalization;

namespace FinanceApp.Converters;

/// <summary>
/// Suffix of a partially paid entry's amount, e.g. "R$ 250,00 já gastos".
/// </summary>
public class EntryTypeToPartialPaidLabelConverter : IValueConverter
{
    public static readonly EntryTypeToPartialPaidLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is FinancialEntryType.Income ? "já recebidos" : "já gastos";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
