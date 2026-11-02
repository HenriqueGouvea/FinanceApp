using FinanceApp.Models;
using FinanceApp.ViewModels;

namespace FinanceApp.Views;

public partial class SecurityPage : ContentPage
{
    private readonly SecurityViewModel _viewModel;

    public SecurityPage(SecurityViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnPinLockTapped(object? sender, TappedEventArgs e) =>
        await OpenPinPageAsync(_viewModel.IsPinEnabled ? PinPurpose.Disable : PinPurpose.Create);

    private async void OnChangePinTapped(object? sender, TappedEventArgs e) =>
        await OpenPinPageAsync(PinPurpose.Change);

    private async Task OpenPinPageAsync(PinPurpose purpose)
    {
        var page = Handler?.MauiContext?.Services.GetService<PinPage>()
            ?? throw new InvalidOperationException("PinPage not found.");

        page.Prepare(purpose, async () =>
        {
            await Navigation.PopModalAsync();
            await _viewModel.LoadCommand.ExecuteAsync(null);
        });

        await Navigation.PushModalAsync(page);
    }
}
