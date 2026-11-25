using FinanceApp.Controls;
using Microsoft.Maui.Graphics;
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

    [Theory]
    [InlineData(150f, 20f, 0)]   // top, in the first slice (0°–270°)
    [InlineData(20f, 100f, 1)]   // left (270°), the second slice starts there
    [InlineData(100f, 180f, 0)]  // bottom (180°)
    [InlineData(100f, 100f, -1)] // the donut's hole
    [InlineData(199f, 1f, -1)]   // outside the circle, in the corner
    public void GetSliceAt_Point_FindsTheSliceUnderIt(float x, float y, int expected)
    {
        CategoryChart.GetSliceAt([300m, 100m], new RectF(0, 0, 200, 200), new PointF(x, y)).Should().Be(expected);
    }

    [Theory]
    [InlineData(100f, 0)]  // the two 48-wide columns are centered: 71–119 and 129–177
    [InlineData(150f, 1)]
    [InlineData(124f, -1)] // the gap between them
    [InlineData(10f, -1)]  // beside them
    public void GetColumnAt_Point_FindsTheColumnOfItsX(float x, int expected)
    {
        CategoryChart.GetColumnAt(2, new RectF(0, 0, 248, 200), new PointF(x, 190)).Should().Be(expected);
    }

    [Fact]
    public void GetColumnHeights_NoAmounts_IsEmpty()
    {
        CategoryChart.GetColumnHeights([]).Should().BeEmpty();
    }
}
