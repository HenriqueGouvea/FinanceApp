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
    private readonly IFinanceService _financeService = Substitute.For<IFinanceService>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly IMessenger _messenger = new WeakReferenceMessenger();
    private readonly List<PaymentSourcesChangedMessage> _sentMessages = [];
    private readonly PaymentSourcesViewModel _vm;

    private static readonly PaymentSource Alelo = new() { Id = 1, Name = "Alelo", Type = PaymentSourceType.MealVoucher };
    private static readonly PaymentSource Itau = new() { Id = 2, Name = "Itaú", Type = PaymentSourceType.BankAccount };
    private static readonly PaymentSource Nubank = new() { Id = 3, Name = "Nubank", Type = PaymentSourceType.CreditCard };
    private static readonly PaymentSource Inter = new() { Id = 4, Name = "Inter", Type = PaymentSourceType.BankAccount };
    private static readonly DateTime Today = new(2026, 4, 10, 0, 0, 0, DateTimeKind.Local);

    public PaymentSourcesViewModelTests()
    {
        _service = Substitute.For<IPaymentSourceService>();
        _service.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<PaymentSource>>([Itau, Inter]));
        _messenger.Register<PaymentSourcesChangedMessage>(this, (_, m) => _sentMessages.Add(m));
        _financeService.GetUsedCreditLimitsAsync(Arg.Any<int>(), Arg.Any<int>())
            .Returns(Task.FromResult<IReadOnlyDictionary<int, decimal>>(new Dictionary<int, decimal>()));
        _timeProvider.LocalTimeZone.Returns(TimeZoneInfo.Local);
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(Today.ToUniversalTime()));
        _vm = new PaymentSourcesViewModel(_service, _financeService, _timeProvider, _messenger);
    }

    // --- LoadAsync ---

    [Fact]
    public async Task LoadAsync_PopulatesSourcesFromService()
    {
        await _vm.LoadAsync();

        _vm.Groups.SelectMany(g => g).Select(s => s.Source).Should().Equal(Itau, Inter);
    }

    [Fact]
    public async Task LoadAsync_CalledTwice_DoesNotDuplicateSources()
    {
        await _vm.LoadAsync();
        await _vm.LoadAsync();

        _vm.Groups.SelectMany(g => g).Select(s => s.Source).Should().HaveCount(2);
    }

    [Fact]
    public async Task LoadAsync_MixedTypes_GroupsAccountsThenCardsThenVouchers()
    {
        _service.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<PaymentSource>>([Alelo, Itau, Nubank, Inter]));

        await _vm.LoadAsync();

        _vm.Groups.Select(g => g.Type).Should().Equal(
            PaymentSourceType.BankAccount, PaymentSourceType.CreditCard, PaymentSourceType.MealVoucher);
        _vm.Groups.Select(g => g.Title).Should().Equal("Contas bancárias", "Cartões de crédito", "Vales");
        _vm.Groups[0].Select(s => s.Source).Should().Equal(Itau, Inter);
        _vm.Groups[1].Select(s => s.Source).Should().Equal(Nubank);
        _vm.Groups[2].Select(s => s.Source).Should().Equal(Alelo);
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

    [Fact]
    public async Task LoadAsync_CreditCard_ShowsTheLimitLeftThisMonth()
    {
        var card = new PaymentSource { Id = 5, Name = "Visa", Type = PaymentSourceType.CreditCard, Balance = 3000m };
        ReturnSources(card);
        _financeService.GetUsedCreditLimitsAsync(2026, 4)
            .Returns(Task.FromResult<IReadOnlyDictionary<int, decimal>>(new Dictionary<int, decimal> { [card.Id] = 1200m }));

        await _vm.LoadAsync();

        var summary = _vm.Groups.Single().Single();
        summary.UsedLimit.Should().Be(1200m);
        summary.AvailableLimit.Should().Be(1800m);
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
    public async Task DeleteAsync_CallsServiceAndReloadsList()
    {
        await _vm.LoadAsync();
        ReturnSources(Inter);

        await _vm.DeleteAsync(Itau);

        await _service.Received(1).DeleteAsync(Itau.Id);
        _vm.Groups.SelectMany(g => g).Select(s => s.Source).Should().Equal(Inter);
    }

    [Fact]
    public async Task DeleteAsync_LastSourceOfType_RemovesItsGroup()
    {
        ReturnSources(Itau, Nubank);
        await _vm.LoadAsync();
        ReturnSources(Itau);

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

    // --- OnPaymentSourceSaved ---

    [Fact]
    public async Task OnPaymentSourceSaved_ReloadsSourcesOnce()
    {
        _vm.OnPaymentSourceSaved();

        await _service.Received(1).GetAllAsync();
        _vm.Groups.SelectMany(g => g).Select(s => s.Source).Should().HaveCount(2);
    }

    [Fact]
    public void OnPaymentSourceSaved_SendsPaymentSourcesChangedMessage()
    {
        _vm.OnPaymentSourceSaved();

        _sentMessages.Should().ContainSingle();
    }

    // --- PaymentSourcesChangedMessage ---

    [Fact]
    public async Task PaymentSourcesChangedMessage_FromElsewhere_ReloadsSources()
    {
        await _vm.LoadAsync();
        ReturnSources(Itau, Inter, Nubank);

        _messenger.Send(new PaymentSourcesChangedMessage());

        _vm.Groups.SelectMany(g => g).Select(s => s.Source).Should().Equal(Itau, Inter, Nubank);
    }

    private void ReturnSources(params PaymentSource[] sources) =>
        _service.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<PaymentSource>>(sources));
}
