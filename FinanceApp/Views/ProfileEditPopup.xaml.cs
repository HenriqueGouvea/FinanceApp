using CommunityToolkit.Maui.Views;
using FinanceApp.ViewModels;

namespace FinanceApp.Views;

public partial class ProfileEditPopup : Popup
{
    public ProfileEditPopup(ProfileEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await this.CloseAsync();
    }
}
