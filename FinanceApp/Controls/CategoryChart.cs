using FinanceApp.ViewModels;

namespace FinanceApp.Controls;

/// <summary>
/// The category report's chart: a pie (a donut, each slice a category's share) or columns (each as tall as its
/// category's total against the largest one), in the categories' colors. The legend under it names them.
/// </summary>
public class CategoryChart : GraphicsView
{
    public static readonly BindableProperty RowsProperty = BindableProperty.Create(
        nameof(Rows), typeof(IReadOnlyList<CategoryReportRow>), typeof(CategoryChart), Array.Empty<CategoryReportRow>(),
        propertyChanged: (bindable, _, _) => ((CategoryChart)bindable).Invalidate());

    public static readonly BindableProperty IsPieProperty = BindableProperty.Create(
        nameof(IsPie), typeof(bool), typeof(CategoryChart), true,
        propertyChanged: (bindable, _, _) => ((CategoryChart)bindable).Invalidate());

    private const float ColumnGap = 10;
    private const float DonutHole = 0.55f;

    public IReadOnlyList<CategoryReportRow> Rows
    {
        get => (IReadOnlyList<CategoryReportRow>)GetValue(RowsProperty);
        set => SetValue(RowsProperty, value);
    }

    public bool IsPie
    {
        get => (bool)GetValue(IsPieProperty);
        set => SetValue(IsPieProperty, value);
    }

    public CategoryChart()
    {
        InputTransparent = true;
        Drawable = new ChartDrawable(this);
    }

    /// <summary>
    /// Each slice's start and sweep, in degrees clockwise from the top, sized by its share of all the amounts.
    /// </summary>
    public static IReadOnlyList<(float Start, float Sweep)> GetSlices(IReadOnlyList<decimal> amounts)
    {
        var total = amounts.Where(a => a > 0).Sum();
        if (total <= 0)
            return [.. amounts.Select(_ => (0f, 0f))];

        var slices = new List<(float, float)>();
        var start = 0f;
        foreach (var amount in amounts)
        {
            var sweep = amount > 0 ? (float)(amount / total * 360) : 0;
            slices.Add((start, sweep));
            start += sweep;
        }

        return slices;
    }

    /// <summary>
    /// Each column's height as a share (0–1) of the chart's height: the largest amount fills it.
    /// </summary>
    public static IReadOnlyList<double> GetColumnHeights(IReadOnlyList<decimal> amounts)
    {
        var max = amounts.DefaultIfEmpty(0).Max();
        return [.. amounts.Select(a => max > 0 && a > 0 ? (double)(a / max) : 0)];
    }

    private static Color ColorOf(CategoryReportRow row) =>
        Color.TryParse(row.Color, out var color) ? color : Colors.Gray;

    private sealed class ChartDrawable(CategoryChart chart) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var rows = chart.Rows;
            if (rows.Count == 0)
                return;

            var amounts = rows.Select(r => r.Amount).ToList();
            if (chart.IsPie)
                DrawPie(canvas, dirtyRect, rows, amounts);
            else
                DrawColumns(canvas, dirtyRect, rows, amounts);
        }

        private static void DrawPie(ICanvas canvas, RectF rect, IReadOnlyList<CategoryReportRow> rows, IReadOnlyList<decimal> amounts)
        {
            var size = Math.Min(rect.Width, rect.Height);
            var radius = size / 2;
            var center = new PointF(rect.Center.X, rect.Center.Y);
            var slices = GetSlices(amounts);

            for (var i = 0; i < rows.Count; i++)
            {
                var (start, sweep) = slices[i];
                if (sweep <= 0)
                    continue;

                canvas.FillColor = ColorOf(rows[i]);
                if (sweep >= 359.99f)
                {
                    canvas.FillCircle(center, radius);
                    continue;
                }

                // Canvas angles are counterclockwise from 3 o'clock; the slices go clockwise from 12 o'clock.
                var path = new PathF();
                path.MoveTo(center);
                path.AddArc(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius,
                    90 - start, 90 - start - sweep, clockwise: true);
                path.Close();
                canvas.FillPath(path);
            }

            // The hole makes it a donut, which reads better with many small slices.
            canvas.FillColor = Colors.White;
            canvas.FillCircle(center, radius * DonutHole);
        }

        private static void DrawColumns(ICanvas canvas, RectF rect, IReadOnlyList<CategoryReportRow> rows, IReadOnlyList<decimal> amounts)
        {
            var heights = GetColumnHeights(amounts);
            var width = Math.Min(48, (rect.Width - ColumnGap * (rows.Count - 1)) / rows.Count);
            var used = width * rows.Count + ColumnGap * (rows.Count - 1);
            var x = rect.X + (rect.Width - used) / 2;

            canvas.StrokeColor = Color.FromArgb("#D7C9E6");
            canvas.StrokeSize = 1;
            canvas.DrawLine(rect.Left, rect.Bottom - 0.5f, rect.Right, rect.Bottom - 0.5f);

            for (var i = 0; i < rows.Count; i++)
            {
                var height = (float)(heights[i] * (rect.Height - 1));
                if (height > 0)
                {
                    canvas.FillColor = ColorOf(rows[i]);
                    canvas.FillRoundedRectangle(x, rect.Bottom - 1 - height, width, height, Math.Min(6, width / 4), Math.Min(6, width / 4), 0, 0);
                }
                x += width + ColumnGap;
            }
        }
    }
}
