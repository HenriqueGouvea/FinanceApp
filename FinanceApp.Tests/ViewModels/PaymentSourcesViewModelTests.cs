using CommunityToolkit.Mvvm.Messaging;
using FinanceApp.Messages;
using FinanceApp.Models;
using FinanceApp.Services;
using FinanceApp.ViewModels;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinanceApp.Tests.ViewModels;

public class PaymentSourcesViewModelTests
{
    private readonly IPaymentSourceService _service;
    private readonly IMessenger _messenger = new WeakReferenceMessenger();
    private readonly List<PaymentSourcesChangedMessage> _sentMessages = [];
    private readonly PaymentSourcesViewModel _vm;

    private static readonly PaymentSource Alelo = new() { Id = 1, Name = "Alelo", Type = PaymentSourceType.MealVoucher };
    private static readonly PaymentSource Itau = new() { Id = 2, Name = "Itaú", Type = PaymentSourceType.BankAccount };
    private static readonly PaymentSource Nubank = new() { Id = 3, Name = "Nubank", Type = PaymentSourceType.CreditCard };
    private static readonly PaymentSource Inter = new() { Id = 4, Name = "Inter", Type = PaymentSourceType.BankAccount };

    public PaymentSourcesViewModelTests()
    {
        _service = Substitute.For<IPaymentSourceService>();
        _service.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<PaymentSource>>([Itau, Inter]));
        _messenger.Register<PaymentSourcesChangedMessage>(this, (_, m) => _sentMessages.Add(m));
        _vm = new PaymentSourcesViewModel(_service, _messenger);
    }

    // --- LoadAsync ---

    [Fact]
    public async Task LoadAsync_PopulatesSourcesFromService()
    {
        await _vm.LoadAsync();

        _vm.Groups.SelectMany(g => g).Should().Equal(Itau, Inter);
    }

    [Fact]
    public async Task LoadAsync_CalledTwice_DoesNotDuplicateSources()
    {
        await _vm.LoadAsync();
        await _vm.LoadAsync();

        _vm.Groups.SelectMany(g => g).Should().HaveCount(2);
    }

    [Fact]
    public async Task LoadAsync_MixedTypes_GroupsAccountsThenCardsThenVouchers()
    {
        _service.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<PaymentSource>>([Alelo, Itau, Nubank, Inter]));

        await _vm.LoadAsync();

        _vm.Groups.Select(g => g.Type).Should().Equal(
            PaymentSourceType.BankAccount, PaymentSourceType.CreditCard, PaymentSourceType.MealVoucher);
        _vm.Groups.Select(g => g.Title).Should().Equal("Contas bancárias", "Cartões de crédito", "Vales");
        _vm.Groups[0].Should().Equal(Itau, Inter);
        _vm.Groups[1].Should().Equal(Nubank);
        _vm.Groups[2].Should().Equal(Alelo);
    }

    [Fact]
    public async Task LoadAsync_OnlyBankAccounts_OmitsOtherGroups()
    {
        await _vm.LoadAsync();

        _vm.Groups.Should().ContainSingle().Which.Type.Should().Be(PaymentSourceType.BankAccount);
    }

    [Fact]
    public async Task IsEmpty_ReflectsSourcesCollection()
    {
        _vm.IsEmpty.Should().BeTrue();

        await _vm.LoadAsync();

        _vm.IsEmpty.Should().BeFalse();
    }

    // --- CountEntriesAsync ---

    [Fact]
    public async Task CountEntriesAsync_DelegatesToService()
    {
        _service.CountEntriesAsync(Itau.Id).Returns(3);

        var count = await _vm.CountEntriesAsync(Itau);

        count.Should().Be(3);
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_CallsServiceAndRemovesFromList()
    {
        await _vm.LoadAsync();

        await _vm.DeleteAsync(Itau);

        await _service.Received(1).DeleteAsync(Itau.Id);
        _vm.Groups.SelectMany(g => g).Should().Equal(Inter);
    }

    [Fact]
    public async Task DeleteAsync_LastSourceOfType_RemovesItsGroup()
    {
        _service.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<PaymentSource>>([Itau, Nubank]));
        await _vm.LoadAsync();

        await _vm.DeleteAsync(Nubank);

        _vm.Groups.Should().ContainSingle().Which.Type.Should().Be(PaymentSourceType.BankAccount);
    }

    [Fact]
    public async Task DeleteAsync_SendsPaymentSourcesChangedMessage()
    {
        await _vm.LoadAsync();

        await _vm.DeleteAsync(Itau);

        _sentMessages.Should().ContainSingle();
    }

    // --- OnPaymentSourceSavedAsync ---

    [Fact]
    public async Task OnPaymentSourceSavedAsync_ReloadsSources()
    {
        await _vm.OnPaymentSourceSavedAsync();

        await _service.Received(1).GetAllAsync();
        _vm.Groups.SelectMany(g => g).Should().HaveCount(2);
    }

    [Fact]
    public async Task OnPaymentSourceSavedAsync_SendsPaymentSourcesChangedMessage()
    {
        await _vm.OnPaymentSourceSavedAsync();

        _sentMessages.Should().ContainSingle();
    }
}
