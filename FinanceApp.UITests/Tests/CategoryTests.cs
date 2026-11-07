using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;

namespace FinanceApp.UITests.Tests;

/// <summary>
/// The "Categorias" page and picking a category for an entry. Which categories the entry page offers per entry type
/// is covered by the AddEntryViewModel and CategoryEditViewModel tests.
/// </summary>
[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class CategoryTests : E2ETestBase
{
    public CategoryTests(AppiumServerFixture server) : base(server, data =>
        data.AddEntry("Aluguel E2E", 1500m, categoryId: data.CategoryId("Moradia")))
    {
    }

    [Fact]
    public async Task OpenCategories_FreshInstall_ShowsSeededDefaultsGroupedByType()
    {
        var categories = MainPage.OpenCategories();

        categories.VisibleGroupHeaders.Should().StartWith(AppLabels.OutcomeCategoriesHeader);
        categories.GroupOf("Moradia").Should().Be(AppLabels.OutcomeCategoriesHeader);
        categories.AllCategoryNames().Should().BeEquivalentTo(
            [.. AppLabels.DefaultOutcomeCategories, .. AppLabels.DefaultIncomeCategories]);
        categories.GroupOf("Salário").Should().Be(AppLabels.IncomeCategoriesHeader);

        var rows = await GetCategoryRowsAsync();
        rows.Where(c => c.EntryType == AppFormat.Outcome).Select(c => c.Name)
            .Should().BeEquivalentTo(AppLabels.DefaultOutcomeCategories);
        rows.Where(c => c.EntryType == AppFormat.Income).Select(c => c.Name)
            .Should().BeEquivalentTo(AppLabels.DefaultIncomeCategories);
    }

    [Fact]
    [Trait("Smoke", "true")]
    public async Task AddCategory_ValidName_PersistsAndIsSelectableForEntries()
    {
        MainPage.OpenCategories()
            .AddCategory("Pets E2E")
            .GoToEntries()
            .AddEntry("Ração E2E", AppLabels.OneTime, 130m, category: "Pets E2E");

        MainPage.VisibleEntryCategories.Should().Contain("Pets E2E");

        var category = (await GetCategoryRowsAsync()).Should().ContainSingle(c => c.Name == "Pets E2E").Subject;
        category.Icon.Should().NotBeEmpty();
        category.Color.Should().StartWith("#");
        category.EntryType.Should().Be(AppFormat.Outcome, "new categories default to outcome");
        (await GetEntryRowsAsync()).Single(e => e.Description == "Ração E2E").CategoryId.Should().Be(category.Id);
    }

    [Fact]
    public async Task DeleteCategory_UsedByEntry_KeepsEntryUncategorized()
    {
        MainPage.VisibleEntryCategories.Should().ContainSingle().Which.Should().Be("Moradia");

        MainPage.OpenCategories()
            .DeleteCategory("Moradia")
            .GoToEntries()
            .WaitForEntry("Aluguel E2E");

        MainPage.VisibleEntryCategories.Should().BeEmpty();
        (await GetCategoryRowsAsync()).Should().NotContain(c => c.Name == "Moradia");
        (await GetEntryRowsAsync()).Should().ContainSingle().Which.CategoryId.Should().BeNull();
    }
}
