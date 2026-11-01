using FinanceApp.Models;
using System.Globalization;

namespace FinanceApp.Converters;

public class PaymentSourceTypeToIconConverter : IValueConverter
{
    public static readonly PaymentSourceTypeToIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        PaymentSourceType.BankAccount => "🏦",
        PaymentSourceType.CreditCard => "💳",
        PaymentSourceType.MealVoucher => "🍽️",
        _ => string.Empty
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
