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

    public const string Paid = "Pago";
    public const string Received = "Recebido";
    public const string MarkPaid = "Pagar";
    public const string MarkReceived = "Receber";
    public const string MarkPending = "Pendente";

    public const string PaidAmountOutcome = "Valor já pago neste mês";
    public const string PaidAmountIncome = "Valor já recebido neste mês";

    public const string GoToCurrentMonthBackward = "‹ Mês atual";
    public const string GoToCurrentMonthForward = "Mês atual ›";
    public const string SwipeHint = "Toque no mês para escolher ou deslize para os lados";

    public const string OutcomeCategoriesHeader = "Despesas";
    public const string IncomeCategoriesHeader = "Receitas";

    public static readonly string[] DefaultOutcomeCategories = ["Moradia", "Educação", "Mercado", "Lazer"];
    public static readonly string[] DefaultIncomeCategories = ["Salário", "Investimentos"];
}
