using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;
using FluentAssertions.Execution;

namespace FinanceApp.UITests.Tests;

/// <summary>
/// "Pagar"/"Receber" and "Pendente" from the actions menu, with the row and the month summary's paid totals reacting.
/// </summary>
[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class EntryStatusTests : E2ETestBase
{
    public EntryStatusTests(AppiumServerFixture server) : base(server, data =>
    {
        data.AddEntry("Internet E2E", 120m);
        data.AddEntry("Salário E2E", 5000m, isIncome: true);
    })
    {
    }

    [Fact]
    [Trait("Smoke", "true")]
    public async Task MarkPaid_OutcomeAndIncome_ShowsPaidInRowAndSummaryOnlyInThatMonth()
    {
        MainPage.SummaryToPayText.Should().Be(AppFormat.Money(120));
        MainPage.VisibleEntryStatuses.Should().BeEmpty();

        MainPage.MarkPaid("Internet E2E");

        using (new AssertionScope())
        {
            MainPage.VisibleEntryStatuses.Should().ContainSingle().Which.Should().Be(AppLabels.Paid);
            MainPage.SummaryPaidText.Should().Be(AppFormat.Money(120));
            MainPage.SummaryToPayText.Should().Be(AppFormat.Money(0));
            MainPage.OutcomeTotalText.Should().Be(AppFormat.Money(120), "paying doesn't change the month's total");
        }

        MainPage.RevealActions("Salário E2E");
        MainPage.RevealedStatusActionText.Should().Be(AppLabels.MarkReceived);
        MainPage.TapSummaryBar();
        MainPage.WaitForActionsHidden();
        MainPage.MarkPaid("Salário E2E");

        using (new AssertionScope())
        {
            MainPage.VisibleEntryStatuses.Should().BeEquivalentTo(AppLabels.Paid, AppLabels.Received);
            MainPage.SummaryReceivedText.Should().Be(AppFormat.Money(5000));
            MainPage.SummaryToReceiveText.Should().Be(AppFormat.Money(0));
        }

        MainPage.GoToNextMonth().WaitForEntry("Internet E2E");
        MainPage.VisibleEntryStatuses.Should().BeEmpty();
        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(0));
        MainPage.SummaryReceivedText.Should().Be(AppFormat.Money(0));

        var entryIds = (await GetEntryRowsAsync()).Select(e => e.Id);
        var records = await GetMonthlyEntryRowsAsync();
        records.Select(r => r.FinancialEntryId).Should().BeEquivalentTo(entryIds);
        records.Should().OnlyContain(r => r.Status == AppFormat.Paid && !r.IsCancelled
            && r.Year == AppFormat.CurrentMonth.Year && r.Month == AppFormat.CurrentMonth.Month);
    }

    [Fact]
    public async Task MarkPending_PaidOutcome_RevertsRowSummaryAndDatabase()
    {
        MainPage.MarkPaid("Internet E2E");
        MainPage.RevealActions("Internet E2E");
        MainPage.RevealedStatusActionText.Should().Be(AppLabels.MarkPending);
        MainPage.TapSummaryBar();
        MainPage.WaitForActionsHidden();

        MainPage.MarkPending("Internet E2E");

        MainPage.VisibleEntryStatuses.Should().BeEmpty();
        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(0));
        MainPage.SummaryToPayText.Should().Be(AppFormat.Money(120));
        (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Which.Status.Should().Be(AppFormat.Pending);
    }
}
