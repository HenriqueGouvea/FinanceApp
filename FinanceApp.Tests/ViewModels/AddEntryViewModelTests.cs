using FinanceApp.Models;
using FinanceApp.Services;
using FinanceApp.ViewModels;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinanceApp.Tests.ViewModels;

public class AddEntryViewModelTests
{
    private readonly IFinanceService _service;
    private readonly ICategoryService _categoryService;
    private readonly AddEntryViewModel _vm;

    private static readonly FinancialCategory Housing = new() { Id = 1, Name = "Moradia", Icon = "🏠", Color = "#1E88E5" };
    private static readonly FinancialCategory Education = new() { Id = 2, Name = "Educação", Icon = "🎓", Color = "#8E24AA" };
    private static readonly FinancialCategory Salary = new() { Id = 3, Name = "Salário", Icon = "💼", Color = "#00897B", EntryType = FinancialEntryType.Income };
    private static readonly DateTime June = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Local);
    private static readonly DateTime JuneTenth = new(2026, 6, 10, 0, 0, 0, DateTimeKind.Local);

    public AddEntryViewModelTests()
    {
        _service = Substitute.For<IFinanceService>();
        _categoryService = Substitute.For<ICategoryService>();
        _categoryService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<FinancialCategory>>([Housing, Education, Salary]));
        _service.GetPaymentsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>()).Returns([]);
        _vm = new AddEntryViewModel(_service, _categoryService);
        _vm.PrepareAsync(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Local)).GetAwaiter().GetResult();
    }

    // --- Prepare ---

    [Fact]
    public async Task Prepare_ResetsFormFieldsToDefaults()
    {
        _vm.Description = "Old description";
        _vm.Amount = 999m;
        _vm.SelectedCategory = Housing;

        await _vm.PrepareAsync(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Local));

        _vm.Description.Should().BeEmpty();
        _vm.Amount.Should().Be(0m);
        _vm.SelectedCategory.Should().BeNull();
    }

    [Fact]
    public async Task Prepare_LoadsCategoriesFromService()
    {
        await _vm.PrepareAsync(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Local));

        _vm.Categories.Should().Equal(Housing, Education);
    }

    [Fact]
    public async Task Prepare_CalledTwice_DoesNotDuplicateCategories()
    {
        await _vm.PrepareAsync(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Local));
        await _vm.PrepareAsync(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Local));

        _vm.Categories.Should().HaveCount(2);
    }

    [Fact]
    public async Task Prepare_ResetsRecurrenceAndEntryType()
    {
        _vm.EntryType = FinancialEntryType.Income;
        _vm.SelectedRecurrence = RecurrenceType.Recurrent;

        await _vm.PrepareAsync(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Local));

        _vm.EntryType.Should().Be(FinancialEntryType.Outcome);
        _vm.SelectedRecurrence.Should().BeNull();
        _vm.HasRecurrence.Should().BeFalse();
    }

    [Fact]
    public async Task Prepare_SetsTotalInstallmentsToNull()
    {
        _vm.SelectedRecurrence = RecurrenceType.Installments;
        _vm.TotalInstallments = 5;

        await _vm.PrepareAsync(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Local));

        _vm.TotalInstallments.Should().BeNull();
    }

    [Fact]
    public async Task Prepare_ResetsTitleToNovo()
    {
        await _vm.PrepareAsync(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Local));

        _vm.PageTitle.Should().Be("Novo Lançamento");
    }

    [Fact]
    public async Task Prepare_ResetsEditingId()
    {
        var entry = MakeEntry();
        await _vm.PrepareForEditAsync(entry);

        await _vm.PrepareAsync(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Local));
        _vm.SelectedRecurrence = RecurrenceType.OneTime;

        await _vm.SaveCommand.ExecuteAsync(null);
        await _service.Received().SaveEntryAsync(Arg.Is<FinancialEntry>(e => e.Id == 0));
    }

    // --- PrepareForEdit ---

    [Fact]
    public async Task PrepareForEdit_SetsAllFieldsFromEntry()
    {
        var entry = new FinancialEntry
        {
            Id = 7,
            Description = "Laptop",
            Amount = 1500m,
            CategoryId = Education.Id,
            StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Local),
            EntryType = FinancialEntryType.Outcome,
            Recurrence = RecurrenceType.Installments,
            TotalInstallments = 10
        };

        await _vm.PrepareForEditAsync(entry);

        _vm.Description.Should().Be("Laptop");
        _vm.Amount.Should().Be(1500m);
        _vm.SelectedCategory.Should().BeSameAs(Education);
        _vm.EntryType.Should().Be(FinancialEntryType.Outcome);
        _vm.SelectedRecurrence.Should().Be(RecurrenceType.Installments);
        _vm.TotalInstallments.Should().Be(10);
    }

    [Fact]
    public async Task PrepareForEdit_SetsTitleToEditar()
    {
        await _vm.PrepareForEditAsync(MakeEntry());

        _vm.PageTitle.Should().Be("Editar Lançamento");
    }

    [Fact]
    public async Task PrepareForEdit_SetsEditingId_SoSaveCallsUpdate()
    {
        var entry = MakeEntry();
        await _vm.PrepareForEditAsync(entry);

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveEntryAsync(Arg.Is<FinancialEntry>(e => e.Id == entry.Id));
    }

    [Fact]
    public async Task Save_EditingEntryWithEndDate_PreservesEndDate()
    {
        var entry = MakeEntry();
        entry.EndDate = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Local);
        await _vm.PrepareForEditAsync(entry);

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveEntryAsync(Arg.Is<FinancialEntry>(e => e.EndDate == entry.EndDate));
    }

    [Fact]
    public async Task Save_AfterPrepareFollowingEdit_HasNoEndDate()
    {
        var entry = MakeEntry();
        entry.EndDate = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Local);
        await _vm.PrepareForEditAsync(entry);
        await _vm.PrepareAsync(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Local));
        _vm.SelectedRecurrence = RecurrenceType.OneTime;

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveEntryAsync(Arg.Is<FinancialEntry>(e => e.EndDate == null));
    }

    // --- Categories by entry type ---

    [Fact]
    public void Categories_NewOutcome_OffersOnlyOutcomeCategories()
    {
        _vm.Categories.Should().Equal(Housing, Education);
    }

    [Fact]
    public void Categories_AfterSelectingIncome_OffersOnlyIncomeCategories()
    {
        _vm.SelectIncomeCommand.Execute(null);

        _vm.Categories.Should().Equal(Salary);
    }

    [Fact]
    public void Categories_SwitchingBackToOutcome_RestoresOutcomeCategories()
    {
        _vm.SelectIncomeCommand.Execute(null);

        _vm.SelectOutcomeCommand.Execute(null);

        _vm.Categories.Should().Equal(Housing, Education);
    }

    [Fact]
    public void SelectIncome_WithOutcomeCategorySelected_ClearsSelection()
    {
        _vm.SelectedCategory = Housing;

        _vm.SelectIncomeCommand.Execute(null);

        _vm.SelectedCategory.Should().BeNull();
    }

    [Fact]
    public async Task Prepare_AfterIncomeWasSelected_OffersOutcomeCategories()
    {
        _vm.SelectIncomeCommand.Execute(null);

        await _vm.PrepareAsync(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Local));

        _vm.Categories.Should().Equal(Housing, Education);
    }

    [Fact]
    public async Task PrepareForEdit_IncomeEntry_OffersIncomeCategoriesAndSelectsItsCategory()
    {
        var entry = MakeEntry();
        entry.EntryType = FinancialEntryType.Income;
        entry.CategoryId = Salary.Id;

        await _vm.PrepareForEditAsync(entry);

        _vm.Categories.Should().Equal(Salary);
        _vm.SelectedCategory.Should().BeSameAs(Salary);
    }

    [Fact]
    public async Task PrepareForEdit_EntryWithCategoryOfOtherType_KeepsThatCategory()
    {
        var entry = MakeEntry();
        entry.EntryType = FinancialEntryType.Income;

        await _vm.PrepareForEditAsync(entry);

        _vm.Categories.Should().Equal(Housing, Salary);
        _vm.SelectedCategory.Should().BeSameAs(Housing);
    }

    [Fact]
    public async Task Prepare_AfterEditingMismatchedEntry_NoLongerOffersThatCategory()
    {
        var entry = MakeEntry();
        entry.EntryType = FinancialEntryType.Income;
        await _vm.PrepareForEditAsync(entry);

        await _vm.PrepareAsync(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Local));
        _vm.SelectIncomeCommand.Execute(null);

        _vm.Categories.Should().Equal(Salary);
    }

    // --- IsIncome / SelectIncome / SelectOutcome ---

    [Fact]
    public void IsInstallments_WhenRecurrenceIsInstallments_ReturnsTrue()
    {
        _vm.SelectedRecurrence = RecurrenceType.Installments;

        _vm.IsInstallments.Should().BeTrue();
    }

    [Fact]
    public void IsInstallments_WhenRecurrenceIsRecurrent_ReturnsFalse()
    {
        _vm.SelectedRecurrence = RecurrenceType.Recurrent;

        _vm.IsInstallments.Should().BeFalse();
    }

    [Fact]
    public void IsIncome_WhenEntryTypeIsIncome_ReturnsTrue()
    {
        _vm.EntryType = FinancialEntryType.Income;

        _vm.IsIncome.Should().BeTrue();
    }

    [Fact]
    public void IsIncome_WhenEntryTypeIsOutcome_ReturnsFalse()
    {
        _vm.EntryType = FinancialEntryType.Outcome;

        _vm.IsIncome.Should().BeFalse();
    }

    [Fact]
    public void SelectIncomeCommand_SetsEntryTypeToIncome()
    {
        _vm.SelectIncomeCommand.Execute(null);

        _vm.EntryType.Should().Be(FinancialEntryType.Income);
        _vm.IsIncome.Should().BeTrue();
    }

    [Fact]
    public void SelectOutcomeCommand_SetsEntryTypeToOutcome()
    {
        _vm.EntryType = FinancialEntryType.Income;

        _vm.SelectOutcomeCommand.Execute(null);

        _vm.EntryType.Should().Be(FinancialEntryType.Outcome);
        _vm.IsIncome.Should().BeFalse();
    }

    [Fact]
    public void OnEntryTypeChanged_RaisesPropertyChangedForIsIncome()
    {
        var notified = new List<string>();
        _vm.PropertyChanged += (_, e) => notified.Add(e.PropertyName!);

        _vm.EntryType = FinancialEntryType.Income;

        notified.Should().Contain(nameof(AddEntryViewModel.IsIncome));
    }

    // --- SelectedRecurrence ---

    [Fact]
    public void SelectedRecurrence_ChangedFromInstallmentsToRecurrent_ClearsTotalInstallments()
    {
        _vm.SelectedRecurrence = RecurrenceType.Installments;
        _vm.TotalInstallments = 5;

        _vm.SelectedRecurrence = RecurrenceType.Recurrent;

        _vm.TotalInstallments.Should().BeNull();
    }

    [Fact]
    public void HasRecurrence_AfterSelectingRecurrence_ReturnsTrue()
    {
        _vm.SelectedRecurrence = RecurrenceType.Recurrent;

        _vm.HasRecurrence.Should().BeTrue();
    }

    [Fact]
    public void OnSelectedRecurrenceChanged_RaisesPropertyChangedForHasRecurrence()
    {
        var notified = new List<string>();
        _vm.PropertyChanged += (_, e) => notified.Add(e.PropertyName!);

        _vm.SelectedRecurrence = RecurrenceType.OneTime;

        notified.Should().Contain(nameof(AddEntryViewModel.HasRecurrence));
    }

    // --- InstallmentsTotal ---

    [Fact]
    public void InstallmentsTotal_InstallmentsWithCount_ReturnsAmountTimesCount()
    {
        _vm.SelectedRecurrence = RecurrenceType.Installments;
        _vm.Amount = 150m;
        _vm.TotalInstallments = 4;

        _vm.InstallmentsTotal.Should().Be(600m);
    }

    [Fact]
    public void InstallmentsTotal_InstallmentsWithoutCount_ReturnsNull()
    {
        _vm.SelectedRecurrence = RecurrenceType.Installments;
        _vm.Amount = 150m;

        _vm.InstallmentsTotal.Should().BeNull();
    }

    [Fact]
    public void InstallmentsTotal_NonInstallments_ReturnsNull()
    {
        _vm.SelectedRecurrence = RecurrenceType.Recurrent;
        _vm.Amount = 150m;

        _vm.InstallmentsTotal.Should().BeNull();
    }

    [Fact]
    public void OnAmountChanged_RaisesPropertyChangedForInstallmentsTotal()
    {
        var notified = new List<string>();
        _vm.PropertyChanged += (_, e) => notified.Add(e.PropertyName!);

        _vm.Amount = 10m;

        notified.Should().Contain(nameof(AddEntryViewModel.InstallmentsTotal));
    }

    // --- SaveCommand ---

    [Fact]
    public void SaveCommand_WithoutRecurrence_CannotExecute()
    {
        _vm.SaveCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void SaveCommand_AfterSelectingRecurrence_CanExecute()
    {
        _vm.SelectedRecurrence = RecurrenceType.OneTime;

        _vm.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task SaveCommand_WithoutRecurrence_DoesNotCallService()
    {
        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.DidNotReceive().SaveEntryAsync(Arg.Any<FinancialEntry>());
    }

    [Fact]
    public async Task SaveCommand_CallsServiceWithCorrectEntry()
    {
        _vm.Description = "Salary";
        _vm.Amount = 3000m;
        _vm.EntryType = FinancialEntryType.Income;
        _vm.SelectedRecurrence = RecurrenceType.Recurrent;

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveEntryAsync(Arg.Is<FinancialEntry>(e =>
            e.Description == "Salary" &&
            e.Amount == 3000m &&
            e.EntryType == FinancialEntryType.Income &&
            e.Recurrence == RecurrenceType.Recurrent));
    }

    [Fact]
    public async Task SaveCommand_SetsStartDateToFirstOfTargetMonth()
    {
        await _vm.PrepareAsync(new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Local));
        _vm.SelectedRecurrence = RecurrenceType.OneTime;

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveEntryAsync(Arg.Is<FinancialEntry>(e =>
            e.StartDate.Year == 2026 &&
            e.StartDate.Month == 4 &&
            e.StartDate.Day == 1));
    }

    [Fact]
    public async Task SaveCommand_WithSelectedCategory_PassesCategoryId()
    {
        _vm.SelectedCategory = Education;
        _vm.SelectedRecurrence = RecurrenceType.OneTime;

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveEntryAsync(Arg.Is<FinancialEntry>(e => e.CategoryId == Education.Id));
    }

    [Fact]
    public async Task SaveCommand_WithoutCategory_PassesNullCategoryId()
    {
        _vm.SelectedRecurrence = RecurrenceType.OneTime;

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveEntryAsync(Arg.Is<FinancialEntry>(e => e.CategoryId == null));
    }

    [Fact]
    public async Task SaveCommand_PassesInstallmentsCount()
    {
        _vm.SelectedRecurrence = RecurrenceType.Installments;
        _vm.TotalInstallments = 6;

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveEntryAsync(Arg.Is<FinancialEntry>(e =>
            e.Recurrence == RecurrenceType.Installments &&
            e.TotalInstallments == 6));
    }

    [Fact]
    public async Task SaveCommand_InvokesOnSavedCallbackAfterSave()
    {
        FinancialEntry? captured = null;
        _vm.SetSaveCallback(e => captured = e);
        _vm.SelectedRecurrence = RecurrenceType.OneTime;

        await _vm.SaveCommand.ExecuteAsync(null);

        captured.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveCommand_WithEditingEntry_PassesNonZeroIdToService()
    {
        var entry = MakeEntry();
        await _vm.PrepareForEditAsync(entry);

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveEntryAsync(Arg.Is<FinancialEntry>(e => e.Id == entry.Id));
    }

    // --- Estimated amount ---

    [Fact]
    public async Task Prepare_ResetsIsEstimated()
    {
        _vm.IsEstimated = true;

        await _vm.PrepareAsync(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Local));

        _vm.IsEstimated.Should().BeFalse();
    }

    [Fact]
    public async Task PrepareForEdit_EstimatedEntry_PrefillsIsEstimated()
    {
        var entry = MakeEntry();
        entry.IsEstimated = true;

        await _vm.PrepareForEditAsync(entry);

        _vm.IsEstimated.Should().BeTrue();
    }

    [Fact]
    public async Task SaveCommand_WithIsEstimated_PassesItToService()
    {
        _vm.SelectedRecurrence = RecurrenceType.Recurrent;
        _vm.IsEstimated = true;

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveEntryAsync(Arg.Is<FinancialEntry>(e => e.IsEstimated));
    }

    // --- Due date ---

    [Fact]
    public void DueDays_OfferNoDueDayThenEveryDayOfTheMonth()
    {
        _vm.DueDays.Should().Equal([AddEntryViewModel.NoDueDay, .. Enumerable.Range(1, 31)]);
    }

    [Fact]
    public async Task Prepare_ResetsDueDay()
    {
        _vm.SelectedDueDay = 10;

        await _vm.PrepareAsync(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Local));

        _vm.SelectedDueDay.Should().Be(AddEntryViewModel.NoDueDay);
    }

    [Fact]
    public async Task PrepareForEdit_EntryWithDueDay_PrefillsDueDay()
    {
        var entry = MakeEntry();
        entry.DueDay = 10;

        await _vm.PrepareForEditAsync(entry);

        _vm.SelectedDueDay.Should().Be(10);
    }

    [Fact]
    public async Task PrepareForEdit_EntryWithoutDueDay_SelectsNoDueDay()
    {
        _vm.SelectedDueDay = 10;

        await _vm.PrepareForEditAsync(MakeEntry());

        _vm.SelectedDueDay.Should().Be(AddEntryViewModel.NoDueDay);
    }

    [Fact]
    public async Task SaveCommand_WithDueDay_PassesItToService()
    {
        _vm.SelectedRecurrence = RecurrenceType.Recurrent;
        _vm.SelectedDueDay = 10;

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveEntryAsync(Arg.Is<FinancialEntry>(e => e.DueDay == 10));
    }

    [Fact]
    public async Task SaveCommand_NoDueDay_SavesNullDueDay()
    {
        _vm.SelectedRecurrence = RecurrenceType.Recurrent;

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveEntryAsync(Arg.Is<FinancialEntry>(e => e.DueDay == null));
    }

    [Fact]
    public async Task SaveCommand_EditClearsDueDay_SavesNullDueDay()
    {
        var entry = MakeEntry();
        entry.DueDay = 10;
        await _vm.PrepareForEditAsync(entry);

        _vm.SelectedDueDay = AddEntryViewModel.NoDueDay;
        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveEntryAsync(Arg.Is<FinancialEntry>(e => e.Id == entry.Id && e.DueDay == null));
    }

    // --- Partial payment flag ---

    [Fact]
    public async Task Prepare_ResetsAllowsPartialPayment()
    {
        _vm.AllowsPartialPayment = true;

        await _vm.PrepareAsync(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Local));

        _vm.AllowsPartialPayment.Should().BeFalse();
    }

    [Fact]
    public async Task PrepareForEdit_PartialPaymentEntry_PrefillsAllowsPartialPayment()
    {
        var entry = MakeEntry();
        entry.AllowsPartialPayment = true;

        await _vm.PrepareForEditAsync(entry);

        _vm.AllowsPartialPayment.Should().BeTrue();
    }

    [Fact]
    public async Task SaveCommand_WithAllowsPartialPayment_PassesItToService()
    {
        _vm.SelectedRecurrence = RecurrenceType.Recurrent;
        _vm.AllowsPartialPayment = true;

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveEntryAsync(Arg.Is<FinancialEntry>(e => e.AllowsPartialPayment));
    }

    [Fact]
    public void PartialPaymentLabel_FollowsEntryType()
    {
        _vm.PartialPaymentLabel.Should().Be("Pagamento parcial");

        _vm.EntryType = FinancialEntryType.Income;

        _vm.PartialPaymentLabel.Should().Be("Recebimento parcial");
    }

    [Fact]
    public void ShowsPaidAmount_NewEntry_IsFalseEvenWhenFlagged()
    {
        _vm.AllowsPartialPayment = true;

        _vm.ShowsPaidAmount.Should().BeFalse();
    }

    [Fact]
    public async Task ShowsPaidAmount_EditingWithoutFlag_IsFalse()
    {
        await _vm.PrepareForEditAsync(MakeEntry(), June, 800m);

        _vm.ShowsPaidAmount.Should().BeFalse();
    }

    [Fact]
    public async Task ShowsPaidAmount_EditingFlaggedEntry_IsTrue()
    {
        var entry = MakeEntry();
        entry.AllowsPartialPayment = true;

        await _vm.PrepareForEditAsync(entry, June);

        _vm.ShowsPaidAmount.Should().BeTrue();
    }

    [Fact]
    public async Task ShowsPaidAmount_TogglingFlagWhileEditing_RaisesPropertyChanged()
    {
        await _vm.PrepareForEditAsync(MakeEntry(), June);
        var raised = new List<string?>();
        _vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        _vm.AllowsPartialPayment = true;

        _vm.ShowsPaidAmount.Should().BeTrue();
        raised.Should().Contain(nameof(AddEntryViewModel.ShowsPaidAmount));
    }

    [Fact]
    public async Task ShowsPaidAmount_FlagOffWithRecordedPayments_IsTrueSoTheyCanBeRemoved()
    {
        _service.GetPaymentsAsync(42, 2026, 6).Returns([
            new FinancialPayment { Id = 1, Amount = 100m, Date = JuneTenth }
        ]);

        await _vm.PrepareForEditAsync(MakeEntry(), June, 100m);

        _vm.ShowsPaidAmount.Should().BeTrue();
    }

    // --- Paid amount ---

    [Fact]
    public async Task PrepareForEdit_WithPaidAmount_PrefillsIt()
    {
        await _vm.PrepareForEditAsync(MakeEntry(), new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Local), 300m);

        _vm.PaidAmount.Should().Be(300m);
    }

    [Fact]
    public async Task Prepare_ResetsPaidAmount()
    {
        await _vm.PrepareForEditAsync(MakeEntry(), new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Local), 300m);

        await _vm.PrepareAsync(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Local));

        _vm.PaidAmount.Should().Be(0m);
    }

    [Theory]
    [InlineData(FinancialEntryType.Outcome, "Valor já pago neste mês")]
    [InlineData(FinancialEntryType.Income, "Valor já recebido neste mês")]
    public void PaidAmountLabel_DependsOnEntryType(FinancialEntryType type, string expected)
    {
        _vm.EntryType = type;

        _vm.PaidAmountLabel.Should().Be(expected);
    }

    [Fact]
    public void EntryType_WhenChanged_RaisesPropertyChangedForPaidAmountLabel()
    {
        var notified = new List<string>();
        _vm.PropertyChanged += (_, e) => notified.Add(e.PropertyName!);

        _vm.EntryType = FinancialEntryType.Income;

        notified.Should().Contain(nameof(AddEntryViewModel.PaidAmountLabel));
    }

    [Fact]
    public async Task SaveCommand_EditingWithChangedPaidAmount_SetsItForTheOpenedMonth()
    {
        var entry = MakeEntry();
        await _vm.PrepareForEditAsync(entry, new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Local), 0m);
        _vm.PaidAmount = 250m;

        await _vm.SaveCommand.ExecuteAsync(null);

        Received.InOrder(() =>
        {
            _service.SaveEntryAsync(Arg.Any<FinancialEntry>());
            _service.SetPaidAmountAsync(entry.Id, 2026, 6, 250m);
        });
    }

    [Fact]
    public async Task SaveCommand_EditingWithClearedPaidAmount_SetsZero()
    {
        var entry = MakeEntry();
        await _vm.PrepareForEditAsync(entry, new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Local), 250m);
        _vm.PaidAmount = 0m;

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SetPaidAmountAsync(entry.Id, 2026, 6, 0m);
    }

    [Fact]
    public async Task SaveCommand_EditingWithUnchangedPaidAmount_DoesNotSetIt()
    {
        await _vm.PrepareForEditAsync(MakeEntry(), new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Local), 250m);
        _vm.Amount = 900m;

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.DidNotReceive().SetPaidAmountAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<decimal>());
    }

    [Fact]
    public async Task SaveCommand_EditingWithoutMonth_SetsPaidAmountForStartMonth()
    {
        var entry = MakeEntry();
        await _vm.PrepareForEditAsync(entry);
        _vm.PaidAmount = 100m;

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SetPaidAmountAsync(entry.Id, 2026, 4, 100m);
    }

    [Fact]
    public async Task SaveCommand_NewEntry_DoesNotSetPaidAmount()
    {
        _vm.SelectedRecurrence = RecurrenceType.OneTime;
        _vm.PaidAmount = 100m;

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.DidNotReceive().SetPaidAmountAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<decimal>());
    }

    // --- Payments ---

    [Fact]
    public async Task PrepareForEdit_RecordedPayments_ListsThoseOfTheOpenedMonth()
    {
        _service.GetPaymentsAsync(42, 2026, 6).Returns([
            new FinancialPayment { Id = 1, Amount = 100m, Date = JuneTenth, Description = "Groceries" },
            new FinancialPayment { Id = 2, Amount = 50m, Date = JuneTenth.AddDays(2) }
        ]);

        await _vm.PrepareForEditAsync(MakeEntry(), June, 150m);

        _vm.HasPayments.Should().BeTrue();
        _vm.Payments.Should().Equal(
            new PaymentItem(1, 100m, JuneTenth, "Groceries"),
            new PaymentItem(2, 50m, JuneTenth.AddDays(2), string.Empty));
        _vm.Payments[0].HasDescription.Should().BeTrue();
        _vm.Payments[1].HasDescription.Should().BeFalse();
    }

    [Fact]
    public async Task PrepareForEdit_NoRecordedPayments_HasNoPayments()
    {
        _service.GetPaymentsAsync(42, 2026, 6).Returns([]);

        await _vm.PrepareForEditAsync(MakeEntry(), June, 0m);

        _vm.HasPayments.Should().BeFalse();
    }

    [Fact]
    public async Task Prepare_AfterEditingWithPayments_ClearsPayments()
    {
        _service.GetPaymentsAsync(42, 2026, 6).Returns([new FinancialPayment { Id = 1, Amount = 100m, Date = JuneTenth }]);
        await _vm.PrepareForEditAsync(MakeEntry(), June, 100m);

        await _vm.PrepareAsync(June);

        _vm.Payments.Should().BeEmpty();
    }

    [Theory]
    [InlineData(FinancialEntryType.Outcome, "Pagamentos neste mês")]
    [InlineData(FinancialEntryType.Income, "Recebimentos neste mês")]
    public void PaymentsHeader_DependsOnEntryType(FinancialEntryType type, string expected)
    {
        _vm.EntryType = type;

        _vm.PaymentsHeader.Should().Be(expected);
    }

    [Fact]
    public async Task DeletePaymentCommand_RemovesItFromListAndPaidAmountWithoutSaving()
    {
        _service.GetPaymentsAsync(42, 2026, 6).Returns([
            new FinancialPayment { Id = 1, Amount = 100m, Date = JuneTenth },
            new FinancialPayment { Id = 2, Amount = 50m, Date = JuneTenth }
        ]);
        await _vm.PrepareForEditAsync(MakeEntry(), June, 150m);

        _vm.DeletePaymentCommand.Execute(_vm.Payments[1]);

        _vm.Payments.Should().ContainSingle().Which.Id.Should().Be(1);
        _vm.PaidAmount.Should().Be(100m);
        await _service.DidNotReceive().DeletePaymentAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task DeletePaymentCommand_MoreThanPaidAmount_ClampsPaidAmountAtZero()
    {
        _service.GetPaymentsAsync(42, 2026, 6).Returns([new FinancialPayment { Id = 1, Amount = 300m, Date = JuneTenth }]);
        await _vm.PrepareForEditAsync(MakeEntry(), June, 250m);

        _vm.DeletePaymentCommand.Execute(_vm.Payments[0]);

        _vm.PaidAmount.Should().Be(0m);
    }

    [Fact]
    public async Task SaveCommand_AfterDeletingPayment_DeletesItWithoutSettingPaidAmount()
    {
        _service.GetPaymentsAsync(42, 2026, 6).Returns([
            new FinancialPayment { Id = 1, Amount = 100m, Date = JuneTenth },
            new FinancialPayment { Id = 2, Amount = 50m, Date = JuneTenth }
        ]);
        await _vm.PrepareForEditAsync(MakeEntry(), June, 150m);
        _vm.DeletePaymentCommand.Execute(_vm.Payments[1]);

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).DeletePaymentAsync(2);
        await _service.DidNotReceive().DeletePaymentAsync(1);
        await _service.DidNotReceive().SetPaidAmountAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<decimal>());
    }

    [Fact]
    public async Task SaveCommand_AfterDeletingPaymentAndChangingPaidAmount_DeletesThenSetsNewAmount()
    {
        _service.GetPaymentsAsync(42, 2026, 6).Returns([new FinancialPayment { Id = 1, Amount = 100m, Date = JuneTenth }]);
        await _vm.PrepareForEditAsync(MakeEntry(), June, 300m);
        _vm.DeletePaymentCommand.Execute(_vm.Payments[0]);
        _vm.PaidAmount = 250m;

        await _vm.SaveCommand.ExecuteAsync(null);

        Received.InOrder(() =>
        {
            _service.DeletePaymentAsync(1);
            _service.SetPaidAmountAsync(42, 2026, 6, 250m);
        });
    }

    [Fact]
    public async Task PrepareForEdit_AfterUnsavedDeletion_ForgetsIt()
    {
        _service.GetPaymentsAsync(42, 2026, 6).Returns([new FinancialPayment { Id = 1, Amount = 100m, Date = JuneTenth }]);
        await _vm.PrepareForEditAsync(MakeEntry(), June, 100m);
        _vm.DeletePaymentCommand.Execute(_vm.Payments[0]);

        await _vm.PrepareForEditAsync(MakeEntry(), June, 100m);
        await _vm.SaveCommand.ExecuteAsync(null);

        _vm.Payments.Should().ContainSingle();
        await _service.DidNotReceive().DeletePaymentAsync(Arg.Any<int>());
    }

    private static FinancialEntry MakeEntry() => new()
    {
        Id = 42,
        Description = "Rent",
        Amount = 800m,
        CategoryId = Housing.Id,
        StartDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Local),
        EntryType = FinancialEntryType.Outcome,
        Recurrence = RecurrenceType.Recurrent
    };
}
