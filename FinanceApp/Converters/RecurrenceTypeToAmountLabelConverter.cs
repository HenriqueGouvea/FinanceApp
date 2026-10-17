using FinanceApp.Models;
using System.Globalization;

namespace FinanceApp.Converters;

public class RecurrenceTypeToAmountLabelConverter : IValueConverter
{
    public static readonly RecurrenceTypeToAmountLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        RecurrenceType.OneTime => "Valor total",
        RecurrenceType.Recurrent => "Valor mensal",
        RecurrenceType.Installments => "Valor de cada parcela",
        _ => "Valor"
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
