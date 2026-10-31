using FinanceApp.Models;
using FinanceApp.ViewModels;
using FluentAssertions;
using Xunit;

namespace FinanceApp.Tests.ViewModels;

public class MonthViewModelTests
{
    private readonly MonthViewModel _vm;

    public MonthViewModelTests()
    {
        _vm = new MonthViewModel
        {
            MonthName = "April 2026",
            Date = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Local)
        };
    }

    // --- TotalIncomes / TotalOutcomes / Balance ---

    [Fact]
    public void TotalIncomes_WithNoEntries_ReturnsZero()
    {
        _vm.TotalIncomes.Should().Be(0m);
    }

    [Fact]
    public void TotalOutcomes_WithNoEntries_ReturnsZero()
    {
        _vm.TotalOutcomes.Should().Be(0m);
    }

    [Fact]
    public void Balance_WithNoEntries_ReturnsZero()
    {
        _vm.Balance.Should().Be(0m);
    }

    [Fact]
    public void TotalIncomes_WithMixedEntries_SumsOnlyIncomes()
    {
        _vm.Entries.Add(MakeProjection(1000m, FinancialEntryType.Income));
        _vm.Entries.Add(MakeProjection(500m, FinancialEntryType.Income));
        _vm.Entries.Add(MakeProjection(200m, FinancialEntryType.Outcome));

        _vm.TotalIncomes.Should().Be(1500m);
    }

    [Fact]
    public void TotalOutcomes_WithMixedEntries_SumsOnlyOutcomes()
    {
        _vm.Entries.Add(MakeProjection(300m, FinancialEntryType.Income));
        _vm.Entries.Add(MakeProjection(700m, FinancialEntryType.Outcome));
        _vm.Entries.Add(MakeProjection(150m, FinancialEntryType.Outcome));

        _vm.TotalOutcomes.Should().Be(850m);
    }

    [Fact]
    public void Balance_WhenOutcomesExceedIncomes_ReturnsNegativeValue()
    {
        _vm.Entries.Add(MakeProjection(500m, FinancialEntryType.Income));
        _vm.Entries.Add(MakeProjection(800m, FinancialEntryType.Outcome));

        _vm.Balance.Should().Be(-300m);
    }

    [Fact]
    public void Entries_WhenEntryAdded_RaisesPropertyChangedForComputedProperties()
    {
        var notified = new List<string>();
        _vm.PropertyChanged += (_, e) => notified.Add(e.PropertyName!);

        _vm.Entries.Add(MakeProjection(100m, FinancialEntryType.Income));

        notified.Should().Contain(nameof(MonthViewModel.TotalIncomes))
            .And.Contain(nameof(MonthViewModel.TotalOutcomes))
            .And.Contain(nameof(MonthViewModel.Balance));
    }

    // --- Filtered collections ---

    [Fact]
    public void OutcomeEntries_WithMixedEntries_ReturnsOnlyOutcomes()
    {
        _vm.Entries.Add(MakeProjection(1000m, FinancialEntryType.Income));
        _vm.Entries.Add(MakeProjection(500m, FinancialEntryType.Outcome));
        _vm.Entries.Add(MakeProjection(200m, FinancialEntryType.Outcome));

        _vm.OutcomeEntries.Should().HaveCount(2);
        _vm.OutcomeEntries.Should().AllSatisfy(e => e.EntryType.Should().Be(FinancialEntryType.Outcome));
    }

    [Fact]
    public void IncomeEntries_WithMixedEntries_ReturnsOnlyIncomes()
    {
        _vm.Entries.Add(MakeProjection(1000m, FinancialEntryType.Income));
        _vm.Entries.Add(MakeProjection(300m, FinancialEntryType.Income));
        _vm.Entries.Add(MakeProjection(500m, FinancialEntryType.Outcome));

        _vm.IncomeEntries.Should().HaveCount(2);
        _vm.IncomeEntries.Should().AllSatisfy(e => e.EntryType.Should().Be(FinancialEntryType.Income));
    }

    [Fact]
    public void OutcomeEntries_WithSeveralEntries_FlagsEveryOtherRowAsAlternate()
    {
        _vm.Entries.Add(MakeProjection(100m, FinancialEntryType.Outcome));
        _vm.Entries.Add(MakeProjection(200m, FinancialEntryType.Outcome));
        _vm.Entries.Add(MakeProjection(300m, FinancialEntryType.Outcome));
        _vm.Entries.Add(MakeProjection(400m, FinancialEntryType.Outcome));

        _vm.OutcomeEntries.Select(e => e.IsAlternateRow).Should().Equal(false, true, false, true);
        _vm.OutcomeEntries.Select(e => e.EffectiveAmount).Should().Equal(100m, 200m, 300m, 400m);
    }

    [Fact]
    public void IncomeEntries_InterleavedWithOutcomes_AlternatesWithinItsOwnSection()
    {
        _vm.Entries.Add(MakeProjection(100m, FinancialEntryType.Outcome));
        _vm.Entries.Add(MakeProjection(1000m, FinancialEntryType.Income));
        _vm.Entries.Add(MakeProjection(200m, FinancialEntryType.Outcome));
        _vm.Entries.Add(MakeProjection(2000m, FinancialEntryType.Income));

        _vm.IncomeEntries.Select(e => e.IsAlternateRow).Should().Equal(false, true);
        _vm.OutcomeEntries.Select(e => e.IsAlternateRow).Should().Equal(false, true);
    }

    [Fact]
    public void OutcomeEntries_WhenFirstEntryRemoved_RecomputesAlternation()
    {
        var first = MakeProjection(100m, FinancialEntryType.Outcome);
        _vm.Entries.Add(first);
        _vm.Entries.Add(MakeProjection(200m, FinancialEntryType.Outcome));
        _vm.Entries.Add(MakeProjection(300m, FinancialEntryType.Outcome));

        _vm.Entries.Remove(first);

        _vm.OutcomeEntries.Select(e => e.IsAlternateRow).Should().Equal(false, true);
    }

    [Fact]
    public void OutcomeEntries_WhenEntryAdded_RaisesPropertyChanged()
    {
        var notified = new List<string>();
        _vm.PropertyChanged += (_, e) => notified.Add(e.PropertyName!);

        _vm.Entries.Add(MakeProjection(500m, FinancialEntryType.Outcome));

        notified.Should().Contain(nameof(MonthViewModel.OutcomeEntries));
    }

    [Fact]
    public void IncomeEntries_WhenEntryAdded_RaisesPropertyChanged()
    {
        var notified = new List<string>();
        _vm.PropertyChanged += (_, e) => notified.Add(e.PropertyName!);

        _vm.Entries.Add(MakeProjection(1000m, FinancialEntryType.Income));

        notified.Should().Contain(nameof(MonthViewModel.IncomeEntries));
    }

    [Fact]
    public void OutcomeEntries_WithNoEntries_IsEmpty()
    {
        _vm.OutcomeEntries.Should().BeEmpty();
    }

    [Fact]
    public void IncomeEntries_WithNoEntries_IsEmpty()
    {
        _vm.IncomeEntries.Should().BeEmpty();
    }

    // --- Collapse state defaults ---

    [Fact]
    public void IsOutcomesExpanded_Default_IsTrue()
    {
        _vm.IsOutcomesExpanded.Should().BeTrue();
    }

    [Fact]
    public void IsIncomesExpanded_Default_IsTrue()
    {
        _vm.IsIncomesExpanded.Should().BeTrue();
    }

    // --- Toggle commands ---

    [Fact]
    public void ToggleOutcomesCommand_WhenExpanded_CollapsesSection()
    {
        _vm.IsOutcomesExpanded = true;

        _vm.ToggleOutcomesCommand.Execute(null);

        _vm.IsOutcomesExpanded.Should().BeFalse();
    }

    [Fact]
    public void ToggleOutcomesCommand_WhenCollapsed_ExpandsSection()
    {
        _vm.IsOutcomesExpanded = false;

        _vm.ToggleOutcomesCommand.Execute(null);

        _vm.IsOutcomesExpanded.Should().BeTrue();
    }

    [Fact]
    public void ToggleIncomesCommand_WhenExpanded_CollapsesSection()
    {
        _vm.IsIncomesExpanded = true;

        _vm.ToggleIncomesCommand.Execute(null);

        _vm.IsIncomesExpanded.Should().BeFalse();
    }

    [Fact]
    public void ToggleIncomesCommand_WhenCollapsed_ExpandsSection()
    {
        _vm.IsIncomesExpanded = false;

        _vm.ToggleIncomesCommand.Execute(null);

        _vm.IsIncomesExpanded.Should().BeTrue();
    }

    [Fact]
    public void ToggleOutcomesCommand_WithRevealedEntry_HidesEntryActions()
    {
        _vm.RevealedEntryId = 7;

        _vm.ToggleOutcomesCommand.Execute(null);

        _vm.RevealedEntryId.Should().BeNull();
    }

    [Fact]
    public void ToggleIncomesCommand_WithRevealedEntry_HidesEntryActions()
    {
        _vm.RevealedEntryId = 7;

        _vm.ToggleIncomesCommand.Execute(null);

        _vm.RevealedEntryId.Should().BeNull();
    }

    // --- Paid / pending summary ---

    [Fact]
    public void PaidAndPendingTotals_WithNoEntries_ReturnZero()
    {
        _vm.ReceivedIncomes.Should().Be(0m);
        _vm.PendingIncomes.Should().Be(0m);
        _vm.PaidOutcomes.Should().Be(0m);
        _vm.PendingOutcomes.Should().Be(0m);
    }

    [Fact]
    public void PaidOutcomes_WithMixedStatuses_SumsPaidAndPartialAmountsOfOutcomesOnly()
    {
        _vm.Entries.Add(MakeProjection(300m, FinancialEntryType.Outcome, FinancialMonthlyEntryStatus.Paid, paidAmount: 300m));
        _vm.Entries.Add(MakeProjection(800m, FinancialEntryType.Outcome, FinancialMonthlyEntryStatus.PartiallyPaid, paidAmount: 250m));
        _vm.Entries.Add(MakeProjection(100m, FinancialEntryType.Outcome));
        _vm.Entries.Add(MakeProjection(5000m, FinancialEntryType.Income, FinancialMonthlyEntryStatus.Paid, paidAmount: 5000m));

        _vm.PaidOutcomes.Should().Be(550m);
        _vm.PendingOutcomes.Should().Be(650m);
    }

    [Fact]
    public void ReceivedIncomes_WithMixedStatuses_SumsReceivedAndPendingOfIncomesOnly()
    {
        _vm.Entries.Add(MakeProjection(5000m, FinancialEntryType.Income, FinancialMonthlyEntryStatus.Paid, paidAmount: 5000m));
        _vm.Entries.Add(MakeProjection(1000m, FinancialEntryType.Income, FinancialMonthlyEntryStatus.PartiallyPaid, paidAmount: 400m));
        _vm.Entries.Add(MakeProjection(200m, FinancialEntryType.Income));
        _vm.Entries.Add(MakeProjection(300m, FinancialEntryType.Outcome, FinancialMonthlyEntryStatus.Paid, paidAmount: 300m));

        _vm.ReceivedIncomes.Should().Be(5400m);
        _vm.PendingIncomes.Should().Be(800m);
    }

    [Fact]
    public void PaidAndPendingTotals_AlwaysAddUpToSectionTotals()
    {
        _vm.Entries.Add(MakeProjection(800m, FinancialEntryType.Outcome, FinancialMonthlyEntryStatus.PartiallyPaid, paidAmount: 250m));
        _vm.Entries.Add(MakeProjection(100m, FinancialEntryType.Outcome, FinancialMonthlyEntryStatus.Paid, paidAmount: 100m));
        _vm.Entries.Add(MakeProjection(1000m, FinancialEntryType.Income));

        (_vm.PaidOutcomes + _vm.PendingOutcomes).Should().Be(_vm.TotalOutcomes);
        (_vm.ReceivedIncomes + _vm.PendingIncomes).Should().Be(_vm.TotalIncomes);
    }

    [Fact]
    public void Entries_WhenEntryAdded_RaisesPropertyChangedForPaidAndPendingTotals()
    {
        var notified = new List<string>();
        _vm.PropertyChanged += (_, e) => notified.Add(e.PropertyName!);

        _vm.Entries.Add(MakeProjection(100m, FinancialEntryType.Outcome));

        notified.Should().Contain(nameof(MonthViewModel.ReceivedIncomes))
            .And.Contain(nameof(MonthViewModel.PendingIncomes))
            .And.Contain(nameof(MonthViewModel.PaidOutcomes))
            .And.Contain(nameof(MonthViewModel.PendingOutcomes));
    }

    // --- Entry actions ---

    [Fact]
    public void HideEntryActionsCommand_WithRevealedEntry_ClearsRevealedEntry()
    {
        _vm.RevealedEntryId = 7;

        _vm.HideEntryActionsCommand.Execute(null);

        _vm.RevealedEntryId.Should().BeNull();
    }

    private static MonthlyEntryProjection MakeProjection(
        decimal amount,
        FinancialEntryType type,
        FinancialMonthlyEntryStatus status = FinancialMonthlyEntryStatus.Pending,
        decimal paidAmount = 0) =>
        new(
            FinancialEntryId: 1,
            FinancialMonthlyEntryId: null,
            Description: "Test",
            EffectiveAmount: amount,
            EntryType: type,
            Category: null,
            Status: status,
            IsProjected: true,
            InstallmentLabel: null,
            InstallmentTotalAmount: null,
            PaidAmount: paidAmount
        );
}
