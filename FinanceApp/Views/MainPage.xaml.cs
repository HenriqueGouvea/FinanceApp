using CommunityToolkit.Maui.Extensions;
using FinanceApp.Models;
using FinanceApp.Services;
using FinanceApp.ViewModels;

namespace FinanceApp.Views;

public partial class MainPage : ContentPage
{
    private readonly IFinanceService _financeService;

    public MainPage(MainViewModel viewModel, IFinanceService financeService)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _financeService = financeService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is MainViewModel viewModel)
            await viewModel.InitializeCommand.ExecuteAsync(null);
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        var popup = ResolvePopup();
        var vm = (AddEntryViewModel)popup.BindingContext;

        if (BindingContext is not MainViewModel mainVm) return;

        vm.Prepare(mainVm.SelectedMonth?.Date ?? DateTime.Today);
        vm.SetSaveCallback(async (_) =>
        {
            await popup.CloseAsync();
            if (mainVm.RefreshAllCommand.CanExecute(null))
                await mainVm.RefreshAllCommand.ExecuteAsync(null);
        });

        await this.ShowPopupAsync(popup);
    }

    private async void OnEntrySelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not MonthlyEntryProjection projection)
            return;

        if (sender is CollectionView cv)
            cv.SelectedItem = null;

        var entry = await _financeService.GetEntryByIdAsync(projection.FinancialEntryId);
        if (entry is null) return;

        var popup = ResolvePopup();
        var vm = (AddEntryViewModel)popup.BindingContext;

        if (BindingContext is not MainViewModel mainVm) return;

        vm.PrepareForEdit(entry);
        vm.SetSaveCallback(async (_) =>
        {
            await popup.CloseAsync();
            if (mainVm.RefreshAllCommand.CanExecute(null))
                await mainVm.RefreshAllCommand.ExecuteAsync(null);
        });

        await this.ShowPopupAsync(popup);
    }

    private AddEntryPopup ResolvePopup() =>
        Handler?.MauiContext?.Services.GetService<AddEntryPopup>()
            ?? throw new InvalidOperationException("AddEntryPopup not found.");
}
