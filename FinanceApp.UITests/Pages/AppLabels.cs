namespace FinanceApp.UITests.Pages;

/// <summary>
/// User-facing (pt-BR) texts the tests select by, mirroring the app's labels.
/// </summary>
public static class AppLabels
{
    public const string OneTime = "Única";
    public const string Recurrent = "Recorrente";
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

    public static readonly string[] DefaultCategories = ["Moradia", "Educação", "Mercado", "Lazer"];
}
