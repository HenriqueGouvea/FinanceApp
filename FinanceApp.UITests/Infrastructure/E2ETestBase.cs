using FinanceApp.UITests.Pages;

namespace FinanceApp.UITests.Infrastructure;

/// <summary>
/// Relaunches the app with empty data before each test (xUnit creates one instance per test), skips the first-run
/// setup wizard and lands on the entries page. Tests of the wizard itself pass <c>skipSetup: false</c> and start on it.
/// Derived classes must carry [Collection(E2ECollection.Name)] so they share the Appium server and session.
/// </summary>
public abstract class E2ETestBase
{
    protected AppSession Session { get; }
    protected MainPageObject MainPage { get; }

    protected E2ETestBase(AppiumServerFixture server, bool skipSetup = true)
    {
        Session = server.StartApp();
        try
        {
            var setup = new SetupPageObject(Session).WaitUntilLoaded();
            MainPage = skipSetup ? setup.Skip() : new MainPageObject(Session);
        }
        catch
        {
            server.DiscardSession();
            throw;
        }
    }

    protected Task<List<FinancialEntryRow>> GetEntryRowsAsync() =>
        Session.Database.QueryAsync<FinancialEntryRow>("SELECT * FROM FinancialEntry");

    protected Task<List<FinancialMonthlyEntryRow>> GetMonthlyEntryRowsAsync() =>
        Session.Database.QueryAsync<FinancialMonthlyEntryRow>("SELECT * FROM FinancialMonthlyEntry");

    protected Task<List<FinancialPaymentRow>> GetPaymentRowsAsync() =>
        Session.Database.QueryAsync<FinancialPaymentRow>("SELECT * FROM FinancialPayment ORDER BY Id");

    protected Task<List<FinancialCategoryRow>> GetCategoryRowsAsync() =>
        Session.Database.QueryAsync<FinancialCategoryRow>("SELECT * FROM FinancialCategory");

    protected Task<List<PaymentSourceRow>> GetPaymentSourceRowsAsync() =>
        Session.Database.QueryAsync<PaymentSourceRow>("SELECT * FROM PaymentSource");
}
