using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class EditEntryTests : E2ETestBase
{
    public EditEntryTests(AppiumServerFixture server) : base(server)
    {
        MainPage.AddEntry("Academia E2E", AppLabels.Recurrent, "90");
    }

    [Fact]
    public void EditEntry_TapEntry_OpensPrefilledPopupWithStructuralFieldsLocked()
    {
        var popup = MainPage.OpenEntry("Academia E2E");

        popup.TitleText.Should().Be(AppLabels.EditEntryTitle);
        popup.DescriptionText.Should().Be("Academia E2E");
        popup.AmountText.Should().Be("90");
        popup.RecurrenceText.Should().Be(AppLabels.Recurrent);
        popup.IsRecurrenceEnabled.Should().BeFalse("recurrence is a structural field");
        popup.IsEntryTypeEnabled.Should().BeFalse("entry type is a structural field");
    }

    [Fact]
    public async Task EditEntry_ChangeDescriptionAndAmount_UpdatesListAndDatabase()
    {
        MainPage.OpenEntry("Academia E2E")
            .EnterDescription("Academia Premium E2E")
            .EnterAmount("150")
            .Save();

        MainPage.WaitForEntry("Academia Premium E2E");
        MainPage.IsEntryVisible("Academia E2E").Should().BeFalse();
        MainPage.OutcomeTotalText.Should().Be(AppFormat.Money(150));

        MainPage.GoToNextMonth().WaitForEntry("Academia Premium E2E");
        MainPage.VisibleEntryAmounts.Should().ContainSingle().Which.Should().Be(AppFormat.Money(150));

        var row = (await GetEntryRowsAsync()).Should().ContainSingle().Subject;
        row.Description.Should().Be("Academia Premium E2E");
        row.Amount.Should().Be(150m);
        row.Recurrence.Should().Be(AppFormat.Recurrent);
        row.StartDate.Date.Should().Be(AppFormat.CurrentMonth);
    }
}
