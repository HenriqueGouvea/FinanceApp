using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class EntryActionsMenuTests : E2ETestBase
{
    public EntryActionsMenuTests(AppiumServerFixture server) : base(server)
    {
        MainPage.AddEntry("Farmácia E2E", AppLabels.OneTime, "45");
    }

    [Fact]
    public void LongPress_Entry_RevealsDeleteAction()
    {
        MainPage.IsDeleteActionRevealed.Should().BeFalse();

        MainPage.RevealActions("Farmácia E2E");

        MainPage.IsDeleteActionRevealed.Should().BeTrue();
    }

    [Fact]
    public void TapOutside_MenuOpen_HidesDeleteAction()
    {
        MainPage.RevealActions("Farmácia E2E");

        MainPage.TapMonthTitle();

        MainPage.WaitForActionsHidden();
    }

    [Fact]
    public void TapEntry_MenuOpen_HidesMenuWithoutOpeningEditPopup()
    {
        MainPage.RevealActions("Farmácia E2E");

        MainPage.TapEntry("Farmácia E2E");

        MainPage.WaitForActionsHidden();
        new AddEntryPopupObject(Session).IsOpen.Should().BeFalse();
    }
}
