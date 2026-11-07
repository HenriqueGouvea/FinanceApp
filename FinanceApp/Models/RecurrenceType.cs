namespace FinanceApp.Models;

/// <summary>
/// How an entry repeats. <see cref="Annual"/> falls in the same month(s) every year (e.g. IPVA, IPTU): its
/// <see cref="FinancialEntry.TotalInstallments"/> are the installments within each year, in consecutive months from its
/// start month (one when not set).
/// </summary>
public enum RecurrenceType { OneTime = 0, Recurrent = 1, Installments = 2, Annual = 3 }
