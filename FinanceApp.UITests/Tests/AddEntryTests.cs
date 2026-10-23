using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class AddEntryTests : E2ETestBase
{
    public AddEntryTests(AppiumServerFixture server) : base(server)
    {
    }

    [Fact]
    public async Task AddEntry_OneTimeOutcome_ShowsInCurrentMonthAndPersists()
    {
        MainPage.OutcomeCountText.Should().Be(AppFormat.EntryCount(0));

        var popup = MainPage.OpenAddEntry();
        popup.TitleText.Should().Be(AppLabels.NewEntryTitle);
        popup.IsAmountVisible.Should().BeFalse("the amount field only appears once a recurrence is chosen");

        popup.EnterDescription("Aluguel E2E")
             .SelectRecurrence(AppLabels.OneTime);
        popup.IsAmountVisible.Should().BeTrue();
        popup.IsInstallmentsVisible.Should().BeFalse();

        popup.EnterAmount(1500)
             .Save();

        MainPage.WaitForEntry("Aluguel E2E");
        MainPage.OutcomeCountText.Should().Be(AppFormat.EntryCount(1));
        MainPage.OutcomeTotalText.Should().Be(AppFormat.Money(1500));
        MainPage.SummaryOutcomeText.Should().Be(AppFormat.Money(1500));
        MainPage.SummaryBalanceText.Should().Be(AppFormat.Money(-1500));
        MainPage.VisibleEntryAmounts.Should().ContainSingle().Which.Should().Be(AppFormat.Money(1500));

        var row = (await GetEntryRowsAsync()).Should().ContainSingle().Subject;
        row.Description.Should().Be("Aluguel E2E");
        row.Amount.Should().Be(1500m);
        row.Recurrence.Should().Be(AppFormat.OneTime);
        row.EntryType.Should().Be(AppFormat.Outcome);
        row.StartDate.Date.Should().Be(AppFormat.CurrentMonth);
        row.EndDate.Should().BeNull();
    }

    [Fact]
    public async Task AddEntry_AmountField_MasksTypedDigitsAsCurrency()
    {
        var popup = MainPage.OpenAddEntry()
            .EnterDescription("Celular E2E")
            .SelectRecurrence(AppLabels.OneTime);
        popup.AmountText.Should().Be(AppFormat.Money(0));

        popup.EnterAmount(1234.56m);
        popup.AmountText.Should().Be(AppFormat.Money(1234.56m)).And.Be("R$ 1.234,56");
        popup.Save();

        MainPage.WaitForEntry("Celular E2E");
        MainPage.VisibleEntryAmounts.Should().ContainSingle().Which.Should().Be("R$ 1.234,56");
        MainPage.OutcomeTotalText.Should().Be("R$ 1.234,56");
        MainPage.SummaryBalanceText.Should().Be("-R$ 1.234,56");

        (await GetEntryRowsAsync()).Should().ContainSingle().Which.Amount.Should().Be(1234.56m);
    }

    [Fact]
    public void AddEntry_RecurrencePicker_ExplainsRecurrentOption()
    {
        var popup = MainPage.OpenAddEntry();

        popup.RecurrenceOptions().Should().Equal(AppLabels.OneTime, "Todo mês (recorrente)", AppLabels.Installments);
    }

    [Fact]
    public async Task AddEntry_Income_ShowsInIncomeSectionAndUpdatesBalance()
    {
        MainPage.AddEntry("Salário E2E", AppLabels.Recurrent, 5000m, isIncome: true);

        MainPage.IncomeCountText.Should().Be(AppFormat.EntryCount(1));
        MainPage.IncomeTotalText.Should().Be(AppFormat.Money(5000));
        MainPage.OutcomeCountText.Should().Be(AppFormat.EntryCount(0));
        MainPage.SummaryIncomeText.Should().Be(AppFormat.Money(5000));
        MainPage.SummaryBalanceText.Should().Be(AppFormat.Money(5000));

        var row = (await GetEntryRowsAsync()).Should().ContainSingle().Subject;
        row.EntryType.Should().Be(AppFormat.Income);
        row.Recurrence.Should().Be(AppFormat.Recurrent);
    }

    [Fact]
    public async Task AddEntry_Recurrent_AppearsFromStartMonthOnwards()
    {
        MainPage.AddEntry("Internet E2E", AppLabels.Recurrent, 120m);

        MainPage.GoToNextMonth().WaitForEntry("Internet E2E");
        MainPage.GoToNextMonth().WaitForEntry("Internet E2E");
        MainPage.GoToPreviousMonth().GoToPreviousMonth().GoToPreviousMonth();
        MainPage.IsEntryVisible("Internet E2E").Should().BeFalse("a recurrent entry starts in the month it was created");

        var row = (await GetEntryRowsAsync()).Should().ContainSingle().Subject;
        row.Recurrence.Should().Be(AppFormat.Recurrent);
        row.TotalInstallments.Should().BeNull();
        (await GetMonthlyEntryRowsAsync()).Should().BeEmpty("projections are virtual until the user acts on a month");
    }

    [Fact]
    public async Task AddEntry_Installments_ShowsInstallmentLabelForExactlyNMonths()
    {
        var popup = MainPage.OpenAddEntry()
            .EnterDescription("Notebook E2E")
            .SelectRecurrence(AppLabels.Installments);
        popup.IsInstallmentsVisible.Should().BeTrue();

        popup.EnterInstallments("3").EnterAmount(100);
        popup.InstallmentsTotalText.Should().Be($"Total: {AppFormat.Money(300)}");
        popup.Save();

        MainPage.WaitForEntry("Notebook E2E");
        MainPage.VisibleInstallmentLabels.Should().ContainSingle().Which.Should().Be($"1/3 · Total {AppFormat.Money(300)}");
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
    public async Task AddEntry_WithCategory_ShowsCategoryChipAndLinksCategory()
    {
        MainPage.AddEntry("Supermercado E2E", AppLabels.OneTime, 350m, category: "Mercado");

        MainPage.VisibleEntryCategories.Should().ContainSingle().Which.Should().Be("Mercado");

        var category = (await GetCategoryRowsAsync()).Single(c => c.Name == "Mercado");
        var row = (await GetEntryRowsAsync()).Should().ContainSingle().Subject;
        row.CategoryId.Should().Be(category.Id);
    }

    [Fact]
    public async Task AddEntry_Cancelled_DoesNotPersist()
    {
        var popup = MainPage.OpenAddEntry()
            .EnterDescription("Descartado E2E")
            .SelectRecurrence(AppLabels.OneTime)
            .EnterAmount(10);

        popup.Cancel();

        MainPage.IsEntryVisible("Descartado E2E").Should().BeFalse();
        MainPage.OutcomeCountText.Should().Be(AppFormat.EntryCount(0));
        (await GetEntryRowsAsync()).Should().BeEmpty();
    }
}
