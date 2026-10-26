using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class MonthSummaryTests : E2ETestBase
{
    public MonthSummaryTests(AppiumServerFixture server) : base(server)
    {
        MainPage.AddEntry("Internet E2E", AppLabels.Recurrent, 120m);
        MainPage.AddEntry("Aluguel E2E", AppLabels.Recurrent, 1500m);
        MainPage.AddEntry("Salário E2E", AppLabels.Recurrent, 5000m, isIncome: true);
    }

    [Fact]
    public void Summary_NewEntries_ShowsEverythingAsPending()
    {
        MainPage.SummaryReceivedText.Should().Be(AppFormat.Money(0));
        MainPage.SummaryToReceiveText.Should().Be(AppFormat.Money(5000));
        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(0));
        MainPage.SummaryToPayText.Should().Be(AppFormat.Money(1620));
    }

    [Fact]
    public async Task MarkPaid_OutcomeAndIncome_MovesAmountsToPaidAndReceivedOnlyInThatMonth()
    {
        MainPage.MarkPaid("Internet E2E");

        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(120));
        MainPage.SummaryToPayText.Should().Be(AppFormat.Money(1500));
        MainPage.SummaryOutcomeText.Should().Be(AppFormat.Money(1620), "paying doesn't change the month's total");

        MainPage.MarkPaid("Salário E2E");

        MainPage.SummaryReceivedText.Should().Be(AppFormat.Money(5000));
        MainPage.SummaryToReceiveText.Should().Be(AppFormat.Money(0));

        MainPage.GoToNextMonth().WaitForEntry("Internet E2E");
        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(0));
        MainPage.SummaryToPayText.Should().Be(AppFormat.Money(1620));
        MainPage.SummaryReceivedText.Should().Be(AppFormat.Money(0));
        MainPage.SummaryToReceiveText.Should().Be(AppFormat.Money(5000));

        (await GetMonthlyEntryRowsAsync()).Should().HaveCount(2)
            .And.OnlyContain(r => r.Status == AppFormat.Paid && r.Month == AppFormat.CurrentMonth.Month);
    }

    [Fact]
    public void MarkPending_PaidOutcome_MovesAmountBackToPay()
    {
        MainPage.MarkPaid("Aluguel E2E");
        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(1500));

        MainPage.MarkPending("Aluguel E2E");

        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(0));
        MainPage.SummaryToPayText.Should().Be(AppFormat.Money(1620));
    }

    [Fact]
    public void DeleteEntry_PaidOutcome_RemovesItFromPaidTotal()
    {
        MainPage.MarkPaid("Internet E2E");

        MainPage.DeleteEntry("Internet E2E", AppLabels.DeleteAllMonths).WaitForEntryGone("Internet E2E");

        MainPage.WaitFor(() => MainPage.SummaryToPayText == AppFormat.Money(1500));
        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(0));
    }

    [Fact]
    public void Summary_FreshMonth_ShowsTotalsPageFirst()
    {
        MainPage.VisibleSummaryPage.Should().Be(SummaryPage.Totals);
        MainPage.SummaryIncomeText.Should().Be(AppFormat.Money(5000));
        MainPage.SummaryOutcomeText.Should().Be(AppFormat.Money(1620));
        MainPage.SummaryBalanceText.Should().Be(AppFormat.Money(3380));
    }

    [Fact]
    public void SwipeSummary_Left_ShowsPaidThenReceivedPagesWithoutChangingMonth()
    {
        var month = MainPage.MonthTitleText;

        MainPage.SwipeSummary("left").WaitForSummaryPage(SummaryPage.Paid);
        MainPage.MonthTitleText.Should().Be(month, "swiping the summary must not change month");

        MainPage.SwipeSummary("left").WaitForSummaryPage(SummaryPage.Received);
        MainPage.SwipeSummary("left");
        MainPage.WaitForSummaryPage(SummaryPage.Received);
        MainPage.MonthTitleText.Should().Be(month);
    }

    [Fact]
    public void SwipeSummary_Right_GoesBackToPreviousPage()
    {
        MainPage.SwipeSummary("left").WaitForSummaryPage(SummaryPage.Paid);

        MainPage.SwipeSummary("right").WaitForSummaryPage(SummaryPage.Totals);
    }

    [Fact]
    public void TapSummaryDot_AnyPage_ShowsThatPage()
    {
        MainPage.TapSummaryDot(SummaryPage.Received);
        MainPage.TapSummaryDot(SummaryPage.Paid);
        MainPage.TapSummaryDot(SummaryPage.Totals);
    }

    [Fact]
    public void SummaryPage_ChangeMonth_KeepsSelectedPage()
    {
        MainPage.SwipeSummary("left").WaitForSummaryPage(SummaryPage.Paid);

        MainPage.GoToNextMonth().WaitForEntry("Internet E2E");

        MainPage.VisibleSummaryPage.Should().Be(SummaryPage.Paid);
    }
}
