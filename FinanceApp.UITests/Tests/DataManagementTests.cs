using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;
using FluentAssertions.Execution;

namespace FinanceApp.UITests.Tests;

/// <summary>
/// "Seus dados" in "Configurações": exporting a backup and a spreadsheet through the share sheet, importing a backup
/// picked from Downloads, and wiping everything. The backup's contents are covered by the unit tests; these check the
/// wiring from the settings to the files, the database and the screens.
/// </summary>
[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class DataManagementTests : E2ETestBase, IDisposable
{
    private readonly string _backupFileName = $"mensal-backup-e2e-{Guid.NewGuid().ToString("N")[..8]}.json";

    public DataManagementTests(AppiumServerFixture server) : base(server, data =>
    {
        var account = data.AddPaymentSource("Itaú", AppFormat.BankAccount, 1500m);
        data.AddEntry("Aluguel", 2000m, dueDay: 5, categoryId: data.CategoryId("Moradia"), paymentSourceId: account);
    })
    {
    }

    public void Dispose() => DeviceFiles.DeleteDownload(_backupFileName);

    [Fact]
    public async Task ExportData_SharesTheBackupAndTheSpreadsheet()
    {
        var settings = MainPage.OpenSettings();

        var sharedFile = settings.ExportData();

        var backup = await DeviceFiles.ReadAppFileAsync("cache/export/mensal-backup-*.json");
        var csv = await DeviceFiles.ReadAppFileAsync("cache/export/mensal-lancamentos-*.csv");
        using (new AssertionScope())
        {
            sharedFile.Should().StartWith("mensal-backup-").And.EndWith(".json");
            backup.Should().Contain("\"format\": \"mensal-backup\"").And.Contain("\"description\": \"Aluguel\"").And.Contain("\"name\": \"Itaú\"");
            csv.Should().Contain("Despesa;Aluguel;2000,00;Todo mês;");
        }

        settings.CloseShareSheet();
    }

    [Fact]
    public async Task ImportBackup_PickedFromDownloads_ReplacesTheDataAndShowsIt()
    {
        DeviceFiles.AddDownload(_backupFileName, BackupJson(AppFormat.CurrentMonth));

        var home = MainPage.OpenSettings().ImportBackup(_backupFileName);

        home.WaitForEntry("Internet");
        using (new AssertionScope())
        {
            home.IsEntryVisible("Aluguel").Should().BeFalse();
            home.HeaderAvatarInitialText.Should().Be("B");
            (await GetEntryRowsAsync()).Select(e => (e.Id, e.Description)).Should().Equal((3, "Internet"));
            (await GetPaymentSourceRowsAsync()).Select(s => (s.Id, s.Name)).Should().Equal((7, "Conta Nova"));
            (await GetCategoryRowsAsync()).Select(c => c.Name).Should().Equal("Casa");
        }
    }

    [Fact]
    public async Task DeleteAllData_Confirmed_WipesEverythingAndStartsOverWithTheSetupWizard()
    {
        MainPage.OpenSettings().DeleteAllData();

        using (new AssertionScope())
        {
            (await GetEntryRowsAsync()).Should().BeEmpty();
            (await GetPaymentSourceRowsAsync()).Should().BeEmpty();
            (await GetCategoryRowsAsync()).Select(c => c.Name).Should().Equal(
                "Moradia", "Educação", "Mercado", "Saúde", "Transporte", "Lazer", "Salário", "Investimentos");
        }
    }

    // A backup as "Exportar dados" writes it, with ids that differ from the seeded rows' so they're known to come from it.
    private static string BackupJson(DateTime month) => $$"""
        {
          "format": "mensal-backup",
          "version": 1,
          "exportedAt": "{{month:yyyy-MM-dd}}T10:00:00-03:00",
          "profileName": "Bia",
          "categories": [ { "id": 4, "name": "Casa", "icon": "🏠", "color": "#1E88E5", "entryType": "Outcome" } ],
          "paymentSources": [ { "id": 7, "name": "Conta Nova", "type": "BankAccount", "balance": 100 } ],
          "entries": [
            {
              "id": 3, "description": "Internet", "amount": 120, "startDate": "{{month:yyyy-MM-dd}}T00:00:00",
              "recurrence": "Recurrent", "entryType": "Outcome", "categoryId": 4, "paymentSourceId": 7
            }
          ],
          "monthlyEntries": [],
          "payments": []
        }
        """;
}
