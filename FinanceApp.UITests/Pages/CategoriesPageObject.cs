using FinanceApp.UITests.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace FinanceApp.UITests.Pages;

public class CategoriesPageObject
{
    private static readonly By AddCategoryButton = AppSession.AutomationId("AddCategoryButton");
    private static readonly By CategoryRow = AppSession.AutomationId("CategoryRow");
    private static readonly By CategoryName = AppSession.AutomationId("CategoryNameLabel");
    private static readonly By DeleteCategoryButton = AppSession.AutomationId("DeleteCategoryButton");
    private static readonly By NameEntry = AppSession.AutomationId("CategoryNameEntry");
    private static readonly By IncomeTypeButton = AppSession.AutomationId("CategoryIncomeTypeButton");
    private static readonly By GroupHeader = AppSession.AutomationId("CategoryGroupHeader");
    private const string SaveButtonId = "SaveCategoryButton";
    private static readonly By FlyoutButton =
        MobileBy.AndroidUIAutomator("new UiSelector().className(\"android.widget.ImageButton\").instance(0)");

    private readonly AppSession _session;

    public CategoriesPageObject(AppSession session)
    {
        _session = session;
    }

    public CategoriesPageObject WaitUntilLoaded()
    {
        _session.WaitForVisible(AddCategoryButton);
        _session.WaitForVisible(CategoryName);
        return this;
    }

    public IReadOnlyList<string> VisibleCategoryNames => _session.VisibleTexts(CategoryName);

    public IReadOnlyList<string> VisibleGroupHeaders => _session.VisibleTexts(GroupHeader);

    /// <summary>
    /// Header of the group ("Despesas"/"Receitas") the category is listed under, i.e. the closest header above it.
    /// </summary>
    public string GroupOf(string name)
    {
        var categoryY = _session.WaitForVisible(CategoryNameWithText(name)).Location.Y;
        return _session.Driver.FindElements(GroupHeader)
            .Where(h => h.Displayed && h.Location.Y < categoryY)
            .MaxBy(h => h.Location.Y)?.Text
            ?? throw new InvalidOperationException($"No group header above \"{name}\".");
    }

    public CategoriesPageObject AddCategory(string name, bool isIncome = false)
    {
        _session.Tap(AddCategoryButton);
        var entry = _session.WaitForVisible(NameEntry);
        entry.SendKeys(name);
        _session.HideKeyboardIfShown();
        if (isIncome)
            _session.Tap(IncomeTypeButton);
        _session.ScrollToAndTap(SaveButtonId);
        _session.WaitForGone(NameEntry);
        _session.WaitForVisible(CategoryNameWithText(name));
        return this;
    }

    /// <summary>
    /// Taps the row's "Excluir" button and confirms the native alert.
    /// </summary>
    public CategoriesPageObject DeleteCategory(string name)
    {
        var row = _session.Driver.FindElements(CategoryRow)
            .First(r => r.Displayed && r.FindElements(CategoryName).Any(n => n.Text == name));
        row.FindElement(DeleteCategoryButton).Click();

        _session.Tap(AppSession.DialogOption(AppLabels.Delete));
        _session.WaitForGone(CategoryNameWithText(name));
        return this;
    }

    public MainPageObject GoToEntries()
    {
        _session.Tap(FlyoutButton);
        _session.Tap(AppSession.Text(AppLabels.EntriesMenuItem));
        return new MainPageObject(_session).WaitUntilLoaded();
    }

    private static By CategoryNameWithText(string name) =>
        AppSession.AutomationIdWithText("CategoryNameLabel", name);
}
