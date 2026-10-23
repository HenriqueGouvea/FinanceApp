using System.Globalization;

namespace FinanceApp.Converters;

/// <summary>
/// Formats amounts as Brazilian currency (e.g. "R$ 1.000,00"). <see cref="Parse"/> reads typed digits
/// as cents, which gives the amount entry its banking-app mask: typing 1, 5, 0 gives "R$ 1,50".
/// </summary>
public class MoneyConverter : IValueConverter
{
    public static readonly MoneyConverter Instance = new();

    // Keeps the parsed cents within the range of a long.
    private const int MaxDigits = 15;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        decimal amount => Format(amount),
        _ => string.Empty
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();

    public static string Format(decimal amount)
    {
        var formatted = $"R$ {Math.Abs(amount).ToString("N2", AppCulture.Default)}";
        return amount < 0 ? $"-{formatted}" : formatted;
    }

    public static decimal Parse(string? text)
    {
        var digits = new string((text ?? string.Empty).Where(char.IsAsciiDigit).ToArray()).TrimStart('0');
        if (digits.Length == 0)
            return 0;

        if (digits.Length > MaxDigits)
            digits = digits[..MaxDigits];

        return long.Parse(digits, CultureInfo.InvariantCulture) / 100m;
    }

    /// <summary>
    /// Reformats free text typed into an amount entry. Empty text stays empty so the placeholder shows.
    /// </summary>
    public static string Mask(string? text) => string.IsNullOrEmpty(text) ? string.Empty : Format(Parse(text));
}
