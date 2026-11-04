using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;
using FluentAssertions.Execution;

namespace FinanceApp.UITests.Tests;

/// <summary>
/// The summary pager. How paying an entry moves amounts between its pages is covered by <see cref="EntryStatusTests"/>.
/// </summary>
[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class MonthSummaryTests : E2ETestBase
{
    public MonthSummaryTests(AppiumServerFixture server) : base(server, data =>
    {
        data.AddEntry("Internet E2E", 120m);
        data.AddEntry("Aluguel E2E", 1500m);
        data.AddEntry("Salário E2E", 5000m, isIncome: true);
    })
    {
    }

    [Fact]
    public void SwipeSummary_LeftAndRight_ChangesPagesWithoutChangingMonth()
    {
        var month = MainPage.MonthTitleText;
        using (new AssertionScope())
        {
            MainPage.VisibleSummaryPage.Should().Be(SummaryPage.Totals);
            MainPage.SummaryIncomeText.Should().Be(AppFormat.Money(5000));
            MainPage.SummaryOutcomeText.Should().Be(AppFormat.Money(1620));
            MainPage.SummaryBalanceText.Should().Be(AppFormat.Money(3380));
        }

        MainPage.SwipeSummary("left").WaitForSummaryPage(SummaryPage.Paid);
        MainPage.MonthTitleText.Should().Be(month, "swiping the summary must not change month");

        MainPage.SwipeSummary("left").WaitForSummaryPage(SummaryPage.Received);
        MainPage.SwipeSummary("left");
        MainPage.WaitForSummaryPage(SummaryPage.Received);

        MainPage.SwipeSummary("right").WaitForSummaryPage(SummaryPage.Paid);
        MainPage.MonthTitleText.Should().Be(month);
    }

    [Fact]
    public void TapSummaryDot_ThenChangeMonth_KeepsSelectedPage()
    {
        MainPage.TapSummaryDot(SummaryPage.Received);
        MainPage.TapSummaryDot(SummaryPage.Totals);
        MainPage.TapSummaryDot(SummaryPage.Paid);

        MainPage.GoToNextMonth().WaitForEntry("Internet E2E");

        MainPage.VisibleSummaryPage.Should().Be(SummaryPage.Paid);
    }
}
