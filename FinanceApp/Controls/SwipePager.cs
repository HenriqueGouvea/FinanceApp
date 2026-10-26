using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace FinanceApp.Controls;

/// <summary>
/// Shows one of its <see cref="Pages"/> at a time, switched by swiping sideways or tapping the dots below.
/// On Android a swipe that starts here is kept from the parent month carousel (see SwipePagerHandler).
/// </summary>
[ContentProperty(nameof(Pages))]
public class SwipePager : ContentView
{
    private const uint AnimationLength = 220;
    private const double DotSize = 8;

    public static readonly BindableProperty SelectedIndexProperty = BindableProperty.Create(
        nameof(SelectedIndex), typeof(int), typeof(SwipePager), 0, BindingMode.TwoWay,
        coerceValue: (bindable, value) => ((SwipePager)bindable).Coerce((int)value),
        propertyChanged: (bindable, _, _) => ((SwipePager)bindable).OnSelectedIndexChanged());

    public static readonly BindableProperty IndicatorColorProperty = BindableProperty.Create(
        nameof(IndicatorColor), typeof(Color), typeof(SwipePager), Colors.Gray,
        propertyChanged: (bindable, _, _) => ((SwipePager)bindable).UpdateDots());

    private readonly Grid _pagesHost = new() { IsClippedToBounds = true };
    private readonly HorizontalStackLayout _dots = new() { HorizontalOptions = LayoutOptions.Center };

    public ObservableCollection<View> Pages { get; } = [];

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public Color IndicatorColor
    {
        get => (Color)GetValue(IndicatorColorProperty);
        set => SetValue(IndicatorColorProperty, value);
    }

    public SwipePager()
    {
        Content = new VerticalStackLayout { Spacing = 4, Children = { _pagesHost, _dots } };

        GestureRecognizers.Add(new SwipeGestureRecognizer { Direction = SwipeDirection.Left, Command = new Command(() => SelectedIndex++) });
        GestureRecognizers.Add(new SwipeGestureRecognizer { Direction = SwipeDirection.Right, Command = new Command(() => SelectedIndex--) });

        Pages.CollectionChanged += OnPagesChanged;
        _pagesHost.SizeChanged += (_, _) => PlacePages();
    }

    private int Coerce(int index) => Pages.Count == 0 ? 0 : Math.Clamp(index, 0, Pages.Count - 1);

    private void OnPagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _pagesHost.Children.Clear();
        foreach (var page in Pages)
            _pagesHost.Children.Add(page);

        _dots.Children.Clear();
        for (var i = 0; i < Pages.Count; i++)
        {
            var index = i;
            var dot = new Border
            {
                AutomationId = "PagerDot",
                WidthRequest = DotSize,
                HeightRequest = DotSize,
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = DotSize / 2 },
                // The padding makes the tiny dot an easier tap target.
                Margin = new Thickness(6)
            };
            dot.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => SelectedIndex = index) });
            _dots.Children.Add(dot);
        }

        SelectedIndex = Coerce(SelectedIndex);
        PlacePages();
        UpdateDots();
    }

    private void PlacePages()
    {
        for (var i = 0; i < Pages.Count; i++)
        {
            Pages[i].CancelAnimations();
            Pages[i].TranslationX = (i - SelectedIndex) * _pagesHost.Width;
            Pages[i].IsVisible = i == SelectedIndex;
        }
    }

    private async void OnSelectedIndexChanged()
    {
        UpdateDots();

        // Not measured yet: SizeChanged will place the pages once the width is known.
        if (_pagesHost.Width <= 0)
        {
            PlacePages();
            return;
        }

        var index = SelectedIndex;
        var width = _pagesHost.Width;
        var animations = Pages.Select((page, i) =>
        {
            page.CancelAnimations();
            page.IsVisible = true;
            return page.TranslateToAsync((i - index) * width, 0, AnimationLength, Easing.CubicOut);
        }).ToList();

        await Task.WhenAll(animations);

        // Hidden pages are left out of the layout and the accessibility tree.
        if (index == SelectedIndex)
            for (var i = 0; i < Pages.Count; i++)
                Pages[i].IsVisible = i == index;
    }

    private void UpdateDots()
    {
        for (var i = 0; i < _dots.Children.Count; i++)
        {
            var dot = (Border)_dots.Children[i];
            dot.BackgroundColor = IndicatorColor;
            dot.Opacity = i == SelectedIndex ? 1 : 0.3;
        }
    }
}
