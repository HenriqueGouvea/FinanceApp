using CommunityToolkit.Mvvm.Messaging;
using FinanceApp.Messages;
using FinanceApp.Models;
using FinanceApp.Services;
using FinanceApp.ViewModels;
using FluentAssertions;
using NSubstitute;
using System.Globalization;
using Xunit;

namespace FinanceApp.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly IFinanceService _service;
    private readonly IPreferencesService _preferencesService;
    private readonly IMessenger _messenger = new WeakReferenceMessenger();
    private readonly MainViewModel _vm;

    public MainViewModelTests()
    {
        _service = Substitute.For<IFinanceService>();
        _service.GetMergedEntriesForMonthAsync(Arg.Any<int>(), Arg.Any<int>())
            .Returns(Task.FromResult<IEnumerable<MonthlyEntryProjection>>([]));

        _preferencesService = Substitute.For<IPreferencesService>();
        _preferencesService.Get(Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        _vm = new MainViewModel(_service, _preferencesService, _messenger);
    }

    // --- Initialization ---

    [Fact]
    public async Task InitializeAsync_FirstCall_Creates49Months()
    {
        await _vm.InitializeAsync();

        _vm.Months.Should().HaveCount(49);
    }

    [Fact]
    public async Task InitializeAsync_FirstCall_SetsCurrentPositionTo24()
    {
        await _vm.InitializeAsync();

        _vm.CurrentPosition.Should().Be(24);
    }

    [Fact]
    public async Task InitializeAsync_CalledTwice_DoesNotDuplicateMonths()
    {
        await _vm.InitializeAsync();
        await _vm.InitializeAsync();

        _vm.Months.Should().HaveCount(49);
    }

    [Fact]
    public async Task InitializeAsync_LoadsEntriesForNearbyMonths()
    {
        await _vm.InitializeAsync();

        await _service.Received(5).GetMergedEntriesForMonthAsync(Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public async Task InitializeAsync_PtBrCulture_MonthNameIsLocalizedAndCapitalized()
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
        try
        {
            await _vm.InitializeAsync();
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }

        _vm.Months.Should().Contain(m => m.MonthName.StartsWith("Outubro "));
        _vm.Months.Should().OnlyContain(m => char.IsUpper(m.MonthName[0]));
    }

    // --- Preferences: initial expanded state ---

    [Fact]
    public async Task InitializeAsync_SetsIsIncomesExpandedFromPreferences()
    {
        _preferencesService.Get("incomes_expanded", true).Returns(false);
        var vm = new MainViewModel(_service, _preferencesService, _messenger);

        await vm.InitializeAsync();

        vm.Months.Should().AllSatisfy(m => m.IsIncomesExpanded.Should().BeFalse());
    }

    [Fact]
    public async Task InitializeAsync_SetsIsOutcomesExpandedFromPreferences()
    {
        _preferencesService.Get("outcomes_expanded", true).Returns(false);
        var vm = new MainViewModel(_service, _preferencesService, _messenger);

        await vm.InitializeAsync();

        vm.Months.Should().AllSatisfy(m => m.IsOutcomesExpanded.Should().BeFalse());
    }

    // --- Preferences: persistence on toggle ---

    [Fact]
    public async Task MonthViewModel_WhenIsIncomesExpandedChanges_PersistsPreference()
    {
        await _vm.InitializeAsync();

        _vm.Months[0].ToggleIncomesCommand.Execute(null);

        _preferencesService.Received().Set("incomes_expanded", false);
    }

    [Fact]
    public async Task MonthViewModel_WhenIsOutcomesExpandedChanges_PersistsPreference()
    {
        await _vm.InitializeAsync();

        _vm.Months[0].ToggleOutcomesCommand.Execute(null);

        _preferencesService.Received().Set("outcomes_expanded", false);
    }

    // --- Preferences: sync across all months ---

    [Fact]
    public async Task MonthViewModel_WhenIsIncomesExpandedChanges_SyncsAllOtherMonths()
    {
        await _vm.InitializeAsync();

        _vm.Months[0].ToggleIncomesCommand.Execute(null);

        _vm.Months.Should().AllSatisfy(m => m.IsIncomesExpanded.Should().BeFalse());
    }

    [Fact]
    public async Task MonthViewModel_WhenIsOutcomesExpandedChanges_SyncsAllOtherMonths()
    {
        await _vm.InitializeAsync();

        _vm.Months[0].ToggleOutcomesCommand.Execute(null);

        _vm.Months.Should().AllSatisfy(m => m.IsOutcomesExpanded.Should().BeFalse());
    }

    // --- Refresh / Clear ---

    [Fact]
    public async Task RefreshAllAsync_MarksDistantMonthsAsUnloaded()
    {
        await _vm.InitializeAsync();
        foreach (var month in _vm.Months)
            month.IsLoaded = true;

        await _vm.RefreshAllAsync();

        _vm.Months[0].IsLoaded.Should().BeFalse();
        _vm.Months[48].IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshAllAsync_ReloadsNearbyMonths()
    {
        await _vm.InitializeAsync();
        _service.ClearReceivedCalls();

        await _vm.RefreshAllAsync();

        await _service.Received().GetMergedEntriesForMonthAsync(Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public async Task CategoriesChangedMessage_ReloadsNearbyMonths()
    {
        await _vm.InitializeAsync();
        _service.ClearReceivedCalls();

        _messenger.Send(new CategoriesChangedMessage());

        await _service.Received(5).GetMergedEntriesForMonthAsync(Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public async Task ClearDatabaseAsync_CallsClearAllData()
    {
        await _vm.InitializeAsync();

        await _vm.ClearDatabaseAsync();

        await _service.Received(1).ClearAllDataAsync();
    }

    [Fact]
    public async Task ClearDatabaseAsync_ClearsAllMonthEntries()
    {
        await _vm.InitializeAsync();
        _vm.Months[0].Entries.Add(new MonthlyEntryProjection(
            FinancialEntryId: 1,
            FinancialMonthlyEntryId: null,
            Description: "Test",
            EffectiveAmount: 100m,
            EntryType: FinancialEntryType.Income,
            Category: null,
            Status: FinancialMonthlyEntryStatus.Pending,
            IsProjected: true,
            InstallmentLabel: null,
            InstallmentTotalAmount: null));

        await _vm.ClearDatabaseAsync();

        _vm.Months[0].Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task CurrentPositionChanged_HidesRevealedEntryActions()
    {
        await _vm.InitializeAsync();
        _vm.SelectedMonth!.RevealedEntryId = 7;

        _vm.CurrentPosition = 25;

        _vm.Months.Should().OnlyContain(m => m.RevealedEntryId == null);
    }

    [Fact]
    public async Task DeleteEntryAsync_CallsServiceWithSelectedMonthAndScope()
    {
        await _vm.InitializeAsync();
        var selected = _vm.SelectedMonth!.Date;

        await _vm.DeleteEntryAsync(MakeProjection(7), EntryDeleteScope.ThisAndFuture);

        await _service.Received(1).DeleteEntryAsync(7, selected.Year, selected.Month, EntryDeleteScope.ThisAndFuture);
    }

    [Fact]
    public async Task DeleteEntryAsync_ReloadsNearbyMonths()
    {
        await _vm.InitializeAsync();
        _service.ClearReceivedCalls();

        await _vm.DeleteEntryAsync(MakeProjection(7), EntryDeleteScope.All);

        await _service.Received().GetMergedEntriesForMonthAsync(Arg.Any<int>(), Arg.Any<int>());
    }

    // --- Status ---

    [Fact]
    public async Task ToggleEntryStatusAsync_PendingEntry_MarksPaidInSelectedMonth()
    {
        await _vm.InitializeAsync();
        var selected = _vm.SelectedMonth!.Date;

        await _vm.ToggleEntryStatusAsync(MakeProjection(7));

        await _service.Received(1).SetEntryStatusAsync(7, selected.Year, selected.Month, FinancialMonthlyEntryStatus.Paid);
    }

    [Fact]
    public async Task ToggleEntryStatusAsync_PaidEntry_MarksPending()
    {
        await _vm.InitializeAsync();
        var selected = _vm.SelectedMonth!.Date;

        await _vm.ToggleEntryStatusAsync(MakeProjection(7) with { Status = FinancialMonthlyEntryStatus.Paid });

        await _service.Received(1).SetEntryStatusAsync(7, selected.Year, selected.Month, FinancialMonthlyEntryStatus.Pending);
    }

    [Fact]
    public async Task ToggleEntryStatusAsync_HidesActionsAndReloadsNearbyMonths()
    {
        await _vm.InitializeAsync();
        _vm.SelectedMonth!.RevealedEntryId = 7;
        _service.ClearReceivedCalls();

        await _vm.ToggleEntryStatusAsync(MakeProjection(7));

        _vm.SelectedMonth.RevealedEntryId.Should().BeNull();
        await _service.Received(5).GetMergedEntriesForMonthAsync(Arg.Any<int>(), Arg.Any<int>());
    }

    // --- Month navigation ---

    [Fact]
    public async Task GoToMonth_DateInRange_SelectsThatMonth()
    {
        await _vm.InitializeAsync();
        var target = _vm.CurrentMonthDate.AddMonths(-14);

        _vm.GoToMonth(target);

        _vm.SelectedMonth!.Date.Should().Be(target);
    }

    [Fact]
    public async Task GoToMonth_FirstAndLastMonths_SelectRangeEdges()
    {
        await _vm.InitializeAsync();

        _vm.GoToMonth(_vm.LastMonthDate);
        _vm.CurrentPosition.Should().Be(48);

        _vm.GoToMonth(_vm.FirstMonthDate);
        _vm.CurrentPosition.Should().Be(0);
    }

    [Fact]
    public async Task GoToMonth_DateOutsideRange_KeepsCurrentPosition()
    {
        await _vm.InitializeAsync();

        _vm.GoToMonth(_vm.LastMonthDate.AddMonths(1));
        _vm.GoToMonth(_vm.FirstMonthDate.AddMonths(-1));

        _vm.CurrentPosition.Should().Be(24);
    }

    [Fact]
    public async Task GoToCurrentMonthCommand_AfterNavigatingAway_ReturnsToCurrentMonth()
    {
        await _vm.InitializeAsync();
        _vm.CurrentPosition = 40;

        _vm.GoToCurrentMonthCommand.Execute(null);

        _vm.CurrentPosition.Should().Be(24);
        _vm.SelectedMonth!.Date.Should().Be(_vm.CurrentMonthDate);
    }

    [Fact]
    public async Task IsAwayFromCurrentMonth_OnCurrentMonth_IsFalse()
    {
        await _vm.InitializeAsync();

        _vm.IsAwayFromCurrentMonth.Should().BeFalse();
    }

    [Fact]
    public void IsAwayFromCurrentMonth_BeforeInitialization_IsFalse()
    {
        _vm.IsAwayFromCurrentMonth.Should().BeFalse();
    }

    [Fact]
    public async Task IsAwayFromCurrentMonth_AfterNavigatingAway_IsTrueAndRaisesChange()
    {
        await _vm.InitializeAsync();
        var notified = new List<string>();
        _vm.PropertyChanged += (_, e) => notified.Add(e.PropertyName!);

        _vm.CurrentPosition = 25;

        _vm.IsAwayFromCurrentMonth.Should().BeTrue();
        notified.Should().Contain([nameof(MainViewModel.IsAwayFromCurrentMonth), nameof(MainViewModel.GoToCurrentMonthLabel)]);
    }

    [Theory]
    [InlineData(30, "‹ Mês atual")]
    [InlineData(10, "Mês atual ›")]
    public async Task GoToCurrentMonthLabel_PointsTowardsCurrentMonth(int position, string expected)
    {
        await _vm.InitializeAsync();

        _vm.CurrentPosition = position;

        _vm.GoToCurrentMonthLabel.Should().Be(expected);
    }

    [Fact]
    public async Task SelectedMonth_ReturnsMonthAtCurrentPosition()
    {
        await _vm.InitializeAsync();

        _vm.SelectedMonth.Should().BeSameAs(_vm.Months[24]);
    }

    [Fact]
    public async Task CurrentPositionChanged_TriggersLoadForNewPosition()
    {
        await _vm.InitializeAsync();
        _service.ClearReceivedCalls();

        _vm.CurrentPosition = 28;
        await Task.Delay(100);

        await _service.Received().GetMergedEntriesForMonthAsync(Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public async Task GoToNextMonthCommand_MovesOneMonthForward()
    {
        await _vm.InitializeAsync();

        _vm.GoToNextMonthCommand.Execute(null);

        _vm.CurrentPosition.Should().Be(25);
    }

    [Fact]
    public async Task GoToPreviousMonthCommand_MovesOneMonthBack()
    {
        await _vm.InitializeAsync();

        _vm.GoToPreviousMonthCommand.Execute(null);

        _vm.CurrentPosition.Should().Be(23);
    }

    [Fact]
    public async Task GoToPreviousMonthCommand_OnFirstMonth_CannotExecute()
    {
        await _vm.InitializeAsync();

        _vm.GoToMonth(_vm.FirstMonthDate);

        _vm.GoToPreviousMonthCommand.CanExecute(null).Should().BeFalse();
        _vm.GoToNextMonthCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task GoToNextMonthCommand_OnLastMonth_CannotExecute()
    {
        await _vm.InitializeAsync();

        _vm.GoToMonth(_vm.LastMonthDate);

        _vm.GoToNextMonthCommand.CanExecute(null).Should().BeFalse();
        _vm.GoToPreviousMonthCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task GoToNextMonthCommand_PositionChanges_RaisesCanExecuteChanged()
    {
        await _vm.InitializeAsync();
        var raised = false;
        _vm.GoToNextMonthCommand.CanExecuteChanged += (_, _) => raised = true;

        _vm.CurrentPosition = 30;

        raised.Should().BeTrue();
    }

    // --- Swipe hint ---

    [Fact]
    public async Task ShowSwipeHint_NotSeenBefore_IsShownAfterInitialization()
    {
        await _vm.InitializeAsync();

        _vm.ShowSwipeHint.Should().BeTrue();
        _preferencesService.DidNotReceive().Set("show_month_swipe_hint", Arg.Any<bool>());
    }

    [Fact]
    public void ShowSwipeHint_DismissedBefore_IsHidden()
    {
        _preferencesService.Get("show_month_swipe_hint", Arg.Any<bool>()).Returns(false);

        var vm = new MainViewModel(_service, _preferencesService, _messenger);

        vm.ShowSwipeHint.Should().BeFalse();
    }

    [Fact]
    public async Task ShowSwipeHint_AfterChangingMonth_IsHiddenAndPersisted()
    {
        await _vm.InitializeAsync();

        _vm.CurrentPosition = 25;

        _vm.ShowSwipeHint.Should().BeFalse();
        _preferencesService.Received(1).Set("show_month_swipe_hint", false);
    }

    private static MonthlyEntryProjection MakeProjection(int entryId) => new(
        FinancialEntryId: entryId,
        FinancialMonthlyEntryId: null,
        Description: "Test",
        EffectiveAmount: 100m,
        EntryType: FinancialEntryType.Outcome,
        Category: null,
        Status: FinancialMonthlyEntryStatus.Pending,
        IsProjected: true,
        InstallmentLabel: null,
        InstallmentTotalAmount: null);
}
