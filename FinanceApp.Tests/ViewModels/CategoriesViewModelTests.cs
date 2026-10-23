using CommunityToolkit.Mvvm.Messaging;
using FinanceApp.Messages;
using FinanceApp.Models;
using FinanceApp.Services;
using FinanceApp.ViewModels;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinanceApp.Tests.ViewModels;

public class CategoriesViewModelTests
{
    private readonly ICategoryService _service;
    private readonly IMessenger _messenger = new WeakReferenceMessenger();
    private readonly List<CategoriesChangedMessage> _sentMessages = [];
    private readonly CategoriesViewModel _vm;

    private static readonly FinancialCategory Housing = new() { Id = 1, Name = "Moradia" };
    private static readonly FinancialCategory Leisure = new() { Id = 2, Name = "Lazer" };
    private static readonly FinancialCategory Salary = new() { Id = 3, Name = "Salário", EntryType = FinancialEntryType.Income };

    public CategoriesViewModelTests()
    {
        _service = Substitute.For<ICategoryService>();
        _service.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<FinancialCategory>>([Housing, Leisure]));
        _messenger.Register<CategoriesChangedMessage>(this, (_, m) => _sentMessages.Add(m));
        _vm = new CategoriesViewModel(_service, _messenger);
    }

    // --- LoadAsync ---

    [Fact]
    public async Task LoadAsync_PopulatesCategoriesFromService()
    {
        await _vm.LoadAsync();

        _vm.Groups.SelectMany(g => g).Should().Equal(Housing, Leisure);
    }

    [Fact]
    public async Task LoadAsync_CalledTwice_DoesNotDuplicateCategories()
    {
        await _vm.LoadAsync();
        await _vm.LoadAsync();

        _vm.Groups.SelectMany(g => g).Should().HaveCount(2);
    }

    [Fact]
    public async Task LoadAsync_MixedTypes_GroupsOutcomesBeforeIncomes()
    {
        _service.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<FinancialCategory>>([Salary, Housing, Leisure]));

        await _vm.LoadAsync();

        _vm.Groups.Select(g => g.EntryType).Should().Equal(FinancialEntryType.Outcome, FinancialEntryType.Income);
        _vm.Groups.Select(g => g.Title).Should().Equal("Despesas", "Receitas");
        _vm.Groups[0].Should().Equal(Housing, Leisure);
        _vm.Groups[1].Should().Equal(Salary);
    }

    [Fact]
    public async Task LoadAsync_OnlyOutcomeCategories_OmitsIncomeGroup()
    {
        await _vm.LoadAsync();

        _vm.Groups.Should().ContainSingle().Which.EntryType.Should().Be(FinancialEntryType.Outcome);
    }

    [Fact]
    public async Task IsEmpty_ReflectsCategoriesCollection()
    {
        _vm.IsEmpty.Should().BeTrue();

        await _vm.LoadAsync();

        _vm.IsEmpty.Should().BeFalse();
    }

    // --- CountEntriesAsync ---

    [Fact]
    public async Task CountEntriesAsync_DelegatesToService()
    {
        _service.CountEntriesAsync(Housing.Id).Returns(3);

        var count = await _vm.CountEntriesAsync(Housing);

        count.Should().Be(3);
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_CallsServiceAndRemovesFromList()
    {
        await _vm.LoadAsync();

        await _vm.DeleteAsync(Housing);

        await _service.Received(1).DeleteAsync(Housing.Id);
        _vm.Groups.SelectMany(g => g).Should().Equal(Leisure);
    }

    [Fact]
    public async Task DeleteAsync_LastCategoryOfType_RemovesItsGroup()
    {
        _service.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<FinancialCategory>>([Housing, Salary]));
        await _vm.LoadAsync();

        await _vm.DeleteAsync(Salary);

        _vm.Groups.Should().ContainSingle().Which.EntryType.Should().Be(FinancialEntryType.Outcome);
    }

    [Fact]
    public async Task DeleteAsync_SendsCategoriesChangedMessage()
    {
        await _vm.LoadAsync();

        await _vm.DeleteAsync(Housing);

        _sentMessages.Should().ContainSingle();
    }

    // --- OnCategorySavedAsync ---

    [Fact]
    public async Task OnCategorySavedAsync_ReloadsCategories()
    {
        await _vm.OnCategorySavedAsync();

        await _service.Received(1).GetAllAsync();
        _vm.Groups.SelectMany(g => g).Should().HaveCount(2);
    }

    [Fact]
    public async Task OnCategorySavedAsync_SendsCategoriesChangedMessage()
    {
        await _vm.OnCategorySavedAsync();

        _sentMessages.Should().ContainSingle();
    }
}
