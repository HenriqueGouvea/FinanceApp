using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

/// <summary>
/// The "Pagamento parcial" switch decides whether an entry offers "Pagar parcial" and "Valor já pago neste mês".
/// Its labels and default are covered by the AddEntryViewModel tests.
/// </summary>
[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class PartialPaymentFlagTests : E2ETestBase
{
    public PartialPaymentFlagTests(AppiumServerFixture server) : base(server, data =>
    {
        data.AddEntry("Aluguel E2E", 1500m);
        var market = data.AddEntry("Mercado E2E", 800m, allowsPartialPayment: true);
        data.AddPayment(market, 150m, "Feira");
    })
    {
    }

    [Fact]
    public async Task EditEntry_TurnFlagOn_OffersPaidAmountAndPartialPaymentAction()
    {
        MainPage.RevealActions("Aluguel E2E");
        MainPage.RevealedPartialPaymentActionText.Should().BeNull();
        MainPage.RevealedStatusActionText.Should().Be(AppLabels.MarkPaid, "the other actions stay available");
        MainPage.TapSummaryBar();
        MainPage.WaitForActionsHidden();

        var page = MainPage.OpenEntry("Aluguel E2E");
        page.IsPaidAmountVisible.Should().BeFalse();
        page.SetAllowsPartialPayment(true);
        page.IsPaidAmountVisible.Should().BeTrue();
        page.Save();

        MainPage.RevealActions("Aluguel E2E");
        MainPage.RevealedPartialPaymentActionText.Should().Be(AppLabels.MarkPartialPaid);
        (await GetEntryRowsAsync()).Single(e => e.Description == "Aluguel E2E").AllowsPartialPayment.Should().BeTrue();
    }

    [Fact]
    public async Task EditEntry_TurnFlagOffWithPayments_HidesActionButKeepsPaymentsListedSoTheyCanBeRemoved()
    {
        MainPage.OpenEntry("Mercado E2E").SetAllowsPartialPayment(false).Save();

        MainPage.IsEntryPartiallyPaid("Mercado E2E").Should().BeTrue("turning the flag off keeps what was already paid");
        MainPage.RevealActions("Mercado E2E");
        MainPage.RevealedPartialPaymentActionText.Should().BeNull();
        MainPage.TapSummaryBar();
        MainPage.WaitForActionsHidden();
        (await GetEntryRowsAsync()).Single(e => e.Description == "Mercado E2E").AllowsPartialPayment.Should().BeFalse();

        var page = MainPage.OpenEntry("Mercado E2E");
        page.IsPaidAmountVisible.Should().BeTrue();
        page.Payments.Should().ContainSingle(p => p.Description == "Feira");
        page.DeletePayment("Feira").Save();

        MainPage.WaitFor(() => !MainPage.IsEntryPartiallyPaid("Mercado E2E"));
        (await GetPaymentRowsAsync()).Should().BeEmpty();
    }
}
