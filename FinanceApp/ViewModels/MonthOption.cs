namespace FinanceApp.ViewModels;

public record MonthOption(DateTime Date, string Label, bool IsSelected, bool IsCurrent, bool IsEnabled);
