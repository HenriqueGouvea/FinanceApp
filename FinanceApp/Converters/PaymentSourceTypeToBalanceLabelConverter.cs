using FinanceApp.Models;
using System.Globalization;

namespace FinanceApp.Converters;

public class PaymentSourceTypeToBalanceLabelConverter : IValueConverter
{
    public static readonly PaymentSourceTypeToBalanceLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        PaymentSourceType.BankAccount => "Saldo",
        PaymentSourceType.CreditCard => "Limite",
        PaymentSourceType.MealVoucher => "Saldo",
        _ => string.Empty
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
