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

    [Fact]
    public async Task GetMergedEntries_EntryWithCategory_ResolvesCategoryOnProjection()
    {
        var category = new FinancialCategory { Name = "Moradia", Icon = "🏠", Color = "#1E88E5" };
        await new CategoryService(_context).SaveAsync(category);
        var template = await SaveTemplateAsync(RecurrenceType.Recurrent);
        template.CategoryId = category.Id;
        await _service.SaveEntryAsync(template);

        var projection = (await _service.GetMergedEntriesForMonthAsync(2026, 4)).Single();

        projection.Category.Should().NotBeNull();
        projection.Category!.Name.Should().Be("Moradia");
        projection.Category.Icon.Should().Be("🏠");
        projection.Category.Color.Should().Be("#1E88E5");
    }

    [Fact]
    public async Task GetMergedEntries_EntryWithoutCategory_ProjectionHasNullCategory()
    {
        await SaveTemplateAsync(RecurrenceType.Recurrent);

        var projection = (await _service.GetMergedEntriesForMonthAsync(2026, 4)).Single();

        projection.Category.Should().BeNull();
    }

    [Fact]
    public async Task GetMergedEntries_InstallmentsEntry_ReturnsFullInstallmentTotal()
    {
        await SaveTemplateAsync(RecurrenceType.Installments, totalInstallments: 10);

        var projection = (await _service.GetMergedEntriesForMonthAsync(2026, 6)).Single();

        projection.EffectiveAmount.Should().Be(100m);
        projection.InstallmentLabel.Should().Be("3/10");
        projection.InstallmentTotalAmount.Should().Be(1000m);
    }

    [Fact]
    public async Task GetMergedEntries_InstallmentsEntryWithOverride_TotalUsesTemplateAmount()
    {
        var entry = await SaveTemplateAsync(RecurrenceType.Installments, totalInstallments: 10);
        await _service.SaveMonthlyEntryAsync(new FinancialMonthlyEntry
        {
            FinancialEntryId = entry.Id,
            Year = 2026,
            Month = 4,
            OverrideAmount = 150m
        });

        var projection = (await _service.GetMergedEntriesForMonthAsync(2026, 4)).Single();

        projection.EffectiveAmount.Should().Be(150m);
        projection.InstallmentTotalAmount.Should().Be(1000m);
    }

    [Theory]
    [InlineData(RecurrenceType.OneTime)]
    [InlineData(RecurrenceType.Recurrent)]
    public async Task GetMergedEntries_NonInstallmentsEntry_HasNullInstallmentTotal(RecurrenceType recurrence)
    {
        await SaveTemplateAsync(recurrence);

        var projection = (await _service.GetMergedEntriesForMonthAsync(2026, 4)).Single();

        projection.InstallmentTotalAmount.Should().BeNull();
    }

    [Fact]
    public async Task GetEntryByIdAsync_ExistingEntry_ReturnsCorrectEntry()
    {
        var entry = new FinancialEntry
        {
            Description = "Salary",
            Amount = 3000m,
            StartDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Local),
            Recurrence = RecurrenceType.Recurrent,
            EntryType = FinancialEntryType.Income
        };
        await _service.SaveEntryAsync(entry);

        var result = await _service.GetEntryByIdAsync(entry.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(entry.Id);
        result.Description.Should().Be("Salary");
        result.Amount.Should().Be(3000m);
    }

    [Fact]
    public async Task GetEntryByIdAsync_NonExistentId_ReturnsNull()
    {
        var result = await _service.GetEntryByIdAsync(9999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveEntryAsync_ExistingEntry_UpdatesInsteadOfInserting()
    {
        var entry = new FinancialEntry
        {
            Description = "Original",
            Amount = 100m,
            StartDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Local),
            Recurrence = RecurrenceType.OneTime,
            EntryType = FinancialEntryType.Outcome
        };
        await _service.SaveEntryAsync(entry);
        var originalId = entry.Id;

        entry.Description = "Updated";
        entry.Amount = 200m;
        await _service.SaveEntryAsync(entry);

        var db = await _context.GetConnectionAsync();
        var allEntries = await db.Table<FinancialEntry>().ToListAsync();
        allEntries.Should().HaveCount(1);
        allEntries.Single().Id.Should().Be(originalId);
        allEntries.Single().Description.Should().Be("Updated");
        allEntries.Single().Amount.Should().Be(200m);
    }

    [Fact]
    public async Task SetEntryStatusAsync_ProjectedEntry_PersistsMonthlyRecordWithStatus()
    {
        var entry = await SaveTemplateAsync(RecurrenceType.Recurrent);

        await _service.SetEntryStatusAsync(entry.Id, 2026, 5, FinancialMonthlyEntryStatus.Paid);

        var db = await _context.GetConnectionAsync();
        var record = (await db.Table<FinancialMonthlyEntry>().ToListAsync()).Should().ContainSingle().Subject;
        record.FinancialEntryId.Should().Be(entry.Id);
        record.Year.Should().Be(2026);
        record.Month.Should().Be(5);
        record.Status.Should().Be(FinancialMonthlyEntryStatus.Paid);
        record.IsCancelled.Should().BeFalse();
    }

    [Fact]
    public async Task SetEntryStatusAsync_Paid_ProjectionIsPaidOnlyInThatMonth()
    {
        var entry = await SaveTemplateAsync(RecurrenceType.Installments, totalInstallments: 3);

        await _service.SetEntryStatusAsync(entry.Id, 2026, 5, FinancialMonthlyEntryStatus.Paid);

        var paid = (await _service.GetMergedEntriesForMonthAsync(2026, 5)).Single();
        paid.IsPaid.Should().BeTrue();
        paid.IsProjected.Should().BeFalse();
        paid.InstallmentLabel.Should().Be("2/3");
        (await _service.GetMergedEntriesForMonthAsync(2026, 4)).Single().IsPaid.Should().BeFalse();
        (await _service.GetMergedEntriesForMonthAsync(2026, 6)).Single().IsPaid.Should().BeFalse();
    }

    [Fact]
    public async Task SetEntryStatusAsync_ExistingRecord_UpdatesItWithoutDuplicating()
    {
        var entry = await SaveTemplateAsync(RecurrenceType.Recurrent);
        await _service.SaveMonthlyEntryAsync(new FinancialMonthlyEntry
        {
            FinancialEntryId = entry.Id,
            Year = 2026,
            Month = 5,
            Status = FinancialMonthlyEntryStatus.Paid,
            OverrideAmount = 120m
        });

        await _service.SetEntryStatusAsync(entry.Id, 2026, 5, FinancialMonthlyEntryStatus.Pending);

        var db = await _context.GetConnectionAsync();
        var record = (await db.Table<FinancialMonthlyEntry>().ToListAsync()).Should().ContainSingle().Subject;
        record.Status.Should().Be(FinancialMonthlyEntryStatus.Pending);
        record.OverrideAmount.Should().Be(120m);
    }

    [Fact]
    public async Task SetEntryStatusAsync_DoesNotAffectOtherEntriesInSameMonth()
    {
        var paid = await SaveTemplateAsync(RecurrenceType.Recurrent);
        var other = await SaveTemplateAsync(RecurrenceType.Recurrent);

        await _service.SetEntryStatusAsync(paid.Id, 2026, 4, FinancialMonthlyEntryStatus.Paid);

        var projections = (await _service.GetMergedEntriesForMonthAsync(2026, 4)).ToList();
        projections.Single(p => p.FinancialEntryId == paid.Id).IsPaid.Should().BeTrue();
        projections.Single(p => p.FinancialEntryId == other.Id).IsPaid.Should().BeFalse();
    }

    [Fact]
    public async Task SetEntryStatusAsync_PartiallyPaidEntry_ClearsPaidAmount()
    {
        var entry = await SaveTemplateAsync(RecurrenceType.Recurrent);
        await _service.SetPaidAmountAsync(entry.Id, 2026, 4, 40m);

        await _service.SetEntryStatusAsync(entry.Id, 2026, 4, FinancialMonthlyEntryStatus.Paid);

        var projection = (await _service.GetMergedEntriesForMonthAsync(2026, 4)).Single();
        projection.IsPaid.Should().BeTrue();
        projection.PaidAmount.Should().Be(100m);
        var db = await _context.GetConnectionAsync();
        (await db.Table<FinancialMonthlyEntry>().ToListAsync()).Single().PaidAmount.Should().BeNull();
    }

    [Fact]
    public async Task SetPaidAmountAsync_LessThanAmount_PersistsPartiallyPaidRecord()
    {
        var entry = await SaveTemplateAsync(RecurrenceType.Recurrent);

        await _service.SetPaidAmountAsync(entry.Id, 2026, 5, 40m);

        var db = await _context.GetConnectionAsync();
        var record = (await db.Table<FinancialMonthlyEntry>().ToListAsync()).Should().ContainSingle().Subject;
        record.FinancialEntryId.Should().Be(entry.Id);
        record.Year.Should().Be(2026);
        record.Month.Should().Be(5);
        record.PaidAmount.Should().Be(40m);
        record.Status.Should().Be(FinancialMonthlyEntryStatus.PartiallyPaid);
    }

    [Fact]
    public async Task SetPaidAmountAsync_LessThanAmount_ProjectionShowsPaidAndPendingOnlyInThatMonth()
    {
        var entry = await SaveTemplateAsync(RecurrenceType.Recurrent);

        await _service.SetPaidAmountAsync(entry.Id, 2026, 5, 40m);

        var partial = (await _service.GetMergedEntriesForMonthAsync(2026, 5)).Single();
        partial.IsPartiallyPaid.Should().BeTrue();
        partial.EffectiveAmount.Should().Be(100m);
        partial.PaidAmount.Should().Be(40m);
        partial.PendingAmount.Should().Be(60m);
        var other = (await _service.GetMergedEntriesForMonthAsync(2026, 6)).Single();
        other.IsPending.Should().BeTrue();
        other.PaidAmount.Should().Be(0m);
    }

    [Fact]
    public async Task SetPaidAmountAsync_EqualToAmount_MarksEntryAsPaid()
    {
        var entry = await SaveTemplateAsync(RecurrenceType.Recurrent);

        await _service.SetPaidAmountAsync(entry.Id, 2026, 4, 100m);

        var projection = (await _service.GetMergedEntriesForMonthAsync(2026, 4)).Single();
        projection.IsPaid.Should().BeTrue();
        projection.PendingAmount.Should().Be(0m);
        var db = await _context.GetConnectionAsync();
        (await db.Table<FinancialMonthlyEntry>().ToListAsync()).Single().Status.Should().Be(FinancialMonthlyEntryStatus.Paid);
    }

    [Fact]
    public async Task SetPaidAmountAsync_MoreThanAmount_RaisesEffectiveAmountForThatMonth()
    {
        var entry = await SaveTemplateAsync(RecurrenceType.Recurrent);

        await _service.SetPaidAmountAsync(entry.Id, 2026, 4, 130m);

        var projection = (await _service.GetMergedEntriesForMonthAsync(2026, 4)).Single();
        projection.IsPaid.Should().BeTrue();
        projection.EffectiveAmount.Should().Be(130m);
        projection.PaidAmount.Should().Be(130m);
        (await _service.GetMergedEntriesForMonthAsync(2026, 5)).Single().EffectiveAmount.Should().Be(100m);
    }

    [Fact]
    public async Task SetPaidAmountAsync_Zero_RevertsToPendingAndClearsPaidAmount()
    {
        var entry = await SaveTemplateAsync(RecurrenceType.Recurrent);
        await _service.SetPaidAmountAsync(entry.Id, 2026, 4, 40m);

        await _service.SetPaidAmountAsync(entry.Id, 2026, 4, 0m);

        (await _service.GetMergedEntriesForMonthAsync(2026, 4)).Single().IsPending.Should().BeTrue();
        var db = await _context.GetConnectionAsync();
        var record = (await db.Table<FinancialMonthlyEntry>().ToListAsync()).Should().ContainSingle().Subject;
        record.Status.Should().Be(FinancialMonthlyEntryStatus.Pending);
        record.PaidAmount.Should().BeNull();
    }

    [Fact]
    public async Task SetPaidAmountAsync_ExistingRecord_UsesOverrideAmountAndKeepsIt()
    {
        var entry = await SaveTemplateAsync(RecurrenceType.Recurrent);
        await _service.SaveMonthlyEntryAsync(new FinancialMonthlyEntry
        {
            FinancialEntryId = entry.Id,
            Year = 2026,
            Month = 4,
            OverrideAmount = 50m
        });

        await _service.SetPaidAmountAsync(entry.Id, 2026, 4, 50m);

        var db = await _context.GetConnectionAsync();
        var record = (await db.Table<FinancialMonthlyEntry>().ToListAsync()).Should().ContainSingle().Subject;
        record.Status.Should().Be(FinancialMonthlyEntryStatus.Paid);
        record.OverrideAmount.Should().Be(50m);
    }

    [Fact]
    public async Task SetPaidAmountAsync_TemplateAmountLoweredBelowPaidAmount_ProjectionBecomesPaid()
    {
        var entry = await SaveTemplateAsync(RecurrenceType.Recurrent);
        await _service.SetPaidAmountAsync(entry.Id, 2026, 4, 40m);

        entry.Amount = 40m;
        await _service.SaveEntryAsync(entry);

        (await _service.GetMergedEntriesForMonthAsync(2026, 4)).Single().IsPaid.Should().BeTrue();
    }

    [Fact]
    public async Task SetPaidAmountAsync_UnknownEntry_DoesNotPersistRecord()
    {
        await _service.SetPaidAmountAsync(999, 2026, 4, 40m);

        var db = await _context.GetConnectionAsync();
        (await db.Table<FinancialMonthlyEntry>().ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task AddPaidAmountAsync_ProjectedEntry_PersistsPartiallyPaidRecord()
    {
        var entry = await SaveTemplateAsync(RecurrenceType.Recurrent);

        await _service.AddPaidAmountAsync(entry.Id, 2026, 5, 30m);

        var db = await _context.GetConnectionAsync();
        var record = (await db.Table<FinancialMonthlyEntry>().ToListAsync()).Should().ContainSingle().Subject;
        record.Month.Should().Be(5);
        record.PaidAmount.Should().Be(30m);
        record.Status.Should().Be(FinancialMonthlyEntryStatus.PartiallyPaid);
    }

    [Fact]
    public async Task AddPaidAmountAsync_PartiallyPaidEntry_AddsToPaidAmount()
    {
        var entry = await SaveTemplateAsync(RecurrenceType.Recurrent);
        await _service.SetPaidAmountAsync(entry.Id, 2026, 4, 30m);

        await _service.AddPaidAmountAsync(entry.Id, 2026, 4, 25m);

        var projection = (await _service.GetMergedEntriesForMonthAsync(2026, 4)).Single();
        projection.IsPartiallyPaid.Should().BeTrue();
        projection.PaidAmount.Should().Be(55m);
        projection.PendingAmount.Should().Be(45m);
    }

    [Fact]
    public async Task AddPaidAmountAsync_ReachingAmount_MarksEntryAsPaid()
    {
        var entry = await SaveTemplateAsync(RecurrenceType.Recurrent);
        await _service.SetPaidAmountAsync(entry.Id, 2026, 4, 60m);

        await _service.AddPaidAmountAsync(entry.Id, 2026, 4, 40m);

        (await _service.GetMergedEntriesForMonthAsync(2026, 4)).Single().IsPaid.Should().BeTrue();
    }

    [Fact]
    public async Task AddPaidAmountAsync_PaidEntry_AddsOnTopOfWholeAmount()
    {
        var entry = await SaveTemplateAsync(RecurrenceType.Recurrent);
        await _service.SetEntryStatusAsync(entry.Id, 2026, 4, FinancialMonthlyEntryStatus.Paid);

        await _service.AddPaidAmountAsync(entry.Id, 2026, 4, 20m);

        var projection = (await _service.GetMergedEntriesForMonthAsync(2026, 4)).Single();
        projection.IsPaid.Should().BeTrue();
        projection.EffectiveAmount.Should().Be(120m);
    }

    [Fact]
    public async Task AddPaidAmountAsync_UnknownEntry_DoesNotPersistRecord()
    {
        await _service.AddPaidAmountAsync(999, 2026, 4, 40m);

        var db = await _context.GetConnectionAsync();
        (await db.Table<FinancialMonthlyEntry>().ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task GetMergedEntries_EstimatedTemplate_ProjectionIsEstimated()
    {
        var entry = new FinancialEntry
        {
            Description = "Energy",
            Amount = 200m,
            StartDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Local),
            Recurrence = RecurrenceType.Recurrent,
            EntryType = FinancialEntryType.Outcome,
            IsEstimated = true
        };
        await _service.SaveEntryAsync(entry);
        await _service.SetEntryStatusAsync(entry.Id, 2026, 5, FinancialMonthlyEntryStatus.Paid);

        (await _service.GetMergedEntriesForMonthAsync(2026, 4)).Single().IsEstimated.Should().BeTrue();
        (await _service.GetMergedEntriesForMonthAsync(2026, 5)).Single().IsEstimated.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteEntryAsync_OneTimeEntry_RemovesTemplate()
    {
        var entry = await SaveTemplateAsync(RecurrenceType.OneTime);

        await _service.DeleteEntryAsync(entry.Id, 2026, 4, EntryDeleteScope.ThisMonth);

        (await _service.GetEntryByIdAsync(entry.Id)).Should().BeNull();
        (await _service.GetMergedEntriesForMonthAsync(2026, 4)).Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteEntryAsync_ThisMonth_HidesOnlyThatMonth()
    {
        var entry = await SaveTemplateAsync(RecurrenceType.Recurrent);

        await _service.DeleteEntryAsync(entry.Id, 2026, 5, EntryDeleteScope.ThisMonth);

        (await _service.GetMergedEntriesForMonthAsync(2026, 4)).Should().ContainSingle();
        (await _service.GetMergedEntriesForMonthAsync(2026, 5)).Should().BeEmpty();
        (await _service.GetMergedEntriesForMonthAsync(2026, 6)).Should().ContainSingle();
    }

    [Fact]
    public async Task DeleteEntryAsync_ThisMonthWithExistingPhysicalRecord_MarksItCancelled()
    {
        var entry = await SaveTemplateAsync(RecurrenceType.Recurrent);
        await _service.SaveMonthlyEntryAsync(new FinancialMonthlyEntry
        {
            FinancialEntryId = entry.Id,
            Year = 2026,
            Month = 5,
            Status = FinancialMonthlyEntryStatus.Paid
        });

        await _service.DeleteEntryAsync(entry.Id, 2026, 5, EntryDeleteScope.ThisMonth);

        var db = await _context.GetConnectionAsync();
        var records = await db.Table<FinancialMonthlyEntry>().ToListAsync();
        records.Should().ContainSingle().Which.IsCancelled.Should().BeTrue();
        (await _service.GetMergedEntriesForMonthAsync(2026, 5)).Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteEntryAsync_ThisAndFuture_KeepsPastMonthsAndHidesFutureOnes()
    {
        var entry = await SaveTemplateAsync(RecurrenceType.Recurrent);
        await _service.SaveMonthlyEntryAsync(new FinancialMonthlyEntry { FinancialEntryId = entry.Id, Year = 2026, Month = 5 });
        await _service.SaveMonthlyEntryAsync(new FinancialMonthlyEntry { FinancialEntryId = entry.Id, Year = 2026, Month = 7 });

        await _service.DeleteEntryAsync(entry.Id, 2026, 6, EntryDeleteScope.ThisAndFuture);

        (await _service.GetMergedEntriesForMonthAsync(2026, 4)).Should().ContainSingle();
        (await _service.GetMergedEntriesForMonthAsync(2026, 5)).Should().ContainSingle();
        (await _service.GetMergedEntriesForMonthAsync(2026, 6)).Should().BeEmpty();
        (await _service.GetMergedEntriesForMonthAsync(2027, 1)).Should().BeEmpty();

        var db = await _context.GetConnectionAsync();
        var records = await db.Table<FinancialMonthlyEntry>().ToListAsync();
        records.Should().ContainSingle().Which.Month.Should().Be(5);
    }

    [Fact]
    public async Task DeleteEntryAsync_ThisAndFutureOnInstallments_TruncatesRemainingInstallments()
    {
        var entry = await SaveTemplateAsync(RecurrenceType.Installments, totalInstallments: 6);

        await _service.DeleteEntryAsync(entry.Id, 2026, 7, EntryDeleteScope.ThisAndFuture);

        (await _service.GetMergedEntriesForMonthAsync(2026, 6)).Should().ContainSingle();
        (await _service.GetMergedEntriesForMonthAsync(2026, 7)).Should().BeEmpty();
        (await _service.GetMergedEntriesForMonthAsync(2026, 9)).Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteEntryAsync_ThisAndFutureFromStartMonth_RemovesTemplate()
    {
        var entry = await SaveTemplateAsync(RecurrenceType.Recurrent);

        await _service.DeleteEntryAsync(entry.Id, 2026, 4, EntryDeleteScope.ThisAndFuture);

        (await _service.GetEntryByIdAsync(entry.Id)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteEntryAsync_All_RemovesTemplateAndMonthlyRecords()
    {
        var entry = await SaveTemplateAsync(RecurrenceType.Recurrent);
        await _service.SaveMonthlyEntryAsync(new FinancialMonthlyEntry { FinancialEntryId = entry.Id, Year = 2026, Month = 5 });

        await _service.DeleteEntryAsync(entry.Id, 2026, 8, EntryDeleteScope.All);

        (await _service.GetEntryByIdAsync(entry.Id)).Should().BeNull();
        var db = await _context.GetConnectionAsync();
        (await db.Table<FinancialMonthlyEntry>().ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteEntryAsync_All_KeepsOtherEntries()
    {
        var deleted = await SaveTemplateAsync(RecurrenceType.Recurrent);
        var kept = await SaveTemplateAsync(RecurrenceType.Recurrent);

        await _service.DeleteEntryAsync(deleted.Id, 2026, 4, EntryDeleteScope.All);

        (await _service.GetMergedEntriesForMonthAsync(2026, 4))
            .Should().ContainSingle().Which.FinancialEntryId.Should().Be(kept.Id);
    }

    private async Task<FinancialEntry> SaveTemplateAsync(RecurrenceType recurrence, int? totalInstallments = null)
    {
        var entry = new FinancialEntry
        {
            Description = "Gym",
            Amount = 100m,
            StartDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Local),
            Recurrence = recurrence,
            TotalInstallments = totalInstallments,
            EntryType = FinancialEntryType.Outcome
        };
        await _service.SaveEntryAsync(entry);
        return entry;
    }
}
