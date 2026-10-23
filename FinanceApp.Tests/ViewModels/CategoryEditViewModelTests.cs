using FinanceApp.Models;
using FinanceApp.Services;
using FinanceApp.ViewModels;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinanceApp.Tests.ViewModels;

public class CategoryEditViewModelTests
{
    private readonly ICategoryService _service;
    private readonly CategoryEditViewModel _vm;

    public CategoryEditViewModelTests()
    {
        _service = Substitute.For<ICategoryService>();
        _vm = new CategoryEditViewModel(_service);
        _vm.Prepare();
    }

    // --- Prepare ---

    [Fact]
    public void Prepare_ResetsFieldsAndSelectsFirstIconAndColor()
    {
        _vm.Name = "Old";
        _vm.Icon = _vm.Icons[3];
        _vm.Color = _vm.Colors[3];

        _vm.Prepare();

        _vm.Name.Should().BeEmpty();
        _vm.Icon.Should().Be(_vm.Icons[0]);
        _vm.Color.Should().Be(_vm.Colors[0]);
        _vm.PopupTitle.Should().Be("Nova Categoria");
    }

    [Fact]
    public void Prepare_WithoutType_DefaultsToOutcome()
    {
        _vm.SelectIncomeCommand.Execute(null);

        _vm.Prepare();

        _vm.EntryType.Should().Be(FinancialEntryType.Outcome);
        _vm.IsIncome.Should().BeFalse();
    }

    [Fact]
    public void Prepare_WithIncomeType_StartsAsIncome()
    {
        _vm.Prepare(FinancialEntryType.Income);

        _vm.EntryType.Should().Be(FinancialEntryType.Income);
        _vm.IsIncome.Should().BeTrue();
    }

    [Fact]
    public async Task Prepare_AfterEdit_SavesAsNewCategory()
    {
        _vm.PrepareForEdit(MakeCategory());

        _vm.Prepare();
        _vm.Name = "Saúde";
        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveAsync(Arg.Is<FinancialCategory>(c => c.Id == 0));
    }

    // --- PrepareForEdit ---

    [Fact]
    public void PrepareForEdit_SetsAllFieldsFromCategory()
    {
        _vm.PrepareForEdit(MakeCategory());

        _vm.Name.Should().Be("Lazer");
        _vm.Icon.Should().Be("🎉");
        _vm.Color.Should().Be("#43A047");
        _vm.PopupTitle.Should().Be("Editar Categoria");
    }

    [Fact]
    public void PrepareForEdit_IncomeCategory_SetsIncomeType()
    {
        var category = MakeCategory();
        category.EntryType = FinancialEntryType.Income;

        _vm.PrepareForEdit(category);

        _vm.EntryType.Should().Be(FinancialEntryType.Income);
    }

    [Fact]
    public async Task PrepareForEdit_SaveKeepsCategoryId()
    {
        _vm.PrepareForEdit(MakeCategory());

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveAsync(Arg.Is<FinancialCategory>(c => c.Id == 9));
    }

    // --- SelectIcon / SelectColor ---

    [Fact]
    public void SelectIconCommand_SetsIcon()
    {
        _vm.SelectIconCommand.Execute("🚗");

        _vm.Icon.Should().Be("🚗");
    }

    [Fact]
    public void SelectColorCommand_SetsColor()
    {
        _vm.SelectColorCommand.Execute("#E53935");

        _vm.Color.Should().Be("#E53935");
    }

    // --- SelectIncome / SelectOutcome ---

    [Fact]
    public void SelectIncomeCommand_SetsIncomeTypeAndRaisesIsIncome()
    {
        var notified = new List<string>();
        _vm.PropertyChanged += (_, e) => notified.Add(e.PropertyName!);

        _vm.SelectIncomeCommand.Execute(null);

        _vm.EntryType.Should().Be(FinancialEntryType.Income);
        notified.Should().Contain(nameof(CategoryEditViewModel.IsIncome));
    }

    [Fact]
    public void SelectOutcomeCommand_SetsOutcomeType()
    {
        _vm.Prepare(FinancialEntryType.Income);

        _vm.SelectOutcomeCommand.Execute(null);

        _vm.EntryType.Should().Be(FinancialEntryType.Outcome);
    }

    // --- SaveCommand ---

    [Fact]
    public async Task SaveCommand_PassesSelectedType()
    {
        _vm.Name = "Salário";
        _vm.SelectIncomeCommand.Execute(null);

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveAsync(Arg.Is<FinancialCategory>(c => c.EntryType == FinancialEntryType.Income));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SaveCommand_BlankName_CannotExecute(string name)
    {
        _vm.Name = name;

        _vm.SaveCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void SaveCommand_WithName_CanExecute()
    {
        _vm.Name = "Saúde";

        _vm.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void Name_Changed_RaisesSaveCommandCanExecuteChanged()
    {
        var raised = false;
        _vm.SaveCommand.CanExecuteChanged += (_, _) => raised = true;

        _vm.Name = "Saúde";

        raised.Should().BeTrue();
    }

    [Fact]
    public async Task SaveCommand_PassesTrimmedNameIconAndColor()
    {
        _vm.Name = "  Saúde ";
        _vm.Icon = "💊";
        _vm.Color = "#E53935";

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveAsync(Arg.Is<FinancialCategory>(c =>
            c.Name == "Saúde" &&
            c.Icon == "💊" &&
            c.Color == "#E53935"));
    }

    [Fact]
    public async Task SaveCommand_InvokesSaveCallbackAfterSave()
    {
        var invoked = false;
        _vm.SetSaveCallback(() =>
        {
            invoked = true;
            return Task.CompletedTask;
        });
        _vm.Name = "Saúde";

        await _vm.SaveCommand.ExecuteAsync(null);

        invoked.Should().BeTrue();
    }

    private static FinancialCategory MakeCategory() => new()
    {
        Id = 9,
        Name = "Lazer",
        Icon = "🎉",
        Color = "#43A047"
    };
}
