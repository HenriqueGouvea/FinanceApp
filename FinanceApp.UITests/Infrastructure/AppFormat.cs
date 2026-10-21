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

    private static readonly CultureInfo Culture = new("pt-BR");

    public static DateTime CurrentMonth => new(DateTime.Today.Year, DateTime.Today.Month, 1);

    public static string Money(decimal value) => $"R$ {value.ToString("F2", Culture)}";

    public static string EntryCount(int count) => $"{count} lançamento(s)";

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
