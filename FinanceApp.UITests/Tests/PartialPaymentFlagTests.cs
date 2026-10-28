using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class PartialPaymentFlagTests : E2ETestBase
{
    public PartialPaymentFlagTests(AppiumServerFixture server) : base(server)
    {
        MainPage.AddEntry("Aluguel E2E", AppLabels.Recurrent, 1500m);
        MainPage.AddEntry("Mercado E2E", AppLabels.Recurrent, 800m, allowsPartialPayment: true);
    }

    [Fact]
    public void OpenAddEntry_NewEntry_SwitchIsOffAndLabelFollowsEntryType()
    {
        var popup = MainPage.OpenAddEntry().SelectRecurrence(AppLabels.Recurrent);

        popup.AllowsPartialPayment.Should().BeFalse();
        popup.PartialPaymentSwitchText.Should().Be(AppLabels.PartialPaymentOutcome);

        popup.SelectIncome();

        popup.PartialPaymentSwitchText.Should().Be(AppLabels.PartialPaymentIncome);
    }

    [Fact]
    public async Task AddEntry_WithoutFlag_HidesPartialPaymentActionAndPaidAmount()
    {
        MainPage.RevealActions("Aluguel E2E");
        MainPage.RevealedPartialPaymentActionText.Should().BeNull();
        MainPage.RevealedStatusActionText.Should().Be(AppLabels.MarkPaid, "the other actions stay available");
        MainPage.TapSummaryBar();
        MainPage.WaitForActionsHidden();

        MainPage.OpenEntry("Aluguel E2E").IsPaidAmountVisible.Should().BeFalse();

        (await GetEntryRowsAsync()).Single(e => e.Description == "Aluguel E2E").AllowsPartialPayment.Should().BeFalse();
    }

    [Fact]
    public async Task AddEntry_WithFlag_ShowsPartialPaymentActionInEveryMonthAndPersists()
    {
        MainPage.RevealActions("Mercado E2E");
        MainPage.RevealedPartialPaymentActionText.Should().Be(AppLabels.MarkPartialPaid);
        MainPage.TapSummaryBar();
        MainPage.WaitForActionsHidden();

        MainPage.GoToNextMonth().RevealActions("Mercado E2E");
        MainPage.RevealedPartialPaymentActionText.Should().Be(AppLabels.MarkPartialPaid);
        MainPage.TapSummaryBar();
        MainPage.WaitForActionsHidden();

        var popup = MainPage.OpenEntry("Mercado E2E");
        popup.AllowsPartialPayment.Should().BeTrue();
        popup.IsPaidAmountVisible.Should().BeTrue();

        (await GetEntryRowsAsync()).Single(e => e.Description == "Mercado E2E").AllowsPartialPayment.Should().BeTrue();
    }

    [Fact]
    public async Task AddEntry_IncomeWithFlag_ShowsReceivePartialAction()
    {
        MainPage.AddEntry("Freela E2E", AppLabels.OneTime, 1000m, isIncome: true, allowsPartialPayment: true);

        MainPage.RevealActions("Freela E2E");
        MainPage.RevealedPartialPaymentActionText.Should().Be(AppLabels.MarkPartialReceived);

        (await GetEntryRowsAsync()).Single(e => e.Description == "Freela E2E").AllowsPartialPayment.Should().BeTrue();
    }

    [Fact]
    public async Task EditEntry_TurnFlagOn_ShowsPaidAmountAndPartialPaymentAction()
    {
        var popup = MainPage.OpenEntry("Aluguel E2E");
        popup.IsPaidAmountVisible.Should().BeFalse();

        popup.SetAllowsPartialPayment(true);
        popup.IsPaidAmountVisible.Should().BeTrue();
        popup.Save();

        MainPage.RevealActions("Aluguel E2E");
        MainPage.RevealedPartialPaymentActionText.Should().Be(AppLabels.MarkPartialPaid);

        (await GetEntryRowsAsync()).Single(e => e.Description == "Aluguel E2E").AllowsPartialPayment.Should().BeTrue();
    }

    [Fact]
    public async Task EditEntry_TurnFlagOff_HidesPartialPaymentAction()
    {
        MainPage.OpenEntry("Mercado E2E").SetAllowsPartialPayment(false).Save();

        MainPage.RevealActions("Mercado E2E");
        MainPage.RevealedPartialPaymentActionText.Should().BeNull();

        (await GetEntryRowsAsync()).Single(e => e.Description == "Mercado E2E").AllowsPartialPayment.Should().BeFalse();
    }

    [Fact]
    public async Task EditEntry_TurnFlagOffWithPayments_KeepsPaymentsListedSoTheyCanBeRemoved()
    {
        MainPage.AddPartialPayment("Mercado E2E", 150m, "Feira");
        MainPage.WaitFor(() => MainPage.IsEntryPartiallyPaid("Mercado E2E"));

        MainPage.OpenEntry("Mercado E2E").SetAllowsPartialPayment(false).Save();

        MainPage.IsEntryPartiallyPaid("Mercado E2E").Should().BeTrue("turning the flag off keeps what was already paid");
        MainPage.RevealActions("Mercado E2E");
        MainPage.RevealedPartialPaymentActionText.Should().BeNull();
        MainPage.TapSummaryBar();
        MainPage.WaitForActionsHidden();

        var popup = MainPage.OpenEntry("Mercado E2E");
        popup.IsPaidAmountVisible.Should().BeTrue();
        popup.Payments.Should().ContainSingle(p => p.Description == "Feira");
        popup.DeletePayment("Feira").Save();

        MainPage.WaitFor(() => !MainPage.IsEntryPartiallyPaid("Mercado E2E"));
        (await GetPaymentRowsAsync()).Should().BeEmpty();
    }
}
