using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;
using FluentAssertions.Execution;

namespace FinanceApp.UITests.Tests;

/// <summary>
/// "Relatórios": the month's outcomes and incomes by category. The totals, shares and their order are covered by the
/// FinanceService and ReportsViewModel tests; these check the page reads the month and switches between its views.
/// </summary>
[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class ReportsTests : E2ETestBase
{
    public ReportsTests(AppiumServerFixture server) : base(server, data =>
    {
        data.AddEntry("Farmácia", 80m, categoryIds: [data.CategoryId("Saúde"), data.CategoryId("Educação")]);
        data.AddEntry("Escola", 1000m, categoryIds: [data.CategoryId("Educação")]);
        data.AddEntry("Cinema", 50m, recurrence: AppFormat.OneTime);
        data.AddEntry("Salário", 5000m, isIncome: true, categoryIds: [data.CategoryId("Salário")]);
        data.AddEntry("Presente", 120m, recurrence: AppFormat.OneTime, startMonth: AppFormat.CurrentMonth.AddMonths(1),
            categoryIds: [data.CategoryId("Lazer")]);
    })
    {
    }

    [Fact]
    public void OpenReports_CurrentMonth_ShowsOutcomesByCategoryInEachView()
    {
        var reports = MainPage.OpenReports();

        // "Farmácia" is in two categories: it counts in both, but once in the month's total.
        using (new AssertionScope())
        {
            reports.Month.Should().Be(AppFormat.MonthTitle(AppFormat.CurrentMonth));
            reports.TotalText.Should().Be(AppFormat.Money(1130m));
            reports.Rows.Should().Equal(
                ("Educação", AppFormat.Money(1080m), AppFormat.Share(1080m / 1210m)),
                ("Saúde", AppFormat.Money(80m), AppFormat.Share(80m / 1210m)),
                (AppLabels.Uncategorized, AppFormat.Money(50m), AppFormat.Share(50m / 1210m)));
            reports.IsSharedEntriesNoteVisible.Should().BeTrue();
        }

        reports.ShowPie();
        using (new AssertionScope())
        {
            reports.IsChartVisible.Should().BeTrue();
            reports.Rows.Should().BeEmpty("the table gives way to the chart");
            reports.LegendNames.Should().Equal("🎓 Educação", "🩺 Saúde", $"{AppLabels.UncategorizedIcon} {AppLabels.Uncategorized}");
        }

        reports.ShowColumns();
        reports.LegendNames.Should().HaveCount(3);

        reports.ShowTable().ShowIncomes().WaitForFirstRow("Salário");
        using (new AssertionScope())
        {
            reports.Rows.Should().Equal(("Salário", AppFormat.Money(5000m), AppFormat.Share(1m)));
            reports.TotalText.Should().Be(AppFormat.Money(5000m));
        }
    }

    [Fact]
    public void GoToNextMonth_ShowsThatMonthsEntries()
    {
        var reports = MainPage.OpenReports().GoToNextMonth();

        reports.WaitForFirstRow("Educação");
        using (new AssertionScope())
        {
            reports.Month.Should().Be(AppFormat.MonthTitle(AppFormat.CurrentMonth.AddMonths(1)));
            reports.Rows.Select(r => r.Name).Should().Equal("Educação", "Lazer", "Saúde");
            reports.TotalText.Should().Be(AppFormat.Money(1200m));
        }
    }
}
