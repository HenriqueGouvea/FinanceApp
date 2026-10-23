using FinanceApp.UITests.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace FinanceApp.UITests.Pages;

public class AddEntryPopupObject
{
    private static readonly By Title = AppSession.AutomationId("EntryPopupTitleLabel");
    private static readonly By DescriptionEntry = AppSession.AutomationId("DescriptionEntry");
    private static readonly By EntryTypeToggle = AppSession.AutomationId("EntryTypeToggle");
    private static readonly By SelectIncomeButton = AppSession.AutomationId("SelectIncomeButton");
    private static readonly By SelectOutcomeButton = AppSession.AutomationId("SelectOutcomeButton");
    private static readonly By RecurrencePicker = AppSession.AutomationId("RecurrencePicker");
    private static readonly By InstallmentsEntry = AppSession.AutomationId("InstallmentsEntry");
    private static readonly By AmountEntry = AppSession.AutomationId("AmountEntry");
    private static readonly By InstallmentsTotal = AppSession.AutomationId("InstallmentsTotalLabel");
    private static readonly By CategoryPicker = AppSession.AutomationId("CategoryPicker");
    private static readonly By PickerDialogOption = MobileBy.Id("android:id/text1");
    private const string SaveButtonId = "SaveEntryButton";
    private const string CancelButtonId = "CancelEntryButton";

    private readonly AppSession _session;

    public AddEntryPopupObject(AppSession session)
    {
        _session = session;
    }

    public AddEntryPopupObject WaitUntilOpen()
    {
        _session.WaitForVisible(DescriptionEntry);
        return this;
    }

    public bool IsOpen => _session.IsVisible(DescriptionEntry);

    public string TitleText => _session.WaitForVisible(Title).Text;

    public string DescriptionText => _session.WaitForVisible(DescriptionEntry).Text;

    public string AmountText => _session.WaitForVisible(AmountEntry).Text;

    public string RecurrenceText => _session.WaitForVisible(RecurrencePicker).Text;

    public bool IsAmountVisible => _session.IsVisible(AmountEntry);

    public bool IsInstallmentsVisible => _session.IsVisible(InstallmentsEntry);

    public bool IsRecurrenceEnabled => _session.WaitForVisible(RecurrencePicker).Enabled;

    public bool IsEntryTypeEnabled => _session.WaitForVisible(EntryTypeToggle).Enabled;

    public string InstallmentsTotalText => _session.WaitForVisible(InstallmentsTotal).Text;

    public AddEntryPopupObject EnterDescription(string description)
    {
        Type(DescriptionEntry, description);
        return this;
    }

    public AddEntryPopupObject SelectIncome()
    {
        _session.HideKeyboardIfShown();
        _session.Tap(SelectIncomeButton);
        _session.WaitForGone(SelectIncomeButton);
        return this;
    }

    public AddEntryPopupObject SelectOutcome()
    {
        _session.HideKeyboardIfShown();
        _session.Tap(SelectOutcomeButton);
        _session.WaitForGone(SelectOutcomeButton);
        return this;
    }

    /// <summary>
    /// The MAUI Picker renders as a native dialog on Android, so the option is located by its label.
    /// </summary>
    public AddEntryPopupObject SelectRecurrence(string recurrenceLabel)
    {
        PickOption(RecurrencePicker, recurrenceLabel);
        return this;
    }

    public AddEntryPopupObject EnterInstallments(string installments)
    {
        Type(InstallmentsEntry, installments);
        return this;
    }

    public AddEntryPopupObject EnterAmount(string amount)
    {
        Type(AmountEntry, amount);
        return this;
    }

    public AddEntryPopupObject SelectCategory(string categoryName)
    {
        PickOption(CategoryPicker, categoryName);
        return this;
    }

    /// <summary>
    /// Opens the category picker's native dialog, reads its options and dismisses it without choosing.
    /// </summary>
    public IReadOnlyList<string> CategoryOptions()
    {
        _session.HideKeyboardIfShown();
        _session.Tap(CategoryPicker);
        _session.WaitForVisible(PickerDialogOption);
        var options = _session.VisibleTexts(PickerDialogOption);
        _session.PressBack();
        _session.WaitForGone(PickerDialogOption);
        return options;
    }

    public string CategoryText => _session.WaitForVisible(CategoryPicker).Text;

    public void Save()
    {
        _session.HideKeyboardIfShown();
        _session.ScrollToAndTap(SaveButtonId);
        _session.WaitForGone(DescriptionEntry);
    }

    public void Cancel()
    {
        _session.HideKeyboardIfShown();
        _session.ScrollToAndTap(CancelButtonId);
        _session.WaitForGone(DescriptionEntry);
    }

    private void Type(By locator, string text)
    {
        var entry = _session.WaitForVisible(locator);
        entry.Clear();
        entry.SendKeys(text);
        _session.HideKeyboardIfShown();
    }

    private void PickOption(By picker, string optionLabel)
    {
        _session.HideKeyboardIfShown();
        _session.Tap(picker);
        _session.Tap(AppSession.Text(optionLabel));
        _session.WaitForText(picker, optionLabel);
    }
}
