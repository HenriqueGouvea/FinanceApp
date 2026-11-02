using FinanceApp.Models;
using FluentAssertions;
using Xunit;

namespace FinanceApp.Tests.Models;

public class CreditCardBillTests
{
    private static readonly PaymentSource Card = new() { Id = 3, Name = "Nubank", Type = PaymentSourceType.CreditCard };
    private static readonly PaymentSource Account = new() { Id = 1, Name = "Itaú", Type = PaymentSourceType.BankAccount };

    // --- Status ---

    [Fact]
    public void Status_NoEntryPaid_IsPending()
    {
        var bill = MakeBill(Make(FinancialMonthlyEntryStatus.Pending), Make(FinancialMonthlyEntryStatus.Pending));

        bill.IsPending.Should().BeTrue();
    }

    [Fact]
    public void Status_EveryEntryPaid_IsPaid()
    {
        var bill = MakeBill(Make(FinancialMonthlyEntryStatus.Paid), Make(FinancialMonthlyEntryStatus.Paid));

        bill.IsPaid.Should().BeTrue();
    }

    [Theory]
    [InlineData(FinancialMonthlyEntryStatus.Paid, FinancialMonthlyEntryStatus.Pending)]
    [InlineData(FinancialMonthlyEntryStatus.PartiallyPaid, FinancialMonthlyEntryStatus.Pending)]
    [InlineData(FinancialMonthlyEntryStatus.PartiallyPaid, FinancialMonthlyEntryStatus.Paid)]
    public void Status_SomeEntriesPaid_IsPartiallyPaid(FinancialMonthlyEntryStatus first, FinancialMonthlyEntryStatus second)
    {
        var bill = MakeBill(Make(first), Make(second));

        bill.IsPartiallyPaid.Should().BeTrue();
    }

    // --- Amounts ---

    [Fact]
    public void Amounts_SumTheEntries()
    {
        var bill = MakeBill(
            Make(FinancialMonthlyEntryStatus.Paid, amount: 100m, paidAmount: 100m),
            Make(FinancialMonthlyEntryStatus.PartiallyPaid, amount: 200m, paidAmount: 50m));

        bill.TotalAmount.Should().Be(300m);
        bill.PaidAmount.Should().Be(150m);
        bill.EntryCount.Should().Be(2);
    }

    // --- Due date ---

    [Fact]
    public void IsDueToday_DueTodayAndNotPaid_IsTrue()
    {
        var bill = MakeBill(Make(FinancialMonthlyEntryStatus.Pending)) with { DueDate = DateTime.Today };

        bill.IsDueToday.Should().BeTrue();
    }

    [Fact]
    public void IsDueToday_DueTodayButPaid_IsFalse()
    {
        var bill = MakeBill(Make(FinancialMonthlyEntryStatus.Paid)) with { DueDate = DateTime.Today };

        bill.IsDueToday.Should().BeFalse();
    }

    [Fact]
    public void HasDueDateAndPaymentAccount_FollowTheirValues()
    {
        var bill = MakeBill(Make(FinancialMonthlyEntryStatus.Pending));

        bill.HasDueDate.Should().BeFalse();
        bill.HasPaymentAccount.Should().BeFalse();
        (bill with { DueDate = DateTime.Today, PaymentAccount = Account }).HasDueDate.Should().BeTrue();
        (bill with { PaymentAccount = Account }).HasPaymentAccount.Should().BeTrue();
    }

    // --- GroupFrom ---

    [Fact]
    public void GroupFrom_TakesCardDueDateAndPaymentAccountFromItsEntries()
    {
        var dueDate = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Local);
        var entry = Make(FinancialMonthlyEntryStatus.Pending) with { DueDate = dueDate, BillPaymentAccount = Account };

        var bill = CreditCardBill.GroupFrom([entry]).Single();

        bill.Card.Should().Be(Card);
        bill.DueDate.Should().Be(dueDate);
        bill.PaymentAccount.Should().Be(Account);
    }

    [Fact]
    public void GroupFrom_SkipsEntriesNotOnACreditCard()
    {
        var onAccount = Make(FinancialMonthlyEntryStatus.Pending) with { PaymentSource = Account };
        var unlinked = Make(FinancialMonthlyEntryStatus.Pending) with { PaymentSource = null };

        CreditCardBill.GroupFrom([onAccount, unlinked]).Should().BeEmpty();
    }

    [Fact]
    public void GroupFrom_CollapsedCard_IsNotExpanded()
    {
        var bill = CreditCardBill.GroupFrom([Make(FinancialMonthlyEntryStatus.Pending)], new HashSet<int> { Card.Id }).Single();

        bill.IsExpanded.Should().BeFalse();
    }

    private static CreditCardBill MakeBill(params MonthlyEntryProjection[] entries) =>
        new(Card, PaymentAccount: null, DueDate: null, entries);

    private static MonthlyEntryProjection Make(FinancialMonthlyEntryStatus status, decimal amount = 100m, decimal paidAmount = 0m) =>
        new(
            FinancialEntryId: 1,
            FinancialMonthlyEntryId: null,
            Description: "Netflix",
            EffectiveAmount: amount,
            EntryType: FinancialEntryType.Outcome,
            Category: null,
            Status: status,
            IsProjected: false,
            InstallmentLabel: null,
            InstallmentTotalAmount: null,
            PaidAmount: paidAmount,
            PaymentSource: Card);
}
