using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

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

    public CreditCardBillTests(AppiumServerFixture server) : base(server)
    {
    }

    // --- Card setup ---

    [Theory]
    [InlineData(AppFormat.BankAccount, false)]
    [InlineData(AppFormat.CreditCard, true)]
    [InlineData(AppFormat.MealVoucher, false)]
    public void PaymentSourcePopup_BillFields_OnlyShowForCreditCards(int type, bool expected)
    {
        MainPage.OpenPaymentSources().AreBillFieldsVisibleFor(type).Should().Be(expected);
    }

    [Fact]
    public async Task AddCreditCard_WithBillDueDayAndAccount_Persists()
    {
        MainPage.OpenPaymentSources()
            .AddPaymentSource(Account, AppFormat.BankAccount, 2500m)
            .AddCreditCard(Card, 5000m, BillDueDay, Account);

        var sources = await GetPaymentSourceRowsAsync();
        var account = sources.Single(s => s.Name == Account);
        var card = sources.Single(s => s.Name == Card);
        card.BillDueDay.Should().Be(BillDueDay);
        card.BillPaymentSourceId.Should().Be(account.Id);
        account.BillDueDay.Should().BeNull();
        account.BillPaymentSourceId.Should().BeNull();
    }

    [Fact]
    public void BillPaymentAccount_OffersOnlyBankAccounts()
    {
        var sources = MainPage.OpenPaymentSources()
            .AddPaymentSource(Account, AppFormat.BankAccount, 2500m)
            .AddPaymentSource("Alelo E2E", AppFormat.MealVoucher, 600m)
            .AddPaymentSource("Visa E2E", AppFormat.CreditCard, 3000m)
            .AddCreditCard(Card, 5000m);

        sources.BillPaymentAccountOptions(Card).Should().Equal(Account);
    }

    [Fact]
    public void BillPaymentAccount_NoBankAccount_ExplainsHowToAddOne()
    {
        var sources = MainPage.OpenPaymentSources().AddCreditCard(Card, 5000m);

        sources.BillPaymentAccountOptions(Card).Should().BeEmpty();
    }

    // --- Bill row ---

    [Fact]
    public void AddOutcomesOnCreditCard_GroupedUnderBillWithTotalDueDateAndAccount()
    {
        AddAccountAndCard(BillDueDay, Account);
        MainPage
            .AddEntry(Netflix, AppLabels.Recurrent, 55.9m, paymentSource: Card)
            .AddEntry(Market, AppLabels.Recurrent, 600m, paymentSource: Card)
            .AddEntry(Rent, AppLabels.Recurrent, 1500m, paymentSource: Account);

        MainPage.VisibleBillNames.Should().Equal(AppLabels.BillName(Card));
        MainPage.BillAmountText(Card).Should().Be(AppFormat.Money(655.9m));
        MainPage.BillCountText(Card).Should().Be(AppFormat.EntryCount(2));
        MainPage.BillDueDateText(Card).Should().Be(AppFormat.DueDate(BillDueDate));
        MainPage.BillAccountText(Card).Should().Be(AppLabels.BillAccount(Account));
        MainPage.BillEntryDescriptions(Card).Should().Equal(Netflix, Market);
        MainPage.EntryDueDateText(Netflix).Should().BeNull("entries on a bill fall due with it, so only the bill shows the date");

        MainPage.OutcomeTotalText.Should().Be(AppFormat.Money(2155.9m), "the bill groups entries that are already counted");
        MainPage.OutcomeCountText.Should().Be(AppFormat.EntryCount(3));
        MainPage.SummaryOutcomeText.Should().Be(AppFormat.Money(2155.9m));
    }

    [Fact]
    public void Bill_CardWithoutDueDayOrAccount_ShowsNeither()
    {
        MainPage.OpenPaymentSources().AddCreditCard(Card, 5000m).GoToEntries();
        MainPage.AddEntry(Netflix, AppLabels.Recurrent, 55.9m, paymentSource: Card);

        MainPage.BillDueDateText(Card).Should().BeNull();
        MainPage.BillAccountText(Card).Should().BeNull();
    }

    [Fact]
    public void IncomeReceivedOnCreditCard_IsNotOnTheBill()
    {
        AddAccountAndCard(BillDueDay, Account);
        MainPage.AddEntry("Estorno E2E", AppLabels.Recurrent, 30m, isIncome: true, paymentSource: Card);

        MainPage.VisibleBillNames.Should().BeEmpty();
    }

    [Fact]
    public void TapBill_CollapsesAndExpandsItsEntries()
    {
        AddAccountAndCard(BillDueDay, Account);
        MainPage.AddEntry(Netflix, AppLabels.Recurrent, 55.9m, paymentSource: Card);

        MainPage.ToggleBill(Card);
        MainPage.BillEntryDescriptions(Card).Should().BeEmpty();
        MainPage.IsEntryVisible(Netflix).Should().BeFalse();
        MainPage.BillAmountText(Card).Should().Be(AppFormat.Money(55.9m));

        MainPage.ToggleBill(Card);
        MainPage.BillEntryDescriptions(Card).Should().Equal(Netflix);
    }

    [Fact]
    public void Bill_RecurrentEntries_ShowsInEveryMonthWithThatMonthsDueDate()
    {
        AddAccountAndCard(BillDueDay, Account);
        MainPage.AddEntry(Netflix, AppLabels.Recurrent, 55.9m, paymentSource: Card);

        MainPage.GoToNextMonth().WaitForBill(Card);

        MainPage.BillAmountText(Card).Should().Be(AppFormat.Money(55.9m));
        MainPage.BillDueDateText(Card).Should().Be(AppFormat.DueDate(BillDueDate.AddMonths(1)));
    }

    // --- Paying the bill ---

    [Fact]
    public async Task PayBill_PaysEveryEntryOnItOnlyThatMonthAndPersists()
    {
        AddAccountAndCard(BillDueDay, Account);
        MainPage
            .AddEntry(Netflix, AppLabels.Recurrent, 55.9m, paymentSource: Card)
            .AddEntry(Market, AppLabels.Recurrent, 600m, paymentSource: Card)
            .AddEntry(Rent, AppLabels.Recurrent, 1500m, paymentSource: Account);

        MainPage.RevealBillActions(Card);
        MainPage.RevealedBillActionText.Should().Be(AppLabels.MarkBillPaid);
        MainPage.PayBill(Card);

        MainPage.IsEntryPaid(Netflix).Should().BeTrue();
        MainPage.IsEntryPaid(Market).Should().BeTrue();
        MainPage.IsEntryPaid(Rent).Should().BeFalse("only the card's entries are on its bill");
        MainPage.SwipeSummary("left").WaitForSummaryPage(SummaryPage.Paid);
        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(655.9m));
        MainPage.SummaryToPayText.Should().Be(AppFormat.Money(1500m));

        var entries = await GetEntryRowsAsync();
        var cardEntryIds = entries.Where(e => e.Description is Netflix or Market).Select(e => e.Id);
        var records = await GetMonthlyEntryRowsAsync();
        records.Select(r => r.FinancialEntryId).Should().BeEquivalentTo(cardEntryIds);
        records.Should().OnlyContain(r =>
            r.Status == AppFormat.Paid && r.Year == AppFormat.CurrentMonth.Year && r.Month == AppFormat.CurrentMonth.Month);

        MainPage.GoToNextMonth().WaitForBill(Card);
        MainPage.IsBillPaid(Card).Should().BeFalse();
    }

    [Fact]
    public async Task MarkBillPending_ResetsEveryEntryOnIt()
    {
        AddAccountAndCard(BillDueDay, Account);
        MainPage
            .AddEntry(Netflix, AppLabels.Recurrent, 55.9m, paymentSource: Card)
            .AddEntry(Market, AppLabels.Recurrent, 600m, paymentSource: Card)
            .PayBill(Card);

        MainPage.RevealBillActions(Card);
        MainPage.RevealedBillActionText.Should().Be(AppLabels.MarkPending);
        MainPage.MarkBillPending(Card);

        MainPage.IsEntryPaid(Netflix).Should().BeFalse();
        MainPage.IsEntryPaid(Market).Should().BeFalse();
        (await GetMonthlyEntryRowsAsync()).Should().OnlyContain(r => r.Status == AppFormat.Pending);
    }

    [Fact]
    public void PayOneEntryOnBill_BillShowsPartiallyPaidUntilEveryEntryIsPaid()
    {
        AddAccountAndCard(BillDueDay, Account);
        MainPage
            .AddEntry(Netflix, AppLabels.Recurrent, 55.9m, paymentSource: Card)
            .AddEntry(Market, AppLabels.Recurrent, 600m, paymentSource: Card)
            .MarkPaid(Netflix);

        MainPage.WaitFor(() => MainPage.IsBillPartiallyPaid(Card));
        MainPage.BillPaidText(Card).Should().Be(AppFormat.Money(55.9m));
        MainPage.IsBillPaid(Card).Should().BeFalse();

        MainPage.MarkPaid(Market);

        MainPage.WaitFor(() => MainPage.IsBillPaid(Card));
        MainPage.BillPaidText(Card).Should().BeNull();
    }

    // --- Entry page ---

    [Fact]
    public async Task EntryPage_OutcomeOnCreditCard_FallsDueWithTheBillInsteadOfAPicker()
    {
        AddAccountAndCard(BillDueDay, Account);

        var entryPage = MainPage.OpenAddEntry()
            .EnterDescription(Netflix)
            .SelectRecurrence(AppLabels.Recurrent)
            .EnterAmount(55.9m)
            .SelectDueDay(25)
            .SelectPaymentSource(Card);

        entryPage.IsDueDayPickerVisible.Should().BeFalse();
        entryPage.CreditCardBillDueHintText.Should().Be(AppLabels.CreditCardBillDueHintFor(BillDueDay));

        entryPage.SelectPaymentSource(Account);
        entryPage.IsDueDayPickerVisible.Should().BeTrue();
        entryPage.DueDayText.Should().Be(AppLabels.DueDay(25), "switching back keeps the day picked before");

        entryPage.SelectPaymentSource(Card).Save();
        MainPage.WaitForBill(Card);

        (await GetEntryRowsAsync()).Single().DueDay.Should().BeNull("the entry falls due with the bill");
    }

    [Fact]
    public void EntryPage_CardWithoutBillDueDay_HintOmitsTheDay()
    {
        MainPage.OpenPaymentSources().AddCreditCard(Card, 5000m).GoToEntries();

        var entryPage = MainPage.OpenAddEntry().EnterDescription(Netflix).SelectPaymentSource(Card);

        entryPage.CreditCardBillDueHintText.Should().Be(AppLabels.CreditCardBillDueHint);
    }

    [Fact]
    public void EntryPage_PaymentSourceComesBeforeTheDueDate()
    {
        MainPage.OpenAddEntry()
            .EnterDescription(Netflix)
            .SelectRecurrence(AppLabels.Recurrent)
            .IsPaymentSourceAboveDueDate.Should().BeTrue();
    }

    [Fact]
    public async Task EntryPage_OutcomeOnCreditCard_HidesEstimatedAndPartialPayment()
    {
        AddAccountAndCard(BillDueDay, Account);

        var entryPage = MainPage.OpenAddEntry()
            .EnterDescription(Market)
            .SelectRecurrence(AppLabels.Recurrent)
            .EnterAmount(600m)
            .SetEstimated(true)
            .SetAllowsPartialPayment(true)
            .SelectPaymentSource(Card);

        entryPage.IsEstimatedSwitchVisible.Should().BeFalse();
        entryPage.IsPartialPaymentSwitchVisible.Should().BeFalse();

        entryPage.SelectPaymentSource(Account);
        entryPage.IsEstimatedSwitchVisible.Should().BeTrue();
        entryPage.IsPartialPaymentSwitchVisible.Should().BeTrue();

        entryPage.SelectPaymentSource(Card).Save();
        MainPage.WaitForBill(Card);

        var entry = (await GetEntryRowsAsync()).Single();
        entry.IsEstimated.Should().BeFalse("a credit card purchase has a known amount");
        entry.AllowsPartialPayment.Should().BeFalse("a credit card purchase is paid with the bill");
    }

    [Fact]
    public void EntryPage_IncomeOnCreditCard_KeepsEstimatedAndPartialPayment()
    {
        AddAccountAndCard(BillDueDay, Account);

        var entryPage = MainPage.OpenAddEntry()
            .EnterDescription("Estorno E2E")
            .SelectIncome()
            .SelectRecurrence(AppLabels.Recurrent)
            .SelectPaymentSource(Card);

        entryPage.IsEstimatedSwitchVisible.Should().BeTrue();
        entryPage.IsPartialPaymentSwitchVisible.Should().BeTrue();
    }

    // --- Credit limit ---

    [Fact]
    public void AvailableLimit_SubtractsUnpaidOutcomesAndFreesUpWhenTheBillIsPaid()
    {
        AddAccountAndCard(BillDueDay, Account);
        MainPage
            .AddEntry(Netflix, AppLabels.Recurrent, 55.9m, paymentSource: Card)
            .AddEntry(Market, AppLabels.OneTime, 600m, paymentSource: Card)
            .AddEntry(Rent, AppLabels.Recurrent, 1500m, paymentSource: Account);

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
    public void AvailableLimit_PurchaseInInstallments_TakesUpTheInstallmentsStillToCome()
    {
        const string phone = "Celular E2E";
        AddAccountAndCard(BillDueDay, Account);
        MainPage.AddEntry(phone, AppLabels.Installments, 200m, installments: "10", paymentSource: Card);

        MainPage.OpenPaymentSources().WaitForAvailableLimit(Card, AppFormat.AvailableLimit(3000m)).GoToEntries();

        MainPage.MarkPaid(phone);
        MainPage.OpenPaymentSources().WaitForAvailableLimit(Card, AppFormat.AvailableLimit(3200m));
    }

    [Fact]
    public void AvailableLimit_BeyondTheLimit_ShowsNegative()
    {
        MainPage.OpenPaymentSources().AddCreditCard(Card, 500m).GoToEntries();
        MainPage.AddEntry(Market, AppLabels.OneTime, 650m, paymentSource: Card);

        MainPage.OpenPaymentSources().WaitForAvailableLimit(Card, AppFormat.AvailableLimit(-150m));
    }

    // --- Card changes ---

    [Fact]
    public void EditCreditCardBill_ChangesDueDateAndAccountOnTheBill()
    {
        AddAccountAndCard(BillDueDay, Account);
        MainPage.AddEntry(Netflix, AppLabels.Recurrent, 55.9m, paymentSource: Card);
        var newDueDay = BillDueDay + 5;

        MainPage.OpenPaymentSources()
            .AddPaymentSource("Inter E2E", AppFormat.BankAccount, 100m)
            .EditCreditCardBill(Card, newDueDay, "Inter E2E")
            .GoToEntries()
            .WaitForBill(Card);

        MainPage.WaitFor(() => MainPage.BillAccountText(Card) == AppLabels.BillAccount("Inter E2E"));
        MainPage.BillDueDateText(Card).Should().Be(AppFormat.DueDate(BillDueDate.AddDays(5)));
    }

    [Fact]
    public async Task DeleteAccountPayingTheBill_BillNoLongerShowsIt()
    {
        AddAccountAndCard(BillDueDay, Account);
        MainPage.AddEntry(Netflix, AppLabels.Recurrent, 55.9m, paymentSource: Card);

        MainPage.OpenPaymentSources().DeletePaymentSource(Account).GoToEntries().WaitForBill(Card);

        MainPage.WaitFor(() => MainPage.BillAccountText(Card) is null);
        MainPage.BillDueDateText(Card).Should().Be(AppFormat.DueDate(BillDueDate));
        (await GetPaymentSourceRowsAsync()).Single().BillPaymentSourceId.Should().BeNull();
    }

    [Fact]
    public void ChangeCardToBankAccount_EntriesLeaveTheBill()
    {
        AddAccountAndCard(BillDueDay, Account);
        MainPage.AddEntry(Netflix, AppLabels.Recurrent, 55.9m, paymentSource: Card);

        MainPage.OpenPaymentSources()
            .EditPaymentSource(Card, Card, AppFormat.BankAccount, 5000m)
            .GoToEntries()
            .WaitForBillGone(Card);

        MainPage.WaitForEntry(Netflix);
        MainPage.VisibleBillNames.Should().BeEmpty();
    }

    private void AddAccountAndCard(int? billDueDay, string? billPaymentAccount) =>
        MainPage.OpenPaymentSources()
            .AddPaymentSource(Account, AppFormat.BankAccount, 2500m)
            .AddCreditCard(Card, 5000m, billDueDay, billPaymentAccount)
            .GoToEntries();
}
