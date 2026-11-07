using FinanceApp.Models;
using FinanceApp.Services;
using FinanceApp.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace FinanceApp.Tests.Services;

public class CategoryServiceTests
{
    private readonly TestDatabaseContext _context;
    private readonly CategoryService _service;
    private readonly FinanceService _financeService;

    public CategoryServiceTests()
    {
        _context = new TestDatabaseContext();
        _service = new CategoryService(_context);
        _financeService = new FinanceService(_context, _service, new PaymentSourceService(_context));
    }

    // --- GetAllAsync ---

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var result = await _service.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_MultipleCategories_ReturnsOrderedByName()
    {
        await _service.SaveAsync(new FinancialCategory { Name = "Transporte" });
        await _service.SaveAsync(new FinancialCategory { Name = "alimentação" });
        await _service.SaveAsync(new FinancialCategory { Name = "Lazer" });

        var result = await _service.GetAllAsync();

        result.Select(c => c.Name).Should().Equal("alimentação", "Lazer", "Transporte");
    }
    [Fact]
    public async Task GetAllAsync_ListLoadedBefore_ReflectsSavesAndDeletes()
    {
        await _service.GetAllAsync();
        var category = new FinancialCategory { Name = "Lazer" };

        await _service.SaveAsync(category);
        (await _service.GetAllAsync()).Select(c => c.Name).Should().Equal("Lazer");

        await _service.SaveAsync(new FinancialCategory { Id = category.Id, Name = "Viagens" });
        (await _service.GetAllAsync()).Select(c => c.Name).Should().Equal("Viagens");

        await _service.DeleteAsync(category.Id);
        (await _service.GetAllAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_RowWrittenElsewhere_ShowsOnlyAfterClearCache()
    {
        await _service.GetAllAsync();
        var db = await _context.GetConnectionAsync();
        await db.InsertAsync(new FinancialCategory { Name = "Lazer" });

        (await _service.GetAllAsync()).Should().BeEmpty();
        _service.ClearCache();
        (await _service.GetAllAsync()).Select(c => c.Name).Should().Equal("Lazer");
    }

    [Fact]
    public async Task GetAllAsync_ConcurrentCalls_ShareOneRead()
    {
        var first = _service.GetAllAsync();
        var second = _service.GetAllAsync();

        (await first).Should().BeSameAs(await second);
    }


    // --- SaveAsync ---

    [Fact]
    public async Task SaveAsync_NewCategory_AssignsIdAndPersistsAllFields()
    {
        var category = new FinancialCategory { Name = "Mercado", Icon = "🛒", Color = "#43A047" };

        await _service.SaveAsync(category);

        category.Id.Should().NotBe(0);
        var stored = (await _service.GetAllAsync()).Single();
        stored.Name.Should().Be("Mercado");
        stored.Icon.Should().Be("🛒");
        stored.Color.Should().Be("#43A047");
    }

    [Fact]
    public async Task SaveAsync_ExistingCategory_UpdatesInsteadOfInserting()
    {
        var category = new FinancialCategory { Name = "Mercado", Icon = "🛒", Color = "#43A047" };
        await _service.SaveAsync(category);

        await _service.SaveAsync(new FinancialCategory { Id = category.Id, Name = "Supermercado", Icon = "🍽️", Color = "#E53935" });

        var stored = (await _service.GetAllAsync()).Single();
        stored.Name.Should().Be("Supermercado");
        stored.Icon.Should().Be("🍽️");
        stored.Color.Should().Be("#E53935");
    }

    [Theory]
    [InlineData(FinancialEntryType.Income)]
    [InlineData(FinancialEntryType.Outcome)]
    public async Task SaveAsync_PersistsEntryType(FinancialEntryType entryType)
    {
        await _service.SaveAsync(new FinancialCategory { Name = "Salário", EntryType = entryType });

        (await _service.GetAllAsync()).Single().EntryType.Should().Be(entryType);
    }

    [Fact]
    public async Task SaveAsync_NameWithSurroundingWhitespace_TrimsName()
    {
        await _service.SaveAsync(new FinancialCategory { Name = "  Saúde  " });

        (await _service.GetAllAsync()).Single().Name.Should().Be("Saúde");
    }

    // --- CountEntriesAsync ---

    [Fact]
    public async Task CountEntriesAsync_ReturnsOnlyEntriesLinkedToCategory()
    {
        var housing = new FinancialCategory { Name = "Moradia" };
        var leisure = new FinancialCategory { Name = "Lazer" };
        await _service.SaveAsync(housing);
        await _service.SaveAsync(leisure);
        await _financeService.SaveEntryAsync(MakeEntry("Rent", housing.Id));
        await _financeService.SaveEntryAsync(MakeEntry("Condo fee", housing.Id));
        await _financeService.SaveEntryAsync(MakeEntry("Cinema", leisure.Id));

        var count = await _service.CountEntriesAsync(housing.Id);

        count.Should().Be(2);
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_RemovesCategory()
    {
        var category = new FinancialCategory { Name = "Lazer" };
        await _service.SaveAsync(category);

        await _service.DeleteAsync(category.Id);

        (await _service.GetAllAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_CategoryInUse_KeepsEntriesAndClearsTheirCategory()
    {
        var category = new FinancialCategory { Name = "Lazer" };
        await _service.SaveAsync(category);
        var entry = MakeEntry("Cinema", category.Id);
        await _financeService.SaveEntryAsync(entry);

        await _service.DeleteAsync(category.Id);

        var stored = await _financeService.GetEntryByIdAsync(entry.Id);
        stored.Should().NotBeNull();
        stored!.CategoryId.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_DoesNotAffectEntriesOfOtherCategories()
    {
        var leisure = new FinancialCategory { Name = "Lazer" };
        var housing = new FinancialCategory { Name = "Moradia" };
        await _service.SaveAsync(leisure);
        await _service.SaveAsync(housing);
        var rent = MakeEntry("Rent", housing.Id);
        await _financeService.SaveEntryAsync(rent);

        await _service.DeleteAsync(leisure.Id);

        (await _financeService.GetEntryByIdAsync(rent.Id))!.CategoryId.Should().Be(housing.Id);
    }

    private static FinancialEntry MakeEntry(string description, int? categoryId) => new()
    {
        Description = description,
        Amount = 100m,
        CategoryId = categoryId,
        StartDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Local),
        Recurrence = RecurrenceType.Recurrent,
        EntryType = FinancialEntryType.Outcome
    };
}
