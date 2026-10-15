using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FinanceApp.Messages;
using FinanceApp.Models;
using FinanceApp.Services;
using System.Collections.ObjectModel;

namespace FinanceApp.ViewModels;

public partial class CategoriesViewModel : ObservableObject
{
    private readonly ICategoryService _categoryService;
    private readonly IMessenger _messenger;

    public ObservableCollection<FinancialCategory> Categories { get; } = [];

    public bool IsEmpty => Categories.Count == 0;

    public CategoriesViewModel(ICategoryService categoryService, IMessenger messenger)
    {
        _categoryService = categoryService;
        _messenger = messenger;
        Categories.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsEmpty));
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        var categories = await _categoryService.GetAllAsync();

        Categories.Clear();
        foreach (var category in categories)
            Categories.Add(category);
    }

    public Task<int> CountEntriesAsync(FinancialCategory category) =>
        _categoryService.CountEntriesAsync(category.Id);

    public async Task DeleteAsync(FinancialCategory category)
    {
        await _categoryService.DeleteAsync(category.Id);
        Categories.Remove(category);
        _messenger.Send(new CategoriesChangedMessage());
    }

    public async Task OnCategorySavedAsync()
    {
        await LoadAsync();
        _messenger.Send(new CategoriesChangedMessage());
    }
}
