using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class DueDateTests : E2ETestBase
{
    private static readonly int Today = DateTime.Today.Day;

    // Never today, and no later than the 28th so it exists in every month.
    private static readonly int OtherDay = DateTime.Today.Day % 28 + 1;

    public DueDateTests(AppiumServerFixture server) : base(server)
    {
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AddEntry_DueToday_ShowsDueTodayInRedAndPersists(bool isIncome)
    {
        MainPage.AddEntry("Conta E2E", AppLabels.Recurrent, 300m, isIncome: isIncome, dueDay: Today);

        MainPage.EntryDueDateText("Conta E2E").Should().Be(AppLabels.DueToday);
        MainPage.CaptureEntryDueDateLabel("Conta E2E").Pixels().Count(IsRed).Should().BeGreaterThan(0);

        (await GetEntryRowsAsync()).Single().DueDay.Should().Be(Today);
    }

    [Fact]
    public async Task AddEntry_DueAnotherDay_ShowsDueDateWithoutRed()
    {
        MainPage.AddEntry("Internet E2E", AppLabels.OneTime, 120m, dueDay: OtherDay);

        MainPage.EntryDueDateText("Internet E2E").Should().Be(AppFormat.DueDate(DueDateIn(AppFormat.CurrentMonth, OtherDay)));
        MainPage.CaptureEntryDueDateLabel("Internet E2E").Pixels().Count(IsRed).Should().Be(0);

        (await GetEntryRowsAsync()).Single().DueDay.Should().Be(OtherDay);
    }

    [Fact]
    public async Task AddEntry_NoDueDay_ShowsNoDueDateAndPersistsNull()
    {
        var popup = MainPage.OpenAddEntry().SelectRecurrence(AppLabels.Recurrent);
        popup.DueDayText.Should().Be(AppLabels.NoDueDay);
        popup.Cancel();

        MainPage.AddEntry("Aluguel E2E", AppLabels.Recurrent, 1500m);

        MainPage.EntryDueDateText("Aluguel E2E").Should().BeNull();
        (await GetEntryRowsAsync()).Single().DueDay.Should().BeNull();
    }

    [Fact]
    public void AddEntry_RecurrentDueToday_FallsDueOnSameDayNextMonthWithoutRed()
    {
        MainPage.AddEntry("Academia E2E", AppLabels.Recurrent, 100m, dueDay: Today);
        var nextMonth = AppFormat.CurrentMonth.AddMonths(1);

        MainPage.GoToNextMonth().WaitForEntry("Academia E2E");

        MainPage.EntryDueDateText("Academia E2E").Should().Be(AppFormat.DueDate(DueDateIn(nextMonth, Today)));
        MainPage.CaptureEntryDueDateLabel("Academia E2E").Pixels().Count(IsRed).Should().Be(0);
    }

    [Fact]
    public void AddEntry_InstallmentsDueAnotherDay_FallsDueInEveryInstallment()
    {
        MainPage.AddEntry("Notebook E2E", AppLabels.Installments, 250m, installments: "3", dueDay: OtherDay);
        var nextMonth = AppFormat.CurrentMonth.AddMonths(1);

        MainPage.GoToNextMonth().WaitForEntry("Notebook E2E");

        MainPage.EntryDueDateText("Notebook E2E").Should().Be(AppFormat.DueDate(DueDateIn(nextMonth, OtherDay)));
    }

    [Fact]
    public void AddEntry_DueDayPastEndOfMonth_FallsDueOnLastDayOfFebruary()
    {
        MainPage.AddEntry("Cartão E2E", AppLabels.Recurrent, 900m, dueDay: 31);
        var february = Enumerable.Range(1, 12).Select(AppFormat.CurrentMonth.AddMonths).First(m => m.Month == 2);

        MainPage.GoToMonth(february).WaitForEntry("Cartão E2E");

        MainPage.EntryDueDateText("Cartão E2E").Should().Be(AppFormat.DueDate(DueDateIn(february, 31)));
    }

    [Fact]
    public void MarkPaid_DueToday_StopsBeingRed()
    {
        MainPage.AddEntry("Luz E2E", AppLabels.Recurrent, 200m, dueDay: Today);

        MainPage.MarkPaid("Luz E2E");

        MainPage.EntryDueDateText("Luz E2E").Should().Be(AppLabels.DueToday);
        MainPage.CaptureEntryDueDateLabel("Luz E2E").Pixels().Count(IsRed).Should().Be(0, "a paid entry no longer needs attention");
    }

    [Fact]
    public async Task EditEntry_ChangeDueDay_UpdatesRowAndPersists()
    {
        MainPage.AddEntry("Escola E2E", AppLabels.Recurrent, 800m, dueDay: OtherDay);

        var popup = MainPage.OpenEntry("Escola E2E");
        popup.DueDayText.Should().Be(AppLabels.DueDay(OtherDay));
        popup.SelectDueDay(Today).Save();

        MainPage.WaitFor(() => MainPage.EntryDueDateText("Escola E2E") == AppLabels.DueToday);
        (await GetEntryRowsAsync()).Single().DueDay.Should().Be(Today);
    }

    [Fact]
    public async Task EditEntry_ClearDueDay_RemovesDueDateAndPersistsNull()
    {
        MainPage.AddEntry("Seguro E2E", AppLabels.Recurrent, 150m, dueDay: Today);

        MainPage.OpenEntry("Seguro E2E").SelectDueDay(null).Save();

        MainPage.WaitFor(() => MainPage.EntryDueDateText("Seguro E2E") is null);
        (await GetEntryRowsAsync()).Single().DueDay.Should().BeNull();
    }

    private static DateTime DueDateIn(DateTime month, int day) =>
        new(month.Year, month.Month, Math.Min(day, DateTime.DaysInMonth(month.Year, month.Month)));

    private static bool IsRed((byte R, byte G, byte B) p) => p.R > p.G + 60 && p.R > p.B + 60;
}
