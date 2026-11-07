using Microsoft.Extensions.Logging.Abstractions;
using FinanceApp.Models;
using FinanceApp.Services;
using FinanceApp.ViewModels;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinanceApp.Tests.ViewModels;

public class PaymentSourceEditViewModelTests
{
    private static readonly PaymentSource Checking = new() { Id = 1, Name = "Itaú", Type = PaymentSourceType.BankAccount };
    private static readonly PaymentSource Savings = new() { Id = 2, Name = "Inter", Type = PaymentSourceType.BankAccount };
    private static readonly PaymentSource Card = new() { Id = 3, Name = "Visa", Type = PaymentSourceType.CreditCard };

    private readonly IPaymentSourceService _service;
    private readonly PaymentSourceEditViewModel _vm;

    public PaymentSourceEditViewModelTests()
    {
        _service = Substitute.For<IPaymentSourceService>();
        _service.GetAllAsync().Returns([Checking, Savings, Card]);
        _vm = new PaymentSourceEditViewModel(_service, new WriteGuard(Substitute.For<IAlertService>(), NullLogger<WriteGuard>.Instance));
    }

    // --- Prepare ---

    [Fact]
    public async Task Prepare_ResetsFieldsToBankAccountWithZeroBalance()
    {
        _vm.Name = "Old";
        _vm.Type = PaymentSourceType.CreditCard;
        _vm.Balance = 300m;

        await _vm.PrepareAsync();

        _vm.Name.Should().BeEmpty();
        _vm.Type.Should().Be(PaymentSourceType.BankAccount);
        _vm.Balance.Should().Be(0m);
        _vm.PopupTitle.Should().Be("Nova Conta ou Cartão de Crédito");
    }

    [Fact]
    public async Task Prepare_AfterEdit_SavesAsNewSource()
    {
        await _vm.PrepareForEditAsync(MakeSource());

        await _vm.PrepareAsync();
        _vm.Name = "Inter";
        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveAsync(Arg.Is<PaymentSource>(s => s.Id == 0));
    }

    // --- PrepareForEdit ---

    [Fact]
    public async Task PrepareForEdit_SetsAllFieldsFromSource()
    {
        await _vm.PrepareForEditAsync(MakeSource());

        _vm.Name.Should().Be("Nubank");
        _vm.Type.Should().Be(PaymentSourceType.CreditCard);
        _vm.Balance.Should().Be(5000m);
        _vm.PopupTitle.Should().Be("Editar Conta ou Cartão de Crédito");
    }

    [Fact]
    public async Task PrepareForEdit_SaveKeepsSourceId()
    {
        await _vm.PrepareForEditAsync(MakeSource());

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveAsync(Arg.Is<PaymentSource>(s => s.Id == 9));
    }

    // --- SelectType ---

    [Theory]
    [InlineData(PaymentSourceType.BankAccount)]
    [InlineData(PaymentSourceType.CreditCard)]
    [InlineData(PaymentSourceType.MealVoucher)]
    public void SelectTypeCommand_SetsType(PaymentSourceType type)
    {
        _vm.SelectTypeCommand.Execute(type);

        _vm.Type.Should().Be(type);
    }

    // --- SaveCommand ---

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
        _vm.Name = "Inter";

        _vm.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void Name_Changed_RaisesSaveCommandCanExecuteChanged()
    {
        var raised = false;
        _vm.SaveCommand.CanExecuteChanged += (_, _) => raised = true;

        _vm.Name = "Inter";

        raised.Should().BeTrue();
    }

    [Fact]
    public async Task SaveCommand_PassesTrimmedNameTypeAndBalance()
    {
        _vm.Name = "  Alelo ";
        _vm.SelectTypeCommand.Execute(PaymentSourceType.MealVoucher);
        _vm.Balance = 750.25m;

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveAsync(Arg.Is<PaymentSource>(s =>
            s.Name == "Alelo" &&
            s.Type == PaymentSourceType.MealVoucher &&
            s.Balance == 750.25m));
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
        _vm.Name = "Inter";

        await _vm.SaveCommand.ExecuteAsync(null);

        invoked.Should().BeTrue();
    }

