using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;
using FluentAssertions.Execution;

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
        data.AddEntry("Aluguel E2E", 1500m, categoryIds: [data.CategoryId("Moradia")]))
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

        var category = (await GetCategoryRowsAsync()).Should().ContainSingle(c => c.Name == "Pets E2E").Subject;
        category.Icon.Should().NotBeEmpty();
        category.Color.Should().StartWith("#");
        category.EntryType.Should().Be(AppFormat.Outcome, "new categories default to outcome");
        var entry = (await GetEntryRowsAsync()).Single(e => e.Description == "Ração E2E");
        (await GetEntryCategoryIdsAsync(entry.Id)).Should().Equal(category.Id);

        // A single category is the row's icon, so the details line doesn't name it.
        using (new AssertionScope())
        {
            MainPage.EntryCategoryIcon("Ração E2E").Should().Be(category.Icon);
            MainPage.EntryCategoryNames("Ração E2E").Should().BeNull();
            MainPage.IsEntryUncategorized("Ração E2E").Should().BeFalse();
        }
    }

    [Fact]
    public async Task AddEntry_TwoCategories_SavesBothWithTheFirstPickedAsTheIcon()
    {
        var entryPage = MainPage.OpenAddEntry()
            .EnterDescription("Farmácia E2E")
            .SelectRecurrence(AppLabels.OneTime)
            .EnterAmount(80m)
            .ToggleCategory("Saúde")
            .ToggleCategory("Educação");

        entryPage.MainCategoryHint.Should().Contain("🩺 Saúde");
        entryPage.Save();
        MainPage.WaitForEntry("Farmácia E2E");

        var categories = await GetCategoryRowsAsync();
        var entry = (await GetEntryRowsAsync()).Single(e => e.Description == "Farmácia E2E");
        (await GetEntryCategoryIdsAsync(entry.Id)).Should().Equal(
            categories.Single(c => c.Name == "Saúde").Id,
            categories.Single(c => c.Name == "Educação").Id);
        using (new AssertionScope())
        {
            MainPage.EntryCategoryIcon("Farmácia E2E").Should().Be("🩺");
            MainPage.EntryCategoryNames("Farmácia E2E").Should().Be("Saúde, Educação");
        }
    }

    [Fact]
    public async Task DeleteCategory_UsedByEntry_KeepsEntryUncategorized()
    {
        MainPage.EntryCategoryIcon("Aluguel E2E").Should().Be("🏠");

        MainPage.OpenCategories()
            .DeleteCategory("Moradia")
            .GoToEntries()
            .WaitForEntry("Aluguel E2E");

        using (new AssertionScope())
        {
            MainPage.EntryCategoryIcon("Aluguel E2E").Should().Be(AppLabels.UncategorizedIcon);
            MainPage.IsEntryUncategorized("Aluguel E2E").Should().BeTrue();
        }
        (await GetCategoryRowsAsync()).Should().NotContain(c => c.Name == "Moradia");
        var entry = (await GetEntryRowsAsync()).Should().ContainSingle().Subject;
        (await GetEntryCategoryIdsAsync(entry.Id)).Should().BeEmpty();
    }
}
