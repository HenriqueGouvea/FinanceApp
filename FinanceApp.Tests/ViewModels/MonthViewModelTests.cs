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

    private static MonthlyEntryProjection MakeProjection(decimal amount, FinancialEntryType type) =>
        new(
            FinancialEntryId: 1,
            FinancialMonthlyEntryId: null,
            Description: "Test",
            EffectiveAmount: amount,
            EntryType: type,
            Category: string.Empty,
            Status: FinancialMonthlyEntryStatus.Pending,
            IsProjected: true,
            InstallmentLabel: null
        );
}
