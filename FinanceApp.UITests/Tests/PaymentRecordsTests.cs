using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

/// <summary>
/// The month's payments listed in the entry page. Recording them is covered by <see cref="PartialPaymentTests"/>;
/// the rules for removing them (reverting to pending, other months, reset by "Pendente") by the FinanceService tests.
/// </summary>
[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class PaymentRecordsTests : E2ETestBase
{
    public PaymentRecordsTests(AppiumServerFixture server) : base(server, data =>
    {
        var market = data.AddEntry("Mercado E2E", 800m, allowsPartialPayment: true);
        data.AddPayment(market, 150m, "Feira");
        data.AddPayment(market, 100m);
    })
    {
    }

    [Fact]
    public async Task DeletePayment_CancelThenSave_KeepsItThenSubtractsItFromPaidAmount()
    {
        var page = MainPage.OpenEntry("Mercado E2E");
        page.PaymentsHeaderText.Should().Be(AppLabels.PaymentsOutcome);
        page.Payments.Should().Equal(
            (AppFormat.PaymentDate(DateTime.Today), "Feira", AppFormat.Money(150)),
            (AppFormat.PaymentDate(DateTime.Today), (string?)null, AppFormat.Money(100)));
        page.PaidAmountText.Should().Be(AppFormat.Money(250));

        page.DeletePayment("Feira").Cancel();

        MainPage.EntryPartialText("Mercado E2E").Should().Be(AppFormat.Money(250));
        (await GetPaymentRowsAsync()).Should().HaveCount(2);

        page = MainPage.OpenEntry("Mercado E2E");
        page.DeletePayment("Feira");
        page.PaidAmountText.Should().Be(AppFormat.Money(100));
        page.Save();

        MainPage.WaitFor(() => MainPage.EntryPartialText("Mercado E2E") == AppFormat.Money(100));
        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(100));
        (await GetPaymentRowsAsync()).Should().ContainSingle().Which.Amount.Should().Be(100m);
        var record = (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Subject;
        record.Status.Should().Be(AppFormat.PartiallyPaid);
        record.PaidAmount.Should().Be(100m);
    }
}
