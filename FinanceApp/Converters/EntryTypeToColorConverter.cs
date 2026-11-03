using FinanceApp.Models;
using System.Globalization;

namespace FinanceApp.Converters;

public class EntryTypeToColorConverter : IValueConverter
{
    public static readonly EntryTypeToColorConverter Instance = new();

    // Mirror the Income / Outcome brand colors in Resources/Styles/Colors.xaml.
    public static readonly Color IncomeColor = Color.FromArgb("#1F8F63");
    public static readonly Color OutcomeColor = Color.FromArgb("#C2414B");

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is FinancialEntryType.Income ? IncomeColor : OutcomeColor;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
