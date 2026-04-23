using FinanceApp.Models;
using FinanceApp.Services;
using FinanceApp.ViewModels;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinanceApp.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly IFinanceService _service;
    private readonly IPreferencesService _preferencesService;
    private readonly MainViewModel _vm;

    public MainViewModelTests()
    {
        _service = Substitute.For<IFinanceService>();
        _service.GetMergedEntriesForMonthAsync(Arg.Any<int>(), Arg.Any<int>())
            .Returns(Task.FromResult<IEnumerable<MonthlyEntryProjection>>([]));

        _preferencesService = Substitute.For<IPreferencesService>();
        _preferencesService.Get(Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        _vm = new MainViewModel(_service, _preferencesService);
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

    // --- Preferences: initial expanded state ---

    [Fact]
    public async Task InitializeAsync_SetsIsIncomesExpandedFromPreferences()
    {
        _preferencesService.Get("incomes_expanded", true).Returns(false);
        var vm = new MainViewModel(_service, _preferencesService);

        await vm.InitializeAsync();

        vm.Months.Should().AllSatisfy(m => m.IsIncomesExpanded.Should().BeFalse());
    }

    [Fact]
    public async Task InitializeAsync_SetsIsOutcomesExpandedFromPreferences()
    {
        _preferencesService.Get("outcomes_expanded", true).Returns(false);
        var vm = new MainViewModel(_service, _preferencesService);

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
            Category: string.Empty,
            Status: FinancialMonthlyEntryStatus.Pending,
            IsProjected: true,
            InstallmentLabel: null));

        await _vm.ClearDatabaseAsync();

        _vm.Months[0].Entries.Should().BeEmpty();
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
}
