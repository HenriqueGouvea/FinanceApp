using CommunityToolkit.Mvvm.Messaging;
using FinanceApp.Messages;
using FinanceApp.Models;
using FinanceApp.Services;
using FinanceApp.ViewModels;
using FluentAssertions;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using NSubstitute;
using Xunit;

namespace FinanceApp.Tests.ViewModels;

public class SetupViewModelTests
{
    private readonly ISetupService _setupService;
    private readonly IPaymentSourceService _paymentSourceService;
    private readonly IUserProfileService _profileService;
    private readonly IMediaPicker _mediaPicker;
    private readonly IMessenger _messenger = new WeakReferenceMessenger();
    private readonly SetupViewModel _vm;

    public SetupViewModelTests()
    {
        _setupService = Substitute.For<ISetupService>();
        _paymentSourceService = Substitute.For<IPaymentSourceService>();
        _paymentSourceService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<PaymentSource>>([]));
        _profileService = Substitute.For<IUserProfileService>();
        _profileService.Name.Returns(string.Empty);
        _profileService.PhotoPath.Returns((string?)null);
        _mediaPicker = Substitute.For<IMediaPicker>();
        _vm = new SetupViewModel(_setupService, _paymentSourceService, _profileService, _mediaPicker, _messenger);
    }

    // --- PrepareAsync ---

    [Fact]
    public async Task PrepareAsync_FirstRun_StartsOnWelcomeWithDefaults()
    {
        await _vm.PrepareAsync(isRerun: false);

        _vm.Step.Should().Be(SetupStep.Welcome);
        _vm.StepNumber.Should().Be(1);
        _vm.StepCount.Should().Be(7);
        _vm.SkipButtonText.Should().Be("Pular");
        _vm.NextButtonText.Should().Be("Continuar");
        _vm.SalaryDay.Should().Be(SetupViewModel.DefaultSalaryDay);
        _vm.Expenses.Select(e => e.Description).Should().Equal(
            "Aluguel ou financiamento", "Condomínio", "Energia", "Água", "Internet", "Celular", "Mercado");
        _vm.Expenses.Should().OnlyContain(e => !e.IsSelected);
    }

    [Fact]
    public async Task PrepareAsync_Rerun_ShowsCurrentProfileAndSources()
    {
        _profileService.Name.Returns("Ana");
        _profileService.PhotoPath.Returns("/data/profile.jpg");
        var nubank = new PaymentSource { Id = 1, Name = "Nubank" };
        _paymentSourceService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<PaymentSource>>([nubank]));

        await _vm.PrepareAsync(isRerun: true);

        _vm.SkipButtonText.Should().Be("Fechar");
        _vm.Name.Should().Be("Ana");
        _vm.Initial.Should().Be("A");
        _vm.HasPhoto.Should().BeTrue();
        _vm.AvailableSources.Should().Equal(nubank);
        _vm.NewSources.Should().BeEmpty();
    }

    [Fact]
    public async Task PrepareAsync_AfterUse_ResetsWizard()
    {
        await _vm.PrepareAsync(isRerun: false);
        _vm.NewSourceName = "Nubank";
        _vm.AddSourceCommand.Execute(null);
        _vm.Salary = 5000;
        _vm.Expenses[0].IsSelected = true;
        await _vm.NextCommand.ExecuteAsync(null);

        await _vm.PrepareAsync(isRerun: false);

        _vm.Step.Should().Be(SetupStep.Welcome);
        _vm.NewSources.Should().BeEmpty();
        _vm.AvailableSources.Should().BeEmpty();
        _vm.SalarySource.Should().BeNull();
        _vm.Salary.Should().Be(0);
        _vm.Expenses.Should().OnlyContain(e => !e.IsSelected);
    }

    // --- Navigation ---

    [Fact]
    public async Task Next_GoesThroughEveryStep()
    {
        await _vm.PrepareAsync(isRerun: false);
        var steps = new List<SetupStep> { _vm.Step };

        for (var i = 0; i < 6; i++)
        {
            await _vm.NextCommand.ExecuteAsync(null);
            steps.Add(_vm.Step);
        }

        steps.Should().Equal(
            SetupStep.Welcome, SetupStep.PaymentSources, SetupStep.CreditCards, SetupStep.Income,
            SetupStep.HouseExpenses, SetupStep.OtherExpenses, SetupStep.Summary);
        _vm.IsLastStep.Should().BeTrue();
        _vm.NextButtonText.Should().Be("Começar");
        _vm.Progress.Should().Be(1);
    }

    [Fact]
    public async Task Back_ReturnsToPreviousStep()
    {
        await _vm.PrepareAsync(isRerun: false);
        await _vm.NextCommand.ExecuteAsync(null);

        _vm.BackCommand.Execute(null);

        _vm.Step.Should().Be(SetupStep.Welcome);
        _vm.IsFirstStep.Should().BeTrue();
    }

    [Fact]
    public async Task Back_OnFirstStep_StaysThere()
    {
        await _vm.PrepareAsync(isRerun: false);

        _vm.BackCommand.Execute(null);

        _vm.Step.Should().Be(SetupStep.Welcome);
    }

    // --- Photo ---

    [Fact]
    public async Task PickPhoto_Picked_ShowsIt()
    {
        var photo = new FileResult("/cache/picked.jpg");
        _mediaPicker.PickPhotosAsync(Arg.Any<MediaPickerOptions>()).Returns(Task.FromResult(new List<FileResult> { photo }));

        await _vm.PickPhotoCommand.ExecuteAsync(null);

        _vm.PhotoPath.Should().Be("/cache/picked.jpg");
        _vm.HasPhoto.Should().BeTrue();
    }

    [Fact]
    public async Task PickPhoto_Cancelled_KeepsCurrentPhoto()
    {
        _profileService.PhotoPath.Returns("/data/profile.jpg");
        await _vm.PrepareAsync(isRerun: true);
        _mediaPicker.PickPhotosAsync(Arg.Any<MediaPickerOptions>()).Returns(Task.FromResult(new List<FileResult>()));

        await _vm.PickPhotoCommand.ExecuteAsync(null);

        _vm.PhotoPath.Should().Be("/data/profile.jpg");
    }

    [Fact]
    public async Task PickPhoto_PermissionDenied_KeepsCurrentPhoto()
    {
        _mediaPicker.PickPhotosAsync(Arg.Any<MediaPickerOptions>())
            .Returns<Task<List<FileResult>>>(_ => throw new PermissionException("denied"));

        await _vm.PickPhotoCommand.ExecuteAsync(null);

        _vm.PhotoPath.Should().BeNull();
    }

    [Fact]
    public async Task RemovePhoto_ClearsPhoto()
    {
        _profileService.PhotoPath.Returns("/data/profile.jpg");
        await _vm.PrepareAsync(isRerun: true);

        _vm.RemovePhotoCommand.Execute(null);

        _vm.HasPhoto.Should().BeFalse();
    }

    [Fact]
    public void Initial_NoName_ShowsPlaceholder()
    {
        _vm.Name = "  ";

        _vm.Initial.Should().Be("👤");
    }

    // --- Payment sources ---

    [Fact]
    public async Task AddSource_NoName_CannotExecute()
    {
        await _vm.PrepareAsync(isRerun: false);

        _vm.NewSourceName = "  ";

        _vm.AddSourceCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task AddSource_AddsToListsAndResetsForm()
    {
        await _vm.PrepareAsync(isRerun: false);
        _vm.NewSourceName = " VR ";
        _vm.SelectSourceTypeCommand.Execute(PaymentSourceType.MealVoucher);
        _vm.NewSourceBalance = 800;

        _vm.AddSourceCommand.Execute(null);

        var voucher = _vm.NewSources.Should().ContainSingle().Subject;
        (voucher.Name, voucher.Type, voucher.Balance).Should().Be(("VR", PaymentSourceType.MealVoucher, 800m));
        _vm.AvailableSources.Should().Equal(voucher);
        _vm.BillAccounts.Should().BeEmpty("only bank accounts pay a card's bill");
        _vm.NewSourceName.Should().BeEmpty();
        _vm.NewSourceType.Should().Be(PaymentSourceType.BankAccount);
        _vm.NewSourceBalance.Should().Be(0);
    }

    [Fact]
    public async Task AddSource_FirstBankAccount_BecomesSalaryExpensesAndBillSource()
    {
        await _vm.PrepareAsync(isRerun: false);
        AddSource("VR", PaymentSourceType.MealVoucher);
        AddSource("Nubank", PaymentSourceType.BankAccount);
        AddSource("Itaú", PaymentSourceType.BankAccount);

        _vm.SalarySource!.Name.Should().Be("Nubank");
        _vm.ExpensesSource!.Name.Should().Be("Nubank");
        _vm.OtherExpensesSource!.Name.Should().Be("Nubank");
        _vm.NewCardBillAccount!.Name.Should().Be("Nubank");
        _vm.BillAccounts.Select(a => a.Name).Should().Equal("Nubank", "Itaú");
    }

    [Fact]
    public async Task RemoveSource_SelectedOne_ClearsSelection()
    {
        await _vm.PrepareAsync(isRerun: false);
        AddSource("Nubank", PaymentSourceType.BankAccount);
        var nubank = _vm.NewSources.Single();

        _vm.RemoveSourceCommand.Execute(nubank);

        _vm.NewSources.Should().BeEmpty();
        _vm.AvailableSources.Should().BeEmpty();
        _vm.SalarySource.Should().BeNull();
        _vm.ExpensesSource.Should().BeNull();
    }

    [Fact]
    public async Task Next_OnSourcesStepWithTypedSourceNotAdded_AddsIt()
    {
        await _vm.PrepareAsync(isRerun: false);
        await _vm.NextCommand.ExecuteAsync(null);
        _vm.NewSourceName = "Nubank";
        _vm.NewSourceBalance = 2500;

        await _vm.NextCommand.ExecuteAsync(null);

        _vm.IsCreditCardsStep.Should().BeTrue();
        var nubank = _vm.NewSources.Should().ContainSingle().Subject;
        (nubank.Name, nubank.Type, nubank.Balance).Should().Be(("Nubank", PaymentSourceType.BankAccount, 2500m));
        _vm.SalarySource.Should().BeSameAs(nubank);
        _vm.NewSourceName.Should().BeEmpty();
    }

    [Fact]
    public async Task Next_OnSourcesStepWithEmptyForm_AddsNothing()
    {
        await _vm.PrepareAsync(isRerun: false);
        await _vm.NextCommand.ExecuteAsync(null);
        _vm.NewSourceName = "  ";

        await _vm.NextCommand.ExecuteAsync(null);

        _vm.IsCreditCardsStep.Should().BeTrue();
        _vm.NewSources.Should().BeEmpty();
    }

    // --- Credit cards ---

    [Fact]
    public async Task AddCard_NoName_CannotExecute()
    {
        await _vm.PrepareAsync(isRerun: false);

        _vm.NewCardName = "  ";

        _vm.AddCardCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task AddCard_AddsCardWithBillAndResetsFormKeepingBillAccount()
    {
        await _vm.PrepareAsync(isRerun: false);
        AddSource("Itaú", PaymentSourceType.BankAccount);
        _vm.NewCardName = " Nubank ";
        _vm.NewCardLimit = 8000;
        _vm.NewCardBillDueDay = 10;

        _vm.AddCardCommand.Execute(null);

        var item = _vm.NewCards.Should().ContainSingle().Subject;
        (item.Card.Name, item.Card.Type, item.Card.Balance, item.Card.BillDueDay).Should()
            .Be(("Nubank", PaymentSourceType.CreditCard, 8000m, (int?)10));
        item.BillAccount!.Name.Should().Be("Itaú");
        item.Detail.Should().Be("Vence dia 10 · Débito em Itaú");
        _vm.CreditCards.Should().Equal(item.Card);
        _vm.AvailableSources.Should().Contain(item.Card);
        _vm.NewPurchaseCard.Should().BeSameAs(item.Card, "the first card gets the purchases");
        _vm.NewCardName.Should().BeEmpty();
        _vm.NewCardLimit.Should().Be(0);
        _vm.NewCardBillDueDay.Should().Be(SetupViewModel.NoDueDay);
        _vm.NewCardBillAccount!.Name.Should().Be("Itaú");
    }

    [Fact]
    public async Task AddCard_NoBillDueDayNorAccount_SaysSo()
    {
        await _vm.PrepareAsync(isRerun: false);

        AddCard("Visa");

        var item = _vm.NewCards.Single();
        item.Card.BillDueDay.Should().BeNull();
        item.BillAccount.Should().BeNull();
        item.Detail.Should().Be("Sem vencimento");
    }

    [Fact]
    public async Task RemoveSource_PayingCardBill_UnlinksIt()
    {
        await _vm.PrepareAsync(isRerun: false);
        AddSource("Itaú", PaymentSourceType.BankAccount);
        AddCard("Nubank");

        _vm.RemoveSourceCommand.Execute(_vm.NewSources.Single());

        _vm.NewCards.Single().BillAccount.Should().BeNull();
        _vm.NewCardBillAccount.Should().BeNull();
        _vm.BillAccounts.Should().BeEmpty();
        _vm.OtherExpensesSource.Should().BeNull();
    }

    [Fact]
    public async Task RemoveCard_RemovesItsPurchasesAndSelections()
    {
        await _vm.PrepareAsync(isRerun: false);
        AddCard("Nubank");
        AddCard("Visa");
        var nubank = _vm.NewCards[0];
        AddPurchase("Celular", 250, RecurrenceType.Installments, total: 10, current: 3);
        _vm.NewPurchaseCard = _vm.NewCards[1].Card;
        AddPurchase("Streaming", 40, RecurrenceType.Recurrent);
        _vm.ExpensesSource = nubank.Card;
        _vm.NewPurchaseCard = nubank.Card;

        _vm.RemoveCardCommand.Execute(nubank);

        _vm.NewCards.Select(c => c.Card.Name).Should().Equal("Visa");
        _vm.CreditCards.Select(c => c.Name).Should().Equal("Visa");
        _vm.AvailableSources.Should().NotContain(nubank.Card);
        _vm.Purchases.Select(p => p.Description).Should().Equal("Streaming");
        _vm.ExpensesSource.Should().BeNull();
        _vm.NewPurchaseCard!.Name.Should().Be("Visa");
    }

    [Theory]
    [InlineData("", 100, RecurrenceType.OneTime, null, null, false)]
    [InlineData("Tênis", 0, RecurrenceType.OneTime, null, null, false)]
    [InlineData("Tênis", 100, RecurrenceType.OneTime, null, null, true)]
    [InlineData("Streaming", 40, RecurrenceType.Recurrent, null, null, true)]
    [InlineData("Celular", 250, RecurrenceType.Installments, null, null, false)]
    [InlineData("Celular", 250, RecurrenceType.Installments, 10, 11, false)]
    [InlineData("Celular", 250, RecurrenceType.Installments, 10, 0, false)]
    [InlineData("Celular", 250, RecurrenceType.Installments, 10, 3, true)]
    public async Task AddPurchase_CanExecute_NeedsDescriptionAmountAndValidInstallments(
        string description, int amount, RecurrenceType recurrence, int? total, int? current, bool expected)
    {
        await _vm.PrepareAsync(isRerun: false);
        AddCard("Nubank");

        _vm.NewPurchaseDescription = description;
        _vm.NewPurchaseAmount = amount;
        _vm.SelectPurchaseRecurrenceCommand.Execute(recurrence);
        _vm.NewPurchaseTotalInstallments = total;
        _vm.NewPurchaseCurrentInstallment = current;

        _vm.AddPurchaseCommand.CanExecute(null).Should().Be(expected);
    }

    [Fact]
    public async Task AddPurchase_NoCard_CannotExecute()
    {
        await _vm.PrepareAsync(isRerun: false);

        _vm.NewPurchaseDescription = "Tênis";
        _vm.NewPurchaseAmount = 100;

        _vm.AddPurchaseCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task AddPurchase_AddsAndResetsFormKeepingCard()
    {
        await _vm.PrepareAsync(isRerun: false);
        AddCard("Nubank");

        AddPurchase(" Celular ", 250, RecurrenceType.Installments, total: 10, current: 3);

        var purchase = _vm.Purchases.Should().ContainSingle().Subject;
        purchase.Should().Be(new SetupPurchaseItem("Celular", 250, RecurrenceType.Installments, _vm.CreditCards.Single(), 10, 3));
        purchase.Detail.Should().Be("Parcela 3 de 10 · Nubank");
        _vm.NewPurchaseDescription.Should().BeEmpty();
        _vm.NewPurchaseAmount.Should().Be(0);
        _vm.NewPurchaseRecurrence.Should().Be(RecurrenceType.OneTime);
        _vm.NewPurchaseTotalInstallments.Should().BeNull();
        _vm.NewPurchaseCurrentInstallment.Should().BeNull();
        _vm.NewPurchaseCard!.Name.Should().Be("Nubank");
    }

    [Fact]
    public async Task AddPurchase_NotInstallments_IgnoresTypedInstallments()
    {
        await _vm.PrepareAsync(isRerun: false);
        AddCard("Nubank");
        _vm.NewPurchaseTotalInstallments = 10;
        _vm.NewPurchaseCurrentInstallment = 3;

        AddPurchase("Streaming", 40, RecurrenceType.Recurrent);

        var purchase = _vm.Purchases.Single();
        (purchase.TotalInstallments, purchase.CurrentInstallment).Should().Be(((int?)null, (int?)null));
        purchase.Detail.Should().Be("Todo mês · Nubank");
    }

    [Fact]
    public async Task RemovePurchase_RemovesIt()
    {
        await _vm.PrepareAsync(isRerun: false);
        AddCard("Nubank");
        AddPurchase("Tênis", 300, RecurrenceType.OneTime);

        _vm.RemovePurchaseCommand.Execute(_vm.Purchases.Single());

        _vm.HasPurchases.Should().BeFalse();
    }

    [Fact]
    public async Task Next_OnCardsStepWithTypedCardAndPurchase_AddsBoth()
    {
        await _vm.PrepareAsync(isRerun: false);
        await _vm.NextCommand.ExecuteAsync(null);
        await _vm.NextCommand.ExecuteAsync(null);
        _vm.NewCardName = "Nubank";
        await _vm.NextCommand.ExecuteAsync(null);
        _vm.BackCommand.Execute(null);
        _vm.NewCardName = "Visa";
        _vm.NewPurchaseDescription = "Tênis";
        _vm.NewPurchaseAmount = 300;

        await _vm.NextCommand.ExecuteAsync(null);

        _vm.IsIncomeStep.Should().BeTrue();
        _vm.NewCards.Select(c => c.Card.Name).Should().Equal("Nubank", "Visa");
        _vm.Purchases.Should().ContainSingle(p => p.Description == "Tênis" && p.Card.Name == "Nubank");
    }

    [Fact]
    public async Task PrepareAsync_RerunWithExistingCard_OffersItForPurchases()
    {
        var itau = new PaymentSource { Id = 1, Name = "Itaú", Type = PaymentSourceType.BankAccount };
        var nubank = new PaymentSource { Id = 2, Name = "Nubank", Type = PaymentSourceType.CreditCard };
        _paymentSourceService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<PaymentSource>>([itau, nubank]));

        await _vm.PrepareAsync(isRerun: true);

        _vm.CreditCards.Should().Equal(nubank);
        _vm.NewPurchaseCard.Should().BeSameAs(nubank);
        _vm.BillAccounts.Should().Equal(itau);
        _vm.NewCards.Should().BeEmpty();
    }

    // --- Income ---

    [Fact]
    public async Task SalarySplit_AddsAdvanceToMonthlySalary()
    {
        await _vm.PrepareAsync(isRerun: false);
        _vm.Salary = 3000;
        _vm.Advance = 2000;

        _vm.SalaryCaption.Should().Be("Salário");
        _vm.MonthlySalary.Should().Be(3000, "the advance only counts once the salary is split");

        _vm.IsSalarySplit = true;

        _vm.SalaryCaption.Should().Be("Pagamento (restante do salário)");
        _vm.MonthlySalary.Should().Be(5000);
        _vm.AdvanceDay.Should().Be(SetupViewModel.DefaultAdvanceDay);
    }

    [Fact]
    public async Task Summary_SalarySplit_CreatesPaymentAndAdvance()
    {
        await _vm.PrepareAsync(isRerun: false);
        AddSource("Itaú", PaymentSourceType.BankAccount);
        _vm.IsSalarySplit = true;
        _vm.Salary = 3000;
        _vm.Advance = 2000;

        await GoToSummaryAsync();

        _vm.SummaryRows.Select(r => (r.Description, r.Detail, r.Amount, r.EntryType)).Should().Equal(
            ("Salário", "Todo mês · dia 5 · Itaú", 3000m, FinancialEntryType.Income),
            ("Adiantamento salarial", "Todo mês · dia 20 · Itaú", 2000m, FinancialEntryType.Income));
        _vm.TotalIncome.Should().Be(5000);
    }

    [Fact]
    public async Task Summary_SalaryNotSplit_LeavesAdvanceOut()
    {
        await _vm.PrepareAsync(isRerun: false);
        _vm.IsSalarySplit = true;
        _vm.Salary = 3000;
        _vm.Advance = 2000;
        _vm.IsSalarySplit = false;

        await GoToSummaryAsync();

        _vm.SummaryRows.Select(r => r.Description).Should().Equal("Salário");
    }

    // --- Expenses ---

    [Fact]
    public async Task ToggleExpense_SelectsAndUnselects()
    {
        await _vm.PrepareAsync(isRerun: false);
        var rent = _vm.Expenses[0];

        _vm.ToggleExpenseCommand.Execute(rent);
        rent.IsSelected.Should().BeTrue();

        _vm.ToggleExpenseCommand.Execute(rent);
        rent.IsSelected.Should().BeFalse();
    }

    [Fact]
    public async Task Expenses_VaryingBills_AreEstimatedAndSupermarketIsPaidLittleByLittle()
    {
        await _vm.PrepareAsync(isRerun: false);

        _vm.Expenses.Where(e => e.IsEstimated).Select(e => e.Description).Should().Equal("Energia", "Água", "Mercado");
        _vm.Expenses.Where(e => e.AllowsPartialPayment).Select(e => e.Description).Should().Equal("Mercado");
    }

    [Fact]
    public async Task ExpenseFlags_CanBeToggledByTheUser()
    {
        await _vm.PrepareAsync(isRerun: false);
        var supermarket = _vm.Expenses.Single(e => e.Description == "Mercado");
        var internet = _vm.Expenses.Single(e => e.Description == "Internet");

        supermarket.ToggleEstimatedCommand.Execute(null);
        internet.TogglePartialPaymentCommand.Execute(null);

        supermarket.IsEstimated.Should().BeFalse();
        supermarket.AllowsPartialPayment.Should().BeTrue();
        internet.AllowsPartialPayment.Should().BeTrue();
        internet.IsEstimated.Should().BeFalse();
    }

    // --- Summary ---

    [Fact]
    public async Task Summary_ListsEntriesToCreateWithTotals()
    {
        await _vm.PrepareAsync(isRerun: false);
        _vm.Name = "Ana";
        AddSource("Nubank", PaymentSourceType.BankAccount);
        _vm.Salary = 5000;
        SelectExpense("Aluguel ou financiamento", 1500, dueDay: 10);
        SelectExpense("Energia", 200);
        SelectExpense("Internet", 0);
        SelectExpense("Mercado", 1000);

        await GoToSummaryAsync();

        _vm.SummaryTitle.Should().Be("Tudo pronto, Ana!");
        _vm.SummaryRows.Select(r => (r.Description, r.Detail, r.Amount)).Should().Equal(
            ("Salário", "Todo mês · dia 5 · Nubank", 5000m),
            ("Aluguel ou financiamento", "Todo mês · dia 10 · Nubank", 1500m),
            ("Energia", "Todo mês · valor estimado · Nubank", 200m),
            ("Mercado", "Todo mês · valor estimado · pagamento parcial · Nubank", 1000m));
        _vm.TotalIncome.Should().Be(5000);
        _vm.TotalOutcome.Should().Be(2700);
        _vm.Balance.Should().Be(2300);
    }

    [Fact]
    public async Task Summary_NothingFilled_IsEmpty()
    {
        await _vm.PrepareAsync(isRerun: false);

        await GoToSummaryAsync();

        _vm.HasSummaryRows.Should().BeFalse();
        _vm.SummaryTitle.Should().Be("Tudo pronto!");
    }

    [Fact]
    public async Task Summary_UnselectedExpenseWithAmount_IsLeftOut()
    {
        await _vm.PrepareAsync(isRerun: false);
        SelectExpense("Condomínio", 600);
        _vm.Expenses.Single(e => e.Description == "Condomínio").IsSelected = false;

        await GoToSummaryAsync();

        _vm.HasSummaryRows.Should().BeFalse();
    }

    [Fact]
    public async Task Summary_CardPurchases_ShowHowTheyRecur()
    {
        await _vm.PrepareAsync(isRerun: false);
        AddCard("Nubank");
        AddPurchase("Tênis", 300, RecurrenceType.OneTime);
        AddPurchase("Celular", 250, RecurrenceType.Installments, total: 10, current: 3);
        AddPurchase("Streaming", 40, RecurrenceType.Recurrent);

        await GoToSummaryAsync();

        _vm.SummaryRows.Select(r => (r.Icon, r.Description, r.Detail, r.Amount)).Should().Equal(
            (SetupViewModel.PurchaseIcon, "Tênis", "À vista · Nubank", 300m),
            (SetupViewModel.PurchaseIcon, "Celular", "Parcela 3 de 10 · Nubank", 250m),
            (SetupViewModel.PurchaseIcon, "Streaming", "Todo mês · Nubank", 40m));
        _vm.TotalOutcome.Should().Be(590);
    }

    [Fact]
    public async Task Summary_ExpensesPaidWithCard_DropDueDayAndFlags()
    {
        await _vm.PrepareAsync(isRerun: false);
        AddCard("Nubank");
        _vm.ExpensesSource = _vm.CreditCards.Single();
        SelectExpense("Mercado", 1000, dueDay: 15);

        await GoToSummaryAsync();

        _vm.IsExpensesSourceCreditCard.Should().BeTrue();
        _vm.SummaryRows.Select(r => r.Detail).Should().Equal("Todo mês · Nubank");
    }

    // --- Other expenses ---

    [Fact]
    public async Task OtherExpenses_OfferHealthSchoolAndLeisure()
    {
        await _vm.PrepareAsync(isRerun: false);

        _vm.HasChildren.Should().BeFalse();
        _vm.HealthExpenses.Select(e => (e.Description, e.CategoryName)).Should().Equal(
            ("Plano de saúde", "Saúde"), ("Farmácia", "Saúde"));
        _vm.SchoolExpenses.Select(e => (e.Description, e.CategoryName)).Should().Equal(
            ("Mensalidade escolar", "Educação"), ("Transporte escolar", "Educação"), ("Cursos e atividades", "Educação"));
        _vm.LeisureExpenses.Select(e => (e.Description, e.CategoryName)).Should().Equal(("Restaurantes e delivery", "Lazer"));
        new[] { _vm.HealthExpenses, _vm.SchoolExpenses, _vm.LeisureExpenses }.SelectMany(l => l)
            .Where(e => e.IsEstimated && e.AllowsPartialPayment).Select(e => e.Description)
            .Should().Equal("Farmácia", "Restaurantes e delivery");
    }

    [Fact]
    public async Task Summary_OtherExpenses_UseTheirOwnSourceAndLeaveSchoolOutWithoutChildren()
    {
        await _vm.PrepareAsync(isRerun: false);
        AddSource("Itaú", PaymentSourceType.BankAccount);
        AddSource("VR", PaymentSourceType.MealVoucher);
        _vm.OtherExpensesSource = _vm.NewSources.Single(s => s.Name == "VR");
        Select(_vm.HealthExpenses, "Plano de saúde", 600, dueDay: 10);
        Select(_vm.SchoolExpenses, "Mensalidade escolar", 1200);
        Select(_vm.LeisureExpenses, "Restaurantes e delivery", 400);

        await GoToSummaryAsync();

        _vm.SummaryRows.Select(r => (r.Description, r.Detail)).Should().Equal(
            ("Plano de saúde", "Todo mês · dia 10 · VR"),
            ("Restaurantes e delivery", "Todo mês · valor estimado · pagamento parcial · VR"));
    }

    [Fact]
    public async Task Summary_HasChildren_IncludesSchoolExpenses()
    {
        await _vm.PrepareAsync(isRerun: false);
        _vm.SelectHasChildrenCommand.Execute(true);
        Select(_vm.SchoolExpenses, "Mensalidade escolar", 1200, dueDay: 5);

        await GoToSummaryAsync();

        _vm.SummaryRows.Should().ContainSingle(r => r.Description == "Mensalidade escolar" && r.Amount == 1200 && r.Icon == "🎒");
    }

    // --- Finish ---

    [Fact]
    public async Task Next_OnSummary_CompletesSetupAndCloses()
    {
        string? closed = null;
        var received = false;
        _messenger.Register<SetupCompletedMessage>(this, (_, _) => received = true);
        NewUserSetup? saved = null;
        await _setupService.CompleteAsync(Arg.Do<NewUserSetup>(s => saved = s));
        await _vm.PrepareAsync(isRerun: false);
        _vm.SetClosedCallback(completed => { closed = completed ? "finished" : "skipped"; return Task.CompletedTask; });
        _vm.Name = "Ana";
        _vm.PhotoPath = "/cache/picked.jpg";
        AddSource("Nubank", PaymentSourceType.BankAccount);
        AddCard("Visa");
        AddPurchase("Celular", 250, RecurrenceType.Installments, total: 10, current: 3);
        _vm.Salary = 5000;
        _vm.SalaryDay = SetupViewModel.NoDueDay;
        SelectExpense("Mercado", 1200, dueDay: 15);
        _vm.Expenses.Single(e => e.Description == "Mercado").ToggleEstimatedCommand.Execute(null);
        _vm.OtherExpensesSource = _vm.CreditCards.Single();
        Select(_vm.LeisureExpenses, "Restaurantes e delivery", 400);
        await GoToSummaryAsync();

        await _vm.NextCommand.ExecuteAsync(null);

        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Ana");
        saved.PhotoPath.Should().Be("/cache/picked.jpg");
        saved.PaymentSources.Select(s => s.Name).Should().Equal("Nubank");
        saved.CreditCards.Select(c => (c.Card.Name, c.BillAccount!.Name)).Should().Equal(("Visa", "Nubank"));
        saved.Entries.Select(e => (e.Description, e.Amount, e.EntryType, e.Recurrence, e.DueDay, e.IsEstimated, e.AllowsPartialPayment, e.CategoryName, e.PaymentSource!.Name))
            .Should().Equal(
                ("Salário", 5000m, FinancialEntryType.Income, RecurrenceType.Recurrent, (int?)null, false, false, "Salário", "Nubank"),
                ("Celular", 250m, FinancialEntryType.Outcome, RecurrenceType.Installments, (int?)null, false, false, "", "Visa"),
                ("Mercado", 1200m, FinancialEntryType.Outcome, RecurrenceType.Recurrent, (int?)15, false, true, "Mercado", "Nubank"),
                ("Restaurantes e delivery", 400m, FinancialEntryType.Outcome, RecurrenceType.Recurrent, (int?)null, false, false, "Lazer", "Visa"));
        var purchase = saved.Entries.Single(e => e.Description == "Celular");
        (purchase.TotalInstallments, purchase.CurrentInstallment).Should().Be(((int?)10, (int?)3));
        received.Should().BeTrue();
        closed.Should().Be("finished");
    }

    // --- Skip ---

    [Fact]
    public async Task Skip_FirstRun_SkipsForGoodAndCloses()
    {
        string? closed = null;
        await _vm.PrepareAsync(isRerun: false);
        _vm.SetClosedCallback(completed => { closed = completed ? "finished" : "skipped"; return Task.CompletedTask; });

        await _vm.SkipCommand.ExecuteAsync(null);

        _setupService.Received(1).Skip();
        await _setupService.DidNotReceive().CompleteAsync(Arg.Any<NewUserSetup>());
        closed.Should().Be("skipped");
    }

    [Fact]
    public async Task Skip_Rerun_OnlyCloses()
    {
        string? closed = null;
        await _vm.PrepareAsync(isRerun: true);
        _vm.SetClosedCallback(completed => { closed = completed ? "finished" : "skipped"; return Task.CompletedTask; });
        _vm.Salary = 5000;

        await _vm.SkipCommand.ExecuteAsync(null);

        _setupService.DidNotReceive().Skip();
        await _setupService.DidNotReceive().CompleteAsync(Arg.Any<NewUserSetup>());
        closed.Should().Be("skipped");
    }

    private void AddSource(string name, PaymentSourceType type)
    {
        _vm.NewSourceName = name;
        _vm.SelectSourceTypeCommand.Execute(type);
        _vm.AddSourceCommand.Execute(null);
    }

    private void AddCard(string name)
    {
        _vm.NewCardName = name;
        _vm.AddCardCommand.Execute(null);
    }

    private void AddPurchase(string description, decimal amount, RecurrenceType recurrence, int? total = null, int? current = null)
    {
        _vm.NewPurchaseDescription = description;
        _vm.NewPurchaseAmount = amount;
        _vm.SelectPurchaseRecurrenceCommand.Execute(recurrence);
        _vm.NewPurchaseTotalInstallments = total;
        _vm.NewPurchaseCurrentInstallment = current;
        _vm.AddPurchaseCommand.Execute(null);
    }

    private void SelectExpense(string description, decimal amount, int dueDay = SetupViewModel.NoDueDay) =>
        Select(_vm.Expenses, description, amount, dueDay);

    private void Select(IEnumerable<SetupExpenseItem> expenses, string description, decimal amount, int dueDay = SetupViewModel.NoDueDay)
    {
        var expense = expenses.Single(e => e.Description == description);
        _vm.ToggleExpenseCommand.Execute(expense);
        expense.Amount = amount;
        expense.DueDay = dueDay;
    }

    private async Task GoToSummaryAsync()
    {
        while (!_vm.IsSummaryStep)
            await _vm.NextCommand.ExecuteAsync(null);
    }
}
