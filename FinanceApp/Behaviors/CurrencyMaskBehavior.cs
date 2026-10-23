using FinanceApp.Converters;

namespace FinanceApp.Behaviors;

/// <summary>
/// Turns an <see cref="Entry"/> into a currency field: typed digits fill in from the cents ("R$ 1,50")
/// and the parsed amount is exposed through <see cref="Value"/>. Bind <see cref="Value"/> instead of the entry's Text.
/// </summary>
public class CurrencyMaskBehavior : Behavior<Entry>
{
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(decimal), typeof(CurrencyMaskBehavior), 0m, BindingMode.TwoWay,
        propertyChanged: (bindable, _, newValue) => ((CurrencyMaskBehavior)bindable).OnValueChanged((decimal)newValue));

    private Entry? _entry;

    public decimal Value
    {
        get => (decimal)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    protected override void OnAttachedTo(Entry entry)
    {
        base.OnAttachedTo(entry);
        _entry = entry;
        BindingContext = entry.BindingContext;
        entry.Text = MoneyConverter.Format(Value);
        entry.BindingContextChanged += OnEntryBindingContextChanged;
        entry.TextChanged += OnTextChanged;
        entry.Focused += OnFocused;
    }

    protected override void OnDetachingFrom(Entry entry)
    {
        entry.BindingContextChanged -= OnEntryBindingContextChanged;
        entry.TextChanged -= OnTextChanged;
        entry.Focused -= OnFocused;
        _entry = null;
        base.OnDetachingFrom(entry);
    }

    // Behaviors don't inherit the binding context of the element they are attached to.
    private void OnEntryBindingContextChanged(object? sender, EventArgs e) => BindingContext = _entry?.BindingContext;

    private void OnValueChanged(decimal value)
    {
        if (_entry is null || MoneyConverter.Parse(_entry.Text) == value)
            return;

        _entry.Text = MoneyConverter.Format(value);
    }

    // Changing the native text while Android is still applying a key press crashes AndroidX EmojiCompat's
    // text watcher (stale indices). Both the reformat and the value update (which would flow back into the
    // text through a binding) therefore wait until the edit has finished.
    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_entry is not { } entry)
            return;

        entry.Dispatcher.Dispatch(() =>
        {
            var masked = MoneyConverter.Mask(entry.Text);
            if (masked != entry.Text)
            {
                entry.Text = masked;
                entry.CursorPosition = masked.Length;
            }

            Value = MoneyConverter.Parse(masked);
        });
    }

    private static void OnFocused(object? sender, FocusEventArgs e)
    {
        if (sender is Entry entry)
            entry.CursorPosition = entry.Text?.Length ?? 0;
    }
}
