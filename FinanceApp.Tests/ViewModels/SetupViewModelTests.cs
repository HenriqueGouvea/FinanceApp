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
        _vm.StepCount.Should().Be(5);
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

        for (var i = 0; i < 4; i++)
        {
            await _vm.NextCommand.ExecuteAsync(null);
            steps.Add(_vm.Step);
        }

        steps.Should().Equal(SetupStep.Welcome, SetupStep.PaymentSources, SetupStep.Income, SetupStep.HouseExpenses, SetupStep.Summary);
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
        _vm.NewSourceName = " Visa ";
        _vm.SelectSourceTypeCommand.Execute(PaymentSourceType.CreditCard);
        _vm.NewSourceBalance = 8000;

        _vm.AddSourceCommand.Execute(null);

        var visa = _vm.NewSources.Should().ContainSingle().Subject;
        (visa.Name, visa.Type, visa.Balance).Should().Be(("Visa", PaymentSourceType.CreditCard, 8000m));
        _vm.AvailableSources.Should().Equal(visa);
        _vm.NewSourceName.Should().BeEmpty();
        _vm.NewSourceType.Should().Be(PaymentSourceType.BankAccount);
        _vm.NewSourceBalance.Should().Be(0);
    }

    [Fact]
    public async Task AddSource_FirstBankAccount_BecomesSalaryAndExpensesSource()
    {
        await _vm.PrepareAsync(isRerun: false);
        AddSource("Visa", PaymentSourceType.CreditCard);
        AddSource("Nubank", PaymentSourceType.BankAccount);
        AddSource("Itaú", PaymentSourceType.BankAccount);

        _vm.SalarySource!.Name.Should().Be("Nubank");
        _vm.ExpensesSource!.Name.Should().Be("Nubank");
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
        var supermarket = _vm.Expenses.Single(e => e.Description == "Mercado");
        supermarket.AllowsPartialPayment.Should().BeTrue();
        supermarket.HasDueDay.Should().BeFalse();
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

        await GoToSummaryAsync();

        _vm.SummaryTitle.Should().Be("Tudo pronto, Ana!");
        _vm.SummaryRows.Select(r => (r.Description, r.Detail, r.Amount)).Should().Equal(
            ("Salário", "Todo mês · dia 5 · Nubank", 5000m),
            ("Aluguel ou financiamento", "Todo mês · dia 10 · Nubank", 1500m),
            ("Energia", "Todo mês · valor estimado · Nubank", 200m));
        _vm.TotalIncome.Should().Be(5000);
        _vm.TotalOutcome.Should().Be(1700);
        _vm.Balance.Should().Be(3300);
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

    // --- Finish ---

    [Fact]
    public async Task Next_OnSummary_CompletesSetupAndCloses()
    {
        var closed = false;
        var received = false;
        _messenger.Register<SetupCompletedMessage>(this, (_, _) => received = true);
        NewUserSetup? saved = null;
        await _setupService.CompleteAsync(Arg.Do<NewUserSetup>(s => saved = s));
        await _vm.PrepareAsync(isRerun: false);
        _vm.SetClosedCallback(() => { closed = true; return Task.CompletedTask; });
        _vm.Name = "Ana";
        _vm.PhotoPath = "/cache/picked.jpg";
        AddSource("Nubank", PaymentSourceType.BankAccount);
        AddSource("Visa", PaymentSourceType.CreditCard);
        _vm.Salary = 5000;
        _vm.SalaryDay = SetupViewModel.NoDueDay;
        _vm.ExpensesSource = _vm.NewSources.Single(s => s.Name == "Visa");
        SelectExpense("Mercado", 1200, dueDay: 15);
        await GoToSummaryAsync();

        await _vm.NextCommand.ExecuteAsync(null);

        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Ana");
        saved.PhotoPath.Should().Be("/cache/picked.jpg");
        saved.PaymentSources.Select(s => s.Name).Should().Equal("Nubank", "Visa");
        saved.Entries.Select(e => (e.Description, e.Amount, e.EntryType, e.DueDay, e.IsEstimated, e.AllowsPartialPayment, e.CategoryName, e.PaymentSource!.Name))
            .Should().Equal(
                ("Salário", 5000m, FinancialEntryType.Income, (int?)null, false, false, "Salário", "Nubank"),
                ("Mercado", 1200m, FinancialEntryType.Outcome, (int?)null, true, true, "Mercado", "Visa"));
        received.Should().BeTrue();
        closed.Should().BeTrue();
    }

    // --- Skip ---

    [Fact]
    public async Task Skip_FirstRun_SkipsForGoodAndCloses()
    {
        var closed = false;
        await _vm.PrepareAsync(isRerun: false);
        _vm.SetClosedCallback(() => { closed = true; return Task.CompletedTask; });

        await _vm.SkipCommand.ExecuteAsync(null);

        _setupService.Received(1).Skip();
        await _setupService.DidNotReceive().CompleteAsync(Arg.Any<NewUserSetup>());
        closed.Should().BeTrue();
    }

    [Fact]
    public async Task Skip_Rerun_OnlyCloses()
    {
        var closed = false;
        await _vm.PrepareAsync(isRerun: true);
        _vm.SetClosedCallback(() => { closed = true; return Task.CompletedTask; });
        _vm.Salary = 5000;

        await _vm.SkipCommand.ExecuteAsync(null);

        _setupService.DidNotReceive().Skip();
        await _setupService.DidNotReceive().CompleteAsync(Arg.Any<NewUserSetup>());
        closed.Should().BeTrue();
    }

    private void AddSource(string name, PaymentSourceType type)
    {
        _vm.NewSourceName = name;
        _vm.SelectSourceTypeCommand.Execute(type);
        _vm.AddSourceCommand.Execute(null);
    }

    private void SelectExpense(string description, decimal amount, int dueDay = SetupViewModel.NoDueDay)
    {
        var expense = _vm.Expenses.Single(e => e.Description == description);
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
