using SQLite;

namespace FinanceApp.UITests.Infrastructure;

/// <summary>
/// Writes a test's starting data straight into the app's database before it starts (see <see cref="E2ETestBase"/>), for tests
/// whose arrange is just "these entries already exist". Creating them through the UI costs ~10 s each.
/// Rows are written with plain SQL so the app's models stay out of the black-box suite, like <see cref="TableRows"/>.
/// </summary>
public sealed class SeedData
{
    private readonly SQLiteConnection _db;

    public SeedData(SQLiteConnection db)
    {
        _db = db;
    }

    /// <summary>
    /// Id of a category seeded on first run (e.g. "Moradia").
    /// </summary>
    public int CategoryId(string name) =>
        _db.ExecuteScalar<int>("SELECT Id FROM FinancialCategory WHERE Name = ?", name);

    public int AddPaymentSource(string name, int type, decimal balance, int? billDueDay = null, int? billPaymentSourceId = null)
    {
        _db.Execute(
            "INSERT INTO PaymentSource (Name, Type, Balance, BillDueDay, BillPaymentSourceId) VALUES (?, ?, ?, ?, ?)",
            name, type, balance, billDueDay, billPaymentSourceId);
        return LastId();
    }

    /// <summary>
    /// Adds an entry starting in <paramref name="startMonth"/> (the current month by default), recurrent unless told otherwise.
    /// </summary>
    public int AddEntry(
        string description,
        decimal amount,
        int recurrence = AppFormat.Recurrent,
        bool isIncome = false,
        DateTime? startMonth = null,
        int? installments = null,
        int? dueDay = null,
        bool isEstimated = false,
        bool allowsPartialPayment = false,
        int? categoryId = null,
        int? paymentSourceId = null)
    {
        var month = startMonth ?? AppFormat.CurrentMonth;
        _db.Execute(
            "INSERT INTO FinancialEntry (Description, Amount, StartDate, Recurrence, EntryType, TotalInstallments, CategoryId, " +
            "PaymentSourceId, IsEstimated, AllowsPartialPayment, DueDay, Person, IsPJ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, '', 0)",
            description, amount, new DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Local), recurrence,
            isIncome ? AppFormat.Income : AppFormat.Outcome, installments, categoryId, paymentSourceId,
            isEstimated, allowsPartialPayment, dueDay);
        return LastId();
    }

    /// <summary>
    /// Records a partial payment dated today on the entry's current month, as "Pagar parcial" does.
    /// </summary>
    public void AddPayment(int entryId, decimal amount, string description = "")
    {
        var month = AppFormat.CurrentMonth;
        var monthlyId = _db.ExecuteScalar<int>(
            "SELECT Id FROM FinancialMonthlyEntry WHERE FinancialEntryId = ? AND Year = ? AND Month = ?",
            entryId, month.Year, month.Month);
        if (monthlyId == 0)
        {
            _db.Execute(
                "INSERT INTO FinancialMonthlyEntry (FinancialEntryId, Year, Month, Status, PaidAmount, IsCancelled) VALUES (?, ?, ?, ?, 0, 0)",
                entryId, month.Year, month.Month, AppFormat.PartiallyPaid);
            monthlyId = LastId();
        }

        _db.Execute("UPDATE FinancialMonthlyEntry SET PaidAmount = PaidAmount + ? WHERE Id = ?", amount, monthlyId);
        _db.Execute(
            "INSERT INTO FinancialPayment (FinancialMonthlyEntryId, Amount, Date, Description) VALUES (?, ?, ?, ?)",
            monthlyId, amount, DateTime.Today, description);
    }

    private int LastId() => _db.ExecuteScalar<int>("SELECT last_insert_rowid()");
}
