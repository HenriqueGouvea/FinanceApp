using FinanceApp.Converters;
using FinanceApp.Models;
using FluentAssertions;
using System.Globalization;
using Xunit;

namespace FinanceApp.Tests.Converters;

public class PaymentSourceTypeConverterTests
{
    [Theory]
    [InlineData(PaymentSourceType.BankAccount, "Conta bancária")]
    [InlineData(PaymentSourceType.CreditCard, "Cartão de crédito")]
    [InlineData(PaymentSourceType.MealVoucher, "Vale-refeição")]
    public void LabelConverter_Type_ReturnsPortugueseLabel(PaymentSourceType type, string expected)
    {
        Convert(PaymentSourceTypeToLabelConverter.Instance, type).Should().Be(expected);
    }

    [Theory]
    [InlineData(PaymentSourceType.BankAccount, "🏦")]
    [InlineData(PaymentSourceType.CreditCard, "💳")]
    [InlineData(PaymentSourceType.MealVoucher, "🍽️")]
    public void IconConverter_Type_ReturnsIcon(PaymentSourceType type, string expected)
    {
        Convert(PaymentSourceTypeToIconConverter.Instance, type).Should().Be(expected);
    }

    [Theory]
    [InlineData(PaymentSourceType.BankAccount, "Saldo")]
    [InlineData(PaymentSourceType.CreditCard, "Limite")]
    [InlineData(PaymentSourceType.MealVoucher, "Saldo")]
    public void BalanceLabelConverter_Type_ReturnsBalanceOrLimit(PaymentSourceType type, string expected)
    {
        Convert(PaymentSourceTypeToBalanceLabelConverter.Instance, type).Should().Be(expected);
    }

    [Fact]
    public void Converters_NonTypeValue_ReturnEmpty()
    {
        Convert(PaymentSourceTypeToLabelConverter.Instance, null).Should().Be(string.Empty);
        Convert(PaymentSourceTypeToIconConverter.Instance, null).Should().Be(string.Empty);
        Convert(PaymentSourceTypeToBalanceLabelConverter.Instance, null).Should().Be(string.Empty);
    }

    private static object? Convert(IValueConverter converter, object? value) =>
        converter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);
}
