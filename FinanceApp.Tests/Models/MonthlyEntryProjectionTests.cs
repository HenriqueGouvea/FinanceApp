using FinanceApp.Models;
using FluentAssertions;
using Xunit;

namespace FinanceApp.Tests.Models;

public class MonthlyEntryProjectionTests
{
    [Fact]
    public void Pending_ShowsNoPaymentProgress()
    {
        var projection = Make(FinancialMonthlyEntryStatus.Pending, effectiveAmount: 800m, paidAmount: 0m);

        projection.ShowsPaidSoFar.Should().BeFalse();
        projection.ShowsPaymentProgress.Should().BeFalse();
        projection.IsOverBudget.Should().BeFalse();
    }

    [Fact]
    public void PartiallyPaidWithinPlanned_ShowsPaidSoFarAndProgress()
    {
        var projection = Make(FinancialMonthlyEntryStatus.PartiallyPaid, effectiveAmount: 800m, paidAmount: 250m, plannedAmount: 800m);

        projection.ShowsPaidSoFar.Should().BeTrue();
        projection.ShowsPaymentProgress.Should().BeTrue();
        projection.PlannedOrEffectiveAmount.Should().Be(800m);
    }

    [Fact]
    public void PartiallyPaidAbovePlanned_ShowsProgressButNotPaidSoFar()
    {
        var projection = Make(FinancialMonthlyEntryStatus.PartiallyPaid, effectiveAmount: 2200m, paidAmount: 2200m, plannedAmount: 2000m);

        projection.IsOverBudget.Should().BeTrue();
        projection.OverBudgetAmount.Should().Be(200m);
        projection.ShowsPaidSoFar.Should().BeFalse("the struck planned amount replaces it");
        projection.ShowsPaymentProgress.Should().BeTrue();
    }

    [Fact]
    public void PaidWithinPlanned_ShowsNoPaymentProgress()
    {
        var projection = Make(FinancialMonthlyEntryStatus.Paid, effectiveAmount: 800m, paidAmount: 800m, plannedAmount: 800m);

        projection.ShowsPaidSoFar.Should().BeFalse();
        projection.ShowsPaymentProgress.Should().BeFalse();
    }

    [Fact]
    public void PaidAbovePlanned_ShowsProgress()
    {
        var projection = Make(FinancialMonthlyEntryStatus.Paid, effectiveAmount: 2200m, paidAmount: 2200m, plannedAmount: 2000m);

        projection.ShowsPaymentProgress.Should().BeTrue();
        projection.ShowsPaidSoFar.Should().BeFalse();
    }

    [Fact]
    public void Projected_UsesEffectiveAmountAsPlanned()
    {
        Make(FinancialMonthlyEntryStatus.Pending, effectiveAmount: 800m, paidAmount: 0m)
            .PlannedOrEffectiveAmount.Should().Be(800m);
    }

    [Theory]
    [InlineData(FinancialMonthlyEntryStatus.Pending)]
    [InlineData(FinancialMonthlyEntryStatus.PartiallyPaid)]
    public void DueTodayAndNotPaid_IsDueToday(FinancialMonthlyEntryStatus status)
    {
        Make(status, effectiveAmount: 800m, paidAmount: 0m, dueDate: DateTime.Today).IsDueToday.Should().BeTrue();
    }

    [Fact]
    public void DueTodayButPaid_IsNotDueToday()
    {
        Make(FinancialMonthlyEntryStatus.Paid, effectiveAmount: 800m, paidAmount: 800m, dueDate: DateTime.Today)
            .IsDueToday.Should().BeFalse("a settled entry no longer needs attention");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void DueAnotherDay_IsNotDueToday(int daysFromToday)
    {
        var projection = Make(FinancialMonthlyEntryStatus.Pending, effectiveAmount: 800m, paidAmount: 0m,
            dueDate: DateTime.Today.AddDays(daysFromToday));

        projection.IsDueToday.Should().BeFalse();
        projection.HasDueDate.Should().BeTrue();
    }

    [Fact]
    public void WithoutDueDate_IsNotDueToday()
    {
        var projection = Make(FinancialMonthlyEntryStatus.Pending, effectiveAmount: 800m, paidAmount: 0m);

        projection.IsDueToday.Should().BeFalse();
        projection.HasDueDate.Should().BeFalse();
    }

    private static MonthlyEntryProjection Make(
        FinancialMonthlyEntryStatus status, decimal effectiveAmount, decimal paidAmount, decimal? plannedAmount = null,
        DateTime? dueDate = null) =>
        new(
            FinancialEntryId: 1,
            FinancialMonthlyEntryId: null,
            Description: "Supermarket",
            EffectiveAmount: effectiveAmount,
            EntryType: FinancialEntryType.Outcome,
            Category: null,
            Status: status,
            IsProjected: false,
            InstallmentLabel: null,
            InstallmentTotalAmount: null,
            PaidAmount: paidAmount,
            PlannedAmount: plannedAmount,
            DueDate: dueDate
        );
}
