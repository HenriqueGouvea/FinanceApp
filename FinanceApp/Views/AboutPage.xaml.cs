using FinanceApp.ViewModels;

namespace FinanceApp.Views;

/// <summary>
/// "Sobre o Mensal", pushed from "Configurações".
/// </summary>
public partial class AboutPage : ContentPage
{
    public AboutPage(AboutViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnNoticesTapped(object? sender, TappedEventArgs e) =>
        await Navigation.PushAsync(Handler?.MauiContext?.Services.GetService<OpenSourceNoticesPage>()
            ?? throw new InvalidOperationException($"{nameof(OpenSourceNoticesPage)} not found."));
}
