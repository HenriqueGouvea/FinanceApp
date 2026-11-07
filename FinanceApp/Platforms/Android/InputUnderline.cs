using Android.Graphics.Drawables;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;

namespace FinanceApp;

/// <summary>
/// Text fields and pickers get the concept's underline: a 2dp line, lilac at rest and plum while focused
/// (Android's own underline would use the amber accent, and tinting MAUI's background fills the whole field).
/// </summary>
public static class InputUnderline
{
    private static readonly Android.Graphics.Color Resting = Color.FromArgb("#D7C9E6").ToPlatform();
    private static readonly Android.Graphics.Color Focused = Color.FromArgb("#5B2A86").ToPlatform();

    public static void Apply()
    {
        EntryHandler.Mapper.AppendToMapping(nameof(InputUnderline), (handler, _) => Underline(handler.PlatformView));
        PickerHandler.Mapper.AppendToMapping(nameof(InputUnderline), (handler, _) => Underline(handler.PlatformView));
        DatePickerHandler.Mapper.AppendToMapping(nameof(InputUnderline), (handler, _) => Underline(handler.PlatformView));
    }

    private static void Underline(AView view)
    {
        var density = view.Resources?.DisplayMetrics?.Density ?? 2f;
        var stroke = (int)Math.Round(2 * density);
        var padding = (int)Math.Round(4 * density);

        var line = new GradientDrawable();
        line.SetColor(view.HasFocus ? Focused : Resting);

        // A single layer, as tall as the line and pinned to the bottom edge.
        var background = new LayerDrawable([line]);
        background.SetLayerGravity(0, Android.Views.GravityFlags.Bottom | Android.Views.GravityFlags.FillHorizontal);
        background.SetLayerHeight(0, stroke);

        view.Background = background;
        view.SetPadding(padding, padding * 2, padding, padding * 2);
        view.FocusChange += (_, e) => line.SetColor(e.HasFocus ? Focused : Resting);
    }
}
