namespace FinanceApp.Controls;

/// <summary>
/// Thin bar showing how much of an entry's planned amount was paid in the month. Paying beyond the planned amount
/// fills the bar and draws the excess as a tip in <see cref="OverflowColor"/>.
/// </summary>
public class PaymentProgressBar : GraphicsView
{
    public static readonly BindableProperty PaidAmountProperty = CreateRedrawingProperty(nameof(PaidAmount), typeof(decimal), 0m);
    public static readonly BindableProperty PlannedAmountProperty = CreateRedrawingProperty(nameof(PlannedAmount), typeof(decimal), 0m);
    public static readonly BindableProperty FillColorProperty = CreateRedrawingProperty(nameof(FillColor), typeof(Color), Colors.Orange);
    public static readonly BindableProperty OverflowColorProperty = CreateRedrawingProperty(nameof(OverflowColor), typeof(Color), Colors.Red);
    public static readonly BindableProperty TrackColorProperty = CreateRedrawingProperty(nameof(TrackColor), typeof(Color), Color.FromArgb("#E0E0E0"));

    public decimal PaidAmount
    {
        get => (decimal)GetValue(PaidAmountProperty);
        set => SetValue(PaidAmountProperty, value);
    }

    public decimal PlannedAmount
    {
        get => (decimal)GetValue(PlannedAmountProperty);
        set => SetValue(PlannedAmountProperty, value);
    }

    public Color FillColor
    {
        get => (Color)GetValue(FillColorProperty);
        set => SetValue(FillColorProperty, value);
    }

    public Color OverflowColor
    {
        get => (Color)GetValue(OverflowColorProperty);
        set => SetValue(OverflowColorProperty, value);
    }

    public Color TrackColor
    {
        get => (Color)GetValue(TrackColorProperty);
        set => SetValue(TrackColorProperty, value);
    }

    public PaymentProgressBar()
    {
        HeightRequest = 4;
        InputTransparent = true;
        Drawable = new BarDrawable(this);
    }

    /// <summary>
    /// Shares of the bar width: the paid part up to the planned amount, then the part paid beyond it.
    /// </summary>
    public static (double Fill, double Overflow) GetSegments(decimal paidAmount, decimal plannedAmount)
    {
        var total = Math.Max(paidAmount, plannedAmount);
        if (total <= 0)
            return (0, 0);

        var fill = (double)(Math.Clamp(paidAmount, 0, plannedAmount) / total);
        var overflow = (double)(Math.Max(0, paidAmount - plannedAmount) / total);
        return (fill, overflow);
    }

    private static BindableProperty CreateRedrawingProperty(string name, Type type, object defaultValue) =>
        BindableProperty.Create(name, type, typeof(PaymentProgressBar), defaultValue,
            propertyChanged: (bindable, _, _) => ((PaymentProgressBar)bindable).Invalidate());

    private sealed class BarDrawable(PaymentProgressBar bar) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var radius = dirtyRect.Height / 2;
            var (fill, overflow) = GetSegments(bar.PaidAmount, bar.PlannedAmount);

            canvas.FillColor = bar.TrackColor;
            canvas.FillRoundedRectangle(dirtyRect, radius);

            // The overflow is drawn first, full width up to its end, so the fill covers its left part and both ends stay rounded.
            if (overflow > 0)
            {
                canvas.FillColor = bar.OverflowColor;
                canvas.FillRoundedRectangle(dirtyRect.X, dirtyRect.Y, (float)((fill + overflow) * dirtyRect.Width), dirtyRect.Height, radius);
            }

            if (fill > 0)
            {
                canvas.FillColor = bar.FillColor;
                canvas.FillRoundedRectangle(dirtyRect.X, dirtyRect.Y, (float)(fill * dirtyRect.Width), dirtyRect.Height, radius);
            }
        }
    }
}
