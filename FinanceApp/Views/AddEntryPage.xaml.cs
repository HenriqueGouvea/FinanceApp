using FinanceApp.ViewModels;

namespace FinanceApp.Views;

public partial class AddEntryPage : ContentPage
{
    public AddEntryPage(AddEntryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        // On Android a focus requested while the page is still being pushed is lost, so it waits for the navigation to end.
        Dispatcher.Dispatch(() => DescriptionEntry.Focus());
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
