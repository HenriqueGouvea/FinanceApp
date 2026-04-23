using FinanceApp.Models;
using FinanceApp.Services;
using FinanceApp.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace FinanceApp.Tests.Services;

public class FinanceServiceTests
{
    private readonly TestDatabaseContext _context;
    private readonly FinanceService _service;

    public FinanceServiceTests()
    {
        _context = new TestDatabaseContext();
        _service = new FinanceService(_context);
    }

    [Fact]
    public async Task GetMergedEntries_MultipleTemplates_ReturnsAllActiveProjections()
    {
        await _service.SaveEntryAsync(new FinancialEntry
        {
            Description = "Salary",
            Amount = 3000m,
            StartDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Local),
            Recurrence = RecurrenceType.Recurrent,
            EntryType = FinancialEntryType.Income
        });
        await _service.SaveEntryAsync(new FinancialEntry
        {
            Description = "Annual bonus",
            Amount = 1000m,
            StartDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Local),
            Recurrence = RecurrenceType.OneTime,
            EntryType = FinancialEntryType.Income
        });
        await _service.SaveEntryAsync(new FinancialEntry
        {
            Description = "Laptop",
            Amount = 400m,
            StartDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Local),
            Recurrence = RecurrenceType.Installments,
            TotalInstallments = 3,
            EntryType = FinancialEntryType.Outcome
        });

        var results = await _service.GetMergedEntriesForMonthAsync(2026, 4);

        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetMergedEntries_PhysicalRecordExists_MergesWithTemplate()
    {
        var template = new FinancialEntry
        {
            Description = "Salary",
            Amount = 3000m,
            StartDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Local),
            Recurrence = RecurrenceType.Recurrent,
            EntryType = FinancialEntryType.Income
        };
        await _service.SaveEntryAsync(template);

        await _service.SaveMonthlyEntryAsync(new FinancialMonthlyEntry
        {
            FinancialEntryId = template.Id,
            Year = 2026,
            Month = 4,
            Status = FinancialMonthlyEntryStatus.Paid,
            OverrideAmount = 9999m
        });

        var results = await _service.GetMergedEntriesForMonthAsync(2026, 4);
        var projection = results.Should().HaveCount(1).And.Subject.Single();

        projection.EffectiveAmount.Should().Be(9999m);
        projection.Status.Should().Be(FinancialMonthlyEntryStatus.Paid);
        projection.IsProjected.Should().BeFalse();
        projection.FinancialMonthlyEntryId.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMergedEntries_CancelledPhysicalRecord_ExcludesFromList()
    {
        var template = new FinancialEntry
        {
            Description = "Salary",
            Amount = 3000m,
            StartDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Local),
            Recurrence = RecurrenceType.Recurrent,
            EntryType = FinancialEntryType.Income
        };
        await _service.SaveEntryAsync(template);

        await _service.SaveMonthlyEntryAsync(new FinancialMonthlyEntry
        {
            FinancialEntryId = template.Id,
            Year = 2026,
            Month = 4,
            IsCancelled = true
        });

        var results = await _service.GetMergedEntriesForMonthAsync(2026, 4);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveEntryAsync_OneTimeEntry_AppearsOnlyInItsMonth()
    {
        var entry = new FinancialEntry
        {
            Description = "One-time bonus",
            Amount = 500m,
            StartDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Local),
            Recurrence = RecurrenceType.OneTime,
            EntryType = FinancialEntryType.Income
        };

        await _service.SaveEntryAsync(entry);

        var inApril = await _service.GetMergedEntriesForMonthAsync(2026, 4);
        var inMarch = await _service.GetMergedEntriesForMonthAsync(2026, 3);
        var inMay = await _service.GetMergedEntriesForMonthAsync(2026, 5);

        inApril.Should().HaveCount(1);
        inMarch.Should().BeEmpty();
        inMay.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveEntryAsync_RecurrentEntry_AppearsFromStartDateOnwards()
    {
        var entry = new FinancialEntry
        {
            Description = "Monthly salary",
            Amount = 3000m,
            StartDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Local),
            Recurrence = RecurrenceType.Recurrent,
            EntryType = FinancialEntryType.Income
        };

        await _service.SaveEntryAsync(entry);

        var beforeStart = await _service.GetMergedEntriesForMonthAsync(2026, 3);
        var atStart = await _service.GetMergedEntriesForMonthAsync(2026, 4);
        var farFuture = await _service.GetMergedEntriesForMonthAsync(2030, 12);

        beforeStart.Should().BeEmpty();
        atStart.Should().HaveCount(1);
        farFuture.Should().HaveCount(1);
    }

    [Fact]
    public async Task SaveEntryAsync_InstallmentsEntry_ProjectsExactlyNMonths()
    {
        var entry = new FinancialEntry
        {
            Description = "Laptop installments",
            Amount = 400m,
            StartDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Local),
            Recurrence = RecurrenceType.Installments,
            TotalInstallments = 3,
            EntryType = FinancialEntryType.Outcome
        };

        await _service.SaveEntryAsync(entry);

        var april = await _service.GetMergedEntriesForMonthAsync(2026, 4);
        var june = await _service.GetMergedEntriesForMonthAsync(2026, 6);
        var july = await _service.GetMergedEntriesForMonthAsync(2026, 7);

        april.Should().HaveCount(1);
        june.Should().HaveCount(1);
        july.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveMonthlyEntry_NewRecord_PersistsToDb()
    {
        var template = new FinancialEntry
        {
            Description = "Salary",
            Amount = 3000m,
            StartDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Local),
            Recurrence = RecurrenceType.Recurrent,
            EntryType = FinancialEntryType.Income
        };
        await _service.SaveEntryAsync(template);

        await _service.SaveMonthlyEntryAsync(new FinancialMonthlyEntry
        {
            FinancialEntryId = template.Id,
            Year = 2026,
            Month = 4,
            Status = FinancialMonthlyEntryStatus.Paid,
            OverrideAmount = 750m
        });

        var db = await _context.GetConnectionAsync();
        var persisted = await db.Table<FinancialMonthlyEntry>()
            .Where(r => r.FinancialEntryId == template.Id && r.Year == 2026 && r.Month == 4)
            .FirstOrDefaultAsync();

        persisted.Should().NotBeNull();
        persisted!.Status.Should().Be(FinancialMonthlyEntryStatus.Paid);
        persisted.OverrideAmount.Should().Be(750m);
    }

    [Fact]
    public async Task GetMergedEntries_IncomeEntry_ReturnsProjectionWithIncomeType()
    {
        await _service.SaveEntryAsync(new FinancialEntry
        {
            Description = "Salary",
            Amount = 3000m,
            StartDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Local),
            Recurrence = RecurrenceType.Recurrent,
            EntryType = FinancialEntryType.Income
        });

        var results = await _service.GetMergedEntriesForMonthAsync(2026, 4);

        results.Should().HaveCount(1);
        results.Single().EntryType.Should().Be(FinancialEntryType.Income);
    }

    [Fact]
    public async Task GetMergedEntries_OutcomeEntry_ReturnsProjectionWithOutcomeType()
    {
        await _service.SaveEntryAsync(new FinancialEntry
        {
            Description = "Rent",
            Amount = 1200m,
            StartDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Local),
            Recurrence = RecurrenceType.Recurrent,
            EntryType = FinancialEntryType.Outcome
        });

        var results = await _service.GetMergedEntriesForMonthAsync(2026, 4);

        results.Should().HaveCount(1);
        results.Single().EntryType.Should().Be(FinancialEntryType.Outcome);
    }

    [Fact]
    public async Task GetMergedEntries_MixedTypes_BothAppearWithCorrectType()
    {
        await _service.SaveEntryAsync(new FinancialEntry
        {
            Description = "Salary",
            Amount = 3000m,
            StartDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Local),
            Recurrence = RecurrenceType.Recurrent,
            EntryType = FinancialEntryType.Income
        });
        await _service.SaveEntryAsync(new FinancialEntry
        {
            Description = "Rent",
            Amount = 1200m,
            StartDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Local),
            Recurrence = RecurrenceType.Recurrent,
            EntryType = FinancialEntryType.Outcome
        });

        var results = (await _service.GetMergedEntriesForMonthAsync(2026, 4)).ToList();

        results.Should().HaveCount(2);
        results.Should().ContainSingle(r => r.EntryType == FinancialEntryType.Income && r.Description == "Salary");
        results.Should().ContainSingle(r => r.EntryType == FinancialEntryType.Outcome && r.Description == "Rent");
    }
}
