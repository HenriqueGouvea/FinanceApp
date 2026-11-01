using FinanceApp.Models;
using FinanceApp.Services;
using FinanceApp.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace FinanceApp.Tests.Services;

public class PaymentSourceServiceTests
{
    private readonly TestDatabaseContext _context;
    private readonly PaymentSourceService _service;
    private readonly FinanceService _financeService;

    public PaymentSourceServiceTests()
    {
        _context = new TestDatabaseContext();
        _service = new PaymentSourceService(_context);
        _financeService = new FinanceService(_context);
    }

    // --- GetAllAsync ---

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var result = await _service.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_MultipleSources_ReturnsOrderedByName()
    {
        await _service.SaveAsync(new PaymentSource { Name = "Nubank" });
        await _service.SaveAsync(new PaymentSource { Name = "itaú" });
        await _service.SaveAsync(new PaymentSource { Name = "Alelo" });

        var result = await _service.GetAllAsync();

        result.Select(s => s.Name).Should().Equal("Alelo", "itaú", "Nubank");
    }

    // --- SaveAsync ---

    [Fact]
    public async Task SaveAsync_NewSource_AssignsIdAndPersistsAllFields()
    {
        var source = new PaymentSource { Name = "Nubank", Type = PaymentSourceType.CreditCard, Balance = 5000m };

        await _service.SaveAsync(source);

        source.Id.Should().NotBe(0);
        var stored = (await _service.GetAllAsync()).Single();
        stored.Name.Should().Be("Nubank");
        stored.Type.Should().Be(PaymentSourceType.CreditCard);
        stored.Balance.Should().Be(5000m);
    }

    [Fact]
    public async Task SaveAsync_ExistingSource_UpdatesInsteadOfInserting()
    {
        var source = new PaymentSource { Name = "Conta", Type = PaymentSourceType.BankAccount, Balance = 100m };
        await _service.SaveAsync(source);

        await _service.SaveAsync(new PaymentSource { Id = source.Id, Name = "Alelo", Type = PaymentSourceType.MealVoucher, Balance = 800.5m });

        var stored = (await _service.GetAllAsync()).Single();
        stored.Name.Should().Be("Alelo");
        stored.Type.Should().Be(PaymentSourceType.MealVoucher);
        stored.Balance.Should().Be(800.5m);
    }

    [Fact]
    public async Task SaveAsync_NameWithSurroundingWhitespace_TrimsName()
    {
        await _service.SaveAsync(new PaymentSource { Name = "  Inter  " });

        (await _service.GetAllAsync()).Single().Name.Should().Be("Inter");
    }

    // --- CountEntriesAsync ---

    [Fact]
    public async Task CountEntriesAsync_ReturnsOnlyEntriesLinkedToSource()
    {
        var card = new PaymentSource { Name = "Nubank", Type = PaymentSourceType.CreditCard };
        var account = new PaymentSource { Name = "Itaú" };
        await _service.SaveAsync(card);
        await _service.SaveAsync(account);
        await _financeService.SaveEntryAsync(MakeEntry("Netflix", card.Id));
        await _financeService.SaveEntryAsync(MakeEntry("Spotify", card.Id));
        await _financeService.SaveEntryAsync(MakeEntry("Rent", account.Id));

        var count = await _service.CountEntriesAsync(card.Id);

        count.Should().Be(2);
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_RemovesSource()
    {
        var source = new PaymentSource { Name = "Nubank" };
        await _service.SaveAsync(source);

        await _service.DeleteAsync(source.Id);

        (await _service.GetAllAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_SourceInUse_KeepsEntriesAndClearsTheirSource()
    {
        var source = new PaymentSource { Name = "Nubank" };
        await _service.SaveAsync(source);
        var entry = MakeEntry("Netflix", source.Id);
        await _financeService.SaveEntryAsync(entry);

        await _service.DeleteAsync(source.Id);

        var stored = await _financeService.GetEntryByIdAsync(entry.Id);
        stored.Should().NotBeNull();
        stored!.PaymentSourceId.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_DoesNotAffectEntriesOfOtherSources()
    {
        var card = new PaymentSource { Name = "Nubank" };
        var account = new PaymentSource { Name = "Itaú" };
        await _service.SaveAsync(card);
        await _service.SaveAsync(account);
        var rent = MakeEntry("Rent", account.Id);
        await _financeService.SaveEntryAsync(rent);

        await _service.DeleteAsync(card.Id);

        (await _financeService.GetEntryByIdAsync(rent.Id))!.PaymentSourceId.Should().Be(account.Id);
    }

    private static FinancialEntry MakeEntry(string description, int? paymentSourceId) => new()
    {
        Description = description,
        Amount = 100m,
        PaymentSourceId = paymentSourceId,
        StartDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Local),
        Recurrence = RecurrenceType.Recurrent,
        EntryType = FinancialEntryType.Outcome
    };
}
