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
        MainPage.AddEntry("Academia E2E", AppLabels.Recurrent, 90m);
    }

    [Fact]
    public void EditEntry_TapEntry_OpensPrefilledPageWithStructuralFieldsLocked()
    {
        var popup = MainPage.OpenEntry("Academia E2E");

        popup.TitleText.Should().Be(AppLabels.EditEntryTitle);
        popup.DescriptionText.Should().Be("Academia E2E");
        popup.AmountText.Should().Be(AppFormat.Money(90));
        popup.RecurrenceText.Should().Be(AppLabels.Recurrent);
        popup.IsRecurrenceEnabled.Should().BeFalse("recurrence is a structural field");
        popup.IsEntryTypeEnabled.Should().BeFalse("entry type is a structural field");
    }

    [Fact]
    public async Task EditEntry_ChangeDescriptionAndAmount_UpdatesListAndDatabase()
    {
        MainPage.OpenEntry("Academia E2E")
            .EnterDescription("Academia Premium E2E")
            .EnterAmount(150)
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

    [Fact]
    public async Task EditEntry_BackButton_DiscardsChanges()
    {
        MainPage.OpenEntry("Academia E2E")
            .EnterDescription("Academia Alterada E2E")
            .EnterAmount(150)
            .GoBack();

        MainPage.WaitForEntry("Academia E2E");
        MainPage.IsEntryVisible("Academia Alterada E2E").Should().BeFalse();
        MainPage.OutcomeTotalText.Should().Be(AppFormat.Money(90));

        var row = (await GetEntryRowsAsync()).Should().ContainSingle().Subject;
        row.Description.Should().Be("Academia E2E");
        row.Amount.Should().Be(90m);
    }

    [Fact]
    public async Task EditEntry_SystemBack_DiscardsChanges()
    {
        MainPage.OpenEntry("Academia E2E")
            .EnterAmount(150)
            .PressSystemBack();

        MainPage.WaitForEntry("Academia E2E");
        MainPage.OutcomeTotalText.Should().Be(AppFormat.Money(90));
        (await GetEntryRowsAsync()).Should().ContainSingle().Which.Amount.Should().Be(90m);
    }
}
