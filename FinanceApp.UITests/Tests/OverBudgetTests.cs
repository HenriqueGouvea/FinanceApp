using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class OverBudgetTests : E2ETestBase
{
    public OverBudgetTests(AppiumServerFixture server) : base(server)
    {
        MainPage.AddEntry("Mercado E2E", AppLabels.Recurrent, 800m, isEstimated: true);
    }

    [Fact]
    public async Task PartialPaymentAction_BeyondPlannedAmount_ShowsStruckPlannedAmountOnlyInThatMonth()
    {
        var first = MainPage.OpenPartialPayment("Mercado E2E").EnterAmount(500m);
        first.OverBudgetText.Should().BeNull("R$ 500,00 is within the planned R$ 800,00");
        first.Save();
        MainPage.WaitFor(() => MainPage.EntryPartialText("Mercado E2E") == AppFormat.Money(500));
        MainPage.EntryPlannedAmountText("Mercado E2E").Should().BeNull();

        var second = MainPage.OpenPartialPayment("Mercado E2E").EnterAmount(400m);
        second.OverBudgetText.Should().Be(AppFormat.OverBudget(100));
        second.Save();

        MainPage.WaitFor(() => MainPage.EntryPlannedAmountText("Mercado E2E") == AppFormat.StruckPlanned(800));
        MainPage.EntryAmountText("Mercado E2E").Should().Be(AppFormat.Money(900));
        MainPage.EntryPartialText("Mercado E2E").Should().BeNull("the struck planned amount replaces the amount paid so far");
        MainPage.IsEntryPaymentProgressVisible("Mercado E2E").Should().BeTrue();
        MainPage.IsEntryPartiallyPaid("Mercado E2E").Should().BeTrue("going over the estimate doesn't settle the month");
        MainPage.IsEntryPaid("Mercado E2E").Should().BeFalse();
        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(900));

        MainPage.GoToNextMonth().WaitForEntry("Mercado E2E");
        MainPage.EntryPlannedAmountText("Mercado E2E").Should().BeNull();
        MainPage.IsEntryPaymentProgressVisible("Mercado E2E").Should().BeFalse();
        MainPage.EntryAmountText("Mercado E2E").Should().Be(AppFormat.Money(800));

        (await GetEntryRowsAsync()).Single().Amount.Should().Be(800m, "going over budget only affects that month");
        var record = (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Subject;
        record.Status.Should().Be(AppFormat.PartiallyPaid);
        record.PaidAmount.Should().Be(900m);
    }

    [Fact]
    public async Task MarkPaid_BeyondPlannedAmount_KeepsStruckPlannedAmountAndProgress()
    {
        MainPage.AddPartialPayment("Mercado E2E", 900m);
        MainPage.WaitFor(() => MainPage.EntryPlannedAmountText("Mercado E2E") is not null);

        MainPage.MarkPaid("Mercado E2E");

        MainPage.VisibleEntryStatuses.Should().ContainSingle().Which.Should().Be(AppLabels.Paid);
        MainPage.EntryAmountText("Mercado E2E").Should().Be(AppFormat.Money(900));
        MainPage.EntryPlannedAmountText("Mercado E2E").Should().Be(AppFormat.StruckPlanned(800));
        MainPage.IsEntryPaymentProgressVisible("Mercado E2E").Should().BeTrue();

        var record = (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Subject;
        record.Status.Should().Be(AppFormat.Paid);
        record.PaidAmount.Should().Be(900m);
    }

    [Fact]
    public void PartialPaymentAction_OverBudgetEntry_ShowsProgressAgainstPlannedAmount()
    {
        MainPage.AddPartialPayment("Mercado E2E", 900m);
        MainPage.WaitFor(() => MainPage.EntryPlannedAmountText("Mercado E2E") is not null);

        var popup = MainPage.OpenPartialPayment("Mercado E2E");

        popup.ProgressText.Should().Be(AppFormat.PartialPaymentProgress(AppLabels.AlreadySpent, 900, 800));
        popup.OverBudgetText.Should().Be(AppFormat.OverBudget(100));
    }

    [Fact]
    public void SetPaidAmount_EditFieldBeyondPlannedAmount_ShowsStruckPlannedAmount()
    {
        MainPage.SetPaidAmount("Mercado E2E", 850m);

        MainPage.WaitFor(() => MainPage.EntryPlannedAmountText("Mercado E2E") == AppFormat.StruckPlanned(800));
        MainPage.EntryAmountText("Mercado E2E").Should().Be(AppFormat.Money(850));
    }

    [Fact]
    public async Task DeletePayment_BackWithinPlannedAmount_ShowsPaidSoFarAgain()
    {
        MainPage.AddPartialPayment("Mercado E2E", 500m, "Compra do mês");
        MainPage.AddPartialPayment("Mercado E2E", 400m, "Churrasco");
        MainPage.WaitFor(() => MainPage.EntryPlannedAmountText("Mercado E2E") == AppFormat.StruckPlanned(800));

        var popup = MainPage.OpenEntry("Mercado E2E");
        popup.DeletePayment("Churrasco");
        popup.Save();

        MainPage.WaitFor(() => MainPage.EntryPlannedAmountText("Mercado E2E") is null);
        MainPage.EntryPartialText("Mercado E2E").Should().Be(AppFormat.Money(500));
        MainPage.EntryAmountText("Mercado E2E").Should().Be(AppFormat.Money(800));
        (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Which.PaidAmount.Should().Be(500m);
    }

    [Fact]
    public async Task PartialPaymentAction_IncomeBeyondPlannedAmount_ShowsStruckPlannedAmount()
    {
        MainPage.AddEntry("Freela E2E", AppLabels.OneTime, 1000m, isIncome: true);

        MainPage.AddPartialPayment("Freela E2E", 1200m);

        MainPage.WaitFor(() => MainPage.EntryPlannedAmountText("Freela E2E") == AppFormat.StruckPlanned(1000));
        MainPage.EntryAmountText("Freela E2E").Should().Be(AppFormat.Money(1200));
        MainPage.SummaryReceivedText.Should().Be(AppFormat.Money(1200));
        MainPage.EntryPlannedAmountText("Mercado E2E").Should().BeNull("outcomes are untouched");

        var income = (await GetEntryRowsAsync()).Single(e => e.EntryType == AppFormat.Income);
        (await GetMonthlyEntryRowsAsync()).Should().ContainSingle(r => r.FinancialEntryId == income.Id)
            .Which.PaidAmount.Should().Be(1200m);
    }
}
