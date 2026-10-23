using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class EntryStatusTests : E2ETestBase
{
    public EntryStatusTests(AppiumServerFixture server) : base(server)
    {
        MainPage.AddEntry("Internet E2E", AppLabels.Recurrent, "120");
    }

    [Fact]
    public async Task MarkPaid_RecurrentOutcome_ShowsPaidOnlyInThatMonthAndPersists()
    {
        MainPage.IsEntryPaid("Internet E2E").Should().BeFalse();
        MainPage.VisibleEntryStatuses.Should().BeEmpty();

        MainPage.MarkPaid("Internet E2E");

        MainPage.VisibleEntryStatuses.Should().ContainSingle().Which.Should().Be(AppLabels.Paid);
        MainPage.OutcomeTotalText.Should().Be(AppFormat.Money(120), "paying doesn't change the month's total");

        MainPage.GoToNextMonth().WaitForEntry("Internet E2E");
        MainPage.IsEntryPaid("Internet E2E").Should().BeFalse();
        MainPage.GoToPreviousMonth().WaitForEntry("Internet E2E");
        MainPage.IsEntryPaid("Internet E2E").Should().BeTrue();

        var entry = (await GetEntryRowsAsync()).Single();
        var record = (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Subject;
        record.FinancialEntryId.Should().Be(entry.Id);
        record.Year.Should().Be(AppFormat.CurrentMonth.Year);
        record.Month.Should().Be(AppFormat.CurrentMonth.Month);
        record.Status.Should().Be(AppFormat.Paid);
        record.IsCancelled.Should().BeFalse();
    }

    [Fact]
    public async Task MarkPending_PaidEntry_RevertsStatusInListAndDatabase()
    {
        MainPage.MarkPaid("Internet E2E");
        MainPage.RevealActions("Internet E2E");
        MainPage.RevealedStatusActionText.Should().Be(AppLabels.MarkPending);
        MainPage.TapSummaryBar();
        MainPage.WaitForActionsHidden();

        MainPage.MarkPending("Internet E2E");

        MainPage.VisibleEntryStatuses.Should().BeEmpty();
        (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Which.Status.Should().Be(AppFormat.Pending);
    }

    [Fact]
    public async Task MarkPaid_Income_ShowsReceivedLabels()
    {
        MainPage.AddEntry("Salário E2E", AppLabels.Recurrent, "5000", isIncome: true);
        MainPage.RevealActions("Salário E2E");
        MainPage.RevealedStatusActionText.Should().Be(AppLabels.MarkReceived);
        MainPage.TapSummaryBar();
        MainPage.WaitForActionsHidden();

        MainPage.MarkPaid("Salário E2E");

        MainPage.VisibleEntryStatuses.Should().ContainSingle().Which.Should().Be(AppLabels.Received);
        MainPage.IsEntryPaid("Internet E2E").Should().BeFalse();

        var income = (await GetEntryRowsAsync()).Single(e => e.EntryType == AppFormat.Income);
        (await GetMonthlyEntryRowsAsync()).Should().ContainSingle()
            .Which.FinancialEntryId.Should().Be(income.Id);
    }

    [Fact]
    public async Task MarkPaid_InstallmentInNextMonth_PersistsForThatInstallmentOnly()
    {
        MainPage.AddEntry("Notebook E2E", AppLabels.Installments, "300", installments: "3");
        var nextMonth = AppFormat.CurrentMonth.AddMonths(1);

        MainPage.GoToNextMonth().MarkPaid("Notebook E2E");

        MainPage.IsEntryPaid("Internet E2E").Should().BeFalse();
        MainPage.GoToPreviousMonth().WaitForEntry("Notebook E2E");
        MainPage.IsEntryPaid("Notebook E2E").Should().BeFalse();

        var installment = (await GetEntryRowsAsync()).Single(e => e.Recurrence == AppFormat.Installments);
        var record = (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Subject;
        record.FinancialEntryId.Should().Be(installment.Id);
        record.Year.Should().Be(nextMonth.Year);
        record.Month.Should().Be(nextMonth.Month);
        record.Status.Should().Be(AppFormat.Paid);
    }
}
