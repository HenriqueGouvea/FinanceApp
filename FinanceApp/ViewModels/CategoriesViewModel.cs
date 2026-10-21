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
    private List<FinancialCategory> _categories = [];

    public ObservableCollection<CategoryGroup> Groups { get; } = [];

    public bool IsEmpty => Groups.Count == 0;

    public CategoriesViewModel(ICategoryService categoryService, IMessenger messenger)
    {
        _categoryService = categoryService;
        _messenger = messenger;
        Groups.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsEmpty));
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        _categories = [.. await _categoryService.GetAllAsync()];
        BuildGroups();
    }

    public Task<int> CountEntriesAsync(FinancialCategory category) =>
        _categoryService.CountEntriesAsync(category.Id);

    public async Task DeleteAsync(FinancialCategory category)
    {
        await _categoryService.DeleteAsync(category.Id);
        _categories.Remove(category);
        BuildGroups();
        _messenger.Send(new CategoriesChangedMessage());
    }

    public async Task OnCategorySavedAsync()
    {
        await LoadAsync();
        _messenger.Send(new CategoriesChangedMessage());
    }

    private void BuildGroups()
    {
        Groups.Clear();
        AddGroup(FinancialEntryType.Outcome, "Despesas");
        AddGroup(FinancialEntryType.Income, "Receitas");
    }

    private void AddGroup(FinancialEntryType entryType, string title)
    {
        var categories = _categories.Where(c => c.EntryType == entryType).ToList();
        if (categories.Count > 0)
            Groups.Add(new CategoryGroup(entryType, title, categories));
    }
}
