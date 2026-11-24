using FinanceApp.Controls;
using FluentAssertions;
using Xunit;

namespace FinanceApp.Tests.Controls;

public class CategoryChartTests
{
    [Fact]
    public void GetSlices_Amounts_SweepTheirShareOfTheCircleOneAfterTheOther()
    {
        var slices = CategoryChart.GetSlices([300m, 100m]);

        slices.Should().Equal((0f, 270f), (270f, 90f));
    }

    [Fact]
    public void GetSlices_AllZero_DrawsNothing()
    {
        CategoryChart.GetSlices([0m, 0m]).Should().OnlyContain(s => s.Sweep == 0);
    }

    [Fact]
    public void GetColumnHeights_LargestAmount_FillsTheHeight()
    {
        CategoryChart.GetColumnHeights([400m, 100m, 0m]).Should().Equal(1.0, 0.25, 0.0);
    }

    [Fact]
    public void GetColumnHeights_NoAmounts_IsEmpty()
    {
        CategoryChart.GetColumnHeights([]).Should().BeEmpty();
    }
}
