using FinanceApp.ViewModels;

namespace FinanceApp.Views;

/// <summary>
/// The open source notices bundled with the app, opened from "Sobre o Mensal".
/// </summary>
public partial class OpenSourceNoticesPage : ContentPage
{
    private readonly AboutViewModel _viewModel;

    public OpenSourceNoticesPage(AboutViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadNoticesAsync();
    }
}
