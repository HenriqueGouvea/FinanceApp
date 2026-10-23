using FinanceApp.Models;
using System.Globalization;

namespace FinanceApp.Converters;

public class RecurrenceTypeToLabelConverter : IValueConverter
{
    public static readonly RecurrenceTypeToLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        RecurrenceType.OneTime => "Única",
        RecurrenceType.Recurrent => "Todo mês (recorrente)",
        RecurrenceType.Installments => "Parcelada",
        _ => string.Empty
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
