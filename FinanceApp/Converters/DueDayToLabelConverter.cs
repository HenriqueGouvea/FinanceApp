using System.Globalization;

namespace FinanceApp.Converters;

/// <summary>
/// Labels the due day options of the entry popup: <see cref="ViewModels.AddEntryViewModel.NoDueDay"/> is "Sem vencimento".
/// </summary>
public class DueDayToLabelConverter : IValueConverter
{
    public static readonly DueDayToLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        int day and > 0 => $"Dia {day}",
        _ => "Sem vencimento"
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
