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
    private readonly AddEntryViewModel _vm;

    public AddEntryViewModelTests()
    {
        _service = Substitute.For<IFinanceService>();
        _vm = new AddEntryViewModel(_service);
        _vm.Prepare(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Local));
    }

    [Fact]
    public void Prepare_ResetsFormFieldsToDefaults()
    {
        _vm.Description = "Old description";
        _vm.Amount = 999m;
        _vm.SelectedCategory = "Lazer";

        _vm.Prepare(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Local));

        _vm.Description.Should().BeEmpty();
        _vm.Amount.Should().Be(0m);
        _vm.SelectedCategory.Should().BeEmpty();
    }

    [Fact]
    public void Prepare_ResetsRecurrenceAndEntryType()
    {
        _vm.EntryType = FinancialEntryType.Income;
        _vm.SelectedRecurrence = RecurrenceType.Recurrent;

        _vm.Prepare(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Local));

        _vm.EntryType.Should().Be(FinancialEntryType.Outcome);
        _vm.SelectedRecurrence.Should().Be(RecurrenceType.OneTime);
    }

    [Fact]
    public void Prepare_SetsTotalInstallmentsToNull()
    {
        _vm.SelectedRecurrence = RecurrenceType.Installments;
        _vm.TotalInstallments = 5;

        _vm.Prepare(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Local));

        _vm.TotalInstallments.Should().BeNull();
    }

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
    public void SelectedRecurrence_ChangedFromInstallmentsToRecurrent_ClearsTotalInstallments()
    {
        _vm.SelectedRecurrence = RecurrenceType.Installments;
        _vm.TotalInstallments = 5;

        _vm.SelectedRecurrence = RecurrenceType.Recurrent;

        _vm.TotalInstallments.Should().BeNull();
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
        _vm.Prepare(new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Local));

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveEntryAsync(Arg.Is<FinancialEntry>(e =>
            e.StartDate.Year == 2026 &&
            e.StartDate.Month == 4 &&
            e.StartDate.Day == 1));
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
}
