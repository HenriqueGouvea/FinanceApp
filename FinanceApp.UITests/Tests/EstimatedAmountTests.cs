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
    }

    [Fact]
    public async Task AddEntry_EstimatedWithPartialPayment_ShowsBadgeInEveryMonthUntilTurnedOff()
    {
        MainPage.AddEntry("Luz E2E", AppLabels.Recurrent, 200m, isEstimated: true, allowsPartialPayment: true);

        MainPage.IsEntryEstimated("Luz E2E").Should().BeTrue();
        MainPage.EntryEstimatedSymbol("Luz E2E").Should().Be(AppLabels.EstimatedSymbol);
        MainPage.EntryAmountText("Luz E2E").Should().Be(AppFormat.Money(200));
        MainPage.GoToNextMonth().WaitForEntry("Luz E2E");
        MainPage.IsEntryEstimated("Luz E2E").Should().BeTrue();

        var row = (await GetEntryRowsAsync()).Single();
        row.IsEstimated.Should().BeTrue();
        row.AllowsPartialPayment.Should().BeTrue();

        var page = MainPage.OpenEntry("Luz E2E");
        page.IsEstimated.Should().BeTrue();
        page.SetEstimated(false).Save();

        MainPage.WaitFor(() => !MainPage.IsEntryEstimated("Luz E2E"));
        (await GetEntryRowsAsync()).Single().IsEstimated.Should().BeFalse();
    }
}
