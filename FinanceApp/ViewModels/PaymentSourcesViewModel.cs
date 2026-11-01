using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FinanceApp.Messages;
using FinanceApp.Models;
using FinanceApp.Services;
using System.Collections.ObjectModel;

namespace FinanceApp.ViewModels;

public partial class PaymentSourcesViewModel : ObservableObject
{
    private readonly IPaymentSourceService _paymentSourceService;
    private readonly IMessenger _messenger;
    private List<PaymentSource> _sources = [];

    public ObservableCollection<PaymentSourceGroup> Groups { get; } = [];

    public bool IsEmpty => Groups.Count == 0;

    public PaymentSourcesViewModel(IPaymentSourceService paymentSourceService, IMessenger messenger)
    {
        _paymentSourceService = paymentSourceService;
        _messenger = messenger;
        Groups.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsEmpty));
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        _sources = [.. await _paymentSourceService.GetAllAsync()];
        BuildGroups();
    }

    public Task<int> CountEntriesAsync(PaymentSource source) =>
        _paymentSourceService.CountEntriesAsync(source.Id);

    public async Task DeleteAsync(PaymentSource source)
    {
        await _paymentSourceService.DeleteAsync(source.Id);
        _sources.Remove(source);
        BuildGroups();
        _messenger.Send(new PaymentSourcesChangedMessage());
    }

    public async Task OnPaymentSourceSavedAsync()
    {
        await LoadAsync();
        _messenger.Send(new PaymentSourcesChangedMessage());
    }

    private void BuildGroups()
    {
        Groups.Clear();
        AddGroup(PaymentSourceType.BankAccount, "Contas bancárias");
        AddGroup(PaymentSourceType.CreditCard, "Cartões de crédito");
        AddGroup(PaymentSourceType.MealVoucher, "Vales");
    }

    private void AddGroup(PaymentSourceType type, string title)
    {
        var sources = _sources.Where(s => s.Type == type).ToList();
        if (sources.Count > 0)
            Groups.Add(new PaymentSourceGroup(type, title, sources));
    }
}
