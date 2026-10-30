using FinanceApp.ViewModels;

namespace FinanceApp.Views;

public partial class AddEntryPage : ContentPage
{
    public AddEntryPage(AddEntryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        DescriptionEntry.Focus();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
