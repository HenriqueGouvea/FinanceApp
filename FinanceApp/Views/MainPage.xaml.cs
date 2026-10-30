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
    private bool _isOpeningEntryPage;

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
        if (BindingContext is not MainViewModel mainVm) return;

        mainVm.SelectedMonth?.HideEntryActions();
        await OpenEntryPageAsync(mainVm, vm => vm.PrepareAsync(mainVm.SelectedMonth?.Date ?? DateTime.Today));
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

        await OpenEntryPageAsync(mainVm, vm => vm.PrepareForEditAsync(entry, mainVm.SelectedMonth?.Date, projection.PaidAmount));
    }

    private async Task OpenEntryPageAsync(MainViewModel mainVm, Func<AddEntryViewModel, Task> prepare)
    {
        // A quick double tap would otherwise push the page twice.
        if (_isOpeningEntryPage) return;
        _isOpeningEntryPage = true;

        try
        {
            var page = Resolve<AddEntryPage>();
            var vm = (AddEntryViewModel)page.BindingContext;

            await prepare(vm);
            vm.SetSaveCallback(async (_) =>
            {
                // Refreshing first means the list is already up to date when the page slides away.
                await mainVm.RefreshAllAsync();
                await Navigation.PopAsync();
            });

            await Navigation.PushAsync(page);
        }
        finally
        {
            _isOpeningEntryPage = false;
        }
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

    private async void OnPartialPaymentClicked(object? sender, TappedEventArgs e)
    {
        if (GetProjection(sender) is not { } projection) return;
        if (BindingContext is not MainViewModel { SelectedMonth: { } month } mainVm) return;

        month.HideEntryActions();

        var popup = Resolve<PartialPaymentPopup>();
        var vm = (PartialPaymentViewModel)popup.BindingContext;

        vm.Prepare(projection, month.Date);
        vm.SetSaveCallback(async () =>
        {
            await popup.CloseAsync();
            await mainVm.RefreshAllAsync();
        });

        await this.ShowPopupAsync(popup);
    }

    private async void OnMonthTitleTapped(object? sender, TappedEventArgs e)
    {
        if (BindingContext is not MainViewModel { SelectedMonth: { } month } mainVm) return;

        month.HideEntryActions();

        var popup = Resolve<MonthPickerPopup>();
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

    private T Resolve<T>() where T : class =>
        Handler?.MauiContext?.Services.GetService<T>()
            ?? throw new InvalidOperationException($"{typeof(T).Name} not found.");
}
