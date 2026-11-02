using CommunityToolkit.Maui.Extensions;
using FinanceApp.Models;
using FinanceApp.ViewModels;
using FinanceApp.Views;

namespace FinanceApp;

public partial class AppShell : Shell
{
    private readonly PaymentSourceBalancesViewModel _balancesViewModel;

    public AppShell(PaymentSourceBalancesViewModel balancesViewModel, UserProfileViewModel profileViewModel)
    {
        InitializeComponent();
        _balancesViewModel = balancesViewModel;
        BalancesFooter.BindingContext = balancesViewModel;
        ProfileHeader.BindingContext = profileViewModel;
        profileViewModel.Load();
        Loaded += async (_, _) => await balancesViewModel.LoadCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Opens the setup wizard: on the first run, or again from the menu (<paramref name="isRerun"/>) to add more entries.
    /// </summary>
    public async Task ShowSetupAsync(bool isRerun)
    {
        var page = Handler?.MauiContext?.Services.GetService<SetupPage>()
            ?? throw new InvalidOperationException("SetupPage not found.");

        await page.PrepareAsync(isRerun, async () => await Navigation.PopModalAsync());
        await Navigation.PushModalAsync(page, animated: isRerun);
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

    private async void OnRedoSetupClicked(object? sender, EventArgs e)
    {
        FlyoutIsPresented = false;
        await ShowSetupAsync(isRerun: true);
    }
}
