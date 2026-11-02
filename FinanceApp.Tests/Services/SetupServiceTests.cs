using FinanceApp.Models;
using FinanceApp.Services;
using FinanceApp.Tests.Infrastructure;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinanceApp.Tests.Services;

public sealed class SetupServiceTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 2, 10, 0, 0, DateTimeKind.Local);

    private readonly TestDatabaseContext _context = new();
    private readonly InMemoryPreferences _preferences = new();
    private readonly TempFileSystem _fileSystem = new();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly FinanceService _financeService;
    private readonly CategoryService _categoryService;
    private readonly PaymentSourceService _paymentSourceService;
    private readonly UserProfileService _profileService;
    private readonly SetupService _service;

    public SetupServiceTests()
    {
        _timeProvider.LocalTimeZone.Returns(TimeZoneInfo.Local);
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(Now.ToUniversalTime()));

        _financeService = new FinanceService(_context);
        _categoryService = new CategoryService(_context);
        _paymentSourceService = new PaymentSourceService(_context);
        _profileService = new UserProfileService(_preferences, _fileSystem);
        _service = new SetupService(
            _context, _financeService, _categoryService, _paymentSourceService, _profileService, _preferences, _timeProvider);
    }

    public void Dispose() => _fileSystem.Dispose();

    // --- ShouldShowAsync ---

    [Fact]
    public async Task ShouldShowAsync_FirstRun_ReturnsTrue()
    {
        (await _service.ShouldShowAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldShowAsync_Skipped_ReturnsFalse()
    {
        _service.Skip();

        (await _service.ShouldShowAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task ShouldShowAsync_Completed_ReturnsFalse()
    {
        await _service.CompleteAsync(new NewUserSetup());

        (await _service.ShouldShowAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task ShouldShowAsync_ExistingEntries_ReturnsFalseForGood()
    {
        await _financeService.SaveEntryAsync(new FinancialEntry { Description = "Aluguel", Amount = 1000, StartDate = Now });

        (await _service.ShouldShowAsync()).Should().BeFalse();

        await _financeService.ClearAllDataAsync();
        (await _service.ShouldShowAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task ShouldShowAsync_ExistingPaymentSource_ReturnsFalse()
    {
        await _paymentSourceService.SaveAsync(new PaymentSource { Name = "Nubank" });

        (await _service.ShouldShowAsync()).Should().BeFalse();
    }

    // --- CompleteAsync ---

    [Fact]
    public async Task CompleteAsync_SavesProfile()
    {
        await _service.CompleteAsync(new NewUserSetup { Name = "Ana", PhotoPath = TempFileSystem.CreateExternalFile(".jpg", "photo") });

        _profileService.Name.Should().Be("Ana");
        _profileService.PhotoPath.Should().NotBeNull();
    }

    [Fact]
    public async Task CompleteAsync_PaymentSources_AreCreated()
    {
        await _service.CompleteAsync(new NewUserSetup
        {
            PaymentSources =
            [
                new PaymentSource { Name = "Nubank", Type = PaymentSourceType.BankAccount, Balance = 2500 },
                new PaymentSource { Name = "Visa", Type = PaymentSourceType.CreditCard, Balance = 8000 }
            ]
        });

        var sources = await _paymentSourceService.GetAllAsync();
        sources.Select(s => (s.Name, s.Type, s.Balance)).Should().BeEquivalentTo(
        [
            ("Nubank", PaymentSourceType.BankAccount, 2500m),
            ("Visa", PaymentSourceType.CreditCard, 8000m)
        ]);
    }

    [Fact]
    public async Task CompleteAsync_Entries_AreRecurrentFromCurrentMonth()
    {
        await _service.CompleteAsync(new NewUserSetup
        {
            Entries =
            [
                new SetupEntry { Description = "Salário", Amount = 5000, EntryType = FinancialEntryType.Income, DueDay = 5 },
                new SetupEntry { Description = "Energia", Amount = 180, IsEstimated = true, DueDay = 10 },
                new SetupEntry { Description = "Mercado", Amount = 1200, IsEstimated = true, AllowsPartialPayment = true }
            ]
        });

        var entries = await GetEntriesAsync();
        entries.Should().HaveCount(3).And.OnlyContain(e =>
            e.Recurrence == RecurrenceType.Recurrent && e.StartDate == new DateTime(2026, 11, 1) && e.EndDate == null);
        entries.Select(e => (e.Description, e.Amount, e.EntryType, e.DueDay, e.IsEstimated, e.AllowsPartialPayment))
            .Should().BeEquivalentTo(
            [
                ("Salário", 5000m, FinancialEntryType.Income, (int?)5, false, false),
                ("Energia", 180m, FinancialEntryType.Outcome, (int?)10, true, false),
                ("Mercado", 1200m, FinancialEntryType.Outcome, (int?)null, true, true)
            ]);
    }

    [Fact]
    public async Task CompleteAsync_Entries_AppearFromCurrentMonthOnwards()
    {
        await _service.CompleteAsync(new NewUserSetup
        {
            Entries = [new SetupEntry { Description = "Aluguel", Amount = 1500 }]
        });

        (await _financeService.GetMergedEntriesForMonthAsync(2026, 10)).Should().BeEmpty();
        (await _financeService.GetMergedEntriesForMonthAsync(2027, 3)).Should().ContainSingle(p => p.Description == "Aluguel");
    }

    [Fact]
    public async Task CompleteAsync_ZeroAmountEntry_IsNotCreated()
    {
        await _service.CompleteAsync(new NewUserSetup
        {
            Entries = [new SetupEntry { Description = "Condomínio", Amount = 0 }]
        });

        (await GetEntriesAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task CompleteAsync_CategoryName_LinksCategoryOfSameType()
    {
        await _categoryService.SaveAsync(new FinancialCategory { Name = "Salário", EntryType = FinancialEntryType.Income });
        await _categoryService.SaveAsync(new FinancialCategory { Name = "Moradia", EntryType = FinancialEntryType.Outcome });

        await _service.CompleteAsync(new NewUserSetup
        {
            Entries =
            [
                new SetupEntry { Description = "Salário", Amount = 5000, EntryType = FinancialEntryType.Income, CategoryName = "Salário" },
                new SetupEntry { Description = "Aluguel", Amount = 1500, CategoryName = "Moradia" },
                new SetupEntry { Description = "Mercado", Amount = 1000, CategoryName = "Salário" }
            ]
        });

        var categories = await _categoryService.GetAllAsync();
        var entries = await GetEntriesAsync();
        entries.Single(e => e.Description == "Salário").CategoryId.Should().Be(categories.Single(c => c.Name == "Salário").Id);
        entries.Single(e => e.Description == "Aluguel").CategoryId.Should().Be(categories.Single(c => c.Name == "Moradia").Id);
        entries.Single(e => e.Description == "Mercado").CategoryId.Should().BeNull();
    }

    [Fact]
    public async Task CompleteAsync_EntryWithNewSource_LinksSavedSource()
    {
        var nubank = new PaymentSource { Name = "Nubank" };

        await _service.CompleteAsync(new NewUserSetup
        {
            PaymentSources = [nubank],
            Entries = [new SetupEntry { Description = "Aluguel", Amount = 1500, PaymentSource = nubank }]
        });

        var saved = (await _paymentSourceService.GetAllAsync()).Single();
        (await GetEntriesAsync()).Single().PaymentSourceId.Should().Be(saved.Id);
    }

    [Fact]
    public async Task CompleteAsync_RunAgain_KeepsExistingData()
    {
        await _service.CompleteAsync(new NewUserSetup
        {
            PaymentSources = [new PaymentSource { Name = "Nubank" }],
            Entries = [new SetupEntry { Description = "Aluguel", Amount = 1500 }]
        });

        await _service.CompleteAsync(new NewUserSetup
        {
            PaymentSources = [new PaymentSource { Name = "Itaú" }],
            Entries = [new SetupEntry { Description = "Internet", Amount = 100 }]
        });

        (await _paymentSourceService.GetAllAsync()).Select(s => s.Name).Should().BeEquivalentTo(["Itaú", "Nubank"]);
        (await GetEntriesAsync()).Select(e => e.Description).Should().BeEquivalentTo(["Aluguel", "Internet"]);
    }

    private async Task<List<FinancialEntry>> GetEntriesAsync()
    {
        var db = await _context.GetConnectionAsync();
        return await db.Table<FinancialEntry>().ToListAsync();
    }
}
