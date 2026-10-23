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
    public void GoToCurrentMonthButton_HiddenOnCurrentMonthAndShownAfterSwipe()
    {
        MainPage.IsGoToCurrentMonthVisible.Should().BeFalse();

        MainPage.GoToNextMonth().WaitForGoToCurrentMonth();
        MainPage.GoToCurrentMonthText.Should().Be(AppLabels.GoToCurrentMonthBackward);

        MainPage.GoToPreviousMonth().GoToPreviousMonth().WaitForGoToCurrentMonth();
        MainPage.GoToCurrentMonthText.Should().Be(AppLabels.GoToCurrentMonthForward);
    }

    [Fact]
    public void GoToCurrentMonthButton_FarFromToday_JumpsBackToCurrentMonth()
    {
        MainPage.GoToMonth(AppFormat.CurrentMonth.AddMonths(15));

        MainPage.WaitForGoToCurrentMonth().TapGoToCurrentMonth();

        MainPage.MonthTitleText.Should().Be(AppFormat.MonthTitle(AppFormat.CurrentMonth));
        MainPage.IsGoToCurrentMonthVisible.Should().BeFalse();
    }

    [Fact]
    public void MonthPicker_Open_ShowsCurrentYearWithTwelveMonths()
    {
        var picker = MainPage.OpenMonthPicker();

        picker.Year.Should().Be(AppFormat.CurrentMonth.Year);
        picker.MonthLabels.Should().Equal(Enumerable.Range(1, 12)
            .Select(m => AppFormat.MonthAbbreviation(new DateTime(AppFormat.CurrentMonth.Year, m, 1))));
    }

    [Fact]
    public void MonthPicker_SelectMonthInNextYear_JumpsToThatMonth()
    {
        var target = new DateTime(AppFormat.CurrentMonth.Year + 1, 2, 1);

        MainPage.GoToMonth(target);

        MainPage.MonthTitleText.Should().Be(AppFormat.MonthTitle(target));
        MainPage.WaitForGoToCurrentMonth();
    }

    [Fact]
    public void MonthPicker_SelectMonthInPreviousYear_JumpsToThatMonth()
    {
        var target = new DateTime(AppFormat.CurrentMonth.Year - 1, 7, 1);

        MainPage.GoToMonth(target);

        MainPage.MonthTitleText.Should().Be(AppFormat.MonthTitle(target));
    }

    [Fact]
    public void MonthPicker_LastYearOfRange_DisablesNextYearAndMonthsAfterRange()
    {
        var lastMonth = AppFormat.CurrentMonth.AddMonths(24);
        var picker = MainPage.OpenMonthPicker().ShowYear(lastMonth.Year);

        picker.IsNextYearEnabled.Should().BeFalse();
        picker.IsPreviousYearEnabled.Should().BeTrue();
        picker.IsMonthEnabled(lastMonth).Should().BeTrue();
        if (lastMonth.Month < 12)
            picker.IsMonthEnabled(lastMonth.AddMonths(1)).Should().BeFalse();
    }

    [Fact]
    public void MonthPicker_CurrentMonthButton_ReturnsToCurrentMonth()
    {
        MainPage.GoToMonth(AppFormat.CurrentMonth.AddMonths(-10));

        MainPage.OpenMonthPicker().SelectCurrentMonth();

        MainPage.WaitForMonth(AppFormat.CurrentMonth);
        MainPage.IsGoToCurrentMonthVisible.Should().BeFalse();
    }

    [Fact]
    public void MonthPicker_Cancel_KeepsSelectedMonth()
    {
        MainPage.GoToNextMonth();
        var picker = MainPage.OpenMonthPicker();
        picker.ShowYear(AppFormat.CurrentMonth.Year + 1);

        picker.Cancel();

        MainPage.MonthTitleText.Should().Be(AppFormat.MonthTitle(AppFormat.CurrentMonth.AddMonths(1)));
    }

    [Fact]
    public void MonthPicker_JumpFarAway_LoadsThatMonthsEntries()
    {
        MainPage.AddEntry("Aluguel E2E", AppLabels.Recurrent, "1500");
        var target = AppFormat.CurrentMonth.AddMonths(18);

        MainPage.GoToMonth(target).WaitForEntry("Aluguel E2E");

        MainPage.OutcomeTotalText.Should().Be(AppFormat.Money(1500));
        MainPage.GoToPreviousMonth().WaitForEntry("Aluguel E2E");
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
