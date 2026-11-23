using CommunityToolkit.Maui.Extensions;
using FinanceApp.ViewModels;

namespace FinanceApp.Views;

public partial class ReportsPage : ContentPage
{
    private readonly ReportsViewModel _viewModel;

    public ReportsPage(ReportsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    // Entries may have changed on "Início" since the last visit.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnMonthTitleTapped(object? sender, TappedEventArgs e)
    {
        var popup = Handler?.MauiContext?.Services.GetService<MonthPickerPopup>()
            ?? throw new InvalidOperationException("MonthPickerPopup not found.");
        var vm = (MonthPickerViewModel)popup.BindingContext;

        vm.Prepare(_viewModel.Month, _viewModel.CurrentMonth, _viewModel.FirstMonth, _viewModel.LastMonth);
        vm.SetSelectCallback(async date =>
        {
            await popup.CloseAsync();
            await _viewModel.GoToMonthAsync(date);
        });

        await this.ShowPopupAsync(popup);
    }
}
