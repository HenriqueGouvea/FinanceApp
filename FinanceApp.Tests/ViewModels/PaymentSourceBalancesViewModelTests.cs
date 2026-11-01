using CommunityToolkit.Mvvm.Messaging;
using FinanceApp.Messages;
using FinanceApp.Models;
using FinanceApp.Services;
using FinanceApp.ViewModels;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinanceApp.Tests.ViewModels;

public class PaymentSourceBalancesViewModelTests
{
    private readonly IPaymentSourceService _service;
    private readonly IMessenger _messenger = new WeakReferenceMessenger();
    private readonly List<PaymentSourcesChangedMessage> _sentMessages = [];
    private readonly PaymentSourceBalancesViewModel _vm;

    private static readonly PaymentSource Itau = new() { Id = 1, Name = "Itaú", Type = PaymentSourceType.BankAccount, Balance = 2500m };
    private static readonly PaymentSource Nubank = new() { Id = 2, Name = "Nubank", Type = PaymentSourceType.CreditCard, Balance = 5000m };

    public PaymentSourceBalancesViewModelTests()
    {
        _service = Substitute.For<IPaymentSourceService>();
        ReturnSources(Itau, Nubank);
        _messenger.Register<PaymentSourcesChangedMessage>(this, (_, m) => _sentMessages.Add(m));
        _vm = new PaymentSourceBalancesViewModel(_service, _messenger);
    }

    // --- LoadAsync ---

    [Fact]
    public async Task LoadAsync_PopulatesSourcesFromService()
    {
        await _vm.LoadAsync();

        _vm.Sources.Should().Equal(Itau, Nubank);
    }

    [Fact]
    public async Task LoadAsync_CalledTwice_DoesNotDuplicateSources()
    {
        await _vm.LoadAsync();
        await _vm.LoadAsync();

        _vm.Sources.Should().HaveCount(2);
    }

    [Fact]
    public async Task HasSources_ReflectsSourcesCollection()
    {
        _vm.HasSources.Should().BeFalse();

        await _vm.LoadAsync();

        _vm.HasSources.Should().BeTrue();
    }

    [Fact]
    public async Task HasSources_NoSourcesStored_StaysFalse()
    {
        ReturnSources();

        await _vm.LoadAsync();

        _vm.HasSources.Should().BeFalse();
    }

    // --- PaymentSourcesChangedMessage ---

    [Fact]
    public async Task PaymentSourcesChangedMessage_ReloadsBalances()
    {
        await _vm.LoadAsync();
        var updated = new PaymentSource { Id = 1, Name = "Itaú", Type = PaymentSourceType.BankAccount, Balance = 100m };
        ReturnSources(updated, Nubank);

        _messenger.Send(new PaymentSourcesChangedMessage());

        _vm.Sources.Should().Equal(updated, Nubank);
    }

    // --- OnPaymentSourceSaved ---

    [Fact]
    public void OnPaymentSourceSaved_SendsPaymentSourcesChangedMessage()
    {
        _vm.OnPaymentSourceSaved();

        _sentMessages.Should().ContainSingle();
    }

    [Fact]
    public async Task OnPaymentSourceSaved_ReloadsBalances()
    {
        _vm.OnPaymentSourceSaved();

        await _service.Received(1).GetAllAsync();
        _vm.Sources.Should().Equal(Itau, Nubank);
    }

    private void ReturnSources(params PaymentSource[] sources) =>
        _service.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<PaymentSource>>(sources));
}
