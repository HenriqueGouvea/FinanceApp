using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;
using FluentAssertions.Execution;

namespace FinanceApp.UITests.Tests;

/// <summary>
/// The setup wizard shown on the first run, which turns the user's accounts, credit cards, salary and expenses into
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
    private const string Phone = "Celular novo";
    private const string HealthPlan = "Plano de saúde";
    private const string School = "Mensalidade escolar";
    private const string EatingOut = "Restaurantes e delivery";
    private const string Fuel = "Combustível";

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
            _setup.IsOptionalHintVisible.Should().BeTrue();
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

        var settings = main.RedoSetup().Next().Next().Next().EnterSalary(5000).Close();
        main = settings.GoToHome();

        main.IncomeCountText.Should().Be(AppFormat.EntryCount(0));
        main.ReadMenuGreeting().Should().Be(AppLabels.MenuGreetingWithoutName);
        (await GetEntryRowsAsync()).Should().BeEmpty();
    }

    [Fact]
    [Trait("Smoke", "true")]
    public async Task FillEveryStep_SummaryThenFinish_CreatesSourcesCardAndEntries()
    {
        _setup.EnterName(Name).Next()
            .AddSource(Account, AppFormat.BankAccount, 2500)
            .Next()
            .AddCard(Card, 8000, billDueDay: 10)
            .AddInstallmentPurchase(Phone, 250, current: 3, total: 10);

        using (new AssertionScope())
        {
            _setup.CardDetails.Should().Equal($"Vence dia 10 · Débito em {Account}");
            _setup.PurchaseDescriptions.Should().Equal(Phone);
        }

        _setup.Next()
            .EnterSalary(5000)
            .SelectSalaryDay(6)
            .Next()
            .SelectExpense(Rent, 1500, dueDay: 10)
            .SelectExpense(Energy, 200, dueDay: 15)
            .SelectExpense(Supermarket, 1000)
            .Next()
            .Next();

        using (new AssertionScope())
        {
            _setup.SummaryTitleText.Should().Be(AppLabels.SetupSummaryTitleFor(Name));
            _setup.NextButtonText.Should().Be(AppLabels.SetupStart);
            _setup.SummaryRows.Should().Equal(
                (Salary, AppLabels.SetupSchedule("dia 6", Account), AppFormat.Money(5000)),
                (Phone, AppLabels.SetupInstallmentSchedule(3, 10, Card), AppFormat.Money(250)),
                (Rent, AppLabels.SetupSchedule("dia 10", Account), AppFormat.Money(1500)),
                (Energy, AppLabels.SetupSchedule("dia 15", AppLabels.EstimatedValue, Account), AppFormat.Money(200)),
                (Supermarket, AppLabels.SetupSchedule(AppLabels.EstimatedValue, AppLabels.PartialPaymentValue, Account), AppFormat.Money(1000)));
            _setup.SummaryIncomeText.Should().Be(AppFormat.Money(5000));
            _setup.SummaryOutcomeText.Should().Be(AppFormat.Money(2950));
            _setup.SummaryBalanceText.Should().Be(AppFormat.Money(2050));
            _setup.IsRecurrenceGuideVisible.Should().BeTrue();
        }

        var main = _setup.Finish();

        // The incomes section sits below the fold on small screens, so the month summary covers both sides.
        main.WaitForEntry(Rent).WaitForEntry(Energy);
        main.SummaryIncomeText.Should().Be(AppFormat.Money(5000));
        main.SummaryOutcomeText.Should().Be(AppFormat.Money(2950));
        main.ReadMenuGreeting().Should().Be(AppLabels.MenuGreeting(Name));

        var sources = await GetPaymentSourceRowsAsync();
        var accountId = sources.Single(s => s.Name == Account).Id;
        sources.Select(s => (s.Name, s.Type, s.Balance, s.BillDueDay, s.BillPaymentSourceId)).Should().BeEquivalentTo(
        [
            (Account, AppFormat.BankAccount, 2500m, (int?)null, (int?)null),
            (Card, AppFormat.CreditCard, 8000m, (int?)10, (int?)accountId)
        ]);
        var cardId = sources.Single(s => s.Name == Card).Id;

        var categories = await GetCategoryRowsAsync();
        int CategoryId(string name) => categories.Single(c => c.Name == name).Id;

        var entries = await GetEntryRowsAsync();
        var phone = entries.Single(e => e.Description == Phone);
        (phone.Recurrence, phone.TotalInstallments, phone.StartDate.Date, phone.Amount, phone.PaymentSourceId).Should()
            .Be((AppFormat.Installments, (int?)10, AppFormat.CurrentMonth.AddMonths(-2), 250m, (int?)cardId),
                "the 3rd of 10 installments falls this month");
        entries.Where(e => e != phone).Should().OnlyContain(e =>
            e.Recurrence == AppFormat.Recurrent && e.StartDate.Date == AppFormat.CurrentMonth && e.EndDate == null);
        entries.Where(e => e != phone)
            .Select(e => (e.Description, e.Amount, e.EntryType, e.DueDay, e.IsEstimated, e.AllowsPartialPayment, e.CategoryId, e.PaymentSourceId))
            .Should().BeEquivalentTo(
            [
                (Salary, 5000m, AppFormat.Income, (int?)6, false, false, (int?)CategoryId("Salário"), (int?)accountId),
                (Rent, 1500m, AppFormat.Outcome, (int?)10, false, false, (int?)CategoryId("Moradia"), (int?)accountId),
                (Energy, 200m, AppFormat.Outcome, (int?)15, true, false, (int?)CategoryId("Moradia"), (int?)accountId),
                (Supermarket, 1000m, AppFormat.Outcome, (int?)null, true, true, (int?)CategoryId("Mercado"), (int?)accountId)
            ]);
    }

    [Fact]
    public async Task SplitSalaryChildrenCarAndEatingOut_Finish_CreatesBothIncomesAndOtherExpenses()
    {
        _setup.Next().Next().Next()
            .EnterSalary(3000)
            .SplitSalary(2000);
        _setup.MonthlySalaryText.Should().Be(AppFormat.Money(5000));

        _setup.Next().Next()
            .SelectExpense(HealthPlan, 600, dueDay: 10)
            .SelectHasChildren()
            .SelectExpense(School, 1200, dueDay: 5)
            .SelectHasCar()
            .SelectExpense(Fuel, 500)
            .SelectExpense(EatingOut, 400)
            .Next();

        _setup.SummaryRows.Should().Equal(
            (Salary, AppLabels.SetupSchedule("dia 5"), AppFormat.Money(3000)),
            (AppLabels.SetupAdvanceDescription, AppLabels.SetupSchedule("dia 20"), AppFormat.Money(2000)),
            (HealthPlan, AppLabels.SetupSchedule("dia 10"), AppFormat.Money(600)),
            (School, AppLabels.SetupSchedule("dia 5"), AppFormat.Money(1200)),
            (Fuel, AppLabels.SetupSchedule(AppLabels.EstimatedValue), AppFormat.Money(500)),
            (EatingOut, AppLabels.SetupSchedule(AppLabels.EstimatedValue, AppLabels.PartialPaymentValue), AppFormat.Money(400)));

        var main = _setup.Finish();

        main.WaitForEntry(HealthPlan);
        main.SummaryIncomeText.Should().Be(AppFormat.Money(5000));
        main.SummaryOutcomeText.Should().Be(AppFormat.Money(2700));

        var categories = await GetCategoryRowsAsync();
        int? CategoryId(string name) => categories.Single(c => c.Name == name).Id;
        (await GetEntryRowsAsync())
            .Select(e => (e.Description, e.EntryType, e.DueDay, e.IsEstimated, e.AllowsPartialPayment, e.CategoryId))
            .Should().BeEquivalentTo(
            [
                (Salary, AppFormat.Income, (int?)5, false, false, CategoryId("Salário")),
                (AppLabels.SetupAdvanceDescription, AppFormat.Income, (int?)20, false, false, CategoryId("Salário")),
                (HealthPlan, AppFormat.Outcome, (int?)10, false, false, CategoryId("Saúde")),
                (School, AppFormat.Outcome, (int?)5, false, false, CategoryId("Educação")),
                (Fuel, AppFormat.Outcome, (int?)null, true, false, CategoryId("Transporte")),
                (EatingOut, AppFormat.Outcome, (int?)null, true, true, CategoryId("Lazer"))
            ]);
    }

    [Fact]
    public async Task SkipStep_AddedAccount_IsDiscardedAndListedOnSummary()
    {
        _setup.Next()
            .AddSource(Account, AppFormat.BankAccount, 2500)
            .SkipStep()
            .Next()
            .EnterSalary(5000)
            .Next()
            .Next()
            .Next();

        using (new AssertionScope())
        {
            _setup.SkippedStepsText.Should().Be(AppLabels.SetupSkippedSteps("Contas e vales"));
            _setup.SummaryRows.Should().Equal((Salary, AppLabels.SetupSchedule("dia 5"), AppFormat.Money(5000)));
            _setup.IsOptionalHintVisible.Should().BeFalse();
        }

        var main = _setup.Finish();
        main.WaitFor(() => main.SummaryIncomeText == AppFormat.Money(5000));
        (await GetPaymentSourceRowsAsync()).Should().BeEmpty("the skipped step's account isn't created");
        (await GetEntryRowsAsync()).Single().PaymentSourceId.Should().BeNull();
    }

    [Fact]
    public async Task RedoSetup_AddsEntriesAndKeepsExistingOnes()
    {
        _setup.EnterName(Name).Next().Next().Next().Next().SelectExpense(Rent, 1500).Next().Next();
        var main = _setup.Finish();

        var again = main.RedoSetup();
        again.SkipButtonText.Should().Be(AppLabels.SetupClose);
        again.NameText.Should().Be(Name);
        // The account is typed but "Adicionar" isn't tapped: "Continuar" adds it.
        again.Next().TypeSourceName(Account).Next().Next().Next().SelectExpense(Internet, 100, dueDay: 20).Next().Next();
        main = again.Finish();

        main.WaitForEntry(Rent).WaitForEntry(Internet);
        main.OutcomeTotalText.Should().Be(AppFormat.Money(1600));
        (await GetEntryRowsAsync()).Select(e => e.Description).Should().BeEquivalentTo([Rent, Internet]);
        var account = (await GetPaymentSourceRowsAsync()).Should().ContainSingle().Subject;
        (account.Name, account.Type).Should().Be((Account, AppFormat.BankAccount));
        (await GetEntryRowsAsync()).Single(e => e.Description == Internet).PaymentSourceId.Should().Be(account.Id,
            "the first account added pays the house expenses");
    }
}
