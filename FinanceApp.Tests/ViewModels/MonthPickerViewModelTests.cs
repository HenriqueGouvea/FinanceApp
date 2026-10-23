using FinanceApp.ViewModels;
using FluentAssertions;
using System.Globalization;
using Xunit;

namespace FinanceApp.Tests.ViewModels;

public class MonthPickerViewModelTests
{
    private static readonly DateTime CurrentMonth = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Local);
    private static readonly DateTime FirstMonth = CurrentMonth.AddMonths(-24);
    private static readonly DateTime LastMonth = CurrentMonth.AddMonths(24);

    private readonly MonthPickerViewModel _vm = new();
    private readonly List<DateTime> _selected = [];

    public MonthPickerViewModelTests()
    {
        _vm.SetSelectCallback(_selected.Add);
        _vm.Prepare(new DateTime(2027, 3, 1, 0, 0, 0, DateTimeKind.Local), CurrentMonth, FirstMonth, LastMonth);
    }

    // --- Prepare ---

    [Fact]
    public void Prepare_ShowsYearOfSelectedMonthWithTwelveOptions()
    {
        _vm.Year.Should().Be(2027);
        _vm.MonthOptions.Select(o => o.Date.Month).Should().Equal(Enumerable.Range(1, 12));
        _vm.MonthOptions.Should().OnlyContain(o => o.Date.Year == 2027);
    }

    [Fact]
    public void Prepare_MarksOnlySelectedMonthAsSelected()
    {
        _vm.MonthOptions.Should().ContainSingle(o => o.IsSelected).Which.Date.Month.Should().Be(3);
    }

    [Fact]
    public void Prepare_CurrentMonthInAnotherYear_NoOptionMarkedCurrent()
    {
        _vm.MonthOptions.Should().NotContain(o => o.IsCurrent);
    }

    [Fact]
    public void Prepare_SelectedCurrentMonth_MarksItCurrentAndSelected()
    {
        _vm.Prepare(CurrentMonth, CurrentMonth, FirstMonth, LastMonth);

        var option = _vm.MonthOptions.Single(o => o.Date.Month == 10);
        option.IsCurrent.Should().BeTrue();
        option.IsSelected.Should().BeTrue();
    }

    [Fact]
    public void Prepare_SameYearAgain_RebuildsSelection()
    {
        _vm.Prepare(new DateTime(2027, 8, 1, 0, 0, 0, DateTimeKind.Local), CurrentMonth, FirstMonth, LastMonth);

        _vm.MonthOptions.Should().HaveCount(12);
        _vm.MonthOptions.Should().ContainSingle(o => o.IsSelected).Which.Date.Month.Should().Be(8);
    }

    [Fact]
    public void Prepare_PtBrCulture_LabelsAreCapitalizedAbbreviationsWithoutDot()
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
        try
        {
            _vm.Prepare(CurrentMonth.AddMonths(12), CurrentMonth, FirstMonth, LastMonth);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }

        _vm.MonthOptions.Select(o => o.Label).Should().Equal(
            "Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez");
    }

    // --- Range ---

    [Fact]
    public void MonthOptions_LastYear_DisablesMonthsAfterRange()
    {
        _vm.Prepare(LastMonth, CurrentMonth, FirstMonth, LastMonth);

        _vm.MonthOptions.Where(o => o.IsEnabled).Select(o => o.Date.Month).Should().Equal(Enumerable.Range(1, 10));
    }

    [Fact]
    public void MonthOptions_FirstYear_DisablesMonthsBeforeRange()
    {
        _vm.Prepare(FirstMonth, CurrentMonth, FirstMonth, LastMonth);

        _vm.MonthOptions.Where(o => o.IsEnabled).Select(o => o.Date.Month).Should().Equal(Enumerable.Range(10, 3));
    }

    [Fact]
    public void PreviousYearCommand_AtFirstYear_CannotExecute()
    {
        _vm.Prepare(FirstMonth, CurrentMonth, FirstMonth, LastMonth);

        _vm.PreviousYearCommand.CanExecute(null).Should().BeFalse();
        _vm.NextYearCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void NextYearCommand_AtLastYear_CannotExecute()
    {
        _vm.Prepare(LastMonth, CurrentMonth, FirstMonth, LastMonth);

        _vm.NextYearCommand.CanExecute(null).Should().BeFalse();
        _vm.PreviousYearCommand.CanExecute(null).Should().BeTrue();
    }

    // --- Year navigation ---

    [Fact]
    public void NextYearCommand_ShowsFollowingYearMonths()
    {
        _vm.NextYearCommand.Execute(null);

        _vm.Year.Should().Be(2028);
        _vm.MonthOptions.Should().HaveCount(12).And.OnlyContain(o => o.Date.Year == 2028);
        _vm.MonthOptions.Should().NotContain(o => o.IsSelected);
    }

    [Fact]
    public void PreviousYearCommand_ShowsPreviousYearMonths()
    {
        _vm.PreviousYearCommand.Execute(null);

        _vm.Year.Should().Be(2026);
        _vm.MonthOptions.Single(o => o.Date.Month == 10).IsCurrent.Should().BeTrue();
    }

    [Fact]
    public void NextYearCommand_ReachingLastYear_RaisesCanExecuteChanged()
    {
        var raised = false;
        _vm.NextYearCommand.CanExecuteChanged += (_, _) => raised = true;

        _vm.NextYearCommand.Execute(null);

        raised.Should().BeTrue();
        _vm.NextYearCommand.CanExecute(null).Should().BeFalse();
    }

    // --- Selection ---

    [Fact]
    public void SelectMonthCommand_EnabledMonth_InvokesCallbackWithItsDate()
    {
        var option = _vm.MonthOptions.Single(o => o.Date.Month == 6);

        _vm.SelectMonthCommand.Execute(option);

        _selected.Should().ContainSingle().Which.Should().Be(new DateTime(2027, 6, 1));
    }

    [Fact]
    public void SelectMonthCommand_DisabledMonth_DoesNotInvokeCallback()
    {
        _vm.Prepare(LastMonth, CurrentMonth, FirstMonth, LastMonth);

        _vm.SelectMonthCommand.Execute(_vm.MonthOptions.Single(o => o.Date.Month == 12));

        _selected.Should().BeEmpty();
    }

    [Fact]
    public void SelectCurrentMonthCommand_InvokesCallbackWithCurrentMonth()
    {
        _vm.SelectCurrentMonthCommand.Execute(null);

        _selected.Should().ContainSingle().Which.Should().Be(CurrentMonth);
    }
}
