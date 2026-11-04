using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

/// <summary>
/// Changing month by swiping, with the arrows, the "Mês atual" pill and the month picker. The range limits, the
/// picker's grid and the pill's direction are covered by the MainViewModel and MonthPickerViewModel tests.
/// </summary>
[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class MonthNavigationTests : E2ETestBase
{
    private const string Rent = "Aluguel E2E";

    public MonthNavigationTests(AppiumServerFixture server) : base(server, data => data.AddEntry(Rent, 1500m))
    {
    }

    [Fact]
    [Trait("Smoke", "true")]
    public void Swipe_LeftAndRight_ChangesMonthAndDismissesHintForGood()
    {
        MainPage.MonthTitleText.Should().Be(AppFormat.MonthTitle(AppFormat.CurrentMonth));
        MainPage.SwipeHintText.Should().Be(AppLabels.SwipeHint);

        MainPage.GoToNextMonth().WaitForSwipeHintGone();
        MainPage.MonthTitleText.Should().Be(AppFormat.MonthTitle(AppFormat.CurrentMonth.AddMonths(1)));

        MainPage.GoToPreviousMonth().GoToPreviousMonth();
        MainPage.MonthTitleText.Should().Be(AppFormat.MonthTitle(AppFormat.CurrentMonth.AddMonths(-1)));
        MainPage.IsSwipeHintVisible.Should().BeFalse("the hint is dismissed for good once the user changes month");
    }

    [Fact]
    public void MonthArrows_TapNextAndPrevious_StepOneMonthAndDismissHint()
    {
        MainPage.TapNextMonthArrow().WaitForSwipeHintGone();
        MainPage.MonthTitleText.Should().Be(AppFormat.MonthTitle(AppFormat.CurrentMonth.AddMonths(1)));

        MainPage.TapPreviousMonthArrow().TapPreviousMonthArrow();
        MainPage.MonthTitleText.Should().Be(AppFormat.MonthTitle(AppFormat.CurrentMonth.AddMonths(-1)));
        MainPage.IsSwipeHintVisible.Should().BeFalse();
    }

    [Fact]
    public void GoToCurrentMonthButton_FarFromToday_JumpsBackToCurrentMonth()
    {
        MainPage.IsGoToCurrentMonthVisible.Should().BeFalse();

        MainPage.GoToMonth(AppFormat.CurrentMonth.AddMonths(15));
        MainPage.WaitForGoToCurrentMonth().TapGoToCurrentMonth();

        MainPage.MonthTitleText.Should().Be(AppFormat.MonthTitle(AppFormat.CurrentMonth));
        MainPage.IsGoToCurrentMonthVisible.Should().BeFalse();
    }

    [Fact]
    public void MonthPicker_SelectMonthInNextYear_JumpsToThatMonthWithBadgeInsidePill()
    {
        // February has the longest month name in pt-BR ("Fevereiro"), which once pushed the ▾ badge out of the pill.
        var target = new DateTime(AppFormat.CurrentMonth.Year + 1, 2, 1);

        MainPage.GoToMonth(target);

        MainPage.MonthTitleText.Should().Be(AppFormat.MonthTitle(target));
        var (pill, title, badge) = MainPage.MonthHeaderBounds;
        pill.Contains(badge).Should().BeTrue($"the ▾ badge {badge} must sit inside the pill {pill}");
        badge.Width.Should().BeGreaterThanOrEqualTo(badge.Height - 2, "a clipped badge would be narrower than it is tall");
        title.Right.Should().BeLessThanOrEqualTo(badge.Left, "the month name must not overlap the badge");
    }

    [Fact]
    public void MonthPicker_CancelThenCurrentMonthButton_KeepsThenReturnsToCurrentMonth()
    {
        var month = AppFormat.CurrentMonth.AddMonths(-10);
        MainPage.GoToMonth(month);

        var picker = MainPage.OpenMonthPicker();
        picker.ShowYear(AppFormat.CurrentMonth.Year + 1);
        picker.Cancel();
        MainPage.MonthTitleText.Should().Be(AppFormat.MonthTitle(month));

        MainPage.OpenMonthPicker().SelectCurrentMonth();

        MainPage.WaitForMonth(AppFormat.CurrentMonth);
        MainPage.IsGoToCurrentMonthVisible.Should().BeFalse();
    }

    [Fact]
    [Trait("Smoke", "true")]
    public void MonthPicker_JumpFarAway_LoadsThatMonthsEntries()
    {
        var target = AppFormat.CurrentMonth.AddMonths(18);

        MainPage.GoToMonth(target).WaitForEntry(Rent);

        MainPage.OutcomeTotalText.Should().Be(AppFormat.Money(1500));
        MainPage.GoToPreviousMonth().WaitForEntry(Rent);
    }
}
