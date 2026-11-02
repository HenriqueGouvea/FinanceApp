using CommunityToolkit.Maui.Extensions;
using FinanceApp.ViewModels;

namespace FinanceApp.Views;

/// <summary>
/// "Configurações": the user's name and photo, the app lock and the setup wizard, grouped by subject.
/// </summary>
public partial class SettingsPage : ContentPage
{
    private readonly UserProfileViewModel _viewModel;

    public SettingsPage(UserProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Load();
    }

    private async void OnProfileTapped(object? sender, TappedEventArgs e)
    {
        var popup = Resolve<ProfileEditPopup>();
        var vm = (ProfileEditViewModel)popup.BindingContext;

        vm.Prepare();
        vm.SetSaveCallback(async () => await popup.CloseAsync());
        await this.ShowPopupAsync(popup);
    }

    private async void OnSecurityTapped(object? sender, TappedEventArgs e) =>
        await Navigation.PushAsync(Resolve<SecurityPage>());

    private async void OnRedoSetupTapped(object? sender, TappedEventArgs e)
    {
        if (Shell.Current is AppShell shell)
            await shell.ShowSetupAsync(isRerun: true);
    }

    private T Resolve<T>() where T : class =>
        Handler?.MauiContext?.Services.GetService<T>()
            ?? throw new InvalidOperationException($"{typeof(T).Name} not found.");
}
