using FinanceApp.UITests.Infrastructure;
using OpenQA.Selenium;

namespace FinanceApp.UITests.Pages;

public class AddEntryPopupObject
{
    private static readonly By DescriptionEntry = AppSession.AutomationId("DescriptionEntry");
    private static readonly By RecurrencePicker = AppSession.AutomationId("RecurrencePicker");
    private static readonly By AmountEntry = AppSession.AutomationId("AmountEntry");
    private static readonly By SaveButton = AppSession.AutomationId("SaveEntryButton");

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

    public bool IsAmountVisible => _session.IsVisible(AmountEntry);

    public AddEntryPopupObject EnterDescription(string description)
    {
        var entry = _session.WaitForVisible(DescriptionEntry);
        entry.Clear();
        entry.SendKeys(description);
        return this;
    }

    /// <summary>
    /// The MAUI Picker renders as a native dialog on Android, so the option is located by its label.
    /// </summary>
    public AddEntryPopupObject SelectRecurrence(string recurrenceLabel)
    {
        _session.HideKeyboardIfShown();
        _session.WaitForVisible(RecurrencePicker).Click();
        _session.WaitForVisible(AppSession.Text(recurrenceLabel)).Click();
        return this;
    }

    public AddEntryPopupObject EnterAmount(string amount)
    {
        var entry = _session.WaitForVisible(AmountEntry);
        entry.Clear();
        entry.SendKeys(amount);
        _session.HideKeyboardIfShown();
        return this;
    }

    public void Save()
    {
        _session.WaitForVisible(SaveButton).Click();
    }
}
