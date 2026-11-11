using Microsoft.Extensions.Logging.Abstractions;
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
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
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
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(2026, 4, 15, 12, 0, 0, TimeSpan.Zero));
        _timeProvider.LocalTimeZone.Returns(TimeZoneInfo.Utc);
        _vm = new SetupViewModel(_setupService, _paymentSourceService, _profileService, _mediaPicker, _messenger,
            new WriteGuard(Substitute.For<IAlertService>(), NullLogger<WriteGuard>.Instance), _timeProvider);
    }

    // --- PrepareAsync ---

    [Fact]
    public async Task PrepareAsync_FirstRun_StartsOnWelcomeWithDefaults()
    {
        await _vm.PrepareAsync(isRerun: false);

        _vm.Step.Should().Be(SetupStep.Welcome);
        _vm.StepNumber.Should().Be(1);
        _vm.StepCount.Should().Be(7);
        _vm.SkipButtonText.Should().Be("Pular tudo");
        _vm.NextButtonText.Should().Be("Continuar");
        _vm.SalaryDay.Should().Be(SetupViewModel.DefaultSalaryDay);
        _vm.Expenses.Select(e => e.Description).Should().Equal(
            "Aluguel", "Condomínio", "Energia", "Água", "Internet", "Celular", "Mercado");
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
        AllExpenses().Should().OnlyContain(e => e.PaymentSource!.Name == "Nubank");
        _vm.NewHomeSource!.Name.Should().Be("Nubank");
        _vm.NewCarSource!.Name.Should().Be("Nubank");
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
        AllExpenses().Should().OnlyContain(e => e.PaymentSource == null);
        _vm.NewHomeSource.Should().BeNull();
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
        _vm.HealthExpenses.Should().OnlyContain(e => e.PaymentSource == null);
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
        _vm.Expenses[0].PaymentSource = nubank.Card;
        _vm.NewPurchaseCard = nubank.Card;

        _vm.RemoveCardCommand.Execute(nubank);

        _vm.NewCards.Select(c => c.Card.Name).Should().Equal("Visa");
        _vm.CreditCards.Select(c => c.Name).Should().Equal("Visa");
        _vm.AvailableSources.Should().NotContain(nubank.Card);
        _vm.Purchases.Select(p => p.Description).Should().Equal("Streaming");
        _vm.Expenses[0].PaymentSource.Should().BeNull();
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
    public async Task Expenses_OnlyVaryingOnesOfferEstimatedAndOnlyThosePaidLittleByLittleOfferPartialPayment()
    {
        await _vm.PrepareAsync(isRerun: false);
        _vm.SelectHasChildrenCommand.Execute(true);
        _vm.SelectHasCarCommand.Execute(true);
        SetupExpenseItem[] all = [.. _vm.Expenses, .. _vm.HealthExpenses, .. _vm.SchoolExpenses, .. _vm.CarExpenses, .. _vm.LeisureExpenses];

        all.Where(e => e.CanBeEstimated).Select(e => e.Description).Should().Equal(
            "Condomínio", "Energia", "Água", "Mercado", "Plano de saúde", "Farmácia", "Combustível", "Restaurantes e delivery");
        all.Where(e => e.CanBePartiallyPaid).Select(e => e.Description).Should().Equal(
            "Mercado", "Farmácia", "Combustível", "Restaurantes e delivery");
        all.Should().OnlyContain(e => (!e.IsEstimated || e.CanBeEstimated) && (!e.AllowsPartialPayment || e.CanBePartiallyPaid));
    }

    [Fact]
    public async Task ExpenseFlags_OfferedOnes_CanBeToggledByTheUser()
    {
        await _vm.PrepareAsync(isRerun: false);
        var supermarket = _vm.Expenses.Single(e => e.Description == "Mercado");
        var condo = _vm.Expenses.Single(e => e.Description == "Condomínio");

        supermarket.ToggleEstimatedCommand.Execute(null);
        condo.ToggleEstimatedCommand.Execute(null);

        supermarket.IsEstimated.Should().BeFalse();
        supermarket.AllowsPartialPayment.Should().BeTrue();
        condo.IsEstimated.Should().BeTrue();
    }

    [Fact]
    public async Task ExpenseFlags_NotOffered_CannotBeTurnedOn()
    {
        await _vm.PrepareAsync(isRerun: false);
        var rent = _vm.Expenses.Single(e => e.Description == "Aluguel");
        var energy = _vm.Expenses.Single(e => e.Description == "Energia");

        rent.ToggleEstimatedCommand.Execute(null);
        rent.TogglePartialPaymentCommand.Execute(null);
        energy.TogglePartialPaymentCommand.Execute(null);

        (rent.IsEstimated, rent.AllowsPartialPayment, energy.AllowsPartialPayment).Should().Be((false, false, false));
    }

    [Fact]
    public async Task ExpenseOptions_ShowOnlyOnceSelectedAndOnlyWhenOffered()
    {
        await _vm.PrepareAsync(isRerun: false);
        var rent = _vm.Expenses.Single(e => e.Description == "Aluguel");
        var energy = _vm.Expenses.Single(e => e.Description == "Energia");
        (energy.ShowsEstimatedOption, energy.ShowsPartialPaymentOption).Should().Be((false, false));

        _vm.ToggleExpenseCommand.Execute(rent);
        _vm.ToggleExpenseCommand.Execute(energy);

        (rent.ShowsEstimatedOption, rent.ShowsPartialPaymentOption).Should().Be((false, false));
        (energy.ShowsEstimatedOption, energy.ShowsPartialPaymentOption).Should().Be((true, false));
    }

    // --- Summary ---

    [Fact]
    public async Task Summary_ListsEntriesToCreateWithTotals()
    {
        await _vm.PrepareAsync(isRerun: false);
        _vm.Name = "Ana";
        AddSource("Nubank", PaymentSourceType.BankAccount);
        _vm.Salary = 5000;
        SelectExpense("Aluguel", 1500, dueDay: 10);
        SelectExpense("Energia", 200);
        SelectExpense("Internet", 0);
        SelectExpense("Mercado", 1000);

        await GoToSummaryAsync();

        _vm.SummaryTitle.Should().Be("Tudo pronto, Ana!");
        _vm.SummaryRows.Select(r => (r.Description, r.Detail, r.Amount)).Should().Equal(
            ("Salário", "Todo mês · dia 5 · Nubank", 5000m),
            ("Aluguel", "Todo mês · dia 10 · Nubank", 1500m),
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
    public async Task Summary_ExpensePaidWithCard_DropsDueDayAndFlags()
    {
        await _vm.PrepareAsync(isRerun: false);
        AddSource("Itaú", PaymentSourceType.BankAccount);
        AddCard("Nubank");
        SelectExpense("Mercado", 1000, dueDay: 15);
        SelectExpense("Aluguel", 1500, dueDay: 10);
        var supermarket = _vm.Expenses.Single(e => e.Description == "Mercado");

        supermarket.PaymentSource = _vm.CreditCards.Single();
        await GoToSummaryAsync();

        (supermarket.IsOnCreditCardBill, supermarket.ShowsDueDay, supermarket.ShowsEstimatedOption, supermarket.ShowsPartialPaymentOption)
            .Should().Be((true, false, false, false));
        _vm.SummaryRows.Select(r => (r.Description, r.Detail)).Should().Equal(
            ("Aluguel", "Todo mês · dia 10 · Itaú"),
            ("Mercado", "Todo mês · Nubank"));
    }

    [Fact]
    public async Task Finish_ExpensePaidWithCard_SavesItOnTheCardWithoutDueDayNorFlags()
    {
        NewUserSetup? saved = null;
        await _setupService.CompleteAsync(Arg.Do<NewUserSetup>(s => saved = s));
        await _vm.PrepareAsync(isRerun: false);
        AddCard("Nubank");
        SelectExpense("Mercado", 1000, dueDay: 15);
        _vm.Expenses.Single(e => e.Description == "Mercado").PaymentSource = _vm.CreditCards.Single();
        await GoToSummaryAsync();

        await _vm.NextCommand.ExecuteAsync(null);

        var supermarket = saved!.Entries.Single();
        (supermarket.DueDay, supermarket.IsEstimated, supermarket.AllowsPartialPayment, supermarket.PaymentSource!.Name)
            .Should().Be(((int?)null, false, false, "Nubank"));
    }

    [Fact]
    public async Task ToggleExpense_BuildsItsFieldsOnlyWhileTicked()
    {
        await _vm.PrepareAsync(isRerun: false);
        AddSource("Itaú", PaymentSourceType.BankAccount);
        var rent = _vm.Expenses.Single(e => e.Description == "Aluguel");

        _vm.ToggleExpenseCommand.Execute(rent);
        var whileTicked = rent.Fields.ToList();
        _vm.ToggleExpenseCommand.Execute(rent);

        whileTicked.Should().Equal(rent);
        rent.Fields.Should().BeEmpty();
        rent.Sources.Should().BeSameAs(_vm.AvailableSources);
    }

    [Fact]
    public async Task ExpenseSource_BackToAccount_ShowsDueDayAndFlagsAgain()
    {
        await _vm.PrepareAsync(isRerun: false);
        AddSource("Itaú", PaymentSourceType.BankAccount);
        AddCard("Nubank");
        SelectExpense("Mercado", 1000);
        var supermarket = _vm.Expenses.Single(e => e.Description == "Mercado");
        supermarket.PaymentSource = _vm.CreditCards.Single();

        supermarket.PaymentSource = _vm.BillAccounts.Single();

        (supermarket.IsOnCreditCardBill, supermarket.ShowsDueDay, supermarket.ShowsEstimatedOption, supermarket.ShowsPartialPaymentOption)
            .Should().Be((false, true, true, true));
    }

    [Fact]
    public async Task AddSource_AfterExpensesPicked_KeepsTheirSourcesAndFillsOnlyTheEmptyOnes()
    {
        await _vm.PrepareAsync(isRerun: false);
        AddCard("Nubank");
        var rent = _vm.Expenses.Single(e => e.Description == "Aluguel");
        rent.PaymentSource = _vm.CreditCards.Single();

        AddSource("Itaú", PaymentSourceType.BankAccount);

        rent.PaymentSource!.Name.Should().Be("Nubank");
        AllExpenses().Where(e => e != rent).Should().OnlyContain(e => e.PaymentSource!.Name == "Itaú");
    }

    [Fact]
    public async Task RemoveSource_AboveAnotherPicked_KeepsTheOtherPicks()
    {
        await _vm.PrepareAsync(isRerun: false);
        AddSource("Itaú", PaymentSourceType.BankAccount);
        AddSource("Nubank", PaymentSourceType.BankAccount);
        AddSource("VR", PaymentSourceType.MealVoucher);
        var nubank = _vm.NewSources.Single(s => s.Name == "Nubank");
        var rent = _vm.Expenses.Single(e => e.Description == "Aluguel");
        rent.PaymentSource = nubank;
        _vm.SalarySource = nubank;

        _vm.RemoveSourceCommand.Execute(_vm.NewSources.Single(s => s.Name == "Itaú"));

        rent.PaymentSource.Should().BeSameAs(nubank);
        _vm.SalarySource.Should().BeSameAs(nubank);
        _vm.Expenses.Where(e => e != rent).Should().OnlyContain(e => e.PaymentSource == null);
    }

    [Fact]
    public async Task SkipStep_Expenses_ComeBackPaidWithTheFirstAccount()
    {
        await _vm.PrepareAsync(isRerun: false);
        AddSource("Itaú", PaymentSourceType.BankAccount);
        AddCard("Nubank");
        await GoToStepAsync(SetupStep.HouseExpenses);
        _vm.Expenses[0].PaymentSource = _vm.CreditCards.Single();

        _vm.SkipStepCommand.Execute(null);

        _vm.Expenses.Should().OnlyContain(e => e.PaymentSource!.Name == "Itaú");
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
    public async Task Summary_OtherExpenses_UseEachOnesSourceAndLeaveSchoolOutWithoutChildren()
    {
        await _vm.PrepareAsync(isRerun: false);
        AddSource("Itaú", PaymentSourceType.BankAccount);
        AddSource("VR", PaymentSourceType.MealVoucher);
        Select(_vm.HealthExpenses, "Plano de saúde", 600, dueDay: 10);
        Select(_vm.SchoolExpenses, "Mensalidade escolar", 1200);
        Select(_vm.LeisureExpenses, "Restaurantes e delivery", 400);
        _vm.LeisureExpenses.Single().PaymentSource = _vm.NewSources.Single(s => s.Name == "VR");

        await GoToSummaryAsync();

        _vm.SummaryRows.Select(r => (r.Description, r.Detail)).Should().Equal(
            ("Plano de saúde", "Todo mês · dia 10 · Itaú"),
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

    [Fact]
    public async Task CarExpenses_FuelIsEstimatedAndInsuranceIsFixedAndMonthlyFromTheCurrentMonth()
    {
        await _vm.PrepareAsync(isRerun: false);

        _vm.HasCar.Should().BeFalse();
        _vm.CarExpenses.Select(e => (e.Description, e.CategoryName, e.IsEstimated, e.AllowsPartialPayment, e.OffersYearlyPayment)).Should().Equal(
            ("Combustível", "Transporte", true, false, false),
            ("Seguro do carro", "Transporte", false, false, true));
        var insurance = _vm.CarExpenses.Single(e => e.Description == SetupViewModel.CarInsuranceDescription);
        (insurance.YearlyInstallments, insurance.YearlyStartMonth, insurance.IsPaidYearly).Should().Be((SetupExpenseItem.MonthlyPayment, 4, false));
        _vm.YearlyPaymentOptions.Should().Equal(Enumerable.Range(0, 13));
    }

    [Theory]
    [InlineData(SetupExpenseItem.MonthlyPayment, "Valor mensal", false)]
    [InlineData(1, "Valor", true)]
    [InlineData(10, "Valor de cada parcela", true)]
    public async Task CarInsurance_PaymentChoice_ChangesAmountCaptionAndShowsFirstMonth(int installments, string caption, bool isPaidYearly)
    {
        await _vm.PrepareAsync(isRerun: false);
        var insurance = _vm.CarInsurance!;

        insurance.YearlyInstallments = installments;

        (insurance.AmountCaption, insurance.IsPaidYearly).Should().Be((caption, isPaidYearly));
    }

    [Fact]
    public async Task CarInsurancePayment_ShowsOnlyWithACarAndTheInsuranceTicked()
    {
        await _vm.PrepareAsync(isRerun: false);
        _vm.CarInsurance!.Description.Should().Be(SetupViewModel.CarInsuranceDescription);
        _vm.SelectHasCarCommand.Execute(true);
        _vm.IsCarInsurancePaymentVisible.Should().BeFalse();

        _vm.ToggleExpenseCommand.Execute(_vm.CarInsurance);
        _vm.IsCarInsurancePaymentVisible.Should().BeTrue();

        _vm.SelectHasCarCommand.Execute(false);
        _vm.IsCarInsurancePaymentVisible.Should().BeFalse();
    }

    [Fact]
    public async Task SkipStep_OtherExpenses_ResetsTheCarInsurancePayment()
    {
        await _vm.PrepareAsync(isRerun: false);
        await GoToStepAsync(SetupStep.OtherExpenses);
        _vm.SelectHasCarCommand.Execute(true);
        _vm.ToggleExpenseCommand.Execute(_vm.CarInsurance);
        _vm.CarInsurance!.YearlyInstallments = 10;

        _vm.SkipStepCommand.Execute(null);

        (_vm.CarInsurance!.IsSelected, _vm.CarInsurance.YearlyInstallments, _vm.IsCarInsurancePaymentVisible)
            .Should().Be((false, SetupExpenseItem.MonthlyPayment, false));
    }

    [Theory]
    [InlineData(1, "Todo ano em março · dia 15")]
    [InlineData(10, "Todo ano, março a dezembro (10x) · dia 15")]
    public async Task Summary_CarInsurancePaidYearly_IsAnnualFromItsFirstMonth(int installments, string detail)
    {
        await _vm.PrepareAsync(isRerun: false);
        _vm.SelectHasCarCommand.Execute(true);
        Select(_vm.CarExpenses, SetupViewModel.CarInsuranceDescription, 300, dueDay: 15);
        var insurance = _vm.CarExpenses.Single(e => e.Description == SetupViewModel.CarInsuranceDescription);
        insurance.YearlyInstallments = installments;
        insurance.YearlyStartMonth = 3;

        await GoToSummaryAsync();

        _vm.SummaryRows.Select(r => (r.Description, r.Detail, r.Amount)).Should().Equal(
            (SetupViewModel.CarInsuranceDescription, detail, 300m));
    }

    [Fact]
    public async Task Finish_CarInsuranceInInstallments_SavesAnAnnualEntry()
    {
        NewUserSetup? saved = null;
        await _setupService.CompleteAsync(Arg.Do<NewUserSetup>(s => saved = s));
        await _vm.PrepareAsync(isRerun: false);
        _vm.SelectHasCarCommand.Execute(true);
        Select(_vm.CarExpenses, SetupViewModel.CarInsuranceDescription, 300);
        var insurance = _vm.CarExpenses.Single(e => e.Description == SetupViewModel.CarInsuranceDescription);
        insurance.YearlyInstallments = 10;
        insurance.YearlyStartMonth = 3;
        await GoToSummaryAsync();

        await _vm.NextCommand.ExecuteAsync(null);

        saved!.Entries.Select(e => (e.Description, e.Amount, e.Recurrence, e.TotalInstallments, e.AnnualStartMonth, e.CategoryName))
            .Should().Equal((SetupViewModel.CarInsuranceDescription, 300m, RecurrenceType.Annual, (int?)10, (int?)3, "Transporte"));
    }

    [Fact]
    public async Task Finish_CarInsurancePaidMonthly_SavesARecurrentEntry()
    {
        NewUserSetup? saved = null;
        await _setupService.CompleteAsync(Arg.Do<NewUserSetup>(s => saved = s));
        await _vm.PrepareAsync(isRerun: false);
        _vm.SelectHasCarCommand.Execute(true);
        Select(_vm.CarExpenses, SetupViewModel.CarInsuranceDescription, 250);
        await GoToSummaryAsync();

        await _vm.NextCommand.ExecuteAsync(null);

        saved!.Entries.Select(e => (e.Recurrence, e.TotalInstallments, e.AnnualStartMonth))
            .Should().Equal((RecurrenceType.Recurrent, (int?)null, (int?)null));
    }

    [Fact]
    public async Task Summary_HasCar_IncludesCarExpenses()
    {
        await _vm.PrepareAsync(isRerun: false);
        _vm.SelectHasCarCommand.Execute(true);
        Select(_vm.CarExpenses, "Combustível", 500);
        Select(_vm.CarExpenses, "Seguro do carro", 250, dueDay: 15);

        await GoToSummaryAsync();

        _vm.SummaryRows.Select(r => (r.Description, r.Detail, r.Amount)).Should().Equal(
            ("Combustível", "Todo mês · valor estimado", 500m),
            ("Seguro do carro", "Todo mês · dia 15", 250m));
    }

    [Fact]
    public async Task Summary_NoCar_LeavesCarExpensesOut()
    {
        await _vm.PrepareAsync(isRerun: false);
        _vm.SelectHasCarCommand.Execute(true);
        Select(_vm.CarExpenses, "Combustível", 500);
        _vm.SelectHasCarCommand.Execute(false);

        await GoToSummaryAsync();

        _vm.HasSummaryRows.Should().BeFalse();
    }

    // --- Homes and cars ---

    [Fact]
    public async Task AddHome_NoAmount_CannotExecute()
    {
        await _vm.PrepareAsync(isRerun: false);

        _vm.NewHomeName = "Apartamento";

        _vm.AddHomeCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task AddHome_AddsHomeAndResetsForm()
    {
        await _vm.PrepareAsync(isRerun: false);
        _vm.NewHomeName = " Apartamento ";
        _vm.NewHomeFinancing = 2000;
        _vm.NewHomeFinancingDueDay = 10;
        _vm.NewHomeIptu = 150;
        _vm.NewHomeIptuStartMonth = 2;
        _vm.NewHomeIptuInstallments = 10;

        _vm.AddHomeCommand.Execute(null);

        var home = _vm.Homes.Should().ContainSingle().Subject;
        home.Should().Be(new SetupHomeItem("Apartamento", 2000, 10, 150, 2, 10));
        home.Detail.Should().Be("Financiamento · IPTU todo ano, fevereiro a novembro (10x)");
        (_vm.NewHomeName, _vm.NewHomeFinancing, _vm.NewHomeIptu, _vm.NewHomeIptuStartMonth, _vm.NewHomeIptuInstallments)
            .Should().Be((string.Empty, 0m, 0m, SetupViewModel.DefaultIptuStartMonth, 1));
    }

    [Fact]
    public async Task AddHome_NoName_IsCalledCasa()
    {
        await _vm.PrepareAsync(isRerun: false);
        _vm.NewHomeIptu = 1200;

        _vm.AddHomeCommand.Execute(null);

        _vm.Homes.Single().Name.Should().Be("Casa");
        _vm.Homes.Single().Detail.Should().Be("IPTU todo ano em fevereiro");
    }

    [Fact]
    public async Task AddCar_AddsCarAndResetsForm()
    {
        await _vm.PrepareAsync(isRerun: false);
        _vm.NewCarName = "Onix";
        _vm.NewCarIpva = 800;
        _vm.NewCarIpvaStartMonth = 1;
        _vm.NewCarIpvaInstallments = 3;
        _vm.NewCarLicensing = 160;
        _vm.NewCarLicensingMonth = 7;

        _vm.AddCarCommand.Execute(null);

        var car = _vm.Cars.Should().ContainSingle().Subject;
        car.Should().Be(new SetupCarItem("Onix", 800, 1, 3, 160, 7));
        car.Detail.Should().Be("IPVA todo ano, janeiro a março (3x) · Licenciamento todo ano em julho");
        (_vm.NewCarName, _vm.NewCarIpva, _vm.NewCarLicensing).Should().Be((string.Empty, 0m, 0m));
    }

    [Fact]
    public async Task AddCar_NoAmount_CannotExecute()
    {
        await _vm.PrepareAsync(isRerun: false);

        _vm.NewCarName = "Onix";

        _vm.AddCarCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void DescribeAnnual_InstallmentsAcrossTheYearEnd_WrapToJanuary()
    {
        SetupViewModel.DescribeAnnual(11, 4).Should().Be("Todo ano, novembro a fevereiro (4x)");
    }

    [Fact]
    public async Task Summary_HomesAndCars_CreateFinancingAndAnnualTaxes()
    {
        await _vm.PrepareAsync(isRerun: false);
        AddSource("Itaú", PaymentSourceType.BankAccount);
        SelectExpense("Aluguel", 1500, dueDay: 5);
        _vm.SelectHasOwnHomeCommand.Execute(true);
        _vm.NewHomeName = "Apartamento";
        _vm.NewHomeFinancing = 2000;
        _vm.NewHomeFinancingDueDay = 10;
        _vm.NewHomeIptu = 150;
        _vm.NewHomeIptuInstallments = 10;
        _vm.AddHomeCommand.Execute(null);
        _vm.SelectHasCarCommand.Execute(true);
        _vm.NewCarName = "Onix";
        _vm.NewCarIpva = 800;
        _vm.NewCarIpvaInstallments = 3;
        _vm.NewCarLicensing = 160;
        _vm.AddCarCommand.Execute(null);

        await GoToSummaryAsync();

        _vm.SummaryRows.Select(r => (r.Icon, r.Description, r.Detail, r.Amount)).Should().Equal(
            ("🏠", "Aluguel", "Todo mês · dia 5 · Itaú", 1500m),
            ("🏠", "Financiamento (Apartamento)", "Todo mês · dia 10 · Itaú", 2000m),
            ("🧾", "IPTU (Apartamento)", "Todo ano, fevereiro a novembro (10x) · Itaú", 150m),
            ("🚗", "IPVA (Onix)", "Todo ano, janeiro a março (3x) · Itaú", 800m),
            ("📄", "Licenciamento (Onix)", "Todo ano em julho · Itaú", 160m));
    }

    [Fact]
    public async Task Next_WithTypedHomeAndCar_AddsThem()
    {
        await _vm.PrepareAsync(isRerun: false);
        await GoToStepAsync(SetupStep.HouseExpenses);
        _vm.SelectHasOwnHomeCommand.Execute(true);
        _vm.NewHomeIptu = 1200;
        await _vm.NextCommand.ExecuteAsync(null);
        _vm.SelectHasCarCommand.Execute(true);
        _vm.NewCarLicensing = 160;

        await _vm.NextCommand.ExecuteAsync(null);

        _vm.Homes.Should().ContainSingle();
        _vm.Cars.Should().ContainSingle();
        _vm.SummaryRows.Select(r => r.Description).Should().Equal("IPTU (Casa)", "Licenciamento (Carro)");
    }

    [Fact]
    public async Task Summary_NoOwnHomeNorCar_LeavesTheirTaxesOut()
    {
        await _vm.PrepareAsync(isRerun: false);
        _vm.SelectHasOwnHomeCommand.Execute(true);
        _vm.NewHomeIptu = 1200;
        _vm.AddHomeCommand.Execute(null);
        _vm.SelectHasOwnHomeCommand.Execute(false);
        _vm.SelectHasCarCommand.Execute(true);
        _vm.NewCarIpva = 800;
        _vm.AddCarCommand.Execute(null);
        _vm.SelectHasCarCommand.Execute(false);

        await GoToSummaryAsync();

        _vm.HasSummaryRows.Should().BeFalse();
    }

    [Fact]
    public async Task Finish_AnnualTaxes_AreSentAsAnnualEntries()
    {
        NewUserSetup? saved = null;
        await _setupService.CompleteAsync(Arg.Do<NewUserSetup>(s => saved = s));
        await _vm.PrepareAsync(isRerun: false);
        _vm.SelectHasCarCommand.Execute(true);
        _vm.NewCarIpva = 800;
        _vm.NewCarIpvaStartMonth = 3;
        _vm.NewCarIpvaInstallments = 3;
        _vm.AddCarCommand.Execute(null);
        await GoToSummaryAsync();

        await _vm.NextCommand.ExecuteAsync(null);

        var ipva = saved!.Entries.Single();
        (ipva.Description, ipva.Recurrence, ipva.AnnualStartMonth, ipva.TotalInstallments, ipva.CategoryName)
            .Should().Be(("IPVA (Carro)", RecurrenceType.Annual, (int?)3, (int?)3, "Transporte"));
    }

    [Fact]
    public async Task Finish_HomeAndCar_ArePaidWithTheSourcePickedForEach()
    {
        NewUserSetup? saved = null;
        await _setupService.CompleteAsync(Arg.Do<NewUserSetup>(s => saved = s));
        await _vm.PrepareAsync(isRerun: false);
        AddSource("Itaú", PaymentSourceType.BankAccount);
        AddCard("Nubank");
        _vm.SelectHasOwnHomeCommand.Execute(true);
        _vm.NewHomeFinancing = 2000;
        _vm.NewHomeIptu = 150;
        _vm.AddHomeCommand.Execute(null);
        _vm.SelectHasCarCommand.Execute(true);
        _vm.NewCarIpva = 800;
        _vm.NewCarSource = _vm.CreditCards.Single();
        _vm.AddCarCommand.Execute(null);
        await GoToSummaryAsync();

        await _vm.NextCommand.ExecuteAsync(null);

        _vm.Homes.Single().Detail.Should().EndWith("· Itaú");
        _vm.Cars.Single().Detail.Should().EndWith("· Nubank");
        _vm.NewCarSource!.Name.Should().Be("Nubank", "the next car is usually paid the same way");
        saved!.Entries.Select(e => (e.Description, e.PaymentSource!.Name)).Should().Equal(
            ("Financiamento (Casa)", "Itaú"),
            ("IPTU (Casa)", "Itaú"),
            ("IPVA (Carro)", "Nubank"));
    }

    [Fact]
    public async Task SkipStep_HouseAndOther_DiscardHomesAndCars()
    {
        await _vm.PrepareAsync(isRerun: false);
        await GoToStepAsync(SetupStep.HouseExpenses);
        _vm.SelectHasOwnHomeCommand.Execute(true);
        _vm.NewHomeIptu = 1200;
        _vm.AddHomeCommand.Execute(null);
        _vm.SkipStepCommand.Execute(null);
        _vm.SelectHasCarCommand.Execute(true);
        _vm.NewCarIpva = 800;
        _vm.AddCarCommand.Execute(null);

        _vm.SkipStepCommand.Execute(null);

        (_vm.HasOwnHome, _vm.HasCar).Should().Be((false, false));
        _vm.Homes.Should().BeEmpty();
        _vm.Cars.Should().BeEmpty();
        _vm.HasSummaryRows.Should().BeFalse();
    }

    // --- Skipping steps ---

    [Fact]
    public async Task SkipStep_GoesToNextStepAndSummaryListsIt()
    {
        await _vm.PrepareAsync(isRerun: false);
        await _vm.NextCommand.ExecuteAsync(null);

        _vm.SkipStepCommand.Execute(null);

        _vm.Step.Should().Be(SetupStep.CreditCards);
        await GoToSummaryAsync();
        _vm.HasSkippedSteps.Should().BeTrue();
        _vm.SkippedStepsText.Should().Be("Contas e vales");
    }

    [Fact]
    public async Task SkipStep_EveryStep_ListsThemInOrder()
    {
        await _vm.PrepareAsync(isRerun: false);

        while (!_vm.IsSummaryStep)
            _vm.SkipStepCommand.Execute(null);

        _vm.SkippedStepsText.Should().Be("Nome e foto, Contas e vales, Cartões de crédito, Renda, Gastos da casa, Outros gastos");
        _vm.HasSummaryRows.Should().BeFalse();
    }

    [Fact]
    public async Task SkipStep_OnSummary_DoesNothing()
    {
        await _vm.PrepareAsync(isRerun: false);
        await GoToSummaryAsync();

        _vm.SkipStepCommand.Execute(null);

        _vm.IsSummaryStep.Should().BeTrue();
        _vm.HasSkippedSteps.Should().BeFalse();
    }

    [Fact]
    public async Task SkipStep_Welcome_KeepsStoredProfile()
    {
        _profileService.Name.Returns("Ana");
        _profileService.PhotoPath.Returns("/data/profile.jpg");
        await _vm.PrepareAsync(isRerun: true);
        _vm.Name = "Bia";
        _vm.PhotoPath = null;

        _vm.SkipStepCommand.Execute(null);

        _vm.Name.Should().Be("Ana");
        _vm.PhotoPath.Should().Be("/data/profile.jpg");
    }

    [Fact]
    public async Task SkipStep_Sources_DiscardsThemAndTheirSelections()
    {
        await _vm.PrepareAsync(isRerun: false);
        await _vm.NextCommand.ExecuteAsync(null);
        AddSource("Itaú", PaymentSourceType.BankAccount);
        _vm.NewSourceName = "VR";

        _vm.SkipStepCommand.Execute(null);

        _vm.NewSources.Should().BeEmpty("a skipped step creates nothing, typed or added");
        _vm.AvailableSources.Should().BeEmpty();
        _vm.SalarySource.Should().BeNull();
        _vm.NewCardBillAccount.Should().BeNull();
        _vm.NewSourceName.Should().BeEmpty();
    }

    [Fact]
    public async Task SkipStep_CreditCards_DiscardsCardsAndPurchases()
    {
        await _vm.PrepareAsync(isRerun: false);
        await _vm.NextCommand.ExecuteAsync(null);
        await _vm.NextCommand.ExecuteAsync(null);
        AddCard("Nubank");
        AddPurchase("Tênis", 300, RecurrenceType.OneTime);
        _vm.NewCardName = "Visa";

        _vm.SkipStepCommand.Execute(null);

        _vm.IsIncomeStep.Should().BeTrue();
        _vm.NewCards.Should().BeEmpty();
        _vm.CreditCards.Should().BeEmpty();
        _vm.Purchases.Should().BeEmpty();
        _vm.NewCardName.Should().BeEmpty();
    }

    [Fact]
    public async Task SkipStep_Income_DiscardsSalary()
    {
        await _vm.PrepareAsync(isRerun: false);
        await GoToStepAsync(SetupStep.Income);
        _vm.IsSalarySplit = true;
        _vm.Salary = 3000;
        _vm.Advance = 2000;
        _vm.SalaryDay = 7;

        _vm.SkipStepCommand.Execute(null);

        (_vm.Salary, _vm.Advance, _vm.IsSalarySplit, _vm.SalaryDay).Should().Be((0m, 0m, false, SetupViewModel.DefaultSalaryDay));
    }

    [Fact]
    public async Task SkipStep_Expenses_UnselectsThem()
    {
        await _vm.PrepareAsync(isRerun: false);
        await GoToStepAsync(SetupStep.HouseExpenses);
        SelectExpense("Aluguel", 1500);
        _vm.SkipStepCommand.Execute(null);
        _vm.SelectHasChildrenCommand.Execute(true);
        _vm.SelectHasCarCommand.Execute(true);
        Select(_vm.CarExpenses, "Combustível", 500);
        Select(_vm.LeisureExpenses, "Restaurantes e delivery", 400);

        _vm.SkipStepCommand.Execute(null);

        _vm.HasSummaryRows.Should().BeFalse();
        (_vm.HasChildren, _vm.HasCar).Should().Be((false, false));
        _vm.SkippedStepsText.Should().Be("Gastos da casa, Outros gastos");
    }

    [Fact]
    public async Task SkippedStep_FilledLaterAndContinued_IsNoLongerSkipped()
    {
        await _vm.PrepareAsync(isRerun: false);
        await GoToStepAsync(SetupStep.Income);
        _vm.SkipStepCommand.Execute(null);
        _vm.BackCommand.Execute(null);
        _vm.Salary = 5000;

        await GoToSummaryAsync();

        _vm.HasSkippedSteps.Should().BeFalse();
        _vm.SummaryRows.Select(r => r.Description).Should().Equal("Salário");
    }

    [Fact]
    public async Task PrepareAsync_AfterSkippingSteps_ForgetsThem()
    {
        await _vm.PrepareAsync(isRerun: false);
        _vm.SkipStepCommand.Execute(null);

        await _vm.PrepareAsync(isRerun: false);
        await GoToSummaryAsync();

        _vm.HasSkippedSteps.Should().BeFalse();
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
        Select(_vm.LeisureExpenses, "Restaurantes e delivery", 400);
        _vm.LeisureExpenses.Single().PaymentSource = _vm.CreditCards.Single();
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

    private IEnumerable<SetupExpenseItem> AllExpenses() =>
        [.. _vm.Expenses, .. _vm.HealthExpenses, .. _vm.SchoolExpenses, .. _vm.CarExpenses, .. _vm.LeisureExpenses];

    private void SelectExpense(string description, decimal amount, int dueDay = SetupViewModel.NoDueDay) =>
        Select(_vm.Expenses, description, amount, dueDay);

    private void Select(IEnumerable<SetupExpenseItem> expenses, string description, decimal amount, int dueDay = SetupViewModel.NoDueDay)
    {
        var expense = expenses.Single(e => e.Description == description);
        _vm.ToggleExpenseCommand.Execute(expense);
        expense.Amount = amount;
        expense.DueDay = dueDay;
    }

    private async Task GoToStepAsync(SetupStep step)
    {
        while (_vm.Step != step)
            await _vm.NextCommand.ExecuteAsync(null);
    }

    private async Task GoToSummaryAsync()
    {
        while (!_vm.IsSummaryStep)
            await _vm.NextCommand.ExecuteAsync(null);
    }
}
