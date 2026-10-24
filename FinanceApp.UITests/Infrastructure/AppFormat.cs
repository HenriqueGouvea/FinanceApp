using System.Globalization;

namespace FinanceApp.UITests.Infrastructure;

/// <summary>
/// Reproduces how the app (pt-BR) renders values, so expectations can be computed instead of hardcoded.
/// </summary>
public static class AppFormat
{
    public const int Income = 0;
    public const int Outcome = 1;

    public const int OneTime = 0;
    public const int Recurrent = 1;
    public const int Installments = 2;

    public const int Pending = 0;
    public const int Paid = 1;
    public const int PartiallyPaid = 2;

    private static readonly CultureInfo Culture = new("pt-BR");

    public static DateTime CurrentMonth => new(DateTime.Today.Year, DateTime.Today.Month, 1);

    public static string Money(decimal value)
    {
        var formatted = $"R$ {Math.Abs(value).ToString("N2", Culture)}";
        return value < 0 ? $"-{formatted}" : formatted;
    }

    public static string EntryCount(int count) => $"{count} lançamento(s)";

    /// <summary>
    /// Partial payment line of an entry row (e.g. "Pago R$ 250,00 · falta R$ 550,00").
    /// </summary>
    public static string PartialPayment(string paidLabel, decimal paid, decimal pending) =>
        $"{paidLabel} {Money(paid)} · falta {Money(pending)}";

    public static string MonthTitle(DateTime month)
    {
        var name = month.ToString("MMMM yyyy", Culture);
        return Culture.TextInfo.ToUpper(name[0]) + name[1..];
    }

    /// <summary>
    /// Month label used by the month picker grid (e.g. "Out").
    /// </summary>
    public static string MonthAbbreviation(DateTime month)
    {
        var name = Culture.DateTimeFormat.GetAbbreviatedMonthName(month.Month).TrimEnd('.');
        return Culture.TextInfo.ToUpper(name[0]) + name[1..];
    }
}
