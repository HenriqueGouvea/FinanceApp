using FinanceApp.Controls;
using FluentAssertions;
using Xunit;

namespace FinanceApp.Tests.Controls;

public class PaymentProgressBarTests
{
    [Theory]
    [InlineData(0, 800, 0, 0)]
    [InlineData(200, 800, 0.25, 0)]
    [InlineData(800, 800, 1, 0)]
    [InlineData(1000, 800, 0.8, 0.2)]
    [InlineData(0, 0, 0, 0)]
    public void GetSegments_PaidAgainstPlanned_SplitsFillAndOverflow(decimal paid, decimal planned, double fill, double overflow)
    {
        var segments = PaymentProgressBar.GetSegments(paid, planned);

        segments.Fill.Should().BeApproximately(fill, 0.0001);
        segments.Overflow.Should().BeApproximately(overflow, 0.0001);
    }
}
