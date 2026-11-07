using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

/// <summary>
/// The top bar of "Início": once the month header scrolls away, it shows the month as a pill that opens the month
/// picker. When it switches (the scroll threshold, the reset on changing month) is covered by the MainViewModel tests.
/// </summary>
[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class TopBarTests : E2ETestBase
{
    public TopBarTests(AppiumServerFixture server) : base(server, data =>
    {
        // Enough rows for the month to scroll past its header, even on a large screen.
        for (var i = 1; i <= 12; i++)
            data.AddEntry($"Conta {i:00} E2E", 10m * i);
    })
    {
    }

    [Fact]
    public void ScrollDown_ShowsMonthInTopBar_WhichOpensMonthPicker()
    {
        MainPage.ScrollToCompactMonthTitle().Should().Be(AppFormat.MonthTitle(AppFormat.CurrentMonth));

        MainPage.OpenMonthPickerFromTopBar().SelectMonth(AppFormat.CurrentMonth.AddMonths(1));

        MainPage.WaitForMonth(AppFormat.CurrentMonth.AddMonths(1));
    }
}
