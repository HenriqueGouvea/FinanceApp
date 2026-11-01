using CommunityToolkit.Maui.Extensions;
using FinanceApp.Models;
using FinanceApp.ViewModels;

namespace FinanceApp.Views;

public partial class PaymentSourcesPage : ContentPage
{
    private readonly PaymentSourcesViewModel _viewModel;

    public PaymentSourcesPage(PaymentSourcesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        var popup = ResolvePopup();
        var vm = (PaymentSourceEditViewModel)popup.BindingContext;

        vm.Prepare();
        await ShowEditPopupAsync(popup, vm);
    }

    private async void OnPaymentSourceTapped(object? sender, TappedEventArgs e)
    {
        if (GetPaymentSource(sender) is not { } source) return;

        var popup = ResolvePopup();
        var vm = (PaymentSourceEditViewModel)popup.BindingContext;

        vm.PrepareForEdit(source);
        await ShowEditPopupAsync(popup, vm);
    }

    private async void OnDeletePaymentSourceClicked(object? sender, TappedEventArgs e)
    {
        if (GetPaymentSource(sender) is not { } source) return;

        var usage = await _viewModel.CountEntriesAsync(source);
        var message = usage == 0
            ? $"Excluir \"{source.Name}\"?"
            : $"Excluir \"{source.Name}\"? {usage} lançamento(s) ficarão sem conta ou cartão.";

        if (await DisplayAlertAsync("Excluir conta ou cartão", message, "Excluir", "Cancelar"))
            await _viewModel.DeleteAsync(source);
    }

    private async Task ShowEditPopupAsync(PaymentSourceEditPopup popup, PaymentSourceEditViewModel vm)
    {
        vm.SetSaveCallback(async () =>
        {
            await popup.CloseAsync();
            _viewModel.OnPaymentSourceSaved();
        });

        await this.ShowPopupAsync(popup);
    }

    private static PaymentSource? GetPaymentSource(object? sender) =>
        (sender as BindableObject)?.BindingContext as PaymentSource;

    private PaymentSourceEditPopup ResolvePopup() =>
        Handler?.MauiContext?.Services.GetService<PaymentSourceEditPopup>()
            ?? throw new InvalidOperationException("PaymentSourceEditPopup not found.");
}
