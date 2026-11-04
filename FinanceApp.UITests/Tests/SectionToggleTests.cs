using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class SectionToggleTests : E2ETestBase
{
    public SectionToggleTests(AppiumServerFixture server) : base(server, data =>
    {
        data.AddEntry("Gasolina E2E", 300m);
        data.AddEntry("Freela E2E", 800m, isIncome: true);
    })
    {
    }

    [Fact]
    public void TapSectionHeaders_TogglesOnlyThatSectionInEveryMonth()
    {
        MainPage.ToggleOutcomes().WaitForEntryGone("Gasolina E2E");
        MainPage.IsEntryVisible("Freela E2E").Should().BeTrue();
        MainPage.OutcomeCountText.Should().Be(AppFormat.EntryCount(1), "the header summary stays visible when collapsed");
        MainPage.ToggleOutcomes().WaitForEntry("Gasolina E2E");
        MainPage.CollapsedSectionsHintText.Should().BeNull("both sections are expanded again");

        MainPage.ToggleIncomes().WaitForEntryGone("Freela E2E");
        MainPage.CollapsedSectionsHintText.Should().Be(AppLabels.IncomesCollapsedHint);

        MainPage.GoToNextMonth().WaitForEntry("Gasolina E2E");
        MainPage.IsEntryVisible("Freela E2E").Should().BeFalse("the collapsed state is shared by all months");
    }
}
