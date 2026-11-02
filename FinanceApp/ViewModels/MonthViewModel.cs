using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Models;
using System.Collections.ObjectModel;

namespace FinanceApp.ViewModels;

public partial class MonthViewModel : ObservableObject
{
    private readonly HashSet<int> _collapsedBillCardIds = [];

    public ObservableCollection<MonthlyEntryProjection> Entries { get; } = [];

    [ObservableProperty]
    string monthName = string.Empty;

    [ObservableProperty]
    bool isLoaded;

    [ObservableProperty]
    bool isOutcomesExpanded = true;

    [ObservableProperty]
    bool isIncomesExpanded = true;

    [ObservableProperty]
    int? revealedEntryId;

    /// <summary>
    /// The credit card whose bill shows its actions menu.
    /// </summary>
    [ObservableProperty]
    int? revealedBillCardId;

    /// <summary>
    /// Page of the month summary: totals, paid/to pay or received/to receive.
    /// </summary>
    [ObservableProperty]
    int summaryPage;

    public DateTime Date { get; set; }

    private IEnumerable<MonthlyEntryProjection> AllOutcomes =>
        Entries.Where(e => e.EntryType == FinancialEntryType.Outcome);

    /// <summary>
    /// The outcomes listed on their own: those paid with a credit card are listed under the card's bill instead.
    /// </summary>
    public IEnumerable<MonthlyEntryProjection> OutcomeEntries =>
        WithAlternatingRows(AllOutcomes.Where(e => !e.IsOnCreditCardBill));

    public IEnumerable<MonthlyEntryProjection> IncomeEntries =>
        WithAlternatingRows(Entries.Where(e => e.EntryType == FinancialEntryType.Income));

    /// <summary>
    /// One bill per credit card used this month. Its entries are already counted in the outcome totals.
    /// </summary>
    public IReadOnlyList<CreditCardBill> CreditCardBills =>
        [.. CreditCardBill.GroupFrom(Entries, _collapsedBillCardIds)
            .Select(bill => bill with { Entries = [.. WithAlternatingRows(bill.Entries)] })];

    public decimal TotalIncomes => Entries
        .Where(e => e.EntryType == FinancialEntryType.Income)
        .Sum(e => e.EffectiveAmount);

    public decimal TotalOutcomes => AllOutcomes.Sum(e => e.EffectiveAmount);

    public decimal Balance => TotalIncomes - TotalOutcomes;

    public decimal ReceivedIncomes => IncomeEntries.Sum(e => e.PaidAmount);
    public decimal PendingIncomes => IncomeEntries.Sum(e => e.PendingAmount);
    public decimal PaidOutcomes => AllOutcomes.Sum(e => e.PaidAmount);
    public decimal PendingOutcomes => AllOutcomes.Sum(e => e.PendingAmount);

    public int OutcomeCount => AllOutcomes.Count();
    public int IncomeCount => IncomeEntries.Count();

    public MonthViewModel()
    {
        Entries.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(TotalIncomes));
            OnPropertyChanged(nameof(TotalOutcomes));
            OnPropertyChanged(nameof(Balance));
            OnPropertyChanged(nameof(ReceivedIncomes));
            OnPropertyChanged(nameof(PendingIncomes));
            OnPropertyChanged(nameof(PaidOutcomes));
            OnPropertyChanged(nameof(PendingOutcomes));
            OnPropertyChanged(nameof(OutcomeEntries));
            OnPropertyChanged(nameof(IncomeEntries));
            OnPropertyChanged(nameof(CreditCardBills));
            OnPropertyChanged(nameof(OutcomeCount));
            OnPropertyChanged(nameof(IncomeCount));
        };
    }

    [RelayCommand]
    void ToggleOutcomes()
    {
        HideEntryActions();
        IsOutcomesExpanded = !IsOutcomesExpanded;
    }

    [RelayCommand]
    void ToggleIncomes()
    {
        HideEntryActions();
        IsIncomesExpanded = !IsIncomesExpanded;
    }

    /// <summary>
    /// Shows or hides the entries listed under a credit card's bill.
    /// </summary>
    public void ToggleBill(int cardId)
    {
        HideEntryActions();

        if (!_collapsedBillCardIds.Remove(cardId))
            _collapsedBillCardIds.Add(cardId);

        OnPropertyChanged(nameof(CreditCardBills));
    }

    [RelayCommand]
    public void HideEntryActions()
    {
        RevealedEntryId = null;
        RevealedBillCardId = null;
    }

    private static IEnumerable<MonthlyEntryProjection> WithAlternatingRows(IEnumerable<MonthlyEntryProjection> entries) =>
        entries.Select((entry, index) => entry with { IsAlternateRow = index % 2 == 1 });
}
