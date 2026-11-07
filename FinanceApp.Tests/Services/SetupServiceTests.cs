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

        _categoryService = new CategoryService(_context);
        _paymentSourceService = new PaymentSourceService(_context);
        _financeService = new FinanceService(_context, _categoryService, _paymentSourceService);
        _profileService = new UserProfileService(_preferences, _fileSystem);
        _service = new SetupService(
            _context, _categoryService, _paymentSourceService, _profileService, _preferences, _timeProvider);
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
    public async Task CompleteAsync_SourcesListedBefore_ListsTheNewSources()
    {
        await _paymentSourceService.GetAllAsync();

        await _service.CompleteAsync(new NewUserSetup
        {
            PaymentSources = [new PaymentSource { Name = "Nubank", Type = PaymentSourceType.BankAccount }]
        });

        (await _paymentSourceService.GetAllAsync()).Select(s => s.Name).Should().Equal("Nubank");
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
    public async Task CompleteAsync_CreditCards_AreCreatedWithTheirBillAccount()
    {
        var itau = new PaymentSource { Name = "Itaú", Type = PaymentSourceType.BankAccount };
        var existing = new PaymentSource { Name = "Bradesco", Type = PaymentSourceType.BankAccount };
        await _paymentSourceService.SaveAsync(existing);

        await _service.CompleteAsync(new NewUserSetup
        {
            PaymentSources = [itau],
            CreditCards =
            [
                new SetupCreditCard { Card = new PaymentSource { Name = "Nubank", Balance = 8000, BillDueDay = 10 }, BillAccount = itau },
                new SetupCreditCard { Card = new PaymentSource { Name = "Visa", Balance = 3000 }, BillAccount = existing },
                new SetupCreditCard { Card = new PaymentSource { Name = "Elo" } }
            ]
        });

        var sources = await _paymentSourceService.GetAllAsync();
        var itauId = sources.Single(s => s.Name == "Itaú").Id;
        sources.Where(s => s.Type == PaymentSourceType.CreditCard)
            .Select(s => (s.Name, s.Balance, s.BillDueDay, s.BillPaymentSourceId))
            .Should().BeEquivalentTo(
            [
                ("Nubank", 8000m, (int?)10, (int?)itauId),
                ("Visa", 3000m, (int?)null, (int?)existing.Id),
                ("Elo", 0m, (int?)null, (int?)null)
            ]);
    }

    [Fact]
    public async Task CompleteAsync_InstallmentPurchase_StartsSoCurrentInstallmentFallsThisMonth()
    {
        var card = new PaymentSource { Name = "Nubank" };

        await _service.CompleteAsync(new NewUserSetup
        {
            CreditCards = [new SetupCreditCard { Card = card }],
            Entries =
            [
                new SetupEntry
                {
                    Description = "Celular", Amount = 250, Recurrence = RecurrenceType.Installments,
                    TotalInstallments = 10, CurrentInstallment = 3, PaymentSource = card
                }
            ]
        });

        var entry = (await GetEntriesAsync()).Single();
        (entry.Recurrence, entry.TotalInstallments, entry.StartDate).Should()
            .Be((RecurrenceType.Installments, (int?)10, new DateTime(2026, 9, 1)));
        (await _financeService.GetMergedEntriesForMonthAsync(2026, 11)).Single().InstallmentLabel.Should().Be("3/10");
        (await _financeService.GetMergedEntriesForMonthAsync(2027, 6)).Single().InstallmentLabel.Should().Be("10/10");
        (await _financeService.GetMergedEntriesForMonthAsync(2027, 7)).Should().BeEmpty();
    }

    [Fact]
    public async Task CompleteAsync_InstallmentPurchaseWithoutCurrentInstallment_StartsThisMonth()
    {
        await _service.CompleteAsync(new NewUserSetup
        {
            Entries = [new SetupEntry { Description = "Sofá", Amount = 300, Recurrence = RecurrenceType.Installments, TotalInstallments = 5 }]
        });

        (await GetEntriesAsync()).Single().StartDate.Should().Be(new DateTime(2026, 11, 1));
    }

    [Theory]
    [InlineData(1, 1, 2027)]
    [InlineData(1, 3, 2027)]
    [InlineData(12, 1, 2026)]
    [InlineData(11, 1, 2026)]
    [InlineData(2, 10, 2026)]
    public async Task CompleteAsync_AnnualEntry_StartsThisYearWhileItsInstallmentsAreStillToCome(int startMonth, int installments, int expectedYear)
    {
        await _service.CompleteAsync(new NewUserSetup
        {
            Entries =
            [
                new SetupEntry
                {
                    Description = "IPVA", Amount = 800, Recurrence = RecurrenceType.Annual,
                    TotalInstallments = installments, AnnualStartMonth = startMonth
                }
            ]
        });

        var entry = (await GetEntriesAsync()).Single();
        (entry.Recurrence, entry.StartDate).Should().Be((RecurrenceType.Annual, new DateTime(expectedYear, startMonth, 1)));
        entry.TotalInstallments.Should().Be(installments > 1 ? installments : null);
    }

    [Fact]
    public async Task CompleteAsync_AnnualEntryInInstallments_FallsInItsMonthsEveryYear()
    {
        await _service.CompleteAsync(new NewUserSetup
        {
            Entries =
            [
                new SetupEntry
                {
                    Description = "IPVA", Amount = 800, Recurrence = RecurrenceType.Annual,
                    TotalInstallments = 3, AnnualStartMonth = 1
                }
            ]
        });

        (await _financeService.GetMergedEntriesForMonthAsync(2026, 11)).Should().BeEmpty();
        (await _financeService.GetMergedEntriesForMonthAsync(2027, 2)).Single().InstallmentLabel.Should().Be("2/3");
        (await _financeService.GetMergedEntriesForMonthAsync(2027, 4)).Should().BeEmpty();
        (await _financeService.GetMergedEntriesForMonthAsync(2028, 3)).Single().InstallmentLabel.Should().Be("3/3");
    }

    [Fact]
    public async Task CompleteAsync_OneTimePurchase_OnlyInCurrentMonth()
    {
        await _service.CompleteAsync(new NewUserSetup
        {
            Entries = [new SetupEntry { Description = "Tênis", Amount = 300, Recurrence = RecurrenceType.OneTime }]
        });

        (await _financeService.GetMergedEntriesForMonthAsync(2026, 11)).Should().ContainSingle(p => p.Description == "Tênis");
        (await _financeService.GetMergedEntriesForMonthAsync(2026, 12)).Should().BeEmpty();
    }

    [Fact]
    public async Task CompleteAsync_OutcomePaidWithCreditCard_IsNeitherEstimatedNorPartiallyPaid()
    {
        var card = new PaymentSource { Name = "Nubank" };

        await _service.CompleteAsync(new NewUserSetup
        {
            CreditCards = [new SetupCreditCard { Card = card }],
            Entries = [new SetupEntry { Description = "Mercado", Amount = 1000, IsEstimated = true, AllowsPartialPayment = true, PaymentSource = card }]
        });

        var entry = (await GetEntriesAsync()).Single();
        (entry.IsEstimated, entry.AllowsPartialPayment).Should().Be((false, false));
        entry.PaymentSourceId.Should().Be((await _paymentSourceService.GetAllAsync()).Single().Id);
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

    [Fact]
    public async Task CompleteAsync_FailsHalfway_ThrowsAndSavesNothing()
    {
        var db = await _context.GetConnectionAsync();
        await db.ExecuteAsync("CREATE TRIGGER FailWrite BEFORE INSERT ON FinancialEntry BEGIN SELECT RAISE(ABORT, 'write failed'); END");

        var act = () => _service.CompleteAsync(new NewUserSetup
        {
            Name = "Ana",
            PaymentSources = [new PaymentSource { Name = "Nubank", Type = PaymentSourceType.BankAccount, Balance = 2500 }],
            Entries = [new SetupEntry { Description = "Aluguel", Amount = 1500 }]
        });

        await act.Should().ThrowAsync<SQLite.SQLiteException>();
        (await _paymentSourceService.GetAllAsync()).Should().BeEmpty();
        _profileService.Name.Should().BeEmpty();
        (await _service.ShouldShowAsync()).Should().BeTrue();
    }

    private async Task<List<FinancialEntry>> GetEntriesAsync()
    {
        var db = await _context.GetConnectionAsync();
        return await db.Table<FinancialEntry>().ToListAsync();
    }
}
