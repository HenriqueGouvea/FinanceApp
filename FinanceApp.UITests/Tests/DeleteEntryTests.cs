using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class DeleteEntryTests : E2ETestBase
{
    public DeleteEntryTests(AppiumServerFixture server) : base(server)
    {
    }

    [Fact]
    public async Task DeleteEntry_OneTime_RemovesFromListAndDatabase()
    {
        MainPage.AddEntry("Presente E2E", AppLabels.OneTime, "80");

        MainPage.DeleteEntry("Presente E2E", AppLabels.Delete)
            .WaitForEntryGone("Presente E2E");

        MainPage.OutcomeCountText.Should().Be(AppFormat.EntryCount(0));
        MainPage.OutcomeTotalText.Should().Be(AppFormat.Money(0));
        (await GetEntryRowsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteEntry_RecurrentThisMonthOnly_HidesOnlyCurrentMonth()
    {
        MainPage.AddEntry("Streaming E2E", AppLabels.Recurrent, "40");

        MainPage.DeleteEntry("Streaming E2E", AppLabels.DeleteThisMonth)
            .WaitForEntryGone("Streaming E2E");
        MainPage.GoToNextMonth().WaitForEntry("Streaming E2E");

        var entry = (await GetEntryRowsAsync()).Should().ContainSingle().Subject;
        entry.EndDate.Should().BeNull();
        var monthly = (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Subject;
        monthly.FinancialEntryId.Should().Be(entry.Id);
        monthly.IsCancelled.Should().BeTrue();
        (monthly.Year, monthly.Month).Should().Be((AppFormat.CurrentMonth.Year, AppFormat.CurrentMonth.Month));
    }

    [Fact]
    public async Task DeleteEntry_RecurrentThisAndFutureMonths_KeepsPreviousMonths()
    {
        MainPage.GoToPreviousMonth()
            .AddEntry("Condomínio E2E", AppLabels.Recurrent, "600")
            .GoToNextMonth()
            .WaitForEntry("Condomínio E2E");

        MainPage.DeleteEntry("Condomínio E2E", AppLabels.DeleteThisAndFuture)
            .WaitForEntryGone("Condomínio E2E");
        MainPage.GoToNextMonth();
        MainPage.IsEntryVisible("Condomínio E2E").Should().BeFalse();
        MainPage.GoToPreviousMonth().GoToPreviousMonth().WaitForEntry("Condomínio E2E");

        var entry = (await GetEntryRowsAsync()).Should().ContainSingle().Subject;
        entry.StartDate.Date.Should().Be(AppFormat.CurrentMonth.AddMonths(-1));
        entry.EndDate!.Value.Date.Should().Be(AppFormat.CurrentMonth.AddMonths(-1));
    }

    [Fact]
    public async Task DeleteEntry_RecurrentAllMonths_RemovesEverywhere()
    {
        MainPage.AddEntry("Seguro E2E", AppLabels.Recurrent, "200");
        MainPage.GoToNextMonth()
            .DeleteEntry("Seguro E2E", AppLabels.DeleteThisMonth)
            .WaitForEntryGone("Seguro E2E");
        MainPage.GoToPreviousMonth();

        MainPage.DeleteEntry("Seguro E2E", AppLabels.DeleteAllMonths)
            .WaitForEntryGone("Seguro E2E");
        MainPage.GoToNextMonth().GoToNextMonth();
        MainPage.IsEntryVisible("Seguro E2E").Should().BeFalse();

        (await GetEntryRowsAsync()).Should().BeEmpty();
        (await GetMonthlyEntryRowsAsync()).Should().BeEmpty("monthly overrides are removed with their template");
    }

    [Fact]
    public async Task DeleteEntry_ActionSheetCancelled_KeepsEntry()
    {
        MainPage.AddEntry("Luz E2E", AppLabels.Recurrent, "150");

        MainPage.DeleteEntry("Luz E2E", AppLabels.Cancel);

        MainPage.WaitForEntry("Luz E2E");
        (await GetEntryRowsAsync()).Should().ContainSingle();
        (await GetMonthlyEntryRowsAsync()).Should().BeEmpty();
    }
}
