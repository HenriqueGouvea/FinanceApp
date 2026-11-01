using FinanceApp.Models;
using System.Globalization;

namespace FinanceApp.Converters;

public class PaymentSourceTypeToLabelConverter : IValueConverter
{
    public static readonly PaymentSourceTypeToLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        PaymentSourceType.BankAccount => "Conta bancária",
        PaymentSourceType.CreditCard => "Cartão de crédito",
        PaymentSourceType.MealVoucher => "Vale-refeição",
        _ => string.Empty
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
