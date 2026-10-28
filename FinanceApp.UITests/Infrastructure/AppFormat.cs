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
    /// Progress line of the partial payment popup (e.g. "Já gasto: R$ 250,00 de R$ 800,00").
    /// </summary>
    public static string PartialPaymentProgress(string paidLabel, decimal paid, decimal total) =>
        $"{paidLabel}: {Money(paid)} de {Money(total)}";

    public static string PartialPaymentNewTotal(decimal total) => $"Novo total: {Money(total)}";

    /// <summary>
    /// Warning of the partial payment popup when the new total passes the planned amount (e.g. "▲ R$ 100,00 acima do previsto").
    /// </summary>
    public static string OverBudget(decimal excess) => $"▲ {Money(excess)} acima do previsto";

    /// <summary>
    /// Line under an entry's price once paid beyond its planned amount: ▲ and the planned amount, struck through.
    /// </summary>
    public static string StruckPlanned(decimal planned) => $"▲ {Money(planned)}";

    /// <summary>
    /// Date of a recorded payment in the partial payment popup (e.g. "27/10").
    /// </summary>
    public static string PaymentDate(DateTime date) => date.ToString("dd/MM", Culture);

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
