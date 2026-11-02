using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

/// <summary>
/// The setup wizard shown on the first run, which turns the user's accounts, salary and house expenses into recurrent
/// entries. Picking a photo isn't covered: it opens Android's photo picker, outside the app.
/// </summary>
[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class SetupTests : E2ETestBase
{
    private const string Name = "Ana";
    private const string Account = "Nubank";
    private const string Card = "Visa";
    private const string Salary = "Salário";
    private const string Rent = "Aluguel ou financiamento";
    private const string Energy = "Energia";
    private const string Supermarket = "Mercado";
    private const string Internet = "Internet";

    private readonly SetupPageObject _setup;

    public SetupTests(AppiumServerFixture server) : base(server, skipSetup: false)
    {
        _setup = new SetupPageObject(Session);
    }

    [Fact]
    public void FirstRun_ShowsWelcomeStep()
    {
        _setup.StepText.Should().Be(AppFormat.SetupStep(1));
        _setup.IsWelcomeStepVisible.Should().BeTrue();
        _setup.SkipButtonText.Should().Be(AppLabels.SetupSkip);
        _setup.NextButtonText.Should().Be(AppLabels.SetupContinue);
        _setup.IsBackVisible.Should().BeFalse();
    }

    [Fact]
    public async Task Skip_LandsOnEmptyMonthAndIsNotShownAgain()
    {
        var main = _setup.Skip();

        main.OutcomeCountText.Should().Be(AppFormat.EntryCount(0));
        main.IncomeCountText.Should().Be(AppFormat.EntryCount(0));
        (await GetEntryRowsAsync()).Should().BeEmpty();

        Session.Relaunch();
        new MainPageObject(Session).WaitUntilLoaded();
        _setup.IsWelcomeStepVisible.Should().BeFalse();
    }

    [Fact]
    public void Back_KeepsWhatWasTyped()
    {
        _setup.EnterName(Name);
        _setup.AvatarInitialText.Should().Be("A");

        _setup.Next().Next();
        _setup.IsBackVisible.Should().BeTrue();
        _setup.Back().PressBack();

        _setup.StepText.Should().Be(AppFormat.SetupStep(1));
        _setup.NameText.Should().Be(Name);
    }

    [Fact]
    public void AddSources_FirstAccountIsWhereSalaryAndExpensesGo()
    {
        _setup.Next()
            .AddSource(Card, AppFormat.CreditCard, 8000)
            .AddSource(Account, AppFormat.BankAccount, 2500);

        _setup.SourceNames.Should().Equal(Card, Account);

        _setup.Next();
        _setup.SalarySourceText.Should().Be(Account);
        _setup.SalaryDayText.Should().Be(AppLabels.DueDay(5));
        _setup.Next();
        _setup.ExpensesSourceText.Should().Be(Account);
    }

    [Fact]
    public void Summary_ListsEntriesToCreateWithMonthlyTotals()
    {
        FillWizard();

        _setup.SummaryTitleText.Should().Be(AppLabels.SetupSummaryTitleFor(Name));
        _setup.NextButtonText.Should().Be(AppLabels.SetupStart);
        _setup.SummaryRows.Should().Equal(
            (Salary, AppLabels.SetupSchedule("dia 6", Account), AppFormat.Money(5000)),
            (Rent, AppLabels.SetupSchedule("dia 10", Card), AppFormat.Money(1500)),
            (Energy, AppLabels.SetupSchedule("dia 15", AppLabels.EstimatedValue, Card), AppFormat.Money(200)),
            (Supermarket, AppLabels.SetupSchedule(AppLabels.EstimatedValue, Card), AppFormat.Money(1000)));
        _setup.SummaryIncomeText.Should().Be(AppFormat.Money(5000));
        _setup.SummaryOutcomeText.Should().Be(AppFormat.Money(2700));
        _setup.SummaryBalanceText.Should().Be(AppFormat.Money(2300));
        _setup.IsRecurrenceGuideVisible.Should().BeTrue();
    }

    [Fact]
    public async Task Finish_CreatesSourcesAndRecurrentEntries()
    {
        FillWizard();

        var main = _setup.Finish();

        // The incomes section sits below the fold on small screens, so the month summary covers both sides.
        main.WaitForEntry(Rent).WaitForEntry(Energy);
        main.SummaryIncomeText.Should().Be(AppFormat.Money(5000));
        main.SummaryOutcomeText.Should().Be(AppFormat.Money(2700));
        main.OutcomeCountText.Should().Be(AppFormat.EntryCount(3));
        main.ReadMenuGreeting().Should().Be(AppLabels.MenuGreeting(Name));

        var sources = await GetPaymentSourceRowsAsync();
        sources.Select(s => (s.Name, s.Type, s.Balance)).Should().BeEquivalentTo(
        [
            (Account, AppFormat.BankAccount, 2500m),
            (Card, AppFormat.CreditCard, 8000m)
        ]);
        var accountId = sources.Single(s => s.Name == Account).Id;
        var cardId = sources.Single(s => s.Name == Card).Id;

        var categories = await GetCategoryRowsAsync();
        int CategoryId(string name) => categories.Single(c => c.Name == name).Id;

        var entries = await GetEntryRowsAsync();
        entries.Should().OnlyContain(e =>
            e.Recurrence == AppFormat.Recurrent && e.StartDate.Date == AppFormat.CurrentMonth && e.EndDate == null);
        entries.Select(e => (e.Description, e.Amount, e.EntryType, e.DueDay, e.IsEstimated, e.AllowsPartialPayment, e.CategoryId, e.PaymentSourceId))
            .Should().BeEquivalentTo(
            [
                (Salary, 5000m, AppFormat.Income, (int?)6, false, false, (int?)CategoryId("Salário"), (int?)accountId),
                (Rent, 1500m, AppFormat.Outcome, (int?)10, false, false, (int?)CategoryId("Moradia"), (int?)cardId),
                (Energy, 200m, AppFormat.Outcome, (int?)15, true, false, (int?)CategoryId("Moradia"), (int?)cardId),
                (Supermarket, 1000m, AppFormat.Outcome, (int?)null, true, true, (int?)CategoryId("Mercado"), (int?)cardId)
            ]);
    }

    [Fact]
    public void Finish_EntriesRepeatInNextMonth()
    {
        _setup.Next().Next().EnterSalary(5000).Next().SelectExpense(Rent, 1500).Next();

        var main = _setup.Finish().GoToNextMonth();

        main.WaitForEntry(Salary).WaitForEntry(Rent);
        main.IncomeTotalText.Should().Be(AppFormat.Money(5000));
        main.OutcomeTotalText.Should().Be(AppFormat.Money(1500));
    }

    [Fact]
    public async Task Finish_RemovedSourceAndUntickedExpense_AreLeftOut()
    {
        _setup.Next()
            .AddSource(Account, AppFormat.BankAccount, 2500)
            .AddSource(Card, AppFormat.CreditCard, 8000)
            .RemoveSource(Card);
        _setup.SourceNames.Should().Equal(Account);
        _setup.Next().Next().SelectExpense(Internet, 100).ToggleExpense(Internet);
        _setup.IsExpenseChecked(Internet).Should().BeFalse();

        _setup.Next();
        _setup.IsSummaryEmptyVisible.Should().BeTrue();
        _setup.SummaryTitleText.Should().Be(AppLabels.SetupSummaryTitle);
        _setup.Finish();

        (await GetPaymentSourceRowsAsync()).Select(s => s.Name).Should().Equal(Account);
        (await GetEntryRowsAsync()).Should().BeEmpty();
    }

    [Fact]
    public void HouseExpenses_SupermarketHasNoDueDay()
    {
        _setup.Next().Next().Next().SelectExpense(Supermarket, 1000);

        _setup.IsExpenseDueDayVisible(Supermarket).Should().BeFalse();
    }

    [Fact]
    public async Task RedoSetup_AddsEntriesAndKeepsExistingOnes()
    {
        _setup.EnterName(Name).Next().Next().Next().SelectExpense(Rent, 1500).Next();
        var main = _setup.Finish();

        var again = main.RedoSetup();
        again.SkipButtonText.Should().Be(AppLabels.SetupClose);
        again.NameText.Should().Be(Name);
        again.Next().Next().Next().SelectExpense(Internet, 100, dueDay: 20).Next();
        main = again.Finish();

        main.WaitForEntry(Rent).WaitForEntry(Internet);
        main.OutcomeTotalText.Should().Be(AppFormat.Money(1600));
        (await GetEntryRowsAsync()).Select(e => e.Description).Should().BeEquivalentTo([Rent, Internet]);
    }

    [Fact]
    public async Task RedoSetup_Close_CreatesNothing()
    {
        var main = _setup.Skip();

        var again = main.RedoSetup().Next().Next().EnterSalary(5000);
        main = again.Skip();

        main.IncomeCountText.Should().Be(AppFormat.EntryCount(0));
        (await GetEntryRowsAsync()).Should().BeEmpty();
        main.ReadMenuGreeting().Should().Be(AppLabels.MenuGreetingWithoutName);
    }

    /// <summary>
    /// Goes through every step up to the summary: a name, an account and a card, a salary received on the 6th and
    /// three house expenses paid with the card.
    /// </summary>
    private void FillWizard()
    {
        _setup.EnterName(Name).Next()
            .AddSource(Account, AppFormat.BankAccount, 2500)
            .AddSource(Card, AppFormat.CreditCard, 8000)
            .Next()
            .EnterSalary(5000)
            .SelectSalaryDay(6)
            .Next()
            .SelectExpense(Rent, 1500, dueDay: 10)
            .SelectExpense(Energy, 200, dueDay: 15)
            .SelectExpense(Supermarket, 1000)
            .SelectExpensesSource(Card)
            .Next();
    }
}
