using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;
using FluentAssertions.Execution;

namespace FinanceApp.UITests.Tests;

/// <summary>
/// The setup wizard shown on the first run, which turns the user's accounts, salary and house expenses into recurrent
/// entries. Picking a photo isn't covered: it opens Android's photo picker, outside the app. The wizard's defaults
/// (first account picked, expense flags, unticked expenses left out) are covered by the SetupViewModel and
/// SetupService tests.
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
    public void FirstRun_WelcomeStepThenBack_KeepsWhatWasTyped()
    {
        using (new AssertionScope())
        {
            _setup.StepText.Should().Be(AppFormat.SetupStep(1));
            _setup.WelcomeTitleText.Should().Be(AppLabels.SetupWelcomeTitle);
            _setup.TaglineText.Should().Be(AppLabels.Tagline);
            _setup.SkipButtonText.Should().Be(AppLabels.SetupSkip);
            _setup.NextButtonText.Should().Be(AppLabels.SetupContinue);
            _setup.IsBackVisible.Should().BeFalse();
        }

        _setup.EnterName(Name);
        _setup.AvatarInitialText.Should().Be("A");

        _setup.Next().Next();
        _setup.IsBackVisible.Should().BeTrue();
        _setup.Back().PressBack();

        _setup.StepText.Should().Be(AppFormat.SetupStep(1));
        _setup.NameText.Should().Be(Name);
    }

    [Fact]
    public async Task SkipThenRedoSetupAndClose_LandsOnEmptyMonthAndCreatesNothing()
    {
        var main = _setup.Skip();

        using (new AssertionScope())
        {
            main.MonthTitleText.Should().Be(AppFormat.MonthTitle(AppFormat.CurrentMonth));
            main.SummaryBalanceText.Should().Be(AppFormat.Money(0));
            main.OutcomeCountText.Should().Be(AppFormat.EntryCount(0));
            main.IncomeCountText.Should().Be(AppFormat.EntryCount(0));
        }

        Session.Relaunch();
        main = new MainPageObject(Session).WaitUntilLoaded();
        _setup.IsWelcomeStepVisible.Should().BeFalse("a skipped wizard isn't shown again");

        var settings = main.RedoSetup().Next().Next().EnterSalary(5000).Close();
        main = settings.GoToHome();

        main.IncomeCountText.Should().Be(AppFormat.EntryCount(0));
        main.ReadMenuGreeting().Should().Be(AppLabels.MenuGreetingWithoutName);
        (await GetEntryRowsAsync()).Should().BeEmpty();
    }

    [Fact]
    [Trait("Smoke", "true")]
    public async Task FillEveryStep_SummaryThenFinish_CreatesSourcesAndRecurrentEntries()
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

        using (new AssertionScope())
        {
            _setup.SummaryTitleText.Should().Be(AppLabels.SetupSummaryTitleFor(Name));
            _setup.NextButtonText.Should().Be(AppLabels.SetupStart);
            _setup.SummaryRows.Should().Equal(
                (Salary, AppLabels.SetupSchedule("dia 6", Account), AppFormat.Money(5000)),
                (Rent, AppLabels.SetupSchedule("dia 10", Card), AppFormat.Money(1500)),
                (Energy, AppLabels.SetupSchedule("dia 15", AppLabels.EstimatedValue, Card), AppFormat.Money(200)),
                (Supermarket, AppLabels.SetupSchedule(AppLabels.EstimatedValue, AppLabels.PartialPaymentValue, Card), AppFormat.Money(1000)));
            _setup.SummaryIncomeText.Should().Be(AppFormat.Money(5000));
            _setup.SummaryOutcomeText.Should().Be(AppFormat.Money(2700));
            _setup.SummaryBalanceText.Should().Be(AppFormat.Money(2300));
            _setup.IsRecurrenceGuideVisible.Should().BeTrue();
        }

        var main = _setup.Finish();

        // The incomes section sits below the fold on small screens, so the month summary covers both sides.
        main.WaitForEntry(Rent).WaitForEntry(Energy);
        main.SummaryIncomeText.Should().Be(AppFormat.Money(5000));
        main.SummaryOutcomeText.Should().Be(AppFormat.Money(2700));
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
}
