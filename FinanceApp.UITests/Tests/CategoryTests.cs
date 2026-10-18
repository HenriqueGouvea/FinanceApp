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

        categories.VisibleCategoryNames.Should().BeEquivalentTo(AppLabels.DefaultCategories);
        (await GetCategoryRowsAsync()).Select(c => c.Name).Should().BeEquivalentTo(AppLabels.DefaultCategories);
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
