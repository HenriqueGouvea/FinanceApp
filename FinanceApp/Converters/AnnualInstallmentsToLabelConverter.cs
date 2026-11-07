using System.Globalization;

namespace FinanceApp.Converters;

/// <summary>
/// Labels how a yearly tax is paid: 1 is "Cota única", more are "3 parcelas".
/// </summary>
public class AnnualInstallmentsToLabelConverter : IValueConverter
{
    public static readonly AnnualInstallmentsToLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        int count and > 1 => $"{count} parcelas",
        _ => "Cota única"
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
