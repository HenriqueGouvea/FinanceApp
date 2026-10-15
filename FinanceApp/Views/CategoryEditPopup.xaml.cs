using CommunityToolkit.Maui.Views;
using FinanceApp.ViewModels;

namespace FinanceApp.Views;

public partial class CategoryEditPopup : Popup
{
    public CategoryEditPopup(CategoryEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        Opened += (_, _) => NameEntry.Focus();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await this.CloseAsync();
    }
}
