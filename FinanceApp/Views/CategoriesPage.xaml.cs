using CommunityToolkit.Maui.Extensions;
using FinanceApp.Models;
using FinanceApp.ViewModels;

namespace FinanceApp.Views;

public partial class CategoriesPage : ContentPage
{
    private readonly CategoriesViewModel _viewModel;

    public CategoriesPage(CategoriesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        var popup = ResolvePopup();
        var vm = (CategoryEditViewModel)popup.BindingContext;

        vm.Prepare();
        await ShowEditPopupAsync(popup, vm);
    }

    private async void OnCategoryTapped(object? sender, TappedEventArgs e)
    {
        if (GetCategory(sender) is not { } category) return;

        var popup = ResolvePopup();
        var vm = (CategoryEditViewModel)popup.BindingContext;

        vm.PrepareForEdit(category);
        await ShowEditPopupAsync(popup, vm);
    }

    private async void OnDeleteCategoryClicked(object? sender, TappedEventArgs e)
    {
        if (GetCategory(sender) is not { } category) return;

        var usage = await _viewModel.CountEntriesAsync(category);
        var message = usage == 0
            ? $"Excluir a categoria \"{category.Name}\"?"
            : $"Excluir a categoria \"{category.Name}\"? {usage} lançamento(s) ficarão sem categoria.";

        if (await DisplayAlertAsync("Excluir categoria", message, "Excluir", "Cancelar"))
            await _viewModel.DeleteAsync(category);
    }

    private async Task ShowEditPopupAsync(CategoryEditPopup popup, CategoryEditViewModel vm)
    {
        vm.SetSaveCallback(async () =>
        {
            await popup.CloseAsync();
            await _viewModel.OnCategorySavedAsync();
        });

        await this.ShowPopupAsync(popup);
    }

    private static FinancialCategory? GetCategory(object? sender) =>
        (sender as BindableObject)?.BindingContext as FinancialCategory;

    private CategoryEditPopup ResolvePopup() =>
        Handler?.MauiContext?.Services.GetService<CategoryEditPopup>()
            ?? throw new InvalidOperationException("CategoryEditPopup not found.");
}
