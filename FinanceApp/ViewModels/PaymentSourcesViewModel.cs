using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FinanceApp.Messages;
using FinanceApp.Models;
using FinanceApp.Services;
using System.Collections.ObjectModel;

namespace FinanceApp.ViewModels;

public partial class PaymentSourcesViewModel : ObservableObject, IRecipient<PaymentSourcesChangedMessage>
{
    private readonly IPaymentSourceService _paymentSourceService;
    private readonly IFinanceService _financeService;
    private readonly TimeProvider _timeProvider;
    private readonly IMessenger _messenger;
    private readonly IWriteGuard _writeGuard;
    private IReadOnlyList<PaymentSourceSummary> _sources = [];

    public ObservableCollection<PaymentSourceGroup> Groups { get; } = [];

    public bool IsEmpty => Groups.Count == 0;

    public PaymentSourcesViewModel(
        IPaymentSourceService paymentSourceService,
        IFinanceService financeService,
        TimeProvider timeProvider,
        IMessenger messenger,
        IWriteGuard writeGuard)
    {
        _paymentSourceService = paymentSourceService;
        _financeService = financeService;
        _timeProvider = timeProvider;
        _messenger = messenger;
        _writeGuard = writeGuard;
        Groups.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsEmpty));
        messenger.Register(this);
    }

    // Sources can also be edited from the hamburger menu while this page is open.
    public async void Receive(PaymentSourcesChangedMessage message) => await LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        var sources = await _paymentSourceService.GetAllAsync();
        var today = _timeProvider.GetLocalNow();
        var usedLimits = await _financeService.GetUsedCreditLimitsAsync(today.Year, today.Month);

        _sources = PaymentSourceSummary.Build(sources, usedLimits);
        BuildGroups();
    }

    public Task<int> CountEntriesAsync(PaymentSource source) =>
        _paymentSourceService.CountEntriesAsync(source.Id);

    public async Task DeleteAsync(PaymentSource source)
    {
        if (await _writeGuard.TryAsync(() => _paymentSourceService.DeleteAsync(source.Id)))
            _messenger.Send(new PaymentSourcesChangedMessage());
    }

    /// <summary>
    /// Notifies every screen showing sources, which reloads this list too.
    /// </summary>
    public void OnPaymentSourceSaved() => _messenger.Send(new PaymentSourcesChangedMessage());

    private void BuildGroups()
    {
        Groups.Clear();
        AddGroup(PaymentSourceType.BankAccount, "Contas bancárias");
        AddGroup(PaymentSourceType.CreditCard, "Cartões de crédito");
        AddGroup(PaymentSourceType.MealVoucher, "Vales");
    }

    private void AddGroup(PaymentSourceType type, string title)
    {
        var sources = _sources.Where(s => s.Source.Type == type).ToList();
        if (sources.Count > 0)
            Groups.Add(new PaymentSourceGroup(type, title, sources));
    }
}
