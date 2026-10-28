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
    private static readonly DateTime JuneTenth = new(2026, 6, 10, 0, 0, 0, DateTimeKind.Local);

    public PartialPaymentViewModelTests()
    {
        _service = Substitute.For<IFinanceService>();
        _vm = new PartialPaymentViewModel(_service);
    }

    // --- Prepare ---

    [Fact]
    public void Prepare_PartiallyPaidEntry_ShowsItsDescriptionAndAmounts()
    {
        _vm.Prepare(MakeProjection(paidAmount: 250m), June, JuneTenth);

        _vm.Description.Should().Be("Supermarket");
        _vm.PaidAmount.Should().Be(250m);
        _vm.TotalAmount.Should().Be(800m);
        _vm.Amount.Should().Be(0m);
        _vm.NewPaidAmount.Should().Be(250m);
    }

    [Fact]
    public void Prepare_OverBudgetEntry_ShowsPlannedAmountAsTotal()
    {
        _vm.Prepare(MakeProjection(paidAmount: 900m, effectiveAmount: 900m, plannedAmount: 800m), June, JuneTenth);

        _vm.TotalAmount.Should().Be(800m);
        _vm.NewOverBudgetAmount.Should().Be(100m);
    }

    [Fact]
    public void Prepare_CalledAgain_ResetsAmountAndPaymentDescription()
    {
        _vm.Prepare(MakeProjection(), June, JuneTenth);
        _vm.Amount = 50m;
        _vm.PaymentDescription = "Groceries";

        _vm.Prepare(MakeProjection(), June, JuneTenth);

        _vm.Amount.Should().Be(0m);
        _vm.PaymentDescription.Should().BeEmpty();
    }

    [Theory]
    [InlineData(FinancialEntryType.Outcome, "Pagar parcial", "Já gasto", "Valor gasto agora")]
    [InlineData(FinancialEntryType.Income, "Receber parcial", "Já recebido", "Valor recebido agora")]
    public void Prepare_EntryType_SetsLabels(FinancialEntryType type, string title, string paidLabel, string amountLabel)
    {
        _vm.Prepare(MakeProjection(type: type), June, JuneTenth);

        _vm.Title.Should().Be(title);
        _vm.PaidAmountLabel.Should().Be(paidLabel);
        _vm.AmountLabel.Should().Be(amountLabel);
    }

    [Fact]
    public void Prepare_TodayInMonth_DefaultsDateToTodayWithinMonthBounds()
    {
        _vm.Prepare(MakeProjection(), June, JuneTenth);

        _vm.PaymentDate.Should().Be(JuneTenth);
        _vm.MinimumDate.Should().Be(new DateTime(2026, 6, 1));
        _vm.MaximumDate.Should().Be(new DateTime(2026, 6, 30));
    }

    [Fact]
    public void Prepare_PastMonth_DefaultsDateToItsLastDay()
    {
        _vm.Prepare(MakeProjection(), June, JuneTenth.AddMonths(2));

        _vm.PaymentDate.Should().Be(new DateTime(2026, 6, 30));
    }

    [Fact]
    public void Prepare_FutureMonth_DefaultsDateToItsFirstDay()
    {
        _vm.Prepare(MakeProjection(), June, JuneTenth.AddMonths(-2));

        _vm.PaymentDate.Should().Be(new DateTime(2026, 6, 1));
    }

    // --- Amount ---

    [Fact]
    public void Amount_WhenChanged_UpdatesNewPaidAmount()
    {
        _vm.Prepare(MakeProjection(paidAmount: 250m), June, JuneTenth);

        _vm.Amount = 100m;

        _vm.NewPaidAmount.Should().Be(350m);
    }

    [Fact]
    public void Amount_WhenChanged_RaisesPropertyChangedForNewPaidAmount()
    {
        _vm.Prepare(MakeProjection(), June, JuneTenth);
        var notified = new List<string>();
        _vm.PropertyChanged += (_, e) => notified.Add(e.PropertyName!);

        _vm.Amount = 100m;

        notified.Should().Contain(nameof(PartialPaymentViewModel.NewPaidAmount));
        notified.Should().Contain(nameof(PartialPaymentViewModel.IsNewTotalOverBudget));
    }

    [Theory]
    [InlineData(550, false, 0)]
    [InlineData(600, true, 50)]
    public void Amount_BeyondPlannedAmount_FlagsNewTotalAsOverBudget(decimal amount, bool isOverBudget, decimal overBudgetAmount)
    {
        _vm.Prepare(MakeProjection(paidAmount: 250m), June, JuneTenth);

        _vm.Amount = amount;

        _vm.IsNewTotalOverBudget.Should().Be(isOverBudget);
        _vm.NewOverBudgetAmount.Should().Be(overBudgetAmount);
    }

    // --- Save ---

    [Fact]
    public void SaveCommand_WithZeroAmount_CannotExecute()
    {
        _vm.Prepare(MakeProjection(), June, JuneTenth);

        _vm.SaveCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void SaveCommand_WithAmount_CanExecute()
    {
        _vm.Prepare(MakeProjection(), June, JuneTenth);

        _vm.Amount = 10m;

        _vm.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task SaveCommand_RecordsPaymentForTheEntryMonthThenInvokesCallback()
    {
        var saved = false;
        _vm.Prepare(MakeProjection(), June, JuneTenth);
        _vm.SetSaveCallback(() => saved = true);
        _vm.Amount = 120m;
        _vm.PaymentDate = JuneTenth.AddDays(3);
        _vm.PaymentDescription = "  Weekly groceries ";

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).AddPaymentAsync(7, 2026, 6, 120m, JuneTenth.AddDays(3), "Weekly groceries");
        saved.Should().BeTrue();
    }

    private static MonthlyEntryProjection MakeProjection(
        FinancialEntryType type = FinancialEntryType.Outcome,
        decimal paidAmount = 0,
        decimal effectiveAmount = 800m,
        decimal? plannedAmount = null) =>
        new(
            FinancialEntryId: 7,
            FinancialMonthlyEntryId: null,
            Description: "Supermarket",
            EffectiveAmount: effectiveAmount,
            EntryType: type,
            Category: null,
            Status: paidAmount > 0 ? FinancialMonthlyEntryStatus.PartiallyPaid : FinancialMonthlyEntryStatus.Pending,
            IsProjected: paidAmount == 0,
            InstallmentLabel: null,
            InstallmentTotalAmount: null,
            PaidAmount: paidAmount,
            PlannedAmount: plannedAmount
        );
}
