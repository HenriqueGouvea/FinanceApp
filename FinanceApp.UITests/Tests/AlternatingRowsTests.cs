using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class AlternatingRowsTests : E2ETestBase
{
    // Mirror OutcomeAlternateRow / IncomeAlternateRow in MainPage.xaml.
    private static readonly (byte R, byte G, byte B) White = (0xFF, 0xFF, 0xFF);
    private static readonly (byte R, byte G, byte B) OutcomeAlternate = (0xFE, 0xF4, 0xF4);
    private static readonly (byte R, byte G, byte B) IncomeAlternate = (0xF3, 0xF9, 0xF3);

    public AlternatingRowsTests(AppiumServerFixture server) : base(server)
    {
    }

    [Fact]
    public void OutcomeList_ThreeEntries_TintsEveryOtherRowRed()
    {
        MainPage.AddEntry("Aluguel E2E", AppLabels.OneTime, 1000m);
        MainPage.AddEntry("Internet E2E", AppLabels.OneTime, 100m);
        MainPage.AddEntry("Academia E2E", AppLabels.OneTime, 90m);

        ShouldAlternate(MainPage.VisibleEntryDescriptions, OutcomeAlternate);
    }

    [Fact]
    public void IncomeList_TwoEntries_TintsSecondRowGreenAndKeepsOutcomesApart()
    {
        MainPage.AddEntry("Aluguel E2E", AppLabels.OneTime, 1000m);
        MainPage.AddEntry("Salário E2E", AppLabels.OneTime, 5000m, isIncome: true);
        MainPage.AddEntry("Freela E2E", AppLabels.OneTime, 800m, isIncome: true);

        BackgroundOf("Aluguel E2E").Should().Be(White, "the outcome list restarts the alternation");
        ShouldAlternate(["Salário E2E", "Freela E2E"], IncomeAlternate);
    }

    [Fact]
    public void OutcomeList_FirstEntryDeleted_AlternationFollowsNewOrder()
    {
        MainPage.AddEntry("Presente E2E", AppLabels.OneTime, 80m);
        MainPage.AddEntry("Internet E2E", AppLabels.OneTime, 100m);
        MainPage.AddEntry("Academia E2E", AppLabels.OneTime, 90m);
        var first = MainPage.VisibleEntryDescriptions[0];

        MainPage.DeleteEntry(first, AppLabels.Delete).WaitForEntryGone(first);

        ShouldAlternate(MainPage.VisibleEntryDescriptions, OutcomeAlternate);
    }

    private void ShouldAlternate(IReadOnlyList<string> descriptions, (byte R, byte G, byte B) alternate)
    {
        for (var i = 0; i < descriptions.Count; i++)
            BackgroundOf(descriptions[i]).Should().Be(i % 2 == 1 ? alternate : White, $"\"{descriptions[i]}\" is row {i + 1}");
    }

    private (byte R, byte G, byte B) BackgroundOf(string description) => MainPage.EntryRowBackground(description);
}
