using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;
using FluentAssertions.Execution;

namespace FinanceApp.UITests.Tests;

/// <summary>
/// The "Contas e cartões" page and linking an entry to a source. Credit cards are covered by
/// <see cref="CreditCardBillTests"/>; the picker's labels and options by the AddEntryViewModel tests.
/// </summary>
[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class PaymentSourceTests : E2ETestBase
{
    private const string Itau = "Itaú E2E";
    private const string Inter = "Inter E2E";
    private const string Rent = "Aluguel E2E";

    public PaymentSourceTests(AppiumServerFixture server) : base(server, data =>
    {
        var itau = data.AddPaymentSource(Itau, AppFormat.BankAccount, 2500m);
        data.AddPaymentSource(Inter, AppFormat.BankAccount, 100m);
        data.AddEntry(Rent, 1500m, paymentSourceId: itau);
    })
    {
    }

    [Fact]
    [Trait("Smoke", "true")]
    public async Task AddPaymentSource_VoucherAndCard_ListedUnderTheirGroupsWithBalanceAndPersisted()
    {
        var sources = MainPage.OpenPaymentSources()
            .AddPaymentSource("Alelo E2E", AppFormat.MealVoucher, 1234.56m)
            .AddPaymentSource("Visa E2E", AppFormat.CreditCard, 5000m);

        using (new AssertionScope())
        {
            sources.VisibleGroupHeaders.Should().Equal(
                AppLabels.BankAccountsHeader, AppLabels.CreditCardsHeader, AppLabels.MealVouchersHeader);
            sources.GroupOf("Alelo E2E").Should().Be(AppLabels.MealVouchersHeader);
            sources.BalanceOf("Alelo E2E").Should().Be(AppFormat.SourceBalance(AppLabels.Balance, 1234.56m));
            sources.GroupOf("Visa E2E").Should().Be(AppLabels.CreditCardsHeader);
            sources.BalanceOf("Visa E2E").Should().Be(AppFormat.SourceBalance(AppLabels.Limit, 5000m));
        }

        (await GetPaymentSourceRowsAsync()).Where(s => s.Name is "Alelo E2E" or "Visa E2E")
            .Select(s => (s.Name, s.Type, s.Balance)).Should().BeEquivalentTo(
            [
                ("Alelo E2E", AppFormat.MealVoucher, 1234.56m),
                ("Visa E2E", AppFormat.CreditCard, 5000m)
            ]);
    }

    [Fact]
    public async Task EditPaymentSource_UsedByEntry_UpdatesSourcesPageEntryRowAndDatabase()
    {
        MainPage.VisibleEntryPaymentSources.Should().ContainSingle().Which.Should().Be(Itau);

        var sources = MainPage.OpenPaymentSources().EditPaymentSource(Itau, "Bradesco E2E", AppFormat.BankAccount, 3000m);

        sources.VisibleNames.Should().Equal("Bradesco E2E", Inter);
        sources.BalanceOf("Bradesco E2E").Should().Be(AppFormat.SourceBalance(AppLabels.Balance, 3000m));
        sources.GoToEntries().WaitForEntryPaymentSource("Bradesco E2E");

        var row = (await GetPaymentSourceRowsAsync()).Single(s => s.Name == "Bradesco E2E");
        row.Balance.Should().Be(3000m);
        (await GetEntryRowsAsync()).Single().PaymentSourceId.Should().Be(row.Id);
    }

    [Fact]
    public async Task EditEntry_ChangePaymentSource_UpdatesRowAndPersists()
    {
        var page = MainPage.OpenEntry(Rent);
        page.PaymentSourceText.Should().Be(Itau);
        page.SelectPaymentSource(Inter).Save();

        MainPage.WaitForEntryPaymentSource(Inter);
        MainPage.VisibleEntryPaymentSources.Should().ContainSingle().Which.Should().Be(Inter);

        var inter = (await GetPaymentSourceRowsAsync()).Single(s => s.Name == Inter);
        (await GetEntryRowsAsync()).Single().PaymentSourceId.Should().Be(inter.Id);
    }

    [Fact]
    public async Task DeletePaymentSource_UsedByEntry_KeepsEntryWithoutSource()
    {
        MainPage.OpenPaymentSources()
            .DeletePaymentSource(Itau)
            .GoToEntries()
            .WaitForEntry(Rent);

        MainPage.VisibleEntryPaymentSources.Should().BeEmpty();
        (await GetPaymentSourceRowsAsync()).Select(s => s.Name).Should().Equal(Inter);
        (await GetEntryRowsAsync()).Single().PaymentSourceId.Should().BeNull();
    }
}
