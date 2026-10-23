using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class CategoryTests : E2ETestBase
{
    public CategoryTests(AppiumServerFixture server) : base(server)
    {
    }

    [Fact]
    public async Task OpenCategories_FreshInstall_ShowsSeededDefaults()
    {
        var categories = MainPage.OpenCategories();

        categories.VisibleCategoryNames.Should().BeEquivalentTo(
            [.. AppLabels.DefaultOutcomeCategories, .. AppLabels.DefaultIncomeCategories]);
        categories.VisibleGroupHeaders.Should().Equal(AppLabels.OutcomeCategoriesHeader, AppLabels.IncomeCategoriesHeader);
        categories.GroupOf("Moradia").Should().Be(AppLabels.OutcomeCategoriesHeader);
        categories.GroupOf("Salário").Should().Be(AppLabels.IncomeCategoriesHeader);

        var rows = await GetCategoryRowsAsync();
        rows.Where(c => c.EntryType == AppFormat.Outcome).Select(c => c.Name)
            .Should().BeEquivalentTo(AppLabels.DefaultOutcomeCategories);
        rows.Where(c => c.EntryType == AppFormat.Income).Select(c => c.Name)
            .Should().BeEquivalentTo(AppLabels.DefaultIncomeCategories);
    }

    [Fact]
    public void AddEntry_Outcome_OffersOnlyOutcomeCategories()
    {
        var popup = MainPage.OpenAddEntry();

        popup.CategoryOptions().Should().BeEquivalentTo(AppLabels.DefaultOutcomeCategories);
    }

    [Fact]
    public void AddEntry_SwitchToIncomeAndBack_OffersCategoriesOfSelectedType()
    {
        var popup = MainPage.OpenAddEntry().SelectIncome();

        popup.CategoryOptions().Should().BeEquivalentTo(AppLabels.DefaultIncomeCategories);

        popup.SelectOutcome();
        popup.CategoryOptions().Should().BeEquivalentTo(AppLabels.DefaultOutcomeCategories);
    }

    [Fact]
    public async Task AddEntry_IncomeWithIncomeCategory_ShowsChipAndPersists()
    {
        MainPage.AddEntry("Salário E2E", AppLabels.Recurrent, "5000", isIncome: true, category: "Salário");

        MainPage.VisibleEntryCategories.Should().ContainSingle().Which.Should().Be("Salário");

        var salary = (await GetCategoryRowsAsync()).Single(c => c.Name == "Salário");
        (await GetEntryRowsAsync()).Should().ContainSingle().Which.CategoryId.Should().Be(salary.Id);
    }

    [Fact]
    public async Task AddCategory_Income_ListedUnderIncomesAndOfferedOnlyForIncomes()
    {
        var categories = MainPage.OpenCategories().AddCategory("Freelance E2E", isIncome: true);

        categories.GroupOf("Freelance E2E").Should().Be(AppLabels.IncomeCategoriesHeader);
        (await GetCategoryRowsAsync()).Should().ContainSingle(c => c.Name == "Freelance E2E")
            .Which.EntryType.Should().Be(AppFormat.Income);

        var popup = categories.GoToEntries().OpenAddEntry();
        popup.CategoryOptions().Should().NotContain("Freelance E2E");
        popup.SelectIncome().CategoryOptions().Should().Contain("Freelance E2E");
    }

    [Fact]
    public async Task AddCategory_ValidName_PersistsAndIsSelectableForEntries()
    {
        MainPage.OpenCategories()
            .AddCategory("Pets E2E")
            .GoToEntries()
            .AddEntry("Ração E2E", AppLabels.OneTime, "130", category: "Pets E2E");

        MainPage.VisibleEntryCategories.Should().ContainSingle().Which.Should().Be("Pets E2E");

        var category = (await GetCategoryRowsAsync()).Should().ContainSingle(c => c.Name == "Pets E2E").Subject;
        category.Icon.Should().NotBeEmpty();
        category.Color.Should().StartWith("#");
        category.EntryType.Should().Be(AppFormat.Outcome, "new categories default to outcome");
        (await GetEntryRowsAsync()).Should().ContainSingle().Which.CategoryId.Should().Be(category.Id);
    }

    [Fact]
    public async Task DeleteCategory_UsedByEntry_KeepsEntryUncategorized()
    {
        MainPage.AddEntry("Aluguel E2E", AppLabels.Recurrent, "1500", category: "Moradia");

        MainPage.OpenCategories()
            .DeleteCategory("Moradia")
            .GoToEntries()
            .WaitForEntry("Aluguel E2E");

        MainPage.VisibleEntryCategories.Should().BeEmpty();
        (await GetCategoryRowsAsync()).Should().NotContain(c => c.Name == "Moradia");
        (await GetEntryRowsAsync()).Should().ContainSingle().Which.CategoryId.Should().BeNull();
    }
}
