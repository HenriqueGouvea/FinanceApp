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
