using FinanceApp.Models;
using FinanceApp.Services;
using FinanceApp.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace FinanceApp.Tests.ViewModels;

public class ReportsViewModelTests
{
    private static readonly FinancialCategory Health = new() { Id = 1, Name = "Saúde", Icon = "🩺", Color = "#E53935" };
    private static readonly FinancialCategory Children = new() { Id = 2, Name = "Filhos", Icon = "🧸", Color = "#8E24AA" };
    private static readonly FinancialCategory Salary = new() { Id = 3, Name = "Salário", Icon = "💼", Color = "#00897B", EntryType = FinancialEntryType.Income };

    private readonly IFinanceService _service = Substitute.For<IFinanceService>();
    private readonly ReportsViewModel _vm;

    public ReportsViewModelTests()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 11, 22, 12, 0, 0, TimeSpan.Zero));
        timeProvider.SetLocalTimeZone(TimeZoneInfo.Utc);
        _service.GetCategoryReportAsync(Arg.Any<int>(), Arg.Any<int>()).Returns(new CategoryReport([], 0, 0));
        _vm = new ReportsViewModel(_service, timeProvider);
    }

    [Fact]
    public void Constructor_StartsOnTheCurrentMonthsOutcomesAsATable()
    {
        _vm.Month.Should().Be(new DateTime(2026, 11, 1));
        _vm.MonthTitle.Should().Be(MainViewModel.FormatMonthName(new DateTime(2026, 11, 1)));
        _vm.IsOutcome.Should().BeTrue();
        _vm.IsTable.Should().BeTrue();
    }

    [Fact]
    public async Task Load_OutcomesByCategory_ShowsEachWithItsShareOfTheCategoriesSum()
    {
        GivenNovemberReport(
            new CategoryTotal(FinancialEntryType.Outcome, Children, 300m, 2),
            new CategoryTotal(FinancialEntryType.Outcome, null, 100m, 1));

        await _vm.LoadCommand.ExecuteAsync(null);

        _vm.Rows.Select(r => (r.Name, r.Icon, r.Amount, r.ShareLabel, r.EntryCountLabel)).Should().Equal(
            ("Filhos", "🧸", 300m, "75%", "2 lançamentos"),
            (ReportsViewModel.UncategorizedName, ReportsViewModel.UncategorizedIcon, 100m, "25%", "1 lançamento"));
        _vm.MonthTotal.Should().Be(400m);
        _vm.CategoriesExceedTotal.Should().BeFalse();
        _vm.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task Load_EntryInSeveralCategories_SaysTheCategoriesAddUpToMoreThanTheTotal()
    {
        // The R$ 80,00 pharmacy is in both categories, but counts once in the month's total.
        _service.GetCategoryReportAsync(2026, 11).Returns(new CategoryReport(
            [new CategoryTotal(FinancialEntryType.Outcome, Health, 80m, 1), new CategoryTotal(FinancialEntryType.Outcome, Children, 80m, 1)],
            TotalIncomes: 0, TotalOutcomes: 80m));

        await _vm.LoadCommand.ExecuteAsync(null);

        _vm.CategoriesExceedTotal.Should().BeTrue();
    }

    [Fact]
    public async Task SelectIncomes_ShowsTheIncomesAndTheirTotal()
    {
        GivenNovemberReport(
            new CategoryTotal(FinancialEntryType.Income, Salary, 5000m, 1),
            new CategoryTotal(FinancialEntryType.Outcome, Health, 80m, 1));
        await _vm.LoadCommand.ExecuteAsync(null);

        _vm.SelectIncomesCommand.Execute(null);

        _vm.Rows.Select(r => r.Name).Should().Equal("Salário");
        _vm.MonthTotal.Should().Be(5000m);
        _vm.TotalLabel.Should().Be("Receitas do mês");
    }

    [Fact]
    public async Task Load_MonthWithoutEntries_IsEmpty()
    {
        await _vm.LoadCommand.ExecuteAsync(null);

        _vm.IsEmpty.Should().BeTrue();
        _vm.IsChart.Should().BeFalse();
        _vm.EmptyLabel.Should().Be("Nenhuma despesa neste mês.");
    }

    [Theory]
    [InlineData(ReportView.Pie, false, true, false)]
    [InlineData(ReportView.Columns, false, false, true)]
    [InlineData(ReportView.Table, true, false, false)]
    public async Task SelectView_ShowsOnlyThatView(ReportView view, bool isTable, bool isPie, bool isColumns)
    {
        GivenNovemberReport(new CategoryTotal(FinancialEntryType.Outcome, Health, 80m, 1));
        await _vm.LoadCommand.ExecuteAsync(null);

        _vm.SelectViewCommand.Execute(view);

        (_vm.IsTable, _vm.IsPie, _vm.IsColumns, _vm.IsChart).Should().Be((isTable, isPie, isColumns, !isTable));
    }

    [Fact]
    public async Task NextMonth_LoadsThatMonthsReport()
    {
        await _vm.NextMonthCommand.ExecuteAsync(null);

        _vm.Month.Should().Be(new DateTime(2026, 12, 1));
        await _service.Received(1).GetCategoryReportAsync(2026, 12);
    }

    [Fact]
    public async Task GoToMonthAsync_OutsideTheRange_StaysOnTheMonth()
    {
        await _vm.GoToMonthAsync(new DateTime(2029, 1, 1));

        _vm.Month.Should().Be(new DateTime(2026, 11, 1));
        await _service.DidNotReceive().GetCategoryReportAsync(2029, 1);
    }

    [Fact]
    public async Task PreviousMonth_AtTheFirstMonth_CannotGoFurther()
    {
        await _vm.GoToMonthAsync(_vm.FirstMonth);

        _vm.PreviousMonthCommand.CanExecute(null).Should().BeFalse();
        _vm.NextMonthCommand.CanExecute(null).Should().BeTrue();
    }

    private void GivenNovemberReport(params CategoryTotal[] totals)
    {
        var incomes = totals.Where(t => t.EntryType == FinancialEntryType.Income).Sum(t => t.Amount);
        var outcomes = totals.Where(t => t.EntryType == FinancialEntryType.Outcome).Sum(t => t.Amount);
        _service.GetCategoryReportAsync(2026, 11).Returns(new CategoryReport(totals, incomes, outcomes));
    }
}
