using CommunityToolkit.Maui.Views;
using FinanceApp.ViewModels;

namespace FinanceApp.Views;

public partial class AddEntryPopup : Popup
{
    public AddEntryPopup(AddEntryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        Opened += (_, _) => DescriptionEntry.Focus();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await this.CloseAsync();
    }
}
