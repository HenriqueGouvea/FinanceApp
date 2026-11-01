using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class PaymentSourceTests : E2ETestBase
{
    public PaymentSourceTests(AppiumServerFixture server) : base(server)
    {
    }

    [Fact]
    public async Task OpenPaymentSources_FreshInstall_ShowsEmptyState()
    {
        var sources = MainPage.OpenPaymentSources();

        sources.IsEmptyStateVisible.Should().BeTrue();
        sources.VisibleNames.Should().BeEmpty();
        (await GetPaymentSourceRowsAsync()).Should().BeEmpty();
    }

    [Theory]
    [InlineData(AppFormat.BankAccount, AppLabels.BankAccountsHeader, AppLabels.Balance)]
    [InlineData(AppFormat.CreditCard, AppLabels.CreditCardsHeader, AppLabels.Limit)]
    [InlineData(AppFormat.MealVoucher, AppLabels.MealVouchersHeader, AppLabels.Balance)]
    public async Task AddPaymentSource_EachType_ListedUnderItsGroupWithBalanceAndPersists(int type, string header, string balanceLabel)
    {
        var sources = MainPage.OpenPaymentSources().AddPaymentSource("Fonte E2E", type, 1234.56m);

        sources.IsEmptyStateVisible.Should().BeFalse();
        sources.GroupOf("Fonte E2E").Should().Be(header);
        sources.BalanceOf("Fonte E2E").Should().Be(AppFormat.SourceBalance(balanceLabel, 1234.56m));

        var row = (await GetPaymentSourceRowsAsync()).Should().ContainSingle().Subject;
        row.Name.Should().Be("Fonte E2E");
        row.Type.Should().Be(type);
        row.Balance.Should().Be(1234.56m);
    }

    [Fact]
    public async Task AddPaymentSource_SeveralTypes_GroupsAccountsThenCardsThenVouchers()
    {
        var sources = MainPage.OpenPaymentSources()
            .AddPaymentSource("Alelo E2E", AppFormat.MealVoucher, 600m)
            .AddPaymentSource("Nubank E2E", AppFormat.CreditCard, 5000m)
            .AddPaymentSource("Itaú E2E", AppFormat.BankAccount, 2500m);

        sources.VisibleGroupHeaders.Should().Equal(
            AppLabels.BankAccountsHeader, AppLabels.CreditCardsHeader, AppLabels.MealVouchersHeader);
        (await GetPaymentSourceRowsAsync()).Should().HaveCount(3);
    }

    [Fact]
    public async Task EditPaymentSource_ChangesNameTypeAndBalance_UpdatesRowAndPersists()
    {
        var sources = MainPage.OpenPaymentSources()
            .AddPaymentSource("Conta E2E", AppFormat.BankAccount, 100m)
            .EditPaymentSource("Conta E2E", "Cartão E2E", AppFormat.CreditCard, 3000m);

        sources.VisibleNames.Should().Equal("Cartão E2E");
        sources.GroupOf("Cartão E2E").Should().Be(AppLabels.CreditCardsHeader);
        sources.BalanceOf("Cartão E2E").Should().Be(AppFormat.SourceBalance(AppLabels.Limit, 3000m));

        var row = (await GetPaymentSourceRowsAsync()).Should().ContainSingle().Subject;
        row.Name.Should().Be("Cartão E2E");
        row.Type.Should().Be(AppFormat.CreditCard);
        row.Balance.Should().Be(3000m);
    }

    [Fact]
    public void AddEntry_PickerLabelFollowsEntryTypeAndOffersEverySource()
    {
        var entryPage = MainPage.OpenPaymentSources()
            .AddPaymentSource("Itaú E2E", AppFormat.BankAccount, 2500m)
            .AddPaymentSource("Nubank E2E", AppFormat.CreditCard, 5000m)
            .GoToEntries()
            .OpenAddEntry();

        entryPage.PaymentSourceLabelText.Should().Be(AppLabels.PaidWith);
        entryPage.PaymentSourceOptions().Should().Equal("Itaú E2E", "Nubank E2E");

        entryPage.SelectIncome();

        entryPage.PaymentSourceLabelText.Should().Be(AppLabels.ReceivedIn);
        entryPage.PaymentSourceOptions().Should().Equal("Itaú E2E", "Nubank E2E");
    }

    [Fact]
    public async Task AddEntry_WithPaymentSource_ShowsSourceOnRowAndPersists()
    {
        MainPage.OpenPaymentSources()
            .AddPaymentSource("Nubank E2E", AppFormat.CreditCard, 5000m)
            .GoToEntries()
            .AddEntry("Netflix E2E", AppLabels.Recurrent, 55.9m, paymentSource: "Nubank E2E");

        MainPage.VisibleEntryPaymentSources.Should().ContainSingle().Which.Should().Be("Nubank E2E");

        var source = (await GetPaymentSourceRowsAsync()).Single();
        (await GetEntryRowsAsync()).Should().ContainSingle().Which.PaymentSourceId.Should().Be(source.Id);
    }

    [Fact]
    public async Task AddEntry_WithoutPaymentSource_ShowsNoSourceOnRow()
    {
        MainPage.OpenPaymentSources()
            .AddPaymentSource("Nubank E2E", AppFormat.CreditCard, 5000m)
            .GoToEntries()
            .AddEntry("Padaria E2E", AppLabels.OneTime, 20m);

        MainPage.VisibleEntryPaymentSources.Should().BeEmpty();
        (await GetEntryRowsAsync()).Should().ContainSingle().Which.PaymentSourceId.Should().BeNull();
    }

    [Fact]
    public async Task EditEntry_ChangePaymentSource_UpdatesRowAndPersists()
    {
        MainPage.OpenPaymentSources()
            .AddPaymentSource("Itaú E2E", AppFormat.BankAccount, 2500m)
            .AddPaymentSource("Nubank E2E", AppFormat.CreditCard, 5000m)
            .GoToEntries()
            .AddEntry("Aluguel E2E", AppLabels.Recurrent, 1500m, paymentSource: "Itaú E2E");

        var entryPage = MainPage.OpenEntry("Aluguel E2E");
        entryPage.PaymentSourceText.Should().Be("Itaú E2E");
        entryPage.SelectPaymentSource("Nubank E2E").Save();

        MainPage.WaitForEntryPaymentSource("Nubank E2E");
        MainPage.VisibleEntryPaymentSources.Should().ContainSingle().Which.Should().Be("Nubank E2E");

        var nubank = (await GetPaymentSourceRowsAsync()).Single(s => s.Name == "Nubank E2E");
        (await GetEntryRowsAsync()).Should().ContainSingle().Which.PaymentSourceId.Should().Be(nubank.Id);
    }

    [Fact]
    public void RenamePaymentSource_UsedByEntry_RowShowsNewName()
    {
        MainPage.OpenPaymentSources()
            .AddPaymentSource("Conta E2E", AppFormat.BankAccount, 100m)
            .GoToEntries()
            .AddEntry("Luz E2E", AppLabels.Recurrent, 180m, paymentSource: "Conta E2E");

        MainPage.OpenPaymentSources()
            .EditPaymentSource("Conta E2E", "Inter E2E", AppFormat.BankAccount, 100m)
            .GoToEntries()
            .WaitForEntryPaymentSource("Inter E2E");

        MainPage.VisibleEntryPaymentSources.Should().ContainSingle().Which.Should().Be("Inter E2E");
    }

    [Fact]
    public async Task DeletePaymentSource_UsedByEntry_KeepsEntryWithoutSource()
    {
        MainPage.OpenPaymentSources()
            .AddPaymentSource("Nubank E2E", AppFormat.CreditCard, 5000m)
            .GoToEntries()
            .AddEntry("Netflix E2E", AppLabels.Recurrent, 55.9m, paymentSource: "Nubank E2E");

        var sources = MainPage.OpenPaymentSources().DeletePaymentSource("Nubank E2E");
        sources.IsEmptyStateVisible.Should().BeTrue();

        sources.GoToEntries().WaitForEntry("Netflix E2E");

        MainPage.VisibleEntryPaymentSources.Should().BeEmpty();
        (await GetPaymentSourceRowsAsync()).Should().BeEmpty();
        (await GetEntryRowsAsync()).Should().ContainSingle().Which.PaymentSourceId.Should().BeNull();
    }
}
