using System.Globalization;

namespace FinanceApp.Converters;

/// <summary>
/// Due date line of an entry row: "Vence hoje" on the day, otherwise the day and month (e.g. "Vence 05/11").
/// </summary>
public class DueDateToLabelConverter : IValueConverter
{
    public static readonly DueDateToLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        DateTime date when date.Date == DateTime.Today => "Vence hoje",
        DateTime date => $"Vence {date.ToString("dd/MM", AppCulture.Default)}",
        _ => string.Empty
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