    // --- Credit card bill ---

    [Fact]
    public async Task PrepareAsync_LoadsOnlyBankAccountsAsBillPaymentAccounts()
    {
        await _vm.PrepareAsync();

        _vm.BillPaymentAccounts.Should().Equal(Checking, Savings);
        _vm.HasBillPaymentAccounts.Should().BeTrue();
        _vm.BillPaymentAccount.Should().BeNull();
        _vm.BillDueDay.Should().Be(AddEntryViewModel.NoDueDay);
    }

    [Fact]
    public async Task PrepareAsync_NoBankAccounts_HasNoBillPaymentAccounts()
    {
        _service.GetAllAsync().Returns([Card]);

        await _vm.PrepareAsync();

        _vm.HasBillPaymentAccounts.Should().BeFalse();
    }

    [Fact]
    public async Task PrepareForEditAsync_CreditCard_SelectsItsBillDueDayAndPaymentAccount()
    {
        await _vm.PrepareForEditAsync(MakeSource(billDueDay: 10, billPaymentSourceId: Savings.Id));

        _vm.BillDueDay.Should().Be(10);
        _vm.BillPaymentAccount.Should().Be(Savings);
        _vm.HasBillPaymentAccount.Should().BeTrue();
    }

    [Fact]
    public async Task PrepareForEditAsync_BankAccount_IsNotOfferedToPayItsOwnBill()
    {
        await _vm.PrepareForEditAsync(Checking);

        _vm.BillPaymentAccounts.Should().Equal(Savings);
    }

    [Theory]
    [InlineData(PaymentSourceType.BankAccount, false)]
    [InlineData(PaymentSourceType.CreditCard, true)]
    [InlineData(PaymentSourceType.MealVoucher, false)]
    public void IsCreditCard_FollowsType(PaymentSourceType type, bool expected)
    {
        _vm.SelectTypeCommand.Execute(type);

        _vm.IsCreditCard.Should().Be(expected);
    }

    [Fact]
    public async Task SaveCommand_CreditCard_PassesBillDueDayAndPaymentAccount()
    {
        await _vm.PrepareAsync();
        _vm.Name = "Visa";
        _vm.SelectTypeCommand.Execute(PaymentSourceType.CreditCard);
        _vm.BillDueDay = 15;
        _vm.BillPaymentAccount = Checking;

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveAsync(Arg.Is<PaymentSource>(s =>
            s.BillDueDay == 15 && s.BillPaymentSourceId == Checking.Id));
    }

    [Fact]
    public async Task SaveCommand_CreditCardWithoutBillDueDay_SavesNullDueDay()
    {
        await _vm.PrepareAsync();
        _vm.Name = "Visa";
        _vm.SelectTypeCommand.Execute(PaymentSourceType.CreditCard);

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveAsync(Arg.Is<PaymentSource>(s => s.BillDueDay == null && s.BillPaymentSourceId == null));
    }

    [Fact]
    public async Task SaveCommand_ChangedFromCreditCardToBankAccount_DropsBillFields()
    {
        await _vm.PrepareForEditAsync(MakeSource(billDueDay: 10, billPaymentSourceId: Checking.Id));
        _vm.SelectTypeCommand.Execute(PaymentSourceType.BankAccount);

        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveAsync(Arg.Is<PaymentSource>(s => s.BillDueDay == null && s.BillPaymentSourceId == null));
    }

    [Fact]
    public async Task ClearBillPaymentAccountCommand_UnselectsAccount()
    {
        await _vm.PrepareForEditAsync(MakeSource(billPaymentSourceId: Checking.Id));

        _vm.ClearBillPaymentAccountCommand.Execute(null);

        _vm.BillPaymentAccount.Should().BeNull();
        _vm.HasBillPaymentAccount.Should().BeFalse();
    }

    private static PaymentSource MakeSource(int? billDueDay = null, int? billPaymentSourceId = null) => new()
    {
        Id = 9,
        Name = "Nubank",
        Type = PaymentSourceType.CreditCard,
        Balance = 5000m,
        BillDueDay = billDueDay,
        BillPaymentSourceId = billPaymentSourceId
    };
}
