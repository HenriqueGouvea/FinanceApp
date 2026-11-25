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
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    // The entries open under the report, which may be below the fold: bring them into view when a category is
    // opened, but not when a reload (another month, back from "Início") keeps the same one open.
    private bool _hasScrolledToSelection;
    private int? _scrolledCategoryId;

    private async void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ReportsViewModel.SelectedRow))
            return;

        if (_viewModel.SelectedRow is not { } row)
        {
            _hasScrolledToSelection = false;
            return;
        }

        if (_hasScrolledToSelection && _scrolledCategoryId == row.CategoryId)
            return;

        _hasScrolledToSelection = true;
        _scrolledCategoryId = row.CategoryId;

        await Task.Yield();
        await ReportScroll.ScrollToAsync(SelectedCategoryCard, ScrollToPosition.MakeVisible, animated: true);
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
