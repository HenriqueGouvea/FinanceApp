namespace FinanceApp.Controls;

/// <summary>
/// Panel that slides in from the right edge of its parent when <see cref="IsOpen"/> is true
/// and slides back out when false. The parent should clip to bounds so the closed panel stays hidden.
/// </summary>
public class SlideOutPanel : ContentView
{
    private const uint AnimationLength = 200;

    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(
        nameof(IsOpen), typeof(bool), typeof(SlideOutPanel), false,
        propertyChanged: (bindable, _, newValue) => ((SlideOutPanel)bindable).OnIsOpenChanged((bool)newValue));

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public SlideOutPanel()
    {
        InputTransparent = true;
        SizeChanged += (_, _) =>
        {
            this.CancelAnimations();
            TranslationX = IsOpen ? 0 : Width;
        };
    }

    private async void OnIsOpenChanged(bool isOpen)
    {
        InputTransparent = !isOpen;
        this.CancelAnimations();

        // Not measured yet: SizeChanged will place the panel once the width is known.
        if (Width <= 0)
            return;

        await this.TranslateToAsync(isOpen ? 0 : Width, 0, AnimationLength,
            isOpen ? Easing.CubicOut : Easing.CubicIn);
    }
}
