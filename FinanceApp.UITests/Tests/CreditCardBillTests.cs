using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;
using FluentAssertions.Execution;

namespace FinanceApp.UITests.Tests;

/// <summary>
/// Setting up a card's bill, buying with the card, the bill row, paying it and the available limit. Which entries a
/// bill groups, its status, due date and limit rules are covered by the FinanceService, PaymentSourceService,
/// MonthViewModel, CreditCardBill and AddEntryViewModel tests.
/// </summary>
[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class CreditCardBillTests : E2ETestBase
{
    private const string Account = "Itaú E2E";
    private const string Card = "Nubank E2E";
    private const string Netflix = "Netflix E2E";
    private const string Market = "Mercado E2E";
    private const string Rent = "Aluguel E2E";

    // Never today, so the bill reads "Vence dd/MM" rather than "Vence hoje".
    private static readonly int BillDueDay = DateTime.Today.Day == 10 ? 11 : 10;
    private static readonly DateTime BillDueDate =
        new(AppFormat.CurrentMonth.Year, AppFormat.CurrentMonth.Month, BillDueDay);

    public CreditCardBillTests(AppiumServerFixture server) : base(server, data =>
    {
        var account = data.AddPaymentSource(Account, AppFormat.BankAccount, 2500m);
        var card = data.AddPaymentSource(Card, AppFormat.CreditCard, 5000m, BillDueDay, account);
        data.AddEntry(Netflix, 55.9m, paymentSourceId: card);
        data.AddEntry(Market, 600m, AppFormat.OneTime, paymentSourceId: card);
        data.AddEntry(Rent, 1500m, paymentSourceId: account);
    })
    {
    }

    [Fact]
    public async Task AddCreditCard_WithBillDueDayAndAccount_Persists()
    {
        MainPage.OpenPaymentSources()
            .AddPaymentSource("Inter E2E", AppFormat.BankAccount, 100m)
            .AddCreditCard("Visa E2E", 3000m, BillDueDay, "Inter E2E");

        var sources = await GetPaymentSourceRowsAsync();
        var account = sources.Single(s => s.Name == "Inter E2E");
        var card = sources.Single(s => s.Name == "Visa E2E");
        card.Type.Should().Be(AppFormat.CreditCard);
        card.BillDueDay.Should().Be(BillDueDay);
        card.BillPaymentSourceId.Should().Be(account.Id);
        account.BillDueDay.Should().BeNull();
        account.BillPaymentSourceId.Should().BeNull();
    }

    [Fact]
    public async Task EntryPage_OutcomeOnCreditCard_FallsDueWithTheBillAndHidesEstimatedAndPartialPayment()
    {
        var page = MainPage.OpenAddEntry()
            .EnterDescription("Spotify E2E")
            .SelectRecurrence(AppLabels.Recurrent)
            .EnterAmount(21.9m)
            .SelectDueDay(25)
            .SetEstimated(true)
            .SetAllowsPartialPayment(true)
            .SelectPaymentSource(Card);

        using (new AssertionScope())
        {
            page.IsDueDayPickerVisible.Should().BeFalse();
            page.CreditCardBillDueHintText.Should().Be(AppLabels.CreditCardBillDueHintFor(BillDueDay));
            page.IsEstimatedSwitchVisible.Should().BeFalse();
            page.IsPartialPaymentSwitchVisible.Should().BeFalse();
        }

        page.SelectPaymentSource(Account);
        using (new AssertionScope())
        {
            page.IsDueDayPickerVisible.Should().BeTrue();
            page.DueDayText.Should().Be(AppLabels.DueDay(25), "switching back keeps the day picked before");
            page.IsEstimatedSwitchVisible.Should().BeTrue();
            page.IsPartialPaymentSwitchVisible.Should().BeTrue();
        }

        page.SelectPaymentSource(Card).Save();
        MainPage.WaitFor(() => MainPage.BillCountText(Card) == AppFormat.EntryCount(3));

        var entry = (await GetEntryRowsAsync()).Single(e => e.Description == "Spotify E2E");
        entry.DueDay.Should().BeNull("the entry falls due with the bill");
        entry.IsEstimated.Should().BeFalse("a credit card purchase has a known amount");
        entry.AllowsPartialPayment.Should().BeFalse("a credit card purchase is paid with the bill");
    }

    [Fact]
    public void Bill_CardOutcomes_GroupedWithTotalDueDateAndAccountAndCollapsible()
    {
        using (new AssertionScope())
        {
            MainPage.VisibleBillNames.Should().Equal(AppLabels.BillName(Card));
            MainPage.BillAmountText(Card).Should().Be(AppFormat.Money(655.9m));
            MainPage.BillCountText(Card).Should().Be(AppFormat.EntryCount(2));
            MainPage.BillDueDateText(Card).Should().Be(AppFormat.DueDate(BillDueDate));
            MainPage.BillAccountText(Card).Should().Be(AppLabels.BillAccount(Account));
            MainPage.BillEntryDescriptions(Card).Should().Equal(Netflix, Market);
            MainPage.EntryDueDateText(Netflix).Should().BeNull("entries on a bill fall due with it, so only the bill shows the date");
            MainPage.OutcomeTotalText.Should().Be(AppFormat.Money(2155.9m), "the bill groups entries that are already counted");
            MainPage.OutcomeCountText.Should().Be(AppFormat.EntryCount(3));
        }

        MainPage.ToggleBill(Card);
        MainPage.BillEntryDescriptions(Card).Should().BeEmpty();
        MainPage.IsEntryVisible(Netflix).Should().BeFalse();
        MainPage.BillAmountText(Card).Should().Be(AppFormat.Money(655.9m));

        MainPage.ToggleBill(Card);
        MainPage.BillEntryDescriptions(Card).Should().Equal(Netflix, Market);
    }

    [Fact]
    [Trait("Smoke", "true")]
    public async Task PayBill_ThenMarkPending_PaysAndResetsEveryEntryOnItOnlyThatMonth()
    {
        // Collapsed, so the account's entry listed after the bill stays on screen on small devices (CI's 320×640).
        MainPage.ToggleBill(Card);
        MainPage.RevealBillActions(Card);
        MainPage.RevealedBillActionText.Should().Be(AppLabels.MarkBillPaid);
        MainPage.PayBill(Card);

        MainPage.WaitFor(() => MainPage.IsBillPaid(Card));
        MainPage.IsEntryPaid(Rent).Should().BeFalse("only the card's entries are on its bill");
        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(655.9m));
        MainPage.SummaryToPayText.Should().Be(AppFormat.Money(1500m));

        var entries = await GetEntryRowsAsync();
        var cardEntryIds = entries.Where(e => e.Description is Netflix or Market).Select(e => e.Id);
        var records = await GetMonthlyEntryRowsAsync();
        records.Select(r => r.FinancialEntryId).Should().BeEquivalentTo(cardEntryIds);
        records.Should().OnlyContain(r =>
            r.Status == AppFormat.Paid && r.Year == AppFormat.CurrentMonth.Year && r.Month == AppFormat.CurrentMonth.Month);

        MainPage.RevealBillActions(Card);
        MainPage.RevealedBillActionText.Should().Be(AppLabels.MarkPending);
        MainPage.MarkBillPending(Card);

        MainPage.WaitFor(() => !MainPage.IsBillPaid(Card));
        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(0));
        (await GetMonthlyEntryRowsAsync()).Should().OnlyContain(r => r.Status == AppFormat.Pending);
    }

    [Fact]
    public void AvailableLimit_SubtractsUnpaidOutcomesAndFreesUpWhenTheBillIsPaid()
    {
        var sources = MainPage.OpenPaymentSources().WaitForAvailableLimit(Card, AppFormat.AvailableLimit(4344.1m));
        sources.BalanceOf(Card).Should().Be(AppFormat.SourceBalance(AppLabels.Limit, 5000m), "the limit itself stays as typed");
        sources.AvailableLimitOf(Account).Should().BeNull("only credit cards have a limit");
        sources.GoToEntries()
            .WaitForMenuBalance(Card, AppFormat.Money(4344.1m))
            .WaitForMenuBalance(Account, AppFormat.Money(2500m))
            .PayBill(Card);

        MainPage.WaitForMenuBalance(Card, AppFormat.Money(5000m));
        MainPage.OpenPaymentSources().WaitForAvailableLimit(Card, AppFormat.AvailableLimit(5000m));
    }

    [Fact]
    public void EditCreditCardBill_ChangesDueDateAndAccountOnTheBill()
    {
        var newDueDay = BillDueDay + 5;

        MainPage.OpenPaymentSources()
            .AddPaymentSource("Inter E2E", AppFormat.BankAccount, 100m)
            .EditCreditCardBill(Card, newDueDay, "Inter E2E")
            .GoToEntries()
            .WaitForBill(Card);

        MainPage.WaitFor(() => MainPage.BillAccountText(Card) == AppLabels.BillAccount("Inter E2E"));
        MainPage.BillDueDateText(Card).Should().Be(AppFormat.DueDate(BillDueDate.AddDays(5)));
    }
}
