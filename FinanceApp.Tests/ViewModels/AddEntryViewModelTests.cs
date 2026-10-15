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

    public AddEntryViewModelTests()
    {
        _service = Substitute.For<IFinanceService>();
        _categoryService = Substitute.For<ICategoryService>();
        _categoryService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<FinancialCategory>>([Housing, Education]));
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
        _vm.SelectedRecurrence.Should().Be(RecurrenceType.OneTime);
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

        _vm.PopupTitle.Should().Be("Novo Lançamento");
    }

    [Fact]
    public async Task Prepare_ResetsEditingId()
    {
        var entry = MakeEntry();
        await _vm.PrepareForEditAsync(entry);

        await _vm.PrepareAsync(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Local));

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

        _vm.PopupTitle.Should().Be("Editar Lançamento");
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

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveEntryAsync(Arg.Is<FinancialEntry>(e => e.EndDate == null));
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

    // --- SaveCommand ---

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

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveEntryAsync(Arg.Is<FinancialEntry>(e => e.CategoryId == Education.Id));
    }

    [Fact]
    public async Task SaveCommand_WithoutCategory_PassesNullCategoryId()
    {
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
