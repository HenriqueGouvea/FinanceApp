using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class SectionToggleTests : E2ETestBase
{
    public SectionToggleTests(AppiumServerFixture server) : base(server)
    {
        MainPage.AddEntry("Gasolina E2E", AppLabels.Recurrent, "300");
        MainPage.AddEntry("Freela E2E", AppLabels.Recurrent, "800", isIncome: true);
    }

    [Fact]
    public void TapOutcomesHeader_Expanded_TogglesOnlyOutcomeEntries()
    {
        MainPage.ToggleOutcomes().WaitForEntryGone("Gasolina E2E");

        MainPage.IsEntryVisible("Freela E2E").Should().BeTrue();
        MainPage.OutcomeCountText.Should().Be(AppFormat.EntryCount(1), "the header summary stays visible when collapsed");

        MainPage.ToggleOutcomes().WaitForEntry("Gasolina E2E");
    }

    [Fact]
    public void TapIncomesHeader_Expanded_CollapsesInEveryMonth()
    {
        MainPage.ToggleIncomes().WaitForEntryGone("Freela E2E");

        MainPage.GoToNextMonth().WaitForEntry("Gasolina E2E");
        MainPage.IsEntryVisible("Freela E2E").Should().BeFalse("the collapsed state is shared by all months");
    }
}
