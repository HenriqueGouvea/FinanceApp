using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class EntryActionsMenuTests : E2ETestBase
{
    public EntryActionsMenuTests(AppiumServerFixture server) : base(server, data => data.AddEntry("Farmácia E2E", 45m, AppFormat.OneTime))
    {
    }

    [Fact]
    public void LongPress_Entry_RevealsActionsHiddenByTappingOutsideOrOnTheEntry()
    {
        MainPage.IsDeleteActionRevealed.Should().BeFalse();

        MainPage.RevealActions("Farmácia E2E");
        MainPage.IsDeleteActionRevealed.Should().BeTrue();
        MainPage.RevealedStatusActionText.Should().Be(AppLabels.MarkPaid);

        MainPage.TapSummaryBar();
        MainPage.WaitForActionsHidden();

        MainPage.RevealActions("Farmácia E2E");
        MainPage.TapEntry("Farmácia E2E");
        MainPage.WaitForActionsHidden();
        new AddEntryPageObject(Session).IsOpen.Should().BeFalse("tapping the entry only closes the menu");
    }
}
