using FinanceApp.Data;
using FinanceApp.Models;

namespace FinanceApp.Services;

public class SetupService : ISetupService
{
    private const string CompletedKey = "SetupCompleted";

    private readonly IDatabaseContext _context;
    private readonly IFinanceService _financeService;
    private readonly ICategoryService _categoryService;
    private readonly IPaymentSourceService _paymentSourceService;
    private readonly IUserProfileService _profileService;
    private readonly IPreferencesService _preferences;
    private readonly TimeProvider _timeProvider;

    public SetupService(
        IDatabaseContext context,
        IFinanceService financeService,
        ICategoryService categoryService,
        IPaymentSourceService paymentSourceService,
        IUserProfileService profileService,
        IPreferencesService preferences,
        TimeProvider timeProvider)
    {
        _context = context;
        _financeService = financeService;
        _categoryService = categoryService;
        _paymentSourceService = paymentSourceService;
        _profileService = profileService;
        _preferences = preferences;
        _timeProvider = timeProvider;
    }

    public async Task<bool> ShouldShowAsync()
    {
        if (_preferences.Get(CompletedKey, false))
            return false;

        var db = await _context.GetConnectionAsync();
        var hasData = await db.Table<FinancialEntry>().CountAsync() > 0
            || await db.Table<PaymentSource>().CountAsync() > 0;
        if (hasData)
            Skip();

        return !hasData;
    }

    public async Task CompleteAsync(NewUserSetup setup)
    {
        await _profileService.SaveAsync(setup.Name, setup.PhotoPath);

        foreach (var source in setup.PaymentSources)
            await _paymentSourceService.SaveAsync(source);

        foreach (var item in setup.CreditCards)
        {
            item.Card.Type = PaymentSourceType.CreditCard;
            item.Card.BillPaymentSourceId = item.BillAccount?.Id;
            await _paymentSourceService.SaveAsync(item.Card);
        }

        var categories = await _categoryService.GetAllAsync();
        var now = _timeProvider.GetLocalNow();
        var startDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Local);

        foreach (var item in setup.Entries.Where(e => e.Amount > 0))
        {
            var category = categories.FirstOrDefault(c => c.EntryType == item.EntryType && c.Name == item.CategoryName);
            var totalInstallments = item.Recurrence == RecurrenceType.Installments ? Math.Max(item.TotalInstallments ?? 1, 1) : (int?)null;
            var currentInstallment = totalInstallments is { } total ? Math.Clamp(item.CurrentInstallment ?? 1, 1, total) : 1;
            // A credit card purchase is paid with the bill, so it is never estimated nor paid little by little.
            var isOnCardBill = item.EntryType == FinancialEntryType.Outcome && item.PaymentSource?.Type == PaymentSourceType.CreditCard;

            await _financeService.SaveEntryAsync(new FinancialEntry
            {
                Description = item.Description.Trim(),
                Amount = item.Amount,
                EntryType = item.EntryType,
                Recurrence = item.Recurrence,
                TotalInstallments = totalInstallments,
                StartDate = startDate.AddMonths(1 - currentInstallment),
                DueDay = item.DueDay,
                IsEstimated = item.IsEstimated && !isOnCardBill,
                AllowsPartialPayment = item.AllowsPartialPayment && !isOnCardBill,
                CategoryId = category?.Id,
                PaymentSourceId = item.PaymentSource?.Id
            });
        }

        Skip();
    }

    public void Skip() => _preferences.Set(CompletedKey, true);
}
