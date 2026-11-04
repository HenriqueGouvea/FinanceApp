using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;
using FluentAssertions.Execution;

namespace FinanceApp.UITests.Tests;

[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class EditEntryTests : E2ETestBase
{
    public EditEntryTests(AppiumServerFixture server) : base(server, data => data.AddEntry("Academia E2E", 90m))
    {
    }

    [Fact]
    [Trait("Smoke", "true")]
    public async Task EditEntry_ChangeDescriptionAndAmount_UpdatesListInEveryMonthAndDatabase()
    {
        var page = MainPage.OpenEntry("Academia E2E");
        using (new AssertionScope())
        {
            page.TitleText.Should().Be(AppLabels.EditEntryTitle);
            page.DescriptionText.Should().Be("Academia E2E");
            page.AmountText.Should().Be(AppFormat.Money(90));
            page.RecurrenceText.Should().Be(AppLabels.Recurrent);
            page.IsRecurrenceEnabled.Should().BeFalse("recurrence is a structural field");
            page.IsEntryTypeEnabled.Should().BeFalse("entry type is a structural field");
        }

        page.EnterDescription("Academia Premium E2E")
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
    public async Task EditEntry_BackButtonOrSystemBack_DiscardsChanges()
    {
        MainPage.OpenEntry("Academia E2E")
            .EnterDescription("Academia Alterada E2E")
            .EnterAmount(150)
            .GoBack();

        MainPage.WaitForEntry("Academia E2E");
        MainPage.IsEntryVisible("Academia Alterada E2E").Should().BeFalse();

        MainPage.OpenEntry("Academia E2E")
            .EnterAmount(150)
            .PressSystemBack();

        MainPage.WaitForEntry("Academia E2E");
        MainPage.OutcomeTotalText.Should().Be(AppFormat.Money(90));

        var row = (await GetEntryRowsAsync()).Should().ContainSingle().Subject;
        row.Description.Should().Be("Academia E2E");
        row.Amount.Should().Be(90m);
    }
}
