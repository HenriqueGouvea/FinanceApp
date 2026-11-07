using CommunityToolkit.Mvvm.ComponentModel;
using FinanceApp.Models;

namespace FinanceApp.ViewModels;

/// <summary>
/// A credit card added in the setup wizard, created only when the wizard finishes.
/// </summary>
public partial class SetupCreditCardItem : ObservableObject
{
    public PaymentSource Card { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Detail))]
    PaymentSource? billAccount;

    /// <summary>
    /// "Vence dia 10 · Débito em Nubank": when the bill falls due and which account pays it.
    /// </summary>
    public string Detail
    {
        get
        {
            List<string> parts = [Card.BillDueDay is { } day ? $"Vence dia {day}" : "Sem vencimento"];
            if (BillAccount is { } account)
                parts.Add($"Débito em {account.Name}");
            return string.Join(" · ", parts);
        }
    }

    public SetupCreditCardItem(PaymentSource card, PaymentSource? billAccount)
    {
        Card = card;
        this.billAccount = billAccount;
    }
}
