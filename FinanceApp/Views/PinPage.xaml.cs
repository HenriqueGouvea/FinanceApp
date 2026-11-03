using FinanceApp.Models;
using FinanceApp.ViewModels;

namespace FinanceApp.Views;

/// <summary>
/// Full-screen PIN keypad, pushed modally both to unlock the app and to create, change or disable the PIN.
/// </summary>
public partial class PinPage : ContentPage
{
    private readonly PinViewModel _viewModel;

    public PinPage(PinViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>
    /// <paramref name="onCompleted"/> runs once the purpose is fulfilled and must close the page.
    /// </summary>
    public void Prepare(PinPurpose purpose, Func<Task> onCompleted)
    {
        _viewModel.Prepare(purpose);
        _viewModel.SetCompletedCallback(onCompleted);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.UnlockWithBiometricsCommand.ExecuteAsync(null);
    }

    // The lock screen can't be dismissed: back leaves the app instead, as the home button would.
    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.CanCancel)
        {
            _ = Navigation.PopModalAsync();
            return true;
        }

#if ANDROID
        Platform.CurrentActivity?.MoveTaskToBack(true);
#endif
        return true;
    }

    private async void OnCancelClicked(object? sender, EventArgs e) => await Navigation.PopModalAsync();

    private async void OnForgotPinClicked(object? sender, EventArgs e) =>
        await DisplayAlertAsync(
            "Esqueci o PIN",
            "Sem uma conta, o PIN não pode ser recuperado. Para voltar a usar o app, apague os dados dele nas " +
            "configurações do Android (Apps > Mensal > Armazenamento > Limpar dados). Todos os lançamentos serão perdidos.",
            "Entendi");
}
