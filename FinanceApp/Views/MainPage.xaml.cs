using CommunityToolkit.Maui.Extensions;
using FinanceApp.ViewModels;

namespace FinanceApp.Views;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is MainViewModel viewModel)
            await viewModel.InitializeCommand.ExecuteAsync(null);
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        var popup = Handler?.MauiContext?.Services.GetService<AddEntryPopup>()
            ?? throw new InvalidOperationException("AddEntryPopup not found.");

        var vm = (AddEntryViewModel)popup.BindingContext;

        if (BindingContext is not MainViewModel mainVm)
            return;

        vm.Prepare(mainVm.SelectedMonth?.Date ?? DateTime.Today);

        vm.SetSaveCallback(async (entry) =>
        {
            await popup.CloseAsync();
            if (mainVm.RefreshAllCommand.CanExecute(null))
                await mainVm.RefreshAllCommand.ExecuteAsync(null);
        });

        await this.ShowPopupAsync(popup);
    }
}
