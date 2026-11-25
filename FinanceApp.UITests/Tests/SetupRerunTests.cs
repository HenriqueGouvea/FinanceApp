using FinanceApp.UITests.Infrastructure;
using FinanceApp.UITests.Pages;
using FluentAssertions;
using FluentAssertions.Execution;

namespace FinanceApp.UITests.Tests;

/// <summary>
/// Running the setup wizard again from "Configurações" after it created some entries: it opens with them, saving
/// updates them instead of adding copies, and an unticked one is removed once the user picks from which months. Which
/// rows are filled in, the schedules kept and the amounts of past months are covered by the SetupViewModel and
/// SetupService tests.
/// </summary>
[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class SetupRerunTests : E2ETestBase
{
    private const string Account = "Nubank";
    private const string Salary = "Salário";
    private const string Rent = "Aluguel";
    private const string Internet = "Internet";
    private const string Gym = "Academia";

    public SetupRerunTests(AppiumServerFixture server) : base(server, data =>
    {
        var account = data.AddPaymentSource(Account, AppFormat.BankAccount, 2500m);
        var start = AppFormat.CurrentMonth.AddMonths(-2);
        data.LinkToSetupRow(data.AddEntry(Salary, 5000m, isIncome: true, startMonth: start, dueDay: 5, paymentSourceId: account), "salary");
        data.LinkToSetupRow(data.AddEntry(Rent, 1500m, startMonth: start, paymentSourceId: account), $"expense:{Rent}");
        data.LinkToSetupRow(data.AddEntry(Internet, 100m, startMonth: start, paymentSourceId: account), $"expense:{Internet}");
        data.AddEntry(Gym, 90m, startMonth: start);
    })
    {
    }

    [Theory]
    [InlineData(AppLabels.SetupRemoveFromThisMonth)]
    [InlineData(AppLabels.SetupRemoveFromAllMonths)]
    public async Task RedoSetup_ChangeSalaryAndUntickAnExpense_UpdatesTheEntriesAndRemovesTheUnticked(string monthsOption)
    {
        var setup = MainPage.RedoSetup().Next();
        setup.ExistingSourceNames.Should().Equal(Account);

        setup.Next().Next();
        setup.SalaryText.Should().Be(AppFormat.Money(5000m));
        setup.EnterSalary(6000m).Next();
        setup.IsExpenseChecked(Rent).Should().BeTrue();
        setup.ToggleExpense(Internet).Next().Next();
        setup.RemovedEntriesText.Should().Be(AppLabels.SetupRemovedEntries($"\"{Internet}\""));

        setup.FinishRemoving(monthsOption);

        var entries = (await GetEntryRowsAsync()).ToDictionary(e => e.Description);
        var linked = (await GetSetupLinkRowsAsync()).Select(l => entries.Values.Single(e => e.Id == l.FinancialEntryId).Description);
        using (new AssertionScope())
        {
            entries[Salary].Amount.Should().Be(6000m);
            entries[Salary].StartDate.Should().Be(AppFormat.CurrentMonth.AddMonths(-2), "the salary keeps its past months");
            entries[Rent].EndDate.Should().BeNull();
            entries[Gym].EndDate.Should().BeNull("entries the wizard didn't create are left alone");
            linked.Should().BeEquivalentTo([Salary, Rent], "the unticked expense is no longer the wizard's");
            if (monthsOption == AppLabels.SetupRemoveFromThisMonth)
            {
                entries.Keys.Should().BeEquivalentTo([Salary, Rent, Internet, Gym], "saving updates the entries instead of adding copies");
                entries[Internet].EndDate.Should().Be(AppFormat.CurrentMonth.AddMonths(-1));
            }
            else
            {
                entries.Keys.Should().BeEquivalentTo([Salary, Rent, Gym], "the unticked expense is gone from every month");
            }
        }
    }
}
