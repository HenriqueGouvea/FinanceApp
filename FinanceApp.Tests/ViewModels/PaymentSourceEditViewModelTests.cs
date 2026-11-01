using FinanceApp.Models;
using FinanceApp.Services;
using FinanceApp.ViewModels;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinanceApp.Tests.ViewModels;

public class PaymentSourceEditViewModelTests
{
    private readonly IPaymentSourceService _service;
    private readonly PaymentSourceEditViewModel _vm;

    public PaymentSourceEditViewModelTests()
    {
        _service = Substitute.For<IPaymentSourceService>();
        _vm = new PaymentSourceEditViewModel(_service);
        _vm.Prepare();
    }

    // --- Prepare ---

    [Fact]
    public void Prepare_ResetsFieldsToBankAccountWithZeroBalance()
    {
        _vm.Name = "Old";
        _vm.Type = PaymentSourceType.CreditCard;
        _vm.Balance = 300m;

        _vm.Prepare();

        _vm.Name.Should().BeEmpty();
        _vm.Type.Should().Be(PaymentSourceType.BankAccount);
        _vm.Balance.Should().Be(0m);
        _vm.PopupTitle.Should().Be("Nova Conta ou Cartão");
    }

    [Fact]
    public async Task Prepare_AfterEdit_SavesAsNewSource()
    {
        _vm.PrepareForEdit(MakeSource());

        _vm.Prepare();
        _vm.Name = "Inter";
        await _vm.SaveCommand.ExecuteAsync(null);

        await _service.Received(1).SaveAsync(Arg.Is<PaymentSource>(s => s.Id == 0));
    }

    // --- PrepareForEdit ---

    [Fact]
    public void PrepareForEdit_SetsAllFieldsFromSource()
    {
        _vm.PrepareForEdit(MakeSource());

        _vm.Name.Should().Be("Nubank");
        _vm.Type.Should().Be(PaymentSourceType.CreditCard);
        _vm.Balance.Should().Be(5000m);
        _vm.PopupTitle.Should().Be("Editar Conta ou Cartão");
    }

    [Fact]
    public async Task PrepareForEdit_SaveKeepsSourceId()
    {
        _vm.PrepareForEdit(MakeSource());

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

    private static PaymentSource MakeSource() => new()
    {
        Id = 9,
        Name = "Nubank",
        Type = PaymentSourceType.CreditCard,
        Balance = 5000m
    };
}
