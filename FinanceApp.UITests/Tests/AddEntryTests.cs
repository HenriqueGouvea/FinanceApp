using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;
using FluentAssertions.Execution;

namespace FinanceApp.UITests.Tests;

[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class AddEntryTests : E2ETestBase
{
    public AddEntryTests(AppiumServerFixture server) : base(server)
    {
    }

    [Fact]
    [Trait("Smoke", "true")]
    public async Task AddEntry_OneTimeOutcome_ShowsInCurrentMonthAndPersists()
    {
        MainPage.OutcomeCountText.Should().Be(AppFormat.EntryCount(0));

        var page = MainPage.OpenAddEntry().WaitForDescriptionFocus();
        page.TitleText.Should().Be(AppLabels.NewEntryTitle);
        page.IsAmountVisible.Should().BeFalse("the amount field only appears once a recurrence is chosen");

        page.EnterDescription("Celular E2E")
            .SelectRecurrence(AppLabels.OneTime);
        page.IsInstallmentsVisible.Should().BeFalse();
        page.AmountText.Should().Be(AppFormat.Money(0));

        page.EnterAmount(1234.56m);
        page.AmountText.Should().Be("R$ 1.234,56", "typed digits fill the currency mask from the cents");
        using (new AssertionScope())
        {
            page.IsPaymentSourcePickerVisible.Should().BeFalse("there is no account, card or voucher to pick yet");
            page.IsCreditCardChoiceHintVisible.Should().BeFalse("there is no credit card to pick");
        }
        page.Save();

        MainPage.WaitForEntry("Celular E2E");
        using (new AssertionScope())
        {
            MainPage.OutcomeCountText.Should().Be(AppFormat.EntryCount(1));
            MainPage.OutcomeTotalText.Should().Be(AppFormat.Money(1234.56m));
            MainPage.SummaryOutcomeText.Should().Be(AppFormat.Money(1234.56m));
            MainPage.SummaryBalanceText.Should().Be(AppFormat.Money(-1234.56m));
            MainPage.VisibleEntryAmounts.Should().ContainSingle().Which.Should().Be(AppFormat.Money(1234.56m));
        }

        var row = (await GetEntryRowsAsync()).Should().ContainSingle().Subject;
        row.Description.Should().Be("Celular E2E");
        row.Amount.Should().Be(1234.56m);
        row.Recurrence.Should().Be(AppFormat.OneTime);
        row.EntryType.Should().Be(AppFormat.Outcome);
        row.StartDate.Date.Should().Be(AppFormat.CurrentMonth);
        row.EndDate.Should().BeNull();
    }

    [Fact]
    public async Task AddEntry_RecurrentIncome_ShowsInIncomeSectionFromStartMonthOnwards()
    {
        MainPage.AddEntry("Salário E2E", AppLabels.Recurrent, 5000m, isIncome: true);

        using (new AssertionScope())
        {
            MainPage.IncomeCountText.Should().Be(AppFormat.EntryCount(1));
            MainPage.IncomeTotalText.Should().Be(AppFormat.Money(5000));
            MainPage.OutcomeCountText.Should().Be(AppFormat.EntryCount(0));
            MainPage.SummaryIncomeText.Should().Be(AppFormat.Money(5000));
            MainPage.SummaryBalanceText.Should().Be(AppFormat.Money(5000));
        }

        MainPage.GoToNextMonth().WaitForEntry("Salário E2E");
        MainPage.GoToPreviousMonth().GoToPreviousMonth();
        MainPage.IsEntryVisible("Salário E2E").Should().BeFalse("a recurrent entry starts in the month it was created");

        var row = (await GetEntryRowsAsync()).Should().ContainSingle().Subject;
        row.EntryType.Should().Be(AppFormat.Income);
        row.Recurrence.Should().Be(AppFormat.Recurrent);
        row.TotalInstallments.Should().BeNull();
        (await GetMonthlyEntryRowsAsync()).Should().BeEmpty("projections are virtual until the user acts on a month");
    }

    [Fact]
    [Trait("Smoke", "true")]
    public async Task AddEntry_Installments_ShowsInstallmentLabelForExactlyNMonths()
    {
        var page = MainPage.OpenAddEntry()
            .EnterDescription("Notebook E2E")
            .SelectRecurrence(AppLabels.Installments);
        page.IsInstallmentsVisible.Should().BeTrue();

        page.EnterInstallments("3").EnterAmount(100);
        page.InstallmentsTotalText.Should().Be($"Total: {AppFormat.Money(300)}");
        page.Save();

        MainPage.WaitForEntry("Notebook E2E");
        MainPage.VisibleInstallmentLabels.Should().ContainSingle().Which.Should().Be("1/3");
        MainPage.VisibleInstallmentTotals.Should().ContainSingle().Which.Should().Be(AppFormat.InstallmentTotal(300));
        MainPage.VisibleEntryAmounts.Should().ContainSingle().Which.Should().Be(AppFormat.Money(100));

        MainPage.GoToNextMonth();
        MainPage.VisibleInstallmentLabels.Should().ContainSingle().Which.Should().StartWith("2/3");
        MainPage.GoToNextMonth();
        MainPage.VisibleInstallmentLabels.Should().ContainSingle().Which.Should().StartWith("3/3");
        MainPage.GoToNextMonth();
        MainPage.IsEntryVisible("Notebook E2E").Should().BeFalse("the entry only has 3 installments");

        var row = (await GetEntryRowsAsync()).Should().ContainSingle().Subject;
        row.Recurrence.Should().Be(AppFormat.Installments);
        row.TotalInstallments.Should().Be(3);
        row.Amount.Should().Be(100m);
    }

    [Fact]
    public async Task AddEntry_AnnualInInstallments_FallsInTheSameMonthsEveryYear()
    {
        var page = MainPage.OpenAddEntry()
            .EnterDescription("IPVA E2E")
            .SelectRecurrence(AppLabels.Annual);
        page.IsInstallmentsVisible.Should().BeFalse("an annual entry has its own installments field");

        page.EnterAnnualInstallments("2").EnterAmount(500);
        page.InstallmentsTotalText.Should().Be($"Total: {AppFormat.Money(1000)}");
        page.Save();

        MainPage.WaitForEntry("IPVA E2E");
        MainPage.VisibleInstallmentLabels.Should().ContainSingle().Which.Should().Be("1/2");
        MainPage.GoToNextMonth();
        MainPage.VisibleInstallmentLabels.Should().ContainSingle().Which.Should().StartWith("2/2");
        MainPage.GoToNextMonth();
        MainPage.IsEntryVisible("IPVA E2E").Should().BeFalse("the year's two installments are over");

        MainPage.GoToMonth(AppFormat.CurrentMonth.AddYears(1)).WaitForEntry("IPVA E2E");
        MainPage.VisibleInstallmentLabels.Should().ContainSingle().Which.Should().Be("1/2", "it comes back the next year");

        var row = (await GetEntryRowsAsync()).Should().ContainSingle().Subject;
        row.Recurrence.Should().Be(AppFormat.Annual);
        row.TotalInstallments.Should().Be(2);
        row.StartDate.Date.Should().Be(AppFormat.CurrentMonth);
    }

    [Fact]
    public async Task AddEntry_CancelBackButtonOrSystemBack_DiscardsAndReturnsToMonth()
    {
        var month = AppFormat.CurrentMonth.AddMonths(1);
        MainPage.GoToNextMonth();

        MainPage.OpenAddEntry()
            .EnterDescription("Descartado E2E")
            .SelectRecurrence(AppLabels.OneTime)
            .EnterAmount(10)
            .Cancel();
        MainPage.MonthTitleText.Should().Be(AppFormat.MonthTitle(month));

        var page = MainPage.OpenAddEntry()
            .EnterDescription("Descartado E2E")
            .SelectRecurrence(AppLabels.Recurrent)
            .EnterAmount(10)
            .ScrollToEnd();
        page.IsBackButtonVisible.Should().BeTrue("the header stays on screen while the form scrolls");
        page.GoBack();
        MainPage.MonthTitleText.Should().Be(AppFormat.MonthTitle(month));

        MainPage.OpenAddEntry()
            .EnterDescription("Descartado E2E")
            .SelectRecurrence(AppLabels.OneTime)
            .EnterAmount(10)
            .PressSystemBack();

        MainPage.MonthTitleText.Should().Be(AppFormat.MonthTitle(month));
        MainPage.IsEntryVisible("Descartado E2E").Should().BeFalse();
        MainPage.OutcomeCountText.Should().Be(AppFormat.EntryCount(0));
        (await GetEntryRowsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task AddEntry_SavedFromAnotherMonth_StartsAndStaysInThatMonth()
    {
        var month = AppFormat.CurrentMonth.AddMonths(1);
        MainPage.GoToNextMonth();

        MainPage.AddEntry("Viagem E2E", AppLabels.OneTime, 40m);

        MainPage.MonthTitleText.Should().Be(AppFormat.MonthTitle(month));
        MainPage.OutcomeTotalText.Should().Be(AppFormat.Money(40));
        MainPage.GoToPreviousMonth();
        MainPage.IsEntryVisible("Viagem E2E").Should().BeFalse();

        var row = (await GetEntryRowsAsync()).Should().ContainSingle().Subject;
        row.StartDate.Date.Should().Be(month);
    }
}
