using FinanceApp.UITests.Pages;

namespace FinanceApp.UITests.Infrastructure;

/// <summary>
/// Starts a fresh app session per test (xUnit creates one instance per test) and lands on the entries page.
/// Derived classes must carry [Collection(E2ECollection.Name)] so they share the Appium server.
/// </summary>
public abstract class E2ETestBase : IDisposable
{
    protected AppSession Session { get; }
    protected MainPageObject MainPage { get; }

    protected E2ETestBase(AppiumServerFixture server)
    {
        Session = new AppSession(server);
        try
        {
            MainPage = new MainPageObject(Session).WaitUntilLoaded();
        }
        catch
        {
            Session.Dispose();
            throw;
        }
    }

    protected Task<List<FinancialEntryRow>> GetEntryRowsAsync() =>
        Session.Database.QueryAsync<FinancialEntryRow>("SELECT * FROM FinancialEntry");

    protected Task<List<FinancialMonthlyEntryRow>> GetMonthlyEntryRowsAsync() =>
        Session.Database.QueryAsync<FinancialMonthlyEntryRow>("SELECT * FROM FinancialMonthlyEntry");

    protected Task<List<FinancialCategoryRow>> GetCategoryRowsAsync() =>
        Session.Database.QueryAsync<FinancialCategoryRow>("SELECT * FROM FinancialCategory");

    public void Dispose() => Session.Dispose();
}
