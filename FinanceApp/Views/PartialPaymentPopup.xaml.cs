using CommunityToolkit.Maui.Views;
using FinanceApp.ViewModels;

namespace FinanceApp.Views;

public partial class PartialPaymentPopup : Popup
{
    public PartialPaymentPopup(PartialPaymentViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        Opened += (_, _) => AmountEntry.Focus();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await this.CloseAsync();
    }
}
