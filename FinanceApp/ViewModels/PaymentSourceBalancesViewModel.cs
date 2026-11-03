using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FinanceApp.Messages;
using FinanceApp.Models;
using FinanceApp.Services;
using System.Collections.ObjectModel;

namespace FinanceApp.ViewModels;

/// <summary>
/// Current balance of every payment source (the available limit for credit cards), shown at the bottom of the hamburger menu.
/// </summary>
public partial class PaymentSourceBalancesViewModel : ObservableObject,
    IRecipient<PaymentSourcesChangedMessage>,
    IRecipient<SetupCompletedMessage>
{
    private readonly IPaymentSourceService _paymentSourceService;
    private readonly IFinanceService _financeService;
    private readonly TimeProvider _timeProvider;
    private readonly IMessenger _messenger;

    public ObservableCollection<PaymentSourceSummary> Sources { get; } = [];

    public bool HasSources => Sources.Count > 0;

    public PaymentSourceBalancesViewModel(
        IPaymentSourceService paymentSourceService,
        IFinanceService financeService,
        TimeProvider timeProvider,
        IMessenger messenger)
    {
        _paymentSourceService = paymentSourceService;
        _financeService = financeService;
        _timeProvider = timeProvider;
        _messenger = messenger;
        Sources.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasSources));
        messenger.RegisterAll(this);
    }

    public async void Receive(PaymentSourcesChangedMessage message) => await LoadAsync();
    public async void Receive(SetupCompletedMessage message) => await LoadAsync();

    /// <summary>
    /// Also called each time the menu opens, since paying a card's entries frees up its limit.
    /// </summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        var sources = await _paymentSourceService.GetAllAsync();
        var today = _timeProvider.GetLocalNow();
        var usedLimits = await _financeService.GetUsedCreditLimitsAsync(today.Year, today.Month);

        Sources.Clear();
        foreach (var source in PaymentSourceSummary.Build(sources, usedLimits))
            Sources.Add(source);
    }

    /// <summary>
    /// Called after a source is edited from the menu, so every screen showing sources refreshes (this one included).
    /// </summary>
    public void OnPaymentSourceSaved() => _messenger.Send(new PaymentSourcesChangedMessage());
}
