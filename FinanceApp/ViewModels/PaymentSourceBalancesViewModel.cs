using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FinanceApp.Messages;
using FinanceApp.Models;
using FinanceApp.Services;
using System.Collections.ObjectModel;

namespace FinanceApp.ViewModels;

/// <summary>
/// Current balance of every payment source, shown at the bottom of the hamburger menu.
/// </summary>
public partial class PaymentSourceBalancesViewModel : ObservableObject, IRecipient<PaymentSourcesChangedMessage>
{
    private readonly IPaymentSourceService _paymentSourceService;
    private readonly IMessenger _messenger;

    public ObservableCollection<PaymentSource> Sources { get; } = [];

    public bool HasSources => Sources.Count > 0;

    public PaymentSourceBalancesViewModel(IPaymentSourceService paymentSourceService, IMessenger messenger)
    {
        _paymentSourceService = paymentSourceService;
        _messenger = messenger;
        Sources.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasSources));
        messenger.Register(this);
    }

    public async void Receive(PaymentSourcesChangedMessage message) => await LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        var sources = await _paymentSourceService.GetAllAsync();

        Sources.Clear();
        foreach (var source in sources)
            Sources.Add(source);
    }

    /// <summary>
    /// Called after a source is edited from the menu, so every screen showing sources refreshes (this one included).
    /// </summary>
    public void OnPaymentSourceSaved() => _messenger.Send(new PaymentSourcesChangedMessage());
}
