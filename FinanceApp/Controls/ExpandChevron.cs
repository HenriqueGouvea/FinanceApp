using Microsoft.Maui.Controls.Shapes;

namespace FinanceApp.Controls;

/// <summary>
/// Circular chevron button that rotates to point up when <see cref="IsExpanded"/> is true
/// and down when false. It is input transparent so taps reach the enclosing section header.
/// </summary>
public class ExpandChevron : Border
{
    private const uint AnimationLength = 200;
    private const double Size = 30;

    private readonly Label _glyph;

    public static readonly BindableProperty IsExpandedProperty = BindableProperty.Create(
        nameof(IsExpanded), typeof(bool), typeof(ExpandChevron), false,
        propertyChanged: (bindable, _, newValue) => ((ExpandChevron)bindable).OnIsExpandedChanged((bool)newValue));

    public static readonly BindableProperty AccentColorProperty = BindableProperty.Create(
        nameof(AccentColor), typeof(Color), typeof(ExpandChevron), Colors.Gray,
        propertyChanged: (bindable, _, newValue) => ((ExpandChevron)bindable).ApplyAccentColor((Color)newValue));

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public Color AccentColor
    {
        get => (Color)GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    public ExpandChevron()
    {
        InputTransparent = true;
        WidthRequest = Size;
        HeightRequest = Size;
        Padding = 0;
        StrokeThickness = 0;
        StrokeShape = new RoundRectangle { CornerRadius = Size / 2 };
        VerticalOptions = LayoutOptions.Center;

        _glyph = new Label
        {
            Text = "▼",
            FontSize = 12,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        Content = _glyph;

        ApplyAccentColor(AccentColor);
        _glyph.Rotation = RotationFor(IsExpanded);
        Loaded += (_, _) => _glyph.Rotation = RotationFor(IsExpanded);
    }

    private async void OnIsExpandedChanged(bool isExpanded)
    {
        _glyph.CancelAnimations();

        // Not on screen yet (initial binding): snap so a persisted state doesn't animate on load.
        if (!IsLoaded)
        {
            _glyph.Rotation = RotationFor(isExpanded);
            return;
        }

        await _glyph.RotateToAsync(RotationFor(isExpanded), AnimationLength, Easing.CubicInOut);
    }

    private void ApplyAccentColor(Color color)
    {
        _glyph.TextColor = color;
        BackgroundColor = color.WithAlpha(0.15f);
    }

    private static double RotationFor(bool isExpanded) => isExpanded ? 180 : 0;
}
