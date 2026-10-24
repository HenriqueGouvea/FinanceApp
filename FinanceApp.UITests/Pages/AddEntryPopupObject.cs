using System.Globalization;
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
    private const string RecurrencePickerId = "RecurrencePicker";
    private static readonly By RecurrencePicker = AppSession.AutomationId(RecurrencePickerId);
    private static readonly By InstallmentsEntry = AppSession.AutomationId("InstallmentsEntry");
    private static readonly By AmountEntry = AppSession.AutomationId("AmountEntry");
    private static readonly By InstallmentsTotal = AppSession.AutomationId("InstallmentsTotalLabel");
    private const string CategoryPickerId = "CategoryPicker";
    private const string EstimatedSwitchId = "EstimatedSwitch";
    private const string PaidAmountEntryId = "PaidAmountEntry";
    private const string PaidAmountLabelId = "PaidAmountLabel";
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
        PickOption(RecurrencePickerId, recurrenceLabel);
        return this;
    }

    public AddEntryPopupObject EnterInstallments(string installments)
    {
        Type(InstallmentsEntry, installments);
        return this;
    }

    /// <summary>
    /// The amount field is masked as currency and reads typed digits as cents, so 1500 is typed as "150000".
    /// </summary>
    public AddEntryPopupObject EnterAmount(decimal amount)
    {
        Type(AmountEntry, decimal.ToInt64(amount * 100).ToString(CultureInfo.InvariantCulture));
        _session.WaitForText(AmountEntry, AppFormat.Money(amount));
        return this;
    }

    public bool IsEstimated => _session.ScrollTo(EstimatedSwitchId).GetAttribute("checked") == "true";

    public AddEntryPopupObject SetEstimated(bool isEstimated)
    {
        _session.HideKeyboardIfShown();
        if (IsEstimated != isEstimated)
            _session.ScrollTo(EstimatedSwitchId).Click();
        _session.WaitFor(() => IsEstimated == isEstimated, $"the estimated switch to be {(isEstimated ? "on" : "off")}");
        return this;
    }

    /// <summary>
    /// The paid amount field only exists when editing and may sit below the fold, so the popup is scrolled to look for it.
    /// </summary>
    public bool IsPaidAmountVisible
    {
        get
        {
            _session.HideKeyboardIfShown();
            return _session.TryScrollTo(PaidAmountEntryId) is not null;
        }
    }

    public string PaidAmountLabelText => _session.ScrollTo(PaidAmountLabelId).Text;

    public string PaidAmountText => _session.ScrollTo(PaidAmountEntryId).Text;

    /// <summary>
    /// Masked like the amount field: typed digits are read as cents.
    /// </summary>
    public AddEntryPopupObject EnterPaidAmount(decimal amount)
    {
        _session.HideKeyboardIfShown();
        var entry = _session.ScrollTo(PaidAmountEntryId);
        entry.Clear();
        entry.SendKeys(decimal.ToInt64(amount * 100).ToString(CultureInfo.InvariantCulture));
        _session.WaitForText(AppSession.AutomationId(PaidAmountEntryId), AppFormat.Money(amount));
        _session.HideKeyboardIfShown();
        return this;
    }

    public AddEntryPopupObject ClearPaidAmount()
    {
        _session.HideKeyboardIfShown();
        _session.ScrollTo(PaidAmountEntryId).Clear();
        _session.HideKeyboardIfShown();
        return this;
    }

    public AddEntryPopupObject SelectCategory(string categoryName)
    {
        PickOption(CategoryPickerId, categoryName);
        return this;
    }

    /// <summary>
    /// Opens the category picker's native dialog, reads its options and dismisses it without choosing.
    /// </summary>
    public IReadOnlyList<string> CategoryOptions() => PickerOptions(CategoryPickerId);

    public IReadOnlyList<string> RecurrenceOptions() => PickerOptions(RecurrencePickerId);

    public string CategoryText => _session.ScrollTo(CategoryPickerId).Text;

    public void Save()
    {
        _session.HideKeyboardIfShown();
        _session.ScrollToAndTap(SaveButtonId);
        _session.WaitForGone(AppSession.AutomationId(SaveButtonId));
    }

    public void Cancel()
    {
        _session.HideKeyboardIfShown();
        _session.ScrollToAndTap(CancelButtonId);
        _session.WaitForGone(AppSession.AutomationId(CancelButtonId));
    }

    private void Type(By locator, string text)
    {
        var entry = _session.WaitForVisible(locator);
        entry.Clear();
        entry.SendKeys(text);
        _session.HideKeyboardIfShown();
    }

    private IReadOnlyList<string> PickerOptions(string pickerId)
    {
        _session.HideKeyboardIfShown();
        _session.ScrollTo(pickerId).Click();
        _session.WaitForVisible(PickerDialogOption);
        var options = _session.VisibleTexts(PickerDialogOption);
        _session.PressBack();
        _session.WaitForGone(PickerDialogOption);
        return options;
    }

    private void PickOption(string pickerId, string optionLabel)
    {
        _session.HideKeyboardIfShown();
        _session.ScrollTo(pickerId).Click();
        _session.Tap(AppSession.Text(optionLabel));
        _session.WaitForText(AppSession.AutomationId(pickerId), optionLabel);
    }
}
