using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class PaymentRecordsTests : E2ETestBase
{
    public PaymentRecordsTests(AppiumServerFixture server) : base(server)
    {
        MainPage.AddEntry("Mercado E2E", AppLabels.Recurrent, 800m, allowsPartialPayment: true);
    }

    [Fact]
    public async Task PartialPaymentAction_WithDescription_RecordsPaymentDatedToday()
    {
        MainPage.AddPartialPayment("Mercado E2E", 150m, "Feira");

        MainPage.WaitFor(() => MainPage.EntryPartialText("Mercado E2E") == AppFormat.Money(150));

        var record = (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Subject;
        var payment = (await GetPaymentRowsAsync()).Should().ContainSingle().Subject;
        payment.FinancialMonthlyEntryId.Should().Be(record.Id);
        payment.Amount.Should().Be(150m);
        payment.Description.Should().Be("Feira");
        payment.Date.Date.Should().Be(DateTime.Today);
    }

    [Fact]
    public void OpenEntry_NoPayments_HidesPaymentsList()
    {
        MainPage.OpenEntry("Mercado E2E").PaymentsHeaderText.Should().BeNull();
    }

    [Fact]
    public void OpenEntry_AfterPayments_ListsThemWithDateDescriptionAndAmount()
    {
        MainPage.AddPartialPayment("Mercado E2E", 150m, "Feira");
        MainPage.AddPartialPayment("Mercado E2E", 100m);

        var popup = MainPage.OpenEntry("Mercado E2E");

        popup.PaymentsHeaderText.Should().Be(AppLabels.PaymentsOutcome);
        popup.Payments.Should().Equal(
            (AppFormat.PaymentDate(DateTime.Today), "Feira", AppFormat.Money(150)),
            (AppFormat.PaymentDate(DateTime.Today), (string?)null, AppFormat.Money(100)));
        popup.PaidAmountText.Should().Be(AppFormat.Money(250));
    }

    [Fact]
    public void OpenEntry_PaymentsInAnotherMonth_ListsOnlyThatMonthsPayments()
    {
        MainPage.AddPartialPayment("Mercado E2E", 150m, "Feira");

        MainPage.GoToNextMonth().WaitForEntry("Mercado E2E");

        MainPage.OpenEntry("Mercado E2E").PaymentsHeaderText.Should().BeNull();
    }

    [Fact]
    public async Task DeletePayment_ThenSave_SubtractsItFromPaidAmount()
    {
        MainPage.AddPartialPayment("Mercado E2E", 150m, "Feira");
        MainPage.AddPartialPayment("Mercado E2E", 100m, "Padaria");

        var popup = MainPage.OpenEntry("Mercado E2E");
        popup.DeletePayment("Padaria");
        popup.PaidAmountText.Should().Be(AppFormat.Money(150));
        popup.Payments.Should().ContainSingle().Which.Description.Should().Be("Feira");
        popup.Save();

        MainPage.WaitFor(() => MainPage.EntryPartialText("Mercado E2E") == AppFormat.Money(150));
        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(150));

        (await GetPaymentRowsAsync()).Should().ContainSingle().Which.Description.Should().Be("Feira");
        var record = (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Subject;
        record.Status.Should().Be(AppFormat.PartiallyPaid);
        record.PaidAmount.Should().Be(150m);
    }

    [Fact]
    public async Task DeletePayment_ThenCancel_KeepsPayment()
    {
        MainPage.AddPartialPayment("Mercado E2E", 150m, "Feira");

        var popup = MainPage.OpenEntry("Mercado E2E");
        popup.DeletePayment("Feira");
        popup.Cancel();

        MainPage.EntryPartialText("Mercado E2E").Should().Be(AppFormat.Money(150));
        (await GetPaymentRowsAsync()).Should().ContainSingle();
        MainPage.OpenEntry("Mercado E2E").Payments.Should().ContainSingle().Which.Description.Should().Be("Feira");
    }

    [Fact]
    public async Task DeletePayment_OnlyPayment_RevertsEntryToPending()
    {
        MainPage.AddPartialPayment("Mercado E2E", 150m, "Feira");

        var popup = MainPage.OpenEntry("Mercado E2E");
        popup.DeletePayment("Feira");
        popup.PaymentsHeaderText.Should().BeNull();
        popup.Save();

        MainPage.WaitFor(() => !MainPage.IsEntryPartiallyPaid("Mercado E2E"));
        MainPage.IsEntryPaid("Mercado E2E").Should().BeFalse();
        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(0));

        (await GetPaymentRowsAsync()).Should().BeEmpty();
        var record = (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Subject;
        record.Status.Should().Be(AppFormat.Pending);
        record.PaidAmount.Should().BeNull();
    }

    [Fact]
    public async Task MarkPaidThenPending_EntryWithPayments_KeepsHistoryUntilReset()
    {
        MainPage.AddPartialPayment("Mercado E2E", 150m, "Feira");

        MainPage.MarkPaid("Mercado E2E");
        var popup = MainPage.OpenEntry("Mercado E2E");
        popup.Payments.Should().ContainSingle().Which.Description.Should().Be("Feira");
        popup.Cancel();
        (await GetPaymentRowsAsync()).Should().ContainSingle();

        MainPage.MarkPending("Mercado E2E");

        (await GetPaymentRowsAsync()).Should().BeEmpty("reverting to pending resets the month");
    }

    [Fact]
    public async Task OpenEntry_IncomeWithReceipts_ListsThem()
    {
        MainPage.AddEntry("Freela E2E", AppLabels.OneTime, 1000m, isIncome: true, allowsPartialPayment: true);
        MainPage.AddPartialPayment("Freela E2E", 400m, "Adiantamento");

        var popup = MainPage.OpenEntry("Freela E2E");

        popup.PaymentsHeaderText.Should().Be(AppLabels.PaymentsIncome);
        popup.Payments.Should().ContainSingle().Which.Should().Be(
            (AppFormat.PaymentDate(DateTime.Today), "Adiantamento", AppFormat.Money(400)));

        var income = (await GetEntryRowsAsync()).Single(e => e.EntryType == AppFormat.Income);
        var record = (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Subject;
        record.FinancialEntryId.Should().Be(income.Id);
        (await GetPaymentRowsAsync()).Should().ContainSingle().Which.FinancialMonthlyEntryId.Should().Be(record.Id);
    }

    [Fact]
    public async Task PartialPaymentAction_FutureMonth_DatesPaymentOnItsFirstDay()
    {
        var nextMonth = AppFormat.CurrentMonth.AddMonths(1);

        MainPage.GoToNextMonth().AddPartialPayment("Mercado E2E", 150m, "Antecipado");

        MainPage.WaitFor(() => MainPage.IsEntryPartiallyPaid("Mercado E2E"));
        var record = (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Subject;
        record.Month.Should().Be(nextMonth.Month);
        var payment = (await GetPaymentRowsAsync()).Should().ContainSingle().Subject;
        payment.Date.Date.Should().Be(nextMonth, "the date defaults to today, kept within the month being paid");
    }
}
