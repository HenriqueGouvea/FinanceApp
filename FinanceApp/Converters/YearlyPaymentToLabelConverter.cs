using System.Globalization;

namespace FinanceApp.Converters;

/// <summary>
/// Labels how a yearly expense, like the car insurance, is paid: "Todo mês", "À vista" (once a year) or "10x".
/// </summary>
public class YearlyPaymentToLabelConverter : IValueConverter
{
    public static readonly YearlyPaymentToLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        1 => "À vista",
        int count and > 1 => $"{count}x",
        _ => "Todo mês"
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
