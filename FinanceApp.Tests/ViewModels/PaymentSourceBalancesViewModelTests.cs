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
    private readonly IFinanceService _financeService = Substitute.For<IFinanceService>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly IMessenger _messenger = new WeakReferenceMessenger();
    private readonly List<PaymentSourcesChangedMessage> _sentMessages = [];
    private readonly PaymentSourceBalancesViewModel _vm;

    private static readonly PaymentSource Itau = new() { Id = 1, Name = "Itaú", Type = PaymentSourceType.BankAccount, Balance = 2500m };
    private static readonly PaymentSource Nubank = new() { Id = 2, Name = "Nubank", Type = PaymentSourceType.CreditCard, Balance = 5000m };
    private static readonly DateTime Today = new(2026, 4, 10, 0, 0, 0, DateTimeKind.Local);

    public PaymentSourceBalancesViewModelTests()
    {
        _service = Substitute.For<IPaymentSourceService>();
        ReturnSources(Itau, Nubank);
        _messenger.Register<PaymentSourcesChangedMessage>(this, (_, m) => _sentMessages.Add(m));
        _financeService.GetUsedCreditLimitsAsync(Arg.Any<int>(), Arg.Any<int>())
            .Returns(Task.FromResult<IReadOnlyDictionary<int, decimal>>(new Dictionary<int, decimal>()));
        _timeProvider.LocalTimeZone.Returns(TimeZoneInfo.Local);
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(Today.ToUniversalTime()));
        _vm = new PaymentSourceBalancesViewModel(_service, _financeService, _timeProvider, _messenger);
    }

    // --- LoadAsync ---

    [Fact]
    public async Task LoadAsync_PopulatesSourcesFromService()
    {
        await _vm.LoadAsync();

        _vm.Sources.Select(s => s.Source).Should().Equal(Itau, Nubank);
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

    [Fact]
    public async Task LoadAsync_ShowsAccountBalanceAndCardLimitLeftThisMonth()
    {
        _financeService.GetUsedCreditLimitsAsync(2026, 4)
            .Returns(Task.FromResult<IReadOnlyDictionary<int, decimal>>(new Dictionary<int, decimal> { [Nubank.Id] = 1500m }));

        await _vm.LoadAsync();

        _vm.Sources.Select(s => s.CurrentBalance).Should().Equal(2500m, 3500m);
    }

    // --- PaymentSourcesChangedMessage ---

    [Fact]
    public async Task PaymentSourcesChangedMessage_ReloadsBalances()
    {
        await _vm.LoadAsync();
        var updated = new PaymentSource { Id = 1, Name = "Itaú", Type = PaymentSourceType.BankAccount, Balance = 100m };
        ReturnSources(updated, Nubank);

        _messenger.Send(new PaymentSourcesChangedMessage());

        _vm.Sources.Select(s => s.Source).Should().Equal(updated, Nubank);
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
        _vm.Sources.Select(s => s.Source).Should().Equal(Itau, Nubank);
    }

    private void ReturnSources(params PaymentSource[] sources) =>
        _service.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<PaymentSource>>(sources));
}
