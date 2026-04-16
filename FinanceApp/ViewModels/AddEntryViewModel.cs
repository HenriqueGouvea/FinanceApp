using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Models;
using FinanceApp.Services;

namespace FinanceApp.ViewModels;

public partial class AddEntryViewModel : ObservableObject
{
    private readonly IFinanceService _financeService;
    private Action<FinancialEntry>? _onSaved;
    private DateTime _targetMonth;

    [ObservableProperty] string description = string.Empty;
    [ObservableProperty] decimal amount;
    [ObservableProperty] string selectedCategory = string.Empty;

    public List<string> Categories { get; } = ["Moradia", "Educação", "Contas", "Lazer"];

    public AddEntryViewModel(IFinanceService financeService)
    {
        _financeService = financeService;
    }

    public void Prepare(DateTime targetMonth)
    {
        _targetMonth = new DateTime(targetMonth.Year, targetMonth.Month, 1);
        Description = string.Empty;
        Amount = 0;
        SelectedCategory = string.Empty;
    }

    [RelayCommand]
    private async Task Save()
    {
        var entry = new FinancialEntry
        {
            Description = Description,
            Amount = Amount,
            Category = SelectedCategory,
            Date = new DateTime(_targetMonth.Year, _targetMonth.Month, 1)
        };

        await _financeService.SaveEntryAsync(entry);
        _onSaved?.Invoke(entry);
    }

    public void SetSaveCallback(Action<FinancialEntry> onSaved)
    {
        _onSaved = onSaved;
    }
}
