using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class EstimatedAmountTests : E2ETestBase
{
    public EstimatedAmountTests(AppiumServerFixture server) : base(server)
    {
        MainPage.AddEntry("Luz E2E", AppLabels.Recurrent, 200m, isEstimated: true, allowsPartialPayment: true);
        MainPage.AddEntry("Aluguel E2E", AppLabels.Recurrent, 1500m);
    }

    [Fact]
    public async Task AddEntry_Estimated_ShowsEstimatedIconInEveryMonthAndPersists()
    {
        MainPage.IsEntryEstimated("Luz E2E").Should().BeTrue();
        MainPage.IsEntryEstimated("Aluguel E2E").Should().BeFalse();
        MainPage.EntryEstimatedSymbol("Luz E2E").Should().Be(AppLabels.EstimatedSymbol);
        MainPage.EntryAmountText("Luz E2E").Should().Be(AppFormat.Money(200));

        MainPage.GoToNextMonth().WaitForEntry("Luz E2E");
        MainPage.IsEntryEstimated("Luz E2E").Should().BeTrue();

        var rows = await GetEntryRowsAsync();
        rows.Single(e => e.Description == "Luz E2E").IsEstimated.Should().BeTrue();
        rows.Single(e => e.Description == "Aluguel E2E").IsEstimated.Should().BeFalse();
    }

    [Fact]
    public void OpenAddEntry_NewEntry_EstimatedSwitchIsOff()
    {
        var popup = MainPage.OpenAddEntry().SelectRecurrence(AppLabels.Recurrent);

        popup.IsEstimated.Should().BeFalse();
    }

    [Fact]
    public async Task EditEntry_TurnEstimatedOff_RemovesIconAndPersists()
    {
        var popup = MainPage.OpenEntry("Luz E2E");
        popup.IsEstimated.Should().BeTrue();

        popup.SetEstimated(false).Save();

        MainPage.WaitFor(() => !MainPage.IsEntryEstimated("Luz E2E"));
        (await GetEntryRowsAsync()).Single(e => e.Description == "Luz E2E").IsEstimated.Should().BeFalse();
    }

    [Fact]
    public async Task EditEntry_TurnEstimatedOn_ShowsIconAndPersists()
    {
        MainPage.OpenEntry("Aluguel E2E").SetEstimated(true).Save();

        MainPage.WaitFor(() => MainPage.IsEntryEstimated("Aluguel E2E"));
        (await GetEntryRowsAsync()).Single(e => e.Description == "Aluguel E2E").IsEstimated.Should().BeTrue();
    }

    [Fact]
    public void SetPaidAmount_EstimatedEntry_KeepsEstimatedIcon()
    {
        MainPage.SetPaidAmount("Luz E2E", 230m);

        MainPage.WaitFor(() => MainPage.IsEntryPartiallyPaid("Luz E2E"));
        MainPage.IsEntryEstimated("Luz E2E").Should().BeTrue();
        MainPage.EntryAmountText("Luz E2E").Should().Be(AppFormat.Money(230), "the actual bill came higher than estimated");
    }
}
