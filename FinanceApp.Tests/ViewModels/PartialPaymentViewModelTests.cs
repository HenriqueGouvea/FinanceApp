using FinanceApp.Models;
using FinanceApp.Services;
using FinanceApp.ViewModels;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinanceApp.Tests.ViewModels;

public class PartialPaymentViewModelTests
{
    private readonly IFinanceService _service;
    private readonly PartialPaymentViewModel _vm;

    private static readonly DateTime June = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Local);

    public PartialPaymentViewModelTests()
    {
        _service = Substitute.For<IFinanceService>();
        _vm = new PartialPaymentViewModel(_service);
    }

    // --- Prepare ---

    [Fact]
    public void Prepare_PartiallyPaidEntry_ShowsItsDescriptionAndAmounts()
    {
        _vm.Prepare(MakeProjection(paidAmount: 250m), June);

        _vm.Description.Should().Be("Supermarket");
        _vm.PaidAmount.Should().Be(250m);
        _vm.TotalAmount.Should().Be(800m);
        _vm.Amount.Should().Be(0m);
        _vm.NewPaidAmount.Should().Be(250m);
    }

    [Fact]
    public void Prepare_CalledAgain_ResetsAmount()
    {
        _vm.Prepare(MakeProjection(), June);
        _vm.Amount = 50m;

        _vm.Prepare(MakeProjection(), June);

        _vm.Amount.Should().Be(0m);
    }

    [Theory]
    [InlineData(FinancialEntryType.Outcome, "Pagar parcial", "Já gasto", "Valor gasto agora")]
    [InlineData(FinancialEntryType.Income, "Receber parcial", "Já recebido", "Valor recebido agora")]
    public void Prepare_EntryType_SetsLabels(FinancialEntryType type, string title, string paidLabel, string amountLabel)
    {
        _vm.Prepare(MakeProjection(type: type), June);

        _vm.Title.Should().Be(title);
        _vm.PaidAmountLabel.Should().Be(paidLabel);
        _vm.AmountLabel.Should().Be(amountLabel);
    }

    // --- Amount ---

    [Fact]
    public void Amount_WhenChanged_UpdatesNewPaidAmount()
    {
        _vm.Prepare(MakeProjection(paidAmount: 250m), June);

        _vm.Amount = 100m;

        _vm.NewPaidAmount.Should().Be(350m);
    }

    [Fact]
    public void Amount_WhenChanged_RaisesPropertyChangedForNewPaidAmount()
    {
        _vm.Prepare(MakeProjection(), June);
        var notified = new List<string>();
        _vm.PropertyChanged += (_, e) => notified.Add(e.PropertyName!);

        _vm.Amount = 100m;

        notified.Should().Contain(nameof(PartialPaymentViewModel.NewPaidAmount));
    }

    // --- Save ---

    [Fact]
    public void SaveCommand_WithZeroAmount_CannotExecute()
    {
        _vm.Prepare(MakeProjection(), June);

        _vm.SaveCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void SaveCommand_WithAmount_CanExecute()
    {
        _vm.Prepare(MakeProjection(), June);

        _vm.Amount = 10m;

        _vm.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task SaveCommand_AddsAmountForTheEntryMonthThenInvokesCallback()
    {
        var saved = false;
        _vm.Prepare(MakeProjection(), June);
        _vm.SetSaveCallback(() => saved = true);
        _vm.Amount = 120m;

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).AddPaidAmountAsync(7, 2026, 6, 120m);
        saved.Should().BeTrue();
    }

    private static MonthlyEntryProjection MakeProjection(
        FinancialEntryType type = FinancialEntryType.Outcome,
        decimal paidAmount = 0) =>
        new(
            FinancialEntryId: 7,
            FinancialMonthlyEntryId: null,
            Description: "Supermarket",
            EffectiveAmount: 800m,
            EntryType: type,
            Category: null,
            Status: paidAmount > 0 ? FinancialMonthlyEntryStatus.PartiallyPaid : FinancialMonthlyEntryStatus.Pending,
            IsProjected: paidAmount == 0,
            InstallmentLabel: null,
            InstallmentTotalAmount: null,
            PaidAmount: paidAmount
        );
}
