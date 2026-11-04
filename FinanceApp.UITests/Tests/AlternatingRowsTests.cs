using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;
using FluentAssertions.Execution;

namespace FinanceApp.UITests.Tests;

/// <summary>
/// The tints sampled from the rendered rows. Which rows alternate (per section, after deletes) is covered by the
/// MonthViewModel tests.
/// </summary>
[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class AlternatingRowsTests : E2ETestBase
{
    // Mirror OutcomeAlternateRow / IncomeAlternateRow in MainPage.xaml.
    private static readonly (byte R, byte G, byte B) White = (0xFF, 0xFF, 0xFF);
    private static readonly (byte R, byte G, byte B) OutcomeAlternate = (0xFD, 0xF5, 0xF6);
    private static readonly (byte R, byte G, byte B) IncomeAlternate = (0xF3, 0xFB, 0xF7);

    public AlternatingRowsTests(AppiumServerFixture server) : base(server, data =>
    {
        data.AddEntry("Aluguel E2E", 1000m, AppFormat.OneTime);
        data.AddEntry("Internet E2E", 100m, AppFormat.OneTime);
        data.AddEntry("Salário E2E", 5000m, AppFormat.OneTime, isIncome: true);
        data.AddEntry("Freela E2E", 800m, AppFormat.OneTime, isIncome: true);
    })
    {
    }

    [Fact]
    public void EntryLists_TintEveryOtherRowInTheSectionsColor()
    {
        var outcomes = MainPage.VisibleEntryDescriptions.Where(d => d is "Aluguel E2E" or "Internet E2E").ToList();

        using (new AssertionScope())
        {
            MainPage.EntryRowBackground(outcomes[0]).Should().Be(White);
            MainPage.EntryRowBackground(outcomes[1]).Should().Be(OutcomeAlternate);
            MainPage.EntryRowBackground("Salário E2E").Should().Be(White, "the income list restarts the alternation");
            MainPage.EntryRowBackground("Freela E2E").Should().Be(IncomeAlternate);
        }
    }
}
