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
    private readonly MainViewModel _vm;

    public MainViewModelTests()
    {
        _service = Substitute.For<IFinanceService>();
        _service.GetMergedEntriesForMonthAsync(Arg.Any<int>(), Arg.Any<int>())
            .Returns(Task.FromResult<IEnumerable<MonthlyEntryProjection>>([]));
        _vm = new MainViewModel(_service);
    }

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
