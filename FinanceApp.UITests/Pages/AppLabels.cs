namespace FinanceApp.UITests.Pages;

/// <summary>
/// User-facing (pt-BR) texts the tests select by, mirroring the app's labels.
/// </summary>
public static class AppLabels
{
    public const string OneTime = "Única";
    public const string Recurrent = "Todo mês (recorrente)";
    public const string Installments = "Parcelada";

    public const string NewEntryTitle = "Novo Lançamento";
    public const string EditEntryTitle = "Editar Lançamento";

    public const string Delete = "Excluir";
    public const string Cancel = "Cancelar";
    public const string DeleteThisMonth = "Somente este mês";
    public const string DeleteThisAndFuture = "Este e os próximos meses";
    public const string DeleteAllMonths = "Todos os meses";

    public const string EntriesMenuItem = "Lançamentos";
    public const string CategoriesMenuItem = "Categorias";
    public const string PaymentSourcesMenuItem = "Contas e cartões";
    public const string SecurityMenuItem = "Segurança";

    public const string EnterPin = "Digite seu PIN";
    public const string CreatePin = "Crie um PIN de 4 dígitos";
    public const string ConfirmPin = "Confirme o PIN";
    public const string EnterCurrentPin = "Digite o PIN atual";
    public const string EnterNewPin = "Digite o novo PIN";
    public const string WrongPin = "PIN incorreto. Tente novamente.";
    public const string PinMismatch = "Os PINs não conferem. Crie o PIN novamente.";
    public const string PinNotRecoverable = "o PIN não pode ser recuperado";
    public const string Understood = "Entendi";

    public const string BankAccountsHeader = "Contas bancárias";
    public const string CreditCardsHeader = "Cartões de crédito";
    public const string MealVouchersHeader = "Vales";
    public const string Balance = "Saldo";
    public const string Limit = "Limite";
    public const string PaidWith = "Pago com";
    public const string ReceivedIn = "Recebido em";

    public const string Paid = "Pago";
    public const string Received = "Recebido";
    public const string MarkPaid = "Pagar";
    public const string MarkReceived = "Receber";
    public const string MarkPending = "Pendente";

    public const string PaidAmountOutcome = "Valor já pago neste mês";
    public const string PaidAmountIncome = "Valor já recebido neste mês";

    public const string MarkPartialPaid = "Pagar parcial";
    public const string MarkPartialReceived = "Receber parcial";
    public const string PartialPaymentOutcome = "Pagamento parcial";
    public const string PartialPaymentIncome = "Recebimento parcial";
    public const string AlreadySpent = "Já gasto";
    public const string AlreadyReceived = "Já recebido";
    public const string PaymentsOutcome = "Pagamentos neste mês";
    public const string PaymentsIncome = "Recebimentos neste mês";

    public const string EstimatedSymbol = "±";

    public const string NoDueDay = "Sem vencimento";
    public const string DueToday = "Vence hoje";
    public static string DueDay(int day) => $"Dia {day}";

    public const string GoToCurrentMonthBackward = "‹ Mês atual";
    public const string GoToCurrentMonthForward = "Mês atual ›";
    public const string SwipeHint = "Toque no mês para escolher ou deslize para os lados";

    public const string OutcomeCategoriesHeader = "Despesas";
    public const string IncomeCategoriesHeader = "Receitas";

    public static readonly string[] DefaultOutcomeCategories = ["Moradia", "Educação", "Mercado", "Lazer"];
    public static readonly string[] DefaultIncomeCategories = ["Salário", "Investimentos"];
}
