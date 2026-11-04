using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

/// <summary>
/// Picking and changing the due day. Which date each month falls due on, and when "Vence hoje" applies, are covered
/// by the FinanceService, MonthlyEntryProjection and DueDateConverter tests.
/// </summary>
[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class DueDateTests : E2ETestBase
{
    private static readonly int Today = DateTime.Today.Day;

    // Never today, and no later than the 28th so it exists in every month.
    private static readonly int OtherDay = DateTime.Today.Day % 28 + 1;

    public DueDateTests(AppiumServerFixture server) : base(server, data => data.AddEntry("Escola E2E", 800m, dueDay: OtherDay))
    {
    }

    [Fact]
    public async Task AddEntry_DueDay_ShowsDueDateOnRowAndPersists()
    {
        var page = MainPage.OpenAddEntry().SelectRecurrence(AppLabels.Recurrent);
        page.DueDayText.Should().Be(AppLabels.NoDueDay);
        page.Cancel();

        MainPage.AddEntry("Internet E2E", AppLabels.OneTime, 120m, dueDay: OtherDay);

        MainPage.EntryDueDateText("Internet E2E").Should().Be(AppFormat.DueDate(
            new DateTime(AppFormat.CurrentMonth.Year, AppFormat.CurrentMonth.Month, OtherDay)));
        (await GetEntryRowsAsync()).Single(e => e.Description == "Internet E2E").DueDay.Should().Be(OtherDay);
    }

    [Fact]
    public async Task EditEntry_ChangeThenClearDueDay_UpdatesRowAndPersists()
    {
        var page = MainPage.OpenEntry("Escola E2E");
        page.DueDayText.Should().Be(AppLabels.DueDay(OtherDay));
        page.SelectDueDay(Today).Save();

        MainPage.WaitFor(() => MainPage.EntryDueDateText("Escola E2E") == AppLabels.DueToday);
        (await GetEntryRowsAsync()).Single().DueDay.Should().Be(Today);

        MainPage.OpenEntry("Escola E2E").SelectDueDay(null).Save();

        MainPage.WaitFor(() => MainPage.EntryDueDateText("Escola E2E") is null);
        (await GetEntryRowsAsync()).Single().DueDay.Should().BeNull();
    }
}
