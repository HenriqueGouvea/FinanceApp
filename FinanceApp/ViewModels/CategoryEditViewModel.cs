using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Models;
using FinanceApp.Services;

namespace FinanceApp.ViewModels;

public partial class CategoryEditViewModel : ObservableObject
{
    private readonly ICategoryService _categoryService;
    private Func<Task>? _onSaved;
    private int _editingCategoryId;

    [ObservableProperty] string popupTitle = "Nova Categoria";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    string name = string.Empty;

    [ObservableProperty] string icon = string.Empty;
    [ObservableProperty] string color = string.Empty;

    public IReadOnlyList<string> Icons { get; } =
    [
        "🏠", "🎓", "🧾", "🎉", "🛒", "🍽️", "🚗", "⛽",
        "💊", "🐾", "👕", "✈️", "📱", "💡", "🎁", "💰",
        "💼", "📈", "🏦", "🎮", "📚", "🏋️", "👶", "🔧"
    ];

    public IReadOnlyList<string> Colors { get; } =
    [
        "#E53935", "#D81B60", "#8E24AA", "#5E35B1",
        "#3949AB", "#1E88E5", "#00ACC1", "#00897B",
        "#43A047", "#7CB342", "#FDD835", "#FB8C00",
        "#F4511E", "#6D4C41", "#757575", "#546E7A"
    ];

    public CategoryEditViewModel(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    public void Prepare()
    {
        _editingCategoryId = 0;
        PopupTitle = "Nova Categoria";
        Name = string.Empty;
        Icon = Icons[0];
        Color = Colors[0];
    }

    public void PrepareForEdit(FinancialCategory category)
    {
        _editingCategoryId = category.Id;
        PopupTitle = "Editar Categoria";
        Name = category.Name;
        Icon = category.Icon;
        Color = category.Color;
    }

    public void SetSaveCallback(Func<Task> onSaved)
    {
        _onSaved = onSaved;
    }

    [RelayCommand]
    void SelectIcon(string value) => Icon = value;

    [RelayCommand]
    void SelectColor(string value) => Color = value;

    private bool CanSave() => !string.IsNullOrWhiteSpace(Name);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        await _categoryService.SaveAsync(new FinancialCategory
        {
            Id = _editingCategoryId,
            Name = Name.Trim(),
            Icon = Icon,
            Color = Color
        });

        if (_onSaved is not null)
            await _onSaved();
    }
}
