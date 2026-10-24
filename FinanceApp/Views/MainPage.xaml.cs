using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using FinanceApp.Models;
using FinanceApp.Services;
using FinanceApp.ViewModels;

namespace FinanceApp.Views;

public partial class MainPage : ContentPage
{
    private const string CancelOption = "Cancelar";
    private const string DeleteOption = "Excluir";
    private const string ThisMonthOption = "Somente este mês";
    private const string ThisAndFutureOption = "Este e os próximos meses";
    private const string AllMonthsOption = "Todos os meses";

    private readonly IFinanceService _financeService;
    private bool _isLongPressInProgress;

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
        var popup = ResolvePopup<AddEntryPopup>();
        var vm = (AddEntryViewModel)popup.BindingContext;

        if (BindingContext is not MainViewModel mainVm) return;

        mainVm.SelectedMonth?.HideEntryActions();
        await vm.PrepareAsync(mainVm.SelectedMonth?.Date ?? DateTime.Today);
        vm.SetSaveCallback(async (_) =>
        {
            await popup.CloseAsync();
            if (mainVm.RefreshAllCommand.CanExecute(null))
                await mainVm.RefreshAllCommand.ExecuteAsync(null);
        });

        await this.ShowPopupAsync(popup);
    }

    private async void OnEntryTapped(object? sender, TouchGestureCompletedEventArgs e)
    {
        // TouchBehavior also raises a tap when the finger is lifted after a long press.
        if (_isLongPressInProgress)
        {
            _isLongPressInProgress = false;
            return;
        }

        if (GetProjection(sender) is not { } projection)
            return;

        if (BindingContext is not MainViewModel mainVm) return;

        if (mainVm.SelectedMonth is { RevealedEntryId: not null } month)
        {
            month.HideEntryActions();
            return;
        }

        var entry = await _financeService.GetEntryByIdAsync(projection.FinancialEntryId);
        if (entry is null) return;

        var popup = ResolvePopup<AddEntryPopup>();
        var vm = (AddEntryViewModel)popup.BindingContext;

        await vm.PrepareForEditAsync(entry, mainVm.SelectedMonth?.Date, projection.PaidAmount);
        vm.SetSaveCallback(async (_) =>
        {
            await popup.CloseAsync();
            if (mainVm.RefreshAllCommand.CanExecute(null))
                await mainVm.RefreshAllCommand.ExecuteAsync(null);
        });

        await this.ShowPopupAsync(popup);
    }

    private void OnEntryLongPressed(object? sender, LongPressCompletedEventArgs e)
    {
        if (GetProjection(sender) is not { } projection) return;
        if (BindingContext is not MainViewModel { SelectedMonth: { } month }) return;

        _isLongPressInProgress = true;
        month.RevealedEntryId = projection.FinancialEntryId;
    }

    private async void OnDeleteEntryClicked(object? sender, TappedEventArgs e)
    {
        if (GetProjection(sender) is not { } projection) return;
        if (BindingContext is not MainViewModel mainVm) return;

        if (mainVm.SelectedMonth is { } month)
            month.HideEntryActions();

        var entry = await _financeService.GetEntryByIdAsync(projection.FinancialEntryId);
        if (entry is null) return;

        EntryDeleteScope? scope;

        if (entry.Recurrence == RecurrenceType.OneTime)
        {
            var choice = await DisplayActionSheetAsync(projection.Description, CancelOption, DeleteOption);
            scope = choice == DeleteOption ? EntryDeleteScope.All : null;
        }
        else
        {
            var choice = await DisplayActionSheetAsync($"Excluir \"{projection.Description}\"", CancelOption, null,
                ThisMonthOption, ThisAndFutureOption, AllMonthsOption);
            scope = choice switch
            {
                ThisMonthOption => EntryDeleteScope.ThisMonth,
                ThisAndFutureOption => EntryDeleteScope.ThisAndFuture,
                AllMonthsOption => EntryDeleteScope.All,
                _ => null
            };
        }

        if (scope is { } selectedScope)
            await mainVm.DeleteEntryAsync(projection, selectedScope);
    }

    private async void OnToggleStatusClicked(object? sender, TappedEventArgs e)
    {
        if (GetProjection(sender) is not { } projection) return;
        if (BindingContext is not MainViewModel mainVm) return;

        await mainVm.ToggleEntryStatusAsync(projection);
    }

    private async void OnMonthTitleTapped(object? sender, TappedEventArgs e)
    {
        if (BindingContext is not MainViewModel { SelectedMonth: { } month } mainVm) return;

        month.HideEntryActions();

        var popup = ResolvePopup<MonthPickerPopup>();
        var vm = (MonthPickerViewModel)popup.BindingContext;

        vm.Prepare(month.Date, mainVm.CurrentMonthDate, mainVm.FirstMonthDate, mainVm.LastMonthDate);
        vm.SetSelectCallback(async date =>
        {
            await popup.CloseAsync();
            mainVm.GoToMonth(date);
        });

        await this.ShowPopupAsync(popup);
    }

    private static MonthlyEntryProjection? GetProjection(object? sender) =>
        (sender as BindableObject)?.BindingContext as MonthlyEntryProjection;

    private T ResolvePopup<T>() where T : Popup =>
        Handler?.MauiContext?.Services.GetService<T>()
            ?? throw new InvalidOperationException($"{typeof(T).Name} not found.");
}
