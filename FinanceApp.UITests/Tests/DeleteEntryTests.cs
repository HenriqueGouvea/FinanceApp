using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

/// <summary>
/// One test per delete option: each one maps an action sheet choice to a different delete scope.
/// What each scope does to the stored months is covered by the FinanceService tests.
/// </summary>
[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class DeleteEntryTests : E2ETestBase
{
    private const string Gift = "Presente E2E";
    private const string Streaming = "Streaming E2E";
    private const string Condo = "Condomínio E2E";
    private const string Insurance = "Seguro E2E";

    public DeleteEntryTests(AppiumServerFixture server) : base(server, data =>
    {
        data.AddEntry(Gift, 80m, AppFormat.OneTime);
        data.AddEntry(Streaming, 40m);
        data.AddEntry(Condo, 600m, startMonth: AppFormat.CurrentMonth.AddMonths(-1));
        data.AddEntry(Insurance, 200m);
    })
    {
    }

    [Fact]
    public async Task DeleteEntry_OneTime_RemovesFromListAndDatabase()
    {
        MainPage.DeleteEntry(Gift, AppLabels.Delete)
            .WaitForEntryGone(Gift);

        MainPage.OutcomeCountText.Should().Be(AppFormat.EntryCount(3));
        MainPage.OutcomeTotalText.Should().Be(AppFormat.Money(840));
        (await GetEntryRowsAsync()).Should().NotContain(e => e.Description == Gift);
    }

    [Fact]
    [Trait("Smoke", "true")]
    public async Task DeleteEntry_ActionSheetCancelledThenThisMonthOnly_HidesOnlyCurrentMonth()
    {
        MainPage.DeleteEntry(Streaming, AppLabels.Cancel);
        MainPage.WaitForEntry(Streaming);
        (await GetMonthlyEntryRowsAsync()).Should().BeEmpty();

        MainPage.DeleteEntry(Streaming, AppLabels.DeleteThisMonth)
            .WaitForEntryGone(Streaming);
        MainPage.GoToNextMonth().WaitForEntry(Streaming);

        var entry = (await GetEntryRowsAsync()).Single(e => e.Description == Streaming);
        entry.EndDate.Should().BeNull();
        var monthly = (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Subject;
        monthly.FinancialEntryId.Should().Be(entry.Id);
        monthly.IsCancelled.Should().BeTrue();
        (monthly.Year, monthly.Month).Should().Be((AppFormat.CurrentMonth.Year, AppFormat.CurrentMonth.Month));
    }

    [Fact]
    public async Task DeleteEntry_RecurrentThisAndFutureMonths_KeepsPreviousMonths()
    {
        MainPage.DeleteEntry(Condo, AppLabels.DeleteThisAndFuture)
            .WaitForEntryGone(Condo);
        MainPage.GoToNextMonth();
        MainPage.IsEntryVisible(Condo).Should().BeFalse();
        MainPage.GoToPreviousMonth().GoToPreviousMonth().WaitForEntry(Condo);

        var entry = (await GetEntryRowsAsync()).Single(e => e.Description == Condo);
        entry.EndDate!.Value.Date.Should().Be(AppFormat.CurrentMonth.AddMonths(-1));
    }

    [Fact]
    public async Task DeleteEntry_RecurrentAllMonths_RemovesEverywhere()
    {
        MainPage.DeleteEntry(Insurance, AppLabels.DeleteAllMonths)
            .WaitForEntryGone(Insurance);
        MainPage.GoToNextMonth().WaitForEntry(Streaming);
        MainPage.IsEntryVisible(Insurance).Should().BeFalse();

        (await GetEntryRowsAsync()).Should().NotContain(e => e.Description == Insurance);
    }
}
