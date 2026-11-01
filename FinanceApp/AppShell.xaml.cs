using CommunityToolkit.Maui.Extensions;
using FinanceApp.Models;
using FinanceApp.ViewModels;
using FinanceApp.Views;

namespace FinanceApp;

public partial class AppShell : Shell
{
    private readonly PaymentSourceBalancesViewModel _balancesViewModel;

    public AppShell(PaymentSourceBalancesViewModel balancesViewModel)
    {
        InitializeComponent();
        _balancesViewModel = balancesViewModel;
        BalancesFooter.BindingContext = balancesViewModel;
        Loaded += async (_, _) => await balancesViewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnSourceTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not PaymentSource source || CurrentPage is not { } page) return;

        var popup = Handler?.MauiContext?.Services.GetService<PaymentSourceEditPopup>()
            ?? throw new InvalidOperationException("PaymentSourceEditPopup not found.");
        var vm = (PaymentSourceEditViewModel)popup.BindingContext;

        vm.PrepareForEdit(source);
        vm.SetSaveCallback(async () =>
        {
            await popup.CloseAsync();
            _balancesViewModel.OnPaymentSourceSaved();
        });

        FlyoutIsPresented = false;
        await page.ShowPopupAsync(popup);
    }

    private async void OnNoSourcesTapped(object? sender, TappedEventArgs e)
    {
        FlyoutIsPresented = false;
        await GoToAsync("//PaymentSourcesPage");
    }
}
