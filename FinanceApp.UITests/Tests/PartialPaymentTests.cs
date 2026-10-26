using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class PartialPaymentTests : E2ETestBase
{
    public PartialPaymentTests(AppiumServerFixture server) : base(server)
    {
        MainPage.AddEntry("Mercado E2E", AppLabels.Recurrent, 800m);
    }

    [Fact]
    public void OpenEntry_Outcome_ShowsEmptyPaidAmountField()
    {
        var popup = MainPage.OpenEntry("Mercado E2E");

        popup.PaidAmountLabelText.Should().Be(AppLabels.PaidAmountOutcome);
        popup.PaidAmountText.Should().Be(AppFormat.Money(0));
    }

    [Fact]
    public void OpenAddEntry_NewEntry_HidesPaidAmountField()
    {
        var popup = MainPage.OpenAddEntry().SelectRecurrence(AppLabels.OneTime);

        popup.IsPaidAmountVisible.Should().BeFalse("a partial payment is recorded on an existing entry's month");
    }

    [Fact]
    public async Task SetPaidAmount_LessThanAmount_ShowsPartialPaymentInListAndSummaryOnlyInThatMonth()
    {
        MainPage.SetPaidAmount("Mercado E2E", 250m);

        MainPage.WaitFor(() => MainPage.IsEntryPartiallyPaid("Mercado E2E"));
        MainPage.EntryPartialText("Mercado E2E").Should().Be(AppFormat.PartialPayment(250, AppLabels.PartialPaidOutcome));
        MainPage.EntryAmountText("Mercado E2E").Should().Be(AppFormat.Money(800));
        MainPage.IsEntryPaid("Mercado E2E").Should().BeFalse();
        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(250));
        MainPage.SummaryToPayText.Should().Be(AppFormat.Money(550));
        MainPage.OutcomeTotalText.Should().Be(AppFormat.Money(800));

        MainPage.GoToNextMonth().WaitForEntry("Mercado E2E");
        MainPage.IsEntryPartiallyPaid("Mercado E2E").Should().BeFalse();
        MainPage.EntryPartialText("Mercado E2E").Should().BeNull();
        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(0));

        var entry = (await GetEntryRowsAsync()).Single();
        entry.Amount.Should().Be(800m, "the partial payment belongs to the month, not the template");
        var record = (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Subject;
        record.FinancialEntryId.Should().Be(entry.Id);
        record.Year.Should().Be(AppFormat.CurrentMonth.Year);
        record.Month.Should().Be(AppFormat.CurrentMonth.Month);
        record.Status.Should().Be(AppFormat.PartiallyPaid);
        record.PaidAmount.Should().Be(250m);
    }

    [Fact]
    public void SetPaidAmount_ReopenEntry_PrefillsPaidAmount()
    {
        MainPage.SetPaidAmount("Mercado E2E", 250m);
        MainPage.WaitFor(() => MainPage.IsEntryPartiallyPaid("Mercado E2E"));

        MainPage.OpenEntry("Mercado E2E").PaidAmountText.Should().Be(AppFormat.Money(250));
    }

    [Fact]
    public async Task SetPaidAmount_WholeAmount_MarksEntryAsPaid()
    {
        MainPage.SetPaidAmount("Mercado E2E", 800m);

        MainPage.WaitFor(() => MainPage.IsEntryPaid("Mercado E2E"));
        MainPage.EntryPartialText("Mercado E2E").Should().BeNull();
        MainPage.VisibleEntryStatuses.Should().ContainSingle().Which.Should().Be(AppLabels.Paid);
        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(800));
        MainPage.SummaryToPayText.Should().Be(AppFormat.Money(0));

        var record = (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Subject;
        record.Status.Should().Be(AppFormat.Paid);
        record.PaidAmount.Should().Be(800m);
    }

    [Fact]
    public async Task SetPaidAmount_MoreThanAmount_RaisesThatMonthsAmount()
    {
        MainPage.SetPaidAmount("Mercado E2E", 850m);

        MainPage.WaitFor(() => MainPage.IsEntryPaid("Mercado E2E"));
        MainPage.EntryAmountText("Mercado E2E").Should().Be(AppFormat.Money(850));
        MainPage.OutcomeTotalText.Should().Be(AppFormat.Money(850));
        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(850));

        MainPage.GoToNextMonth().WaitForEntry("Mercado E2E");
        MainPage.EntryAmountText("Mercado E2E").Should().Be(AppFormat.Money(800));

        (await GetEntryRowsAsync()).Single().Amount.Should().Be(800m);
    }

    [Fact]
    public async Task ClearPaidAmount_PartiallyPaidEntry_RevertsToPending()
    {
        MainPage.SetPaidAmount("Mercado E2E", 250m);
        MainPage.WaitFor(() => MainPage.IsEntryPartiallyPaid("Mercado E2E"));

        MainPage.OpenEntry("Mercado E2E").ClearPaidAmount().Save();

        MainPage.WaitFor(() => !MainPage.IsEntryPartiallyPaid("Mercado E2E"));
        MainPage.IsEntryPaid("Mercado E2E").Should().BeFalse();
        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(0));
        MainPage.SummaryToPayText.Should().Be(AppFormat.Money(800));

        var record = (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Subject;
        record.Status.Should().Be(AppFormat.Pending);
        record.PaidAmount.Should().BeNull();
    }

    [Fact]
    public async Task MarkPaid_PartiallyPaidEntry_SettlesWholeAmount()
    {
        MainPage.SetPaidAmount("Mercado E2E", 250m);
        MainPage.WaitFor(() => MainPage.IsEntryPartiallyPaid("Mercado E2E"));
        MainPage.RevealActions("Mercado E2E");
        MainPage.RevealedStatusActionText.Should().Be(AppLabels.MarkPaid);
        MainPage.TapSummaryBar();
        MainPage.WaitForActionsHidden();

        MainPage.MarkPaid("Mercado E2E");

        MainPage.EntryPartialText("Mercado E2E").Should().BeNull();
        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(800));
        MainPage.SummaryToPayText.Should().Be(AppFormat.Money(0));

        var record = (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Subject;
        record.Status.Should().Be(AppFormat.Paid);
        record.PaidAmount.Should().BeNull();
    }

    [Fact]
    public async Task SetPaidAmount_Income_UsesReceivedLabels()
    {
        MainPage.AddEntry("Freela E2E", AppLabels.OneTime, 1000m, isIncome: true);
        var popup = MainPage.OpenEntry("Freela E2E");
        popup.PaidAmountLabelText.Should().Be(AppLabels.PaidAmountIncome);
        popup.Cancel();

        MainPage.SetPaidAmount("Freela E2E", 400m);

        MainPage.WaitFor(() => MainPage.IsEntryPartiallyPaid("Freela E2E"));
        MainPage.EntryPartialText("Freela E2E").Should().Be(AppFormat.PartialPayment(400, AppLabels.PartialPaidIncome));
        MainPage.SummaryReceivedText.Should().Be(AppFormat.Money(400));
        MainPage.SummaryToReceiveText.Should().Be(AppFormat.Money(600));
        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(0), "outcomes are untouched");

        var income = (await GetEntryRowsAsync()).Single(e => e.EntryType == AppFormat.Income);
        var record = (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Subject;
        record.FinancialEntryId.Should().Be(income.Id);
        record.Status.Should().Be(AppFormat.PartiallyPaid);
        record.PaidAmount.Should().Be(400m);
    }

    [Fact]
    public async Task SetPaidAmount_InstallmentInNextMonth_AppliesToThatInstallmentOnly()
    {
        MainPage.AddEntry("Notebook E2E", AppLabels.Installments, 300m, installments: "3");
        var nextMonth = AppFormat.CurrentMonth.AddMonths(1);

        MainPage.GoToNextMonth().SetPaidAmount("Notebook E2E", 100m);

        MainPage.WaitFor(() => MainPage.IsEntryPartiallyPaid("Notebook E2E"));
        MainPage.EntryPartialText("Notebook E2E").Should().Be(AppFormat.PartialPayment(100, AppLabels.PartialPaidOutcome));
        MainPage.GoToPreviousMonth().WaitForEntry("Notebook E2E");
        MainPage.IsEntryPartiallyPaid("Notebook E2E").Should().BeFalse();

        var installment = (await GetEntryRowsAsync()).Single(e => e.Recurrence == AppFormat.Installments);
        var record = (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Subject;
        record.FinancialEntryId.Should().Be(installment.Id);
        record.Year.Should().Be(nextMonth.Year);
        record.Month.Should().Be(nextMonth.Month);
        record.PaidAmount.Should().Be(100m);
    }

    [Fact]
    public async Task PartialPaymentAction_PendingOutcome_AddsPaymentAndShowsAlreadySpent()
    {
        MainPage.RevealActions("Mercado E2E");
        MainPage.RevealedPartialPaymentActionText.Should().Be(AppLabels.MarkPartialPaid);
        MainPage.TapSummaryBar();
        MainPage.WaitForActionsHidden();

        var popup = MainPage.OpenPartialPayment("Mercado E2E");
        popup.TitleText.Should().Be(AppLabels.MarkPartialPaid);
        popup.DescriptionText.Should().Be("Mercado E2E");
        popup.ProgressText.Should().Be(AppFormat.PartialPaymentProgress(AppLabels.AlreadySpent, 0, 800));
        popup.EnterAmount(150m);
        popup.NewTotalText.Should().Be(AppFormat.PartialPaymentNewTotal(150));
        popup.Save();

        MainPage.WaitFor(() => MainPage.EntryPartialText("Mercado E2E") == AppFormat.PartialPayment(150, AppLabels.PartialPaidOutcome));
        MainPage.IsEntryPartiallyPaid("Mercado E2E").Should().BeTrue();
        MainPage.SummaryPaidText.Should().Be(AppFormat.Money(150));
        MainPage.SummaryToPayText.Should().Be(AppFormat.Money(650));

        var record = (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Subject;
        record.Month.Should().Be(AppFormat.CurrentMonth.Month);
        record.Status.Should().Be(AppFormat.PartiallyPaid);
        record.PaidAmount.Should().Be(150m);
    }

    [Fact]
    public async Task PartialPaymentAction_Twice_AddsUpPayments()
    {
        MainPage.AddPartialPayment("Mercado E2E", 150m);
        MainPage.WaitFor(() => MainPage.IsEntryPartiallyPaid("Mercado E2E"));

        var popup = MainPage.OpenPartialPayment("Mercado E2E");
        popup.ProgressText.Should().Be(AppFormat.PartialPaymentProgress(AppLabels.AlreadySpent, 150, 800));
        popup.EnterAmount(100m);
        popup.NewTotalText.Should().Be(AppFormat.PartialPaymentNewTotal(250));
        popup.Save();

        MainPage.WaitFor(() => MainPage.EntryPartialText("Mercado E2E") == AppFormat.PartialPayment(250, AppLabels.PartialPaidOutcome));
        (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Which.PaidAmount.Should().Be(250m);
    }

    [Fact]
    public async Task PartialPaymentAction_ReachingAmount_MarksPaidAndNoLongerOffersPartialPayment()
    {
        MainPage.AddPartialPayment("Mercado E2E", 300m);
        MainPage.WaitFor(() => MainPage.IsEntryPartiallyPaid("Mercado E2E"));

        MainPage.AddPartialPayment("Mercado E2E", 500m);

        MainPage.WaitFor(() => MainPage.IsEntryPaid("Mercado E2E"));
        MainPage.EntryPartialText("Mercado E2E").Should().BeNull();
        MainPage.RevealActions("Mercado E2E");
        MainPage.RevealedPartialPaymentActionText.Should().BeNull("a paid entry has nothing left to pay");
        MainPage.RevealedStatusActionText.Should().Be(AppLabels.MarkPending);

        var record = (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Subject;
        record.Status.Should().Be(AppFormat.Paid);
        record.PaidAmount.Should().Be(800m);
    }

    [Fact]
    public async Task PartialPaymentAction_Income_UsesReceiveLabels()
    {
        MainPage.AddEntry("Freela E2E", AppLabels.OneTime, 1000m, isIncome: true);
        MainPage.RevealActions("Freela E2E");
        MainPage.RevealedPartialPaymentActionText.Should().Be(AppLabels.MarkPartialReceived);
        MainPage.TapSummaryBar();
        MainPage.WaitForActionsHidden();

        var popup = MainPage.OpenPartialPayment("Freela E2E");
        popup.TitleText.Should().Be(AppLabels.MarkPartialReceived);
        popup.ProgressText.Should().Be(AppFormat.PartialPaymentProgress(AppLabels.AlreadyReceived, 0, 1000));
        popup.EnterAmount(400m).Save();

        MainPage.WaitFor(() => MainPage.EntryPartialText("Freela E2E") == AppFormat.PartialPayment(400, AppLabels.PartialPaidIncome));
        MainPage.SummaryReceivedText.Should().Be(AppFormat.Money(400));
        MainPage.SummaryToReceiveText.Should().Be(AppFormat.Money(600));

        var income = (await GetEntryRowsAsync()).Single(e => e.EntryType == AppFormat.Income);
        var record = (await GetMonthlyEntryRowsAsync()).Should().ContainSingle().Subject;
        record.FinancialEntryId.Should().Be(income.Id);
        record.PaidAmount.Should().Be(400m);
    }

    [Fact]
    public void PartialPaymentAction_NoAmount_CannotSave()
    {
        var popup = MainPage.OpenPartialPayment("Mercado E2E");

        popup.IsSaveEnabled.Should().BeFalse();
        popup.EnterAmount(10m);
        popup.IsSaveEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task PartialPaymentAction_Cancel_SavesNothing()
    {
        MainPage.OpenPartialPayment("Mercado E2E").EnterAmount(150m).Cancel();

        MainPage.IsEntryPartiallyPaid("Mercado E2E").Should().BeFalse();
        (await GetMonthlyEntryRowsAsync()).Should().BeEmpty();
    }
}
