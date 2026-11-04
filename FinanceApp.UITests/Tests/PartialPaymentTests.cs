using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;
using FluentAssertions.Execution;

namespace FinanceApp.UITests.Tests;

/// <summary>
/// The two ways to record what was paid in a month: "Pagar parcial" and the entry page's "Valor já pago neste mês".
/// The status and amount rules (reaching or passing the planned amount, settling, income labels) are covered by the
/// FinanceService, MonthlyEntryProjection and PartialPaymentViewModel tests.
/// </summary>
[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class PartialPaymentTests : E2ETestBase
{
    public PartialPaymentTests(AppiumServerFixture server) : base(server, data =>
        data.AddEntry("Mercado E2E", 800m, allowsPartialPayment: true))
    {
    }

    [Fact]
    [Trait("Smoke", "true")]
    public async Task PartialPaymentAction_TwicePaymentsThenMarkPaid_AddsUpAndSettlesTheMonth()
    {
        MainPage.OpenPartialPayment("Mercado E2E").EnterAmount(50m).Cancel();
        MainPage.IsEntryPartiallyPaid("Mercado E2E").Should().BeFalse("a cancelled payment saves nothing");

        MainPage.RevealActions("Mercado E2E");
        MainPage.RevealedPartialPaymentActionText.Should().Be(AppLabels.MarkPartialPaid);
        MainPage.TapSummaryBar();
        MainPage.WaitForActionsHidden();

        var popup = MainPage.OpenPartialPayment("Mercado E2E");
        using (new AssertionScope())
        {
            popup.TitleText.Should().Be(AppLabels.MarkPartialPaid);
            popup.DescriptionText.Should().Be("Mercado E2E");
            popup.ProgressText.Should().Be(AppFormat.PartialPaymentProgress(AppLabels.AlreadySpent, 0, 800));
        }
        popup.EnterAmount(150m);
        popup.NewTotalText.Should().Be(AppFormat.PartialPaymentNewTotal(150));
        popup.EnterPaymentDescription("Feira").Save();

        MainPage.WaitFor(() => MainPage.EntryPaidSoFarText("Mercado E2E") == AppFormat.Money(150));
        MainPage.EntryOfPlannedText("Mercado E2E").Should().Be(AppFormat.OfPlanned(800));
        MainPage.IsEntryPartiallyPaid("Mercado E2E").Should().BeTrue();
        MainPage.IsEntryPaymentProgressVisible("Mercado E2E").Should().BeTrue();

        popup = MainPage.OpenPartialPayment("Mercado E2E");
        popup.ProgressText.Should().Be(AppFormat.PartialPaymentProgress(AppLabels.AlreadySpent, 150, 800));
        popup.EnterAmount(100m).Save();

        MainPage.WaitFor(() => MainPage.EntryPaidSoFarText("Mercado E2E") == AppFormat.Money(250));
        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(250));
        MainPage.SummaryToPayText.Should().Be(AppFormat.Money(550));

        var record = (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Subject;
        record.Month.Should().Be(AppFormat.CurrentMonth.Month);
        record.Status.Should().Be(AppFormat.PartiallyPaid);
        record.PaidAmount.Should().Be(250m);
        var payments = await GetPaymentRowsAsync();
        payments.Select(p => (p.FinancialMonthlyEntryId, p.Amount, p.Description ?? string.Empty, p.Date.Date)).Should().Equal(
            (record.Id, 150m, "Feira", DateTime.Today),
            (record.Id, 100m, string.Empty, DateTime.Today));

        MainPage.MarkPaid("Mercado E2E");

        MainPage.VisibleEntryStatuses.Should().ContainSingle().Which.Should().Be(AppLabels.Paid);
        MainPage.EntryPaidSoFarText("Mercado E2E").Should().BeNull();
        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(800));
        (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Which.Status.Should().Be(AppFormat.Paid);
    }

    [Fact]
    public async Task SetPaidAmount_ThenClear_ShowsPartialPaymentOnlyInThatMonthAndRevertsToPending()
    {
        var page = MainPage.OpenEntry("Mercado E2E");
        page.PaidAmountLabelText.Should().Be(AppLabels.PaidAmountOutcome);
        page.PaidAmountText.Should().Be(AppFormat.Money(0));
        page.EnterPaidAmount(250m).Save();

        MainPage.WaitFor(() => MainPage.IsEntryPartiallyPaid("Mercado E2E"));
        using (new AssertionScope())
        {
            MainPage.EntryPaidSoFarText("Mercado E2E").Should().Be(AppFormat.Money(250));
            MainPage.EntryOfPlannedText("Mercado E2E").Should().Be(AppFormat.OfPlanned(800));
            MainPage.IsEntryPaid("Mercado E2E").Should().BeFalse();
            MainPage.SummaryPaidText.Should().Be(AppFormat.Money(250));
        }

        MainPage.GoToNextMonth().WaitForEntry("Mercado E2E");
        MainPage.IsEntryPartiallyPaid("Mercado E2E").Should().BeFalse();
        MainPage.GoToPreviousMonth().WaitForEntry("Mercado E2E");

        var entry = (await GetEntryRowsAsync()).Single();
        entry.Amount.Should().Be(800m, "the partial payment belongs to the month, not the template");
        var record = (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Subject;
        record.Status.Should().Be(AppFormat.PartiallyPaid);
        record.PaidAmount.Should().Be(250m);

        page = MainPage.OpenEntry("Mercado E2E");
        page.PaidAmountText.Should().Be(AppFormat.Money(250));
        page.ClearPaidAmount().Save();

        MainPage.WaitFor(() => !MainPage.IsEntryPartiallyPaid("Mercado E2E"));
        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(0));
        record = (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Subject;
        record.Status.Should().Be(AppFormat.Pending);
        record.PaidAmount.Should().BeNull();
    }
}
