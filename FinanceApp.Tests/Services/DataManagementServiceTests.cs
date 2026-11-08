using System.Text;
using FinanceApp.Data;
using FinanceApp.Models;
using FinanceApp.Services;
using FinanceApp.Tests.Infrastructure;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinanceApp.Tests.Services;

public sealed class DataManagementServiceTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Local);

    private readonly Phone _phone = new();
    private readonly Phone _newPhone = new();

    public void Dispose()
    {
        _phone.Dispose();
        _newPhone.Dispose();
    }

    // --- DeleteAllAsync ---

    [Fact]
    public async Task DeleteAllAsync_WithData_RemovesEveryRowAndRestoresDefaultCategories()
    {
        await _phone.SeedAsync();

        await _phone.Service.DeleteAllAsync();

        var db = await _phone.Context.GetConnectionAsync();
        (await db.Table<FinancialEntry>().CountAsync()).Should().Be(0);
        (await db.Table<FinancialMonthlyEntry>().CountAsync()).Should().Be(0);
        (await db.Table<FinancialPayment>().CountAsync()).Should().Be(0);
        (await db.Table<PaymentSource>().CountAsync()).Should().Be(0);
        (await db.Table<FinancialCategory>().ToListAsync()).Select(c => c.Name)
            .Should().Equal(DatabaseContext.CreateDefaultCategories().Select(c => c.Name));
    }

    [Fact]
    public async Task DeleteAllAsync_ProfileAndPin_AreRemoved()
    {
        await _phone.SeedAsync();
        var photoPath = _phone.Profile.PhotoPath;

        await _phone.Service.DeleteAllAsync();

        _phone.Profile.Name.Should().BeEmpty();
        _phone.Profile.PhotoPath.Should().BeNull();
        File.Exists(photoPath).Should().BeFalse();
        (await _phone.AppLock.IsPinEnabledAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAllAsync_SetupWasDone_ShowsSetupAgain()
    {
        await _phone.SeedAsync();
        _phone.Setup.Skip();

        await _phone.Service.DeleteAllAsync();

        (await _phone.Setup.ShouldShowAsync()).Should().BeTrue();
    }
    [Fact]
    public async Task DeleteAllAsync_ListsLoadedBefore_ShowOnlyTheDefaultCategories()
    {
        await _phone.SeedAsync();
        await _phone.Categories.GetAllAsync();
        await _phone.PaymentSources.GetAllAsync();

        await _phone.Service.DeleteAllAsync();

        (await _phone.Categories.GetAllAsync()).Should().HaveCount(DatabaseContext.CreateDefaultCategories().Count);
        (await _phone.PaymentSources.GetAllAsync()).Should().BeEmpty();
    }


    // --- ExportAsync ---

    [Fact]
    public async Task ExportAsync_WritesBackupAndCsvNamedAfterTheDate()
    {
        var files = await _phone.Service.ExportAsync();

        files.Select(Path.GetFileName).Should().Equal("mensal-backup-2026-11-07.json", "mensal-lancamentos-2026-11-07.csv");
        files.Should().OnlyContain(f => File.Exists(f) && f.StartsWith(_phone.FileSystem.CacheDirectory));
    }

    [Fact]
    public async Task ExportAsync_Csv_HasOneRowPerEntryWithNamesAndBrazilianFormat()
    {
        await _phone.SeedAsync();

        var csv = await File.ReadAllTextAsync((await _phone.Service.ExportAsync())[1], Encoding.UTF8);

        csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Should().Equal(
            "Tipo;Descrição;Valor;Recorrência;Início;Fim;Parcelas;Vencimento;Categoria;Conta ou cartão;Valor estimado;Pagamento parcial",
            "Receita;Salário;5000,00;Todo mês;11/2026;;;5;Salário;Itaú;Não;Não",
            "Despesa;Mercado;1234,56;Todo mês;11/2026;;;;Mercado;Itaú;Sim;Sim",
            "Despesa;TV;300,00;Parcelada;11/2026;;10;;;Nubank;Não;Não");
    }

    [Fact]
    public async Task ExportAsync_CsvText_IsQuotedAndNeverRunsAsFormula()
    {
        var db = await _phone.Context.GetConnectionAsync();
        await db.InsertAsync(new FinancialEntry { Description = "Pão; leite \"integral\"", Amount = 10, StartDate = Now.Date, EntryType = FinancialEntryType.Outcome });
        await db.InsertAsync(new FinancialEntry { Description = "=HYPERLINK(\"x\")", Amount = 10, StartDate = Now.Date, EntryType = FinancialEntryType.Outcome });

        var csv = await File.ReadAllTextAsync((await _phone.Service.ExportAsync())[1], Encoding.UTF8);

        csv.Should().Contain(";\"Pão; leite \"\"integral\"\"\";");
        csv.Should().Contain(";\"'=HYPERLINK(\"\"x\"\")\";");
    }

    [Fact]
    public async Task ExportAsync_Backup_IsReadableWithAccentsAndEnumNames()
    {
        await _phone.SeedAsync();

        var json = await File.ReadAllTextAsync((await _phone.Service.ExportAsync())[0], Encoding.UTF8);

        json.Should().Contain("\"format\": \"mensal-backup\"")
            .And.Contain("\"name\": \"Itaú\"")
            .And.Contain("\"type\": \"CreditCard\"")
            .And.Contain("\"profileName\": \"Ana\"");
    }

    [Fact]
    public async Task ExportAsync_SecondExport_RemovesThePreviousFiles()
    {
        var first = await _phone.Service.ExportAsync();
        File.Move(first[0], first[0] + ".old");

        await _phone.Service.ExportAsync();

        File.Exists(first[0] + ".old").Should().BeFalse();
    }

    // --- ImportAsync ---

    [Fact]
    public async Task ImportAsync_ExportFromAnotherPhone_RestoresEveryRowWithItsIds()
    {
        await _phone.SeedAsync();
        var source = await _phone.Context.GetConnectionAsync();

        await ImportIntoNewPhoneAsync();

        var target = await _newPhone.Context.GetConnectionAsync();
        (await target.Table<FinancialCategory>().ToListAsync()).Should().BeEquivalentTo(await source.Table<FinancialCategory>().ToListAsync());
        (await target.Table<PaymentSource>().ToListAsync()).Should().BeEquivalentTo(await source.Table<PaymentSource>().ToListAsync());
        (await target.Table<FinancialEntry>().ToListAsync()).Should().BeEquivalentTo(await source.Table<FinancialEntry>().ToListAsync());
        (await target.Table<FinancialMonthlyEntry>().ToListAsync()).Should().BeEquivalentTo(await source.Table<FinancialMonthlyEntry>().ToListAsync());
        (await target.Table<FinancialPayment>().ToListAsync()).Should().BeEquivalentTo(await source.Table<FinancialPayment>().ToListAsync());
    }

    [Fact]
    public async Task ImportAsync_ExportFromAnotherPhone_ShowsTheSameMonth()
    {
        await _phone.SeedAsync();

        await ImportIntoNewPhoneAsync();

        var expected = await _phone.Finance.GetMergedEntriesForMonthAsync(2026, 11);
        var imported = await _newPhone.Finance.GetMergedEntriesForMonthAsync(2026, 11);
        imported.Should().BeEquivalentTo(expected);
    }
    [Fact]
    public async Task ImportAsync_MonthLoadedBefore_ShowsTheImportedCategoriesAndSources()
    {
        await _phone.SeedAsync();
        await _newPhone.Finance.GetMergedEntriesForMonthAsync(2026, 11);

        await ImportIntoNewPhoneAsync();

        var expected = await _phone.Finance.GetMergedEntriesForMonthAsync(2026, 11);
        var imported = await _newPhone.Finance.GetMergedEntriesForMonthAsync(2026, 11);
        imported.Should().BeEquivalentTo(expected);
    }


    [Fact]
    public async Task ImportAsync_ExportFromAnotherPhone_RestoresNameAndPhoto()
    {
        await _phone.SeedAsync();

        await ImportIntoNewPhoneAsync();

        _newPhone.Profile.Name.Should().Be("Ana");
        _newPhone.Profile.PhotoPath.Should().StartWith(_newPhone.FileSystem.AppDataDirectory).And.EndWith(".jpg");
        (await File.ReadAllTextAsync(_newPhone.Profile.PhotoPath!)).Should().Be("photo");
    }

    [Fact]
    public async Task ImportAsync_PhoneWithData_ReplacesItAndKeepsThePin()
    {
        await _phone.SeedAsync();
        var newDb = await _newPhone.Context.GetConnectionAsync();
        await newDb.InsertAsync(new PaymentSource { Name = "Conta antiga" });
        await newDb.InsertAsync(new FinancialEntry { Description = "Antigo", Amount = 1, StartDate = Now.Date });
        await _newPhone.AppLock.SetPinAsync("1234");

        await ImportIntoNewPhoneAsync();

        (await newDb.Table<PaymentSource>().ToListAsync()).Select(s => s.Name).Should().Equal("Itaú", "Nubank");
        (await newDb.Table<FinancialEntry>().ToListAsync()).Select(e => e.Description).Should().NotContain("Antigo");
        (await _newPhone.AppLock.VerifyPinAsync("1234")).Should().BeTrue();
    }

    [Fact]
    public async Task ImportAsync_BackupWithoutPhoto_RemovesThePhoto()
    {
        await _newPhone.Profile.SaveAsync("Bia", TempFileSystem.CreateExternalFile(".png", "old"));

        await ImportIntoNewPhoneAsync();

        _newPhone.Profile.Name.Should().BeEmpty();
        _newPhone.Profile.PhotoPath.Should().BeNull();
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("{\"name\": \"some other app\"}")]
    [InlineData("{\"format\": \"mensal-backup\", \"version\": 1, \"profilePhoto\": \"%%%\"}")]
    public async Task ImportAsync_NotABackup_ThrowsAndKeepsTheData(string content)
    {
        await _phone.SeedAsync();

        var act = () => _phone.Service.ImportAsync(new MemoryStream(Encoding.UTF8.GetBytes(content)));

        (await act.Should().ThrowAsync<InvalidBackupException>()).Which.IsFromNewerVersion.Should().BeFalse();
        var db = await _phone.Context.GetConnectionAsync();
        (await db.Table<FinancialEntry>().CountAsync()).Should().Be(3);
        _phone.Profile.Name.Should().Be("Ana");
    }

    [Fact]
    public async Task ImportAsync_BackupFromNewerVersion_ThrowsSayingSo()
    {
        var content = $"{{\"format\": \"mensal-backup\", \"version\": {DataBackup.CurrentVersion + 1}}}";

        var act = () => _phone.Service.ImportAsync(new MemoryStream(Encoding.UTF8.GetBytes(content)));

        (await act.Should().ThrowAsync<InvalidBackupException>()).Which.IsFromNewerVersion.Should().BeTrue();
    }

    private async Task ImportIntoNewPhoneAsync()
    {
        var backupPath = (await _phone.Service.ExportAsync())[0];
        await using var stream = File.OpenRead(backupPath);
        await _newPhone.Service.ImportAsync(stream);
    }

    /// <summary>
    /// One device: its own database, preferences, secure storage and files.
    /// </summary>
    private sealed class Phone : IDisposable
    {
        public TestDatabaseContext Context { get; } = new();
        public InMemoryPreferences Preferences { get; } = new();
        public TempFileSystem FileSystem { get; } = new();
        public UserProfileService Profile { get; }
        public AppLockService AppLock { get; }
        public CategoryService Categories { get; }
        public PaymentSourceService PaymentSources { get; }
        public FinanceService Finance { get; }
        public SetupService Setup { get; }
        public DataManagementService Service { get; }

        public Phone()
        {
            var timeProvider = Substitute.For<TimeProvider>();
            timeProvider.LocalTimeZone.Returns(TimeZoneInfo.Local);
            timeProvider.GetUtcNow().Returns(new DateTimeOffset(Now.ToUniversalTime()));

            Profile = new UserProfileService(Preferences, FileSystem);
            AppLock = new AppLockService(new InMemorySecureStorage(), Preferences, timeProvider);
            Categories = new CategoryService(Context);
            PaymentSources = new PaymentSourceService(Context);
            Finance = new FinanceService(Context, Categories, PaymentSources);
            Setup = new SetupService(Context, Categories, PaymentSources, Profile, Preferences, timeProvider);
            Service = new DataManagementService(Context, Categories, PaymentSources, Profile, AppLock, Preferences, FileSystem, timeProvider);
        }

        /// <summary>
        /// Two accounts and cards, three entries (one with a month record and a partial payment), the profile and a PIN.
        /// </summary>
        public async Task SeedAsync()
        {
            var db = await Context.GetConnectionAsync();
            var start = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Local);
            var market = new FinancialCategory { Name = "Mercado", Icon = "🛒", Color = "#F4511E" };
            var salaryCategory = new FinancialCategory { Name = "Salário", Icon = "💼", Color = "#00897B", EntryType = FinancialEntryType.Income };
            await db.InsertAllAsync(new[] { market, salaryCategory });
            var account = new PaymentSource { Name = "Itaú", Balance = 1500.5m };
            await db.InsertAsync(account);
            var card = new PaymentSource { Name = "Nubank", Type = PaymentSourceType.CreditCard, Balance = 5000, BillDueDay = 10, BillPaymentSourceId = account.Id };
            await db.InsertAsync(card);

            var groceries = new FinancialEntry
            {
                Description = "Mercado", Amount = 1234.56m, StartDate = start, Recurrence = RecurrenceType.Recurrent,
                EntryType = FinancialEntryType.Outcome, CategoryId = market.Id, PaymentSourceId = account.Id,
                IsEstimated = true, AllowsPartialPayment = true
            };
            await db.InsertAllAsync(new[]
            {
                groceries,
                new FinancialEntry
                {
                    Description = "TV", Amount = 300, StartDate = start, Recurrence = RecurrenceType.Installments, TotalInstallments = 10,
                    EntryType = FinancialEntryType.Outcome, PaymentSourceId = card.Id
                },
                new FinancialEntry
                {
                    Description = "Salário", Amount = 5000, StartDate = start, Recurrence = RecurrenceType.Recurrent,
                    EntryType = FinancialEntryType.Income, CategoryId = salaryCategory.Id, PaymentSourceId = account.Id, DueDay = 5
                }
            });
            var month = new FinancialMonthlyEntry
            {
                FinancialEntryId = groceries.Id, Year = 2026, Month = 11, Status = FinancialMonthlyEntryStatus.PartiallyPaid, PaidAmount = 200
            };
            await db.InsertAsync(month);
            await db.InsertAsync(new FinancialPayment { FinancialMonthlyEntryId = month.Id, Amount = 200, Date = Now, Description = "Feira" });

            await Profile.SaveAsync("Ana", TempFileSystem.CreateExternalFile(".jpg", "photo"));
            await AppLock.SetPinAsync("4321");
        }

        public void Dispose() => FileSystem.Dispose();
    }
}
