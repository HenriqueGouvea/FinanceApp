using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;
using FluentAssertions.Execution;

namespace FinanceApp.UITests.Tests;

/// <summary>
/// Paying beyond the planned amount. When an entry counts as over budget (after "Pagar", deleting payments, incomes,
/// the edit field) is covered by the FinanceService, MonthlyEntryProjection and PartialPaymentViewModel tests.
/// </summary>
[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class OverBudgetTests : E2ETestBase
{
    public OverBudgetTests(AppiumServerFixture server) : base(server, data =>
    {
        var market = data.AddEntry("Mercado E2E", 800m, isEstimated: true, allowsPartialPayment: true);
        data.AddPayment(market, 500m);
    })
    {
    }

    [Fact]
    public async Task PartialPaymentAction_BeyondPlannedAmount_WarnsAndShowsStruckPlannedAmountOnlyInThatMonth()
    {
        MainPage.EntryPlannedAmountText("Mercado E2E").Should().BeNull("R$ 500,00 is within the planned R$ 800,00");

        var popup = MainPage.OpenPartialPayment("Mercado E2E").EnterAmount(400m);
        popup.OverBudgetText.Should().Be(AppFormat.OverBudget(100));
        popup.Save();

        MainPage.WaitFor(() => MainPage.EntryPlannedAmountText("Mercado E2E") == AppFormat.StruckPlanned(800));
        using (new AssertionScope())
        {
            MainPage.EntryAmountText("Mercado E2E").Should().Be(AppFormat.Money(900));
            MainPage.EntryPartialText("Mercado E2E").Should().BeNull("the struck planned amount replaces the amount paid so far");
            MainPage.IsEntryPaymentProgressVisible("Mercado E2E").Should().BeTrue();
            MainPage.IsEntryPartiallyPaid("Mercado E2E").Should().BeTrue("going over the estimate doesn't settle the month");
            MainPage.SummaryPaidText.Should().Be(AppFormat.Money(900));
        }

        MainPage.GoToNextMonth().WaitForEntry("Mercado E2E");
        MainPage.EntryPlannedAmountText("Mercado E2E").Should().BeNull();
        MainPage.EntryAmountText("Mercado E2E").Should().Be(AppFormat.Money(800));

        (await GetEntryRowsAsync()).Single().Amount.Should().Be(800m, "going over budget only affects that month");
        var record = (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Subject;
        record.Status.Should().Be(AppFormat.PartiallyPaid);
        record.PaidAmount.Should().Be(900m);
    }
}
