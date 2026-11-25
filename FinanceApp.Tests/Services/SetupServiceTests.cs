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
        var links = await GetCategoryLinksAsync();
        int? CategoryOf(string description) =>
            links.SingleOrDefault(l => l.FinancialEntryId == entries.Single(e => e.Description == description).Id)?.FinancialCategoryId;
        CategoryOf("Salário").Should().Be(categories.Single(c => c.Name == "Salário").Id);
        CategoryOf("Aluguel").Should().Be(categories.Single(c => c.Name == "Moradia").Id);
        CategoryOf("Mercado").Should().BeNull();
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

    // --- Running the wizard again ---

    [Fact]
    public async Task CompleteAsync_Entries_AreLinkedToTheirWizardRows()
    {
        await _service.CompleteAsync(new NewUserSetup
        {
            Entries =
            [
                new SetupEntry { Description = "Salário", Amount = 5000, EntryType = FinancialEntryType.Income, Row = SetupRows.Salary },
                new SetupEntry { Description = "Financiamento (Casa)", Amount = 2000, Row = SetupRows.HomeFinancing, Group = "g1", GroupName = "Casa" }
            ]
        });

        var previous = await _service.GetPreviousSetupAsync();

        previous.Select(p => (p.Link.Row, p.Link.Group, p.Link.GroupName, p.Entry.Description)).Should().Equal(
            (SetupRows.Salary, "", "", "Salário"),
            (SetupRows.HomeFinancing, "g1", "Casa", "Financiamento (Casa)"));
    }

    [Fact]
    public async Task CompleteAsync_ExistingEntryWithANewAmount_UpdatesItFromThisMonthOnAndKeepsItsName()
    {
        var rent = await SeedLinkedEntryAsync(new FinancialEntry
        {
            Description = "Aluguel do apê", Amount = 1500, StartDate = new DateTime(2026, 9, 1), Recurrence = RecurrenceType.Recurrent
        }, SetupRows.Expense("Aluguel"));

        await _service.CompleteAsync(new NewUserSetup
        {
            Entries = [new SetupEntry { Description = "Aluguel", Amount = 1600, DueDay = 10, Row = SetupRows.Expense("Aluguel"), ExistingEntryId = rent.Id }]
        });

        var entry = (await GetEntriesAsync()).Should().ContainSingle().Subject;
        (entry.Description, entry.Amount, entry.DueDay, entry.StartDate).Should().Be(("Aluguel do apê", 1600m, (int?)10, new DateTime(2026, 9, 1)));
        (await _financeService.GetMergedEntriesForMonthAsync(2026, 10)).Single().EffectiveAmount.Should().Be(1500m);
        (await _financeService.GetMergedEntriesForMonthAsync(2026, 11)).Single().EffectiveAmount.Should().Be(1600m);
        (await _service.GetPreviousSetupAsync()).Should().ContainSingle(p => p.Entry.Id == rent.Id);
    }

    [Fact]
    public async Task CompleteAsync_ExistingEntry_KeepsItsCategories()
    {
        await _categoryService.SaveAsync(new FinancialCategory { Name = "Moradia", EntryType = FinancialEntryType.Outcome });
        await _categoryService.SaveAsync(new FinancialCategory { Name = "Lazer", EntryType = FinancialEntryType.Outcome });
        var rent = await SeedLinkedEntryAsync(new FinancialEntry
        {
            Description = "Aluguel", Amount = 1500, StartDate = new DateTime(2026, 9, 1), Recurrence = RecurrenceType.Recurrent
        }, SetupRows.Expense("Aluguel"));
        var leisure = (await _categoryService.GetAllAsync()).First(c => c.Name == "Lazer");
        await _financeService.SaveEntryAsync(rent, [leisure.Id]);

        await _service.CompleteAsync(new NewUserSetup
        {
            Entries = [new SetupEntry { Description = "Aluguel", Amount = 1500, CategoryName = "Moradia", ExistingEntryId = rent.Id }]
        });

        (await _financeService.GetEntryCategoryIdsAsync(rent.Id)).Should().Equal(leisure.Id);
    }

    [Fact]
    public async Task CompleteAsync_PurchaseRenamedInTheWizard_TakesTheNewName()
    {
        var purchase = await SeedLinkedEntryAsync(new FinancialEntry
        {
            Description = "Celular", Amount = 300, StartDate = new DateTime(2026, 9, 1), Recurrence = RecurrenceType.Installments, TotalInstallments = 10
        }, SetupRows.Purchase);

        await _service.CompleteAsync(new NewUserSetup
        {
            Entries =
            [
                new SetupEntry
                {
                    Description = "iPhone", Amount = 300, Recurrence = RecurrenceType.Installments, TotalInstallments = 10, CurrentInstallment = 3,
                    Row = SetupRows.Purchase, ExistingEntryId = purchase.Id
                }
            ]
        });

        var entry = (await GetEntriesAsync()).Single();
        (entry.Description, entry.StartDate).Should().Be(("iPhone", new DateTime(2026, 9, 1)));
    }

    [Fact]
    public async Task CompleteAsync_ExistingAnnualEntryInTheSameMonths_KeepsItsStart()
    {
        var ipva = await SeedLinkedEntryAsync(new FinancialEntry
        {
            Description = "IPVA (Carro)", Amount = 1000, StartDate = new DateTime(2026, 1, 1), Recurrence = RecurrenceType.Annual, TotalInstallments = 3
        }, SetupRows.CarIpva);

        await _service.CompleteAsync(new NewUserSetup
        {
            Entries = [AnnualIpva(startMonth: 1, ipva.Id)]
        });

        (await GetEntriesAsync()).Single().StartDate.Should().Be(new DateTime(2026, 1, 1));
    }

    [Fact]
    public async Task CompleteAsync_ExistingAnnualEntryMovedToOtherMonths_EndsItAndStartsANewOneKeepingThePast()
    {
        var ipva = await SeedLinkedEntryAsync(new FinancialEntry
        {
            Description = "IPVA (Carro)", Amount = 1000, StartDate = new DateTime(2026, 1, 1), Recurrence = RecurrenceType.Annual, TotalInstallments = 3
        }, SetupRows.CarIpva);
        await _financeService.SetEntryStatusAsync(ipva.Id, 2026, 2, FinancialMonthlyEntryStatus.Paid);

        await _service.CompleteAsync(new NewUserSetup
        {
            Entries = [AnnualIpva(startMonth: 3, ipva.Id)]
        });

        var entries = await GetEntriesAsync();
        entries.Select(e => (e.Id == ipva.Id, e.StartDate, e.EndDate)).Should().BeEquivalentTo(
        [
            (true, new DateTime(2026, 1, 1), (DateTime?)new DateTime(2026, 10, 1)),
            (false, new DateTime(2027, 3, 1), (DateTime?)null)
        ]);
        (await _financeService.GetMergedEntriesForMonthAsync(2026, 2)).Single().IsPaid.Should().BeTrue("the paid installment stays");
        (await _service.GetPreviousSetupAsync()).Single().Entry.StartDate.Should().Be(new DateTime(2027, 3, 1));
    }

    [Fact]
    public async Task CompleteAsync_MonthlyInsuranceNowYearly_KeepsThePastMonthsOfTheMonthlyOne()
    {
        var categories = await SaveCategoryAsync("Transporte");
        var insurance = await SeedLinkedEntryAsync(new FinancialEntry
        {
            Description = "Seguro do carro", Amount = 200, StartDate = new DateTime(2026, 1, 1), Recurrence = RecurrenceType.Recurrent
        }, SetupRows.Expense("Seguro do carro"));
        await _financeService.SaveEntryAsync(insurance, [categories]);

        await _service.CompleteAsync(new NewUserSetup
        {
            Entries =
            [
                new SetupEntry
                {
                    Description = "Seguro do carro", Amount = 2400, Recurrence = RecurrenceType.Annual, AnnualStartMonth = 3, TotalInstallments = 1,
                    Row = SetupRows.Expense("Seguro do carro"), ExistingEntryId = insurance.Id
                }
            ]
        });

        (await _financeService.GetMergedEntriesForMonthAsync(2026, 10)).Single().EffectiveAmount.Should().Be(200m);
        (await _financeService.GetMergedEntriesForMonthAsync(2026, 11)).Should().BeEmpty();
        var yearly = (await _financeService.GetMergedEntriesForMonthAsync(2027, 3)).Single();
        (yearly.EffectiveAmount, yearly.Category!.Id).Should().Be((2400m, categories));
    }

    [Fact]
    public async Task CompleteAsync_FinancingGivenInstallmentsFromThePast_DoesNotRepeatTheMonthsItHadAlready()
    {
        var financing = await SeedLinkedEntryAsync(new FinancialEntry
        {
            Description = "Financiamento (Casa)", Amount = 2000, StartDate = new DateTime(2026, 9, 1), Recurrence = RecurrenceType.Recurrent
        }, SetupRows.HomeFinancing);

        await _service.CompleteAsync(new NewUserSetup
        {
            Entries =
            [
                new SetupEntry
                {
                    Description = "Financiamento (Casa)", Amount = 2000, Recurrence = RecurrenceType.Installments, TotalInstallments = 120,
                    CurrentInstallment = 50, Row = SetupRows.HomeFinancing, ExistingEntryId = financing.Id
                }
            ]
        });

        (await _financeService.GetMergedEntriesForMonthAsync(2026, 10)).Select(e => e.InstallmentLabel).Should().Equal((string?)null);
        (await _financeService.GetMergedEntriesForMonthAsync(2026, 11)).Single().InstallmentLabel.Should().Be("50/120");
    }

    private async Task<int> SaveCategoryAsync(string name)
    {
        var category = new FinancialCategory { Name = name, EntryType = FinancialEntryType.Outcome };
        await _categoryService.SaveAsync(category);
        return category.Id;
    }

    private static SetupEntry AnnualIpva(int startMonth, int existingEntryId) => new()
    {
        Description = "IPVA (Carro)", Amount = 1000, Recurrence = RecurrenceType.Annual, AnnualStartMonth = startMonth, TotalInstallments = 3,
        Row = SetupRows.CarIpva, ExistingEntryId = existingEntryId
    };

    [Fact]
    public async Task CompleteAsync_EntryToRemoveFromThisMonth_EndsItLastMonthAndForgetsTheRow()
    {
        var rent = await SeedLinkedEntryAsync(new FinancialEntry
        {
            Description = "Aluguel", Amount = 1500, StartDate = new DateTime(2026, 9, 1), Recurrence = RecurrenceType.Recurrent
        }, SetupRows.Expense("Aluguel"));

        await _service.CompleteAsync(new NewUserSetup { EntriesToRemove = [rent.Id], RemoveScope = EntryDeleteScope.ThisAndFuture });

        (await GetEntriesAsync()).Single().EndDate.Should().Be(new DateTime(2026, 10, 1));
        (await _financeService.GetMergedEntriesForMonthAsync(2026, 10)).Should().ContainSingle();
        (await _financeService.GetMergedEntriesForMonthAsync(2026, 11)).Should().BeEmpty();
        (await GetSetupLinksAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task CompleteAsync_EntryToRemoveFromAllMonths_DeletesIt()
    {
        var rent = await SeedLinkedEntryAsync(new FinancialEntry
        {
            Description = "Aluguel", Amount = 1500, StartDate = new DateTime(2026, 9, 1), Recurrence = RecurrenceType.Recurrent
        }, SetupRows.Expense("Aluguel"));

        await _service.CompleteAsync(new NewUserSetup { EntriesToRemove = [rent.Id], RemoveScope = EntryDeleteScope.All });

        (await GetEntriesAsync()).Should().BeEmpty();
        (await GetSetupLinksAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task GetPreviousSetupAsync_EndedOrDeletedEntries_LeavesThemOut()
    {
        await SeedLinkedEntryAsync(new FinancialEntry
        {
            Description = "Notebook", Amount = 300, StartDate = new DateTime(2026, 1, 1), Recurrence = RecurrenceType.Installments, TotalInstallments = 10
        }, SetupRows.Purchase);
        await SeedLinkedEntryAsync(new FinancialEntry
        {
            Description = "Tênis", Amount = 300, StartDate = new DateTime(2026, 10, 1), Recurrence = RecurrenceType.OneTime
        }, SetupRows.Purchase);
        await SeedLinkedEntryAsync(new FinancialEntry
        {
            Description = "Academia", Amount = 100, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 10, 1), Recurrence = RecurrenceType.Recurrent
        }, SetupRows.Expense("Academia"));
        var deleted = await SeedLinkedEntryAsync(new FinancialEntry
        {
            Description = "Internet", Amount = 100, StartDate = new DateTime(2026, 1, 1), Recurrence = RecurrenceType.Recurrent
        }, SetupRows.Expense("Internet"));
        await (await _context.GetConnectionAsync()).DeleteAsync(deleted);
        await SeedLinkedEntryAsync(new FinancialEntry
        {
            Description = "Celular", Amount = 300, StartDate = new DateTime(2026, 3, 1), Recurrence = RecurrenceType.Installments, TotalInstallments = 10
        }, SetupRows.Purchase);

        var previous = await _service.GetPreviousSetupAsync();

        previous.Select(p => p.Entry.Description).Should().Equal("Celular");
    }

    private async Task<FinancialEntry> SeedLinkedEntryAsync(FinancialEntry entry, string row)
    {
        var db = await _context.GetConnectionAsync();
        await db.InsertAsync(entry);
        await db.InsertAsync(new SetupLink { Row = row, FinancialEntryId = entry.Id });
        return entry;
    }

    private async Task<List<SetupLink>> GetSetupLinksAsync()
    {
        var db = await _context.GetConnectionAsync();
        return await db.Table<SetupLink>().ToListAsync();
    }

    private async Task<List<FinancialEntry>> GetEntriesAsync()
    {
        var db = await _context.GetConnectionAsync();
        return await db.Table<FinancialEntry>().ToListAsync();
    }

    private async Task<List<FinancialCategoryEntry>> GetCategoryLinksAsync()
    {
        var db = await _context.GetConnectionAsync();
        return await db.Table<FinancialCategoryEntry>().ToListAsync();
    }
}
