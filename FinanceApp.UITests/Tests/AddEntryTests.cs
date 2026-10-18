using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class AddEntryTests : IDisposable
{
    private const int OneTimeRecurrence = 0;
    private const int OutcomeEntryType = 1;

    private readonly AppSession _session;
    private readonly MainPageObject _mainPage;

    public AddEntryTests(AppiumServerFixture server)
    {
        _session = new AppSession(server);
        _mainPage = new MainPageObject(_session).WaitUntilLoaded();
    }

    [Fact]
    public async Task AddEntry_OneTimeOutcome_ShowsInCurrentMonthAndPersists()
    {
        _mainPage.OutcomeCountText.Should().Be("0 lançamento(s)");

        var popup = _mainPage.OpenAddEntry();
        popup.IsAmountVisible.Should().BeFalse("the amount field only appears once a recurrence is chosen");

        popup.EnterDescription("Aluguel E2E")
             .SelectRecurrence("Única");
        popup.IsAmountVisible.Should().BeTrue();

        popup.EnterAmount("1500")
             .Save();

        _mainPage.WaitForEntry("Aluguel E2E");
        popup.IsOpen.Should().BeFalse();
        _mainPage.OutcomeCountText.Should().Be("1 lançamento(s)");
        _mainPage.OutcomeTotalText.Should().Be("R$ 1500,00");
        _mainPage.VisibleEntryAmounts.Should().ContainSingle().Which.Should().Be("R$ 1500,00");

        var rows = await _session.Database.QueryAsync<FinancialEntryRow>("SELECT * FROM FinancialEntry");
        var row = rows.Should().ContainSingle().Subject;
        row.Description.Should().Be("Aluguel E2E");
        row.Amount.Should().Be(1500m);
        row.Recurrence.Should().Be(OneTimeRecurrence);
        row.EntryType.Should().Be(OutcomeEntryType);
        row.StartDate.Date.Should().Be(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
    }

    public void Dispose() => _session.Dispose();
}
