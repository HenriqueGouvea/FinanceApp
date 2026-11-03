using FinanceApp.Models;
using FluentAssertions;
using Xunit;

namespace FinanceApp.Tests.Models;

public class PaymentSourceSummaryTests
{
    private static readonly PaymentSource Itau = new() { Id = 1, Name = "Itaú", Type = PaymentSourceType.BankAccount, Balance = 2500m };
    private static readonly PaymentSource Nubank = new() { Id = 2, Name = "Nubank", Type = PaymentSourceType.CreditCard, Balance = 5000m };

    // --- CurrentBalance ---

    [Fact]
    public void CurrentBalance_BankAccount_IsTheTypedBalance()
    {
        var summary = new PaymentSourceSummary(Itau);

        summary.CurrentBalance.Should().Be(2500m);
        summary.IsCreditCard.Should().BeFalse();
    }

    [Fact]
    public void CurrentBalance_CreditCard_IsTheLimitLeft()
    {
        var summary = new PaymentSourceSummary(Nubank, UsedLimit: 1200m);

        summary.AvailableLimit.Should().Be(3800m);
        summary.CurrentBalance.Should().Be(3800m);
        summary.IsOverLimit.Should().BeFalse();
    }

    // --- IsOverLimit ---

    [Fact]
    public void IsOverLimit_CreditCardUsedBeyondItsLimit_IsTrue()
    {
        var summary = new PaymentSourceSummary(Nubank, UsedLimit: 5200m);

        summary.AvailableLimit.Should().Be(-200m);
        summary.IsOverLimit.Should().BeTrue();
    }

    [Fact]
    public void IsOverLimit_NegativeBankBalance_IsFalse()
    {
        var overdrawn = new PaymentSource { Id = 3, Name = "Inter", Type = PaymentSourceType.BankAccount, Balance = -50m };

        new PaymentSourceSummary(overdrawn).IsOverLimit.Should().BeFalse();
    }

    // --- Build ---

    [Fact]
    public void Build_PairsEachSourceWithItsUsedLimit_KeepingTheOrder()
    {
        var summaries = PaymentSourceSummary.Build([Nubank, Itau], new Dictionary<int, decimal> { [Nubank.Id] = 700m });

        summaries.Should().Equal(new PaymentSourceSummary(Nubank, 700m), new PaymentSourceSummary(Itau, 0m));
    }
}
