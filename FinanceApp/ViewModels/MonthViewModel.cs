using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Models;
using System.Collections.ObjectModel;

namespace FinanceApp.ViewModels;

public partial class MonthViewModel : ObservableObject
{
    private readonly HashSet<int> _collapsedBillCardIds = [];
    private bool _isReplacingEntries;
    private MonthContent _content = MonthContent.Empty;
    private IReadOnlyList<CreditCardBill> _creditCardBills = [];

    public ObservableCollection<MonthlyEntryProjection> Entries { get; } = [];

    [ObservableProperty]
    string monthName = string.Empty;

    [ObservableProperty]
    bool isLoaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CollapsedSectionsHint))]
    bool isOutcomesExpanded = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CollapsedSectionsHint))]
    bool isIncomesExpanded = true;

    /// <summary>
    /// Tells how to show a collapsed section's entries again, or null while both sections are expanded.
    /// </summary>
    public string? CollapsedSectionsHint => (IsOutcomesExpanded, IsIncomesExpanded) switch
    {
        (false, false) => "Toque em “Despesas” ou “Receitas” para mostrar os lançamentos de novo.",
        (false, true) => "Toque em “Despesas” para mostrar os lançamentos de novo.",
        (true, false) => "Toque em “Receitas” para mostrar os lançamentos de novo.",
        _ => null
    };

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

    /// <summary>
    /// The outcomes listed on their own: those paid with a credit card are listed under the card's bill instead.
    /// </summary>
    public IReadOnlyList<MonthlyEntryProjection> OutcomeEntries => _content.OutcomeEntries;

    public IReadOnlyList<MonthlyEntryProjection> IncomeEntries => _content.IncomeEntries;

    /// <summary>
    /// One bill per credit card used this month. Its entries are already counted in the outcome totals.
    /// </summary>
    public IReadOnlyList<CreditCardBill> CreditCardBills => _creditCardBills;

    public decimal TotalIncomes => _content.TotalIncomes;
    public decimal TotalOutcomes => _content.TotalOutcomes;
    public decimal Balance => TotalIncomes - TotalOutcomes;
    public decimal ReceivedIncomes => _content.ReceivedIncomes;
    public decimal PendingIncomes => _content.PendingIncomes;
    public decimal PaidOutcomes => _content.PaidOutcomes;
    public decimal PendingOutcomes => _content.PendingOutcomes;
    public int OutcomeCount => _content.OutcomeCount;
    public int IncomeCount => _content.IncomeCount;

    public MonthViewModel()
    {
        Entries.CollectionChanged += (_, _) =>
        {
            if (!_isReplacingEntries)
                Apply(MonthContent.Build(Entries));
        };
    }

    /// <summary>
    /// Swaps the month's entries for a freshly loaded list.
    /// </summary>
    public void ReplaceEntries(IEnumerable<MonthlyEntryProjection> entries) => ReplaceEntries(MonthContent.Build(entries));

    /// <summary>
    /// Swaps in a month built ahead (off the UI thread). Each list changed raises one notification, which rebuilds its
    /// rows on screen, and a list that didn't change raises none, so reloading an unchanged month costs nothing.
    /// </summary>
    public void ReplaceEntries(MonthContent content)
    {
        _isReplacingEntries = true;
        try
        {
            Entries.Clear();
            foreach (var entry in content.Entries)
                Entries.Add(entry);
        }
        finally
        {
            _isReplacingEntries = false;
        }

        Apply(content);
    }

    private void Apply(MonthContent content)
    {
        var previous = _content;
        _content = content;

        OnPropertyChanged(nameof(TotalIncomes));
        OnPropertyChanged(nameof(TotalOutcomes));
        OnPropertyChanged(nameof(Balance));
        OnPropertyChanged(nameof(ReceivedIncomes));
        OnPropertyChanged(nameof(PendingIncomes));
        OnPropertyChanged(nameof(PaidOutcomes));
        OnPropertyChanged(nameof(PendingOutcomes));
        OnPropertyChanged(nameof(OutcomeCount));
        OnPropertyChanged(nameof(IncomeCount));

        if (!content.OutcomeEntries.SequenceEqual(previous.OutcomeEntries))
            OnPropertyChanged(nameof(OutcomeEntries));
        if (!content.IncomeEntries.SequenceEqual(previous.IncomeEntries))
            OnPropertyChanged(nameof(IncomeEntries));
        UpdateCreditCardBills();
    }

    private void UpdateCreditCardBills()
    {
        IReadOnlyList<CreditCardBill> bills =
            [.. _content.CreditCardBills.Select(bill => _collapsedBillCardIds.Contains(bill.Card.Id) ? bill with { IsExpanded = false } : bill)];
        if (MonthContent.SameBills(bills, _creditCardBills))
            return;

        _creditCardBills = bills;
        OnPropertyChanged(nameof(CreditCardBills));
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

        UpdateCreditCardBills();
    }

    [RelayCommand]
    public void HideEntryActions()
    {
        RevealedEntryId = null;
        RevealedBillCardId = null;
    }
}
