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
        Drawable = new ChartDrawable(this);
        var tap = new TapGestureRecognizer();
        tap.Tapped += OnTapped;
        GestureRecognizers.Add(tap);
    }

    /// <summary>
    /// Runs with the row of the slice or column tapped.
    /// </summary>
    public System.Windows.Input.ICommand? SelectCommand
    {
        get => (System.Windows.Input.ICommand?)GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }

    public static readonly BindableProperty SelectCommandProperty = BindableProperty.Create(
        nameof(SelectCommand), typeof(System.Windows.Input.ICommand), typeof(CategoryChart));

    private void OnTapped(object? sender, TappedEventArgs e)
    {
        if (e.GetPosition(this) is not { } position)
            return;

        var rows = Rows;
        var amounts = rows.Select(r => r.Amount).ToList();
        var rect = new RectF(0, 0, (float)Width, (float)Height);
        var point = new PointF((float)position.X, (float)position.Y);
        var index = IsPie ? GetSliceAt(amounts, rect, point) : GetColumnAt(rows.Count, rect, point);

        if (index >= 0 && SelectCommand?.CanExecute(rows[index]) == true)
            SelectCommand.Execute(rows[index]);
    }

    /// <summary>
    /// The index of the slice under the point, or -1 outside the pie (its hole included).
    /// </summary>
    public static int GetSliceAt(IReadOnlyList<decimal> amounts, RectF rect, PointF point)
    {
        var radius = Math.Min(rect.Width, rect.Height) / 2;
        var dx = point.X - rect.Center.X;
        var dy = point.Y - rect.Center.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance > radius || distance < radius * DonutHole)
            return -1;

        // Degrees clockwise from 12 o'clock, like the slices.
        var angle = (float)(Math.Atan2(dx, -dy) * 180 / Math.PI);
        if (angle < 0)
            angle += 360;

        var slices = GetSlices(amounts);
        for (var i = 0; i < slices.Count; i++)
        {
            var (start, sweep) = slices[i];
            if (sweep > 0 && angle >= start && angle < start + sweep)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// The index of the column whose width holds the point (at any height, so a short column is easy to tap), or -1
    /// between or beside the columns.
    /// </summary>
    public static int GetColumnAt(int count, RectF rect, PointF point)
    {
        if (count == 0)
            return -1;

        var (x, width) = GetColumnLayout(rect, count);
        for (var i = 0; i < count; i++)
        {
            if (point.X >= x && point.X <= x + width)
                return i;
            x += width + ColumnGap;
        }

        return -1;
    }

    /// <summary>
    /// Where the first column starts and how wide each one is: up to 48, centered in the chart.
    /// </summary>
    private static (float X, float Width) GetColumnLayout(RectF rect, int count)
    {
        var width = Math.Min(48, (rect.Width - ColumnGap * (count - 1)) / count);
        var used = width * count + ColumnGap * (count - 1);
        return (rect.X + (rect.Width - used) / 2, width);
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
            var (x, width) = GetColumnLayout(rect, rows.Count);

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
