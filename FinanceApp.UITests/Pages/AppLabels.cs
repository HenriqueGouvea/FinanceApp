namespace FinanceApp.UITests.Pages;

/// <summary>
/// User-facing (pt-BR) texts the tests select by, mirroring the app's labels.
/// </summary>
public static class AppLabels
{
    public const string OneTime = "Única";
    public const string Recurrent = "Todo mês (recorrente)";
    public const string Installments = "Parcelada";
    public const string Annual = "Todo ano (anual)";
    public static string AnnualInstallments(int count) => count > 1 ? $"{count} parcelas" : "Cota única";

    public const string NewEntryTitle = "Novo Lançamento";
    public const string EditEntryTitle = "Editar Lançamento";

    public const string Delete = "Excluir";
    public const string Cancel = "Cancelar";
    public const string DeleteThisMonth = "Somente este mês";
    public const string DeleteThisAndFuture = "Este e os próximos meses";
    public const string DeleteAllMonths = "Todos os meses";

    public const string HomeMenuItem = "Início";
    public const string CategoriesMenuItem = "Categorias";
    public const string PaymentSourcesMenuItem = "Contas e cartões";
    public const string SettingsMenuItem = "Configurações";
    public const string IncomesCollapsedHint = "Toque em “Receitas” para mostrar os lançamentos de novo.";
    public const string SettingsUserSection = "Usuário";
    public const string SettingsSecuritySection = "Segurança";
    public const string SettingsGuideSection = "Guia inicial";
    public const string AddNameAndPhoto = "Adicionar nome e foto";

    public const string SetupWelcomeTitle = "Bem-vindo ao Mensal!";
    public const string Tagline = "Todas as contas, um só rumo.";
    public const string SetupSkip = "Pular tudo";
    public const string SetupClose = "Fechar";
    public const string SetupContinue = "Continuar";
    public const string SetupStart = "Começar";
    public static string SetupSummaryTitleFor(string name) => $"Tudo pronto, {name}!";
    public static string MenuGreeting(string name) => $"Olá, {name}!";
    public const string MenuGreetingWithoutName = "Olá!";
    public static string SetupSchedule(params string[] parts) => string.Join(" · ", ["Todo mês", .. parts]);
    public static string SetupInstallmentSchedule(int current, int total, string card) => $"Parcela {current} de {total} · {card}";
    public const string SetupAdvanceDescription = "Adiantamento salarial";
    public static string SetupSkippedSteps(string steps) => $"Etapas puladas: {steps}";
    public const string EstimatedValue = "valor estimado";
    public const string PartialPaymentValue = "pagamento parcial";

    public const string EnterPin = "Digite seu PIN";
    public const string CreatePin = "Crie um PIN de 4 dígitos";
    public const string ConfirmPin = "Confirme o PIN";
    public const string EnterCurrentPin = "Digite o PIN atual";
    public const string EnterNewPin = "Digite o novo PIN";
    public const string WrongPin = "PIN incorreto. Tente novamente.";
    /// <summary>
    /// Followed by the remaining wait, e.g. "30 s", which counts down.
    /// </summary>
    public const string TooManyAttempts = "Muitas tentativas. Tente de novo em";
    public const string PinMismatch = "Os PINs não conferem. Crie o PIN novamente.";
    public const string PinNotRecoverable = "o PIN não pode ser recuperado";
    public const string Understood = "Entendi";

    public const string BankAccountsHeader = "Contas bancárias";
    public const string CreditCardsHeader = "Cartões de crédito";
    public const string MealVouchersHeader = "Vales";
    public const string Balance = "Saldo";
    public const string Limit = "Limite";

    public const string Paid = "Pago";
    public const string Received = "Recebido";
    public const string MarkPaid = "Pagar";
    public const string MarkReceived = "Receber";
    public const string MarkPending = "Pendente";

    public const string PaidAmountOutcome = "Valor já pago neste mês";

    public const string MarkPartialPaid = "Pagar parcial";
    public const string AlreadySpent = "Já gasto";
    public const string PaymentsOutcome = "Pagamentos neste mês";

    public const string EstimatedSymbol = "±";

    public const string NoDueDay = "Sem vencimento";
    public const string DueToday = "Vence hoje";
    public static string DueDay(int day) => $"Dia {day}";

    public const string MarkBillPaid = "Pagar fatura";
    public const string BillPaid = "Paga";
    public static string BillName(string card) => $"💳 Fatura {card}";
    public static string BillAccount(string account) => $"Débito em {account}";
    public static string CreditCardBillDueHintFor(int day) => $"Vence com a fatura do cartão de crédito (dia {day})";

    public const string SwipeHint = "Toque no mês para escolher ou deslize para os lados";

    public const string OutcomeCategoriesHeader = "Despesas";
    public const string IncomeCategoriesHeader = "Receitas";

    public static readonly string[] DefaultOutcomeCategories = ["Moradia", "Educação", "Mercado", "Saúde", "Transporte", "Lazer"];
    public static readonly string[] DefaultIncomeCategories = ["Salário", "Investimentos"];
}
