using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class MonthNavigationTests : E2ETestBase
{
    public MonthNavigationTests(AppiumServerFixture server) : base(server)
    {
    }

    [Fact]
    public void Startup_FreshInstall_ShowsCurrentMonthWithEmptyTotals()
    {
        MainPage.MonthTitleText.Should().Be(AppFormat.MonthTitle(AppFormat.CurrentMonth));
        MainPage.SummaryIncomeText.Should().Be(AppFormat.Money(0));
        MainPage.SummaryOutcomeText.Should().Be(AppFormat.Money(0));
        MainPage.SummaryBalanceText.Should().Be(AppFormat.Money(0));
        MainPage.OutcomeCountText.Should().Be(AppFormat.EntryCount(0));
        MainPage.IncomeCountText.Should().Be(AppFormat.EntryCount(0));
    }

    [Fact]
    public void SwipeLeft_CurrentMonth_ShowsNextMonth()
    {
        MainPage.GoToNextMonth();

        MainPage.MonthTitleText.Should().Be(AppFormat.MonthTitle(AppFormat.CurrentMonth.AddMonths(1)));
    }

    [Fact]
    public void SwipeRight_CurrentMonth_ShowsPreviousMonth()
    {
        MainPage.GoToPreviousMonth();

        MainPage.MonthTitleText.Should().Be(AppFormat.MonthTitle(AppFormat.CurrentMonth.AddMonths(-1)));
    }

    [Fact]
    public void AddEntry_InAnotherMonth_StartsInThatMonth()
    {
        MainPage.GoToNextMonth().AddEntry("Viagem E2E", AppLabels.OneTime, "2000");

        MainPage.GoToPreviousMonth();
        MainPage.IsEntryVisible("Viagem E2E").Should().BeFalse();
        MainPage.GoToNextMonth().WaitForEntry("Viagem E2E");
    }
}
