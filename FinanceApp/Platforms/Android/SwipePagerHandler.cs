using Android.Content;
using Android.Views;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace FinanceApp.Controls;

/// <summary>
/// The pager lives inside the month CarouselView, which would otherwise take every horizontal swipe for itself
/// and change month. Touches starting on the pager keep the parents from intercepting, unless the finger
/// moves mostly vertically, so the page can still scroll.
/// </summary>
public class SwipePagerHandler : ContentViewHandler
{
    protected override ContentViewGroup CreatePlatformView() =>
        new HorizontalSwipeViewGroup(Context) { CrossPlatformLayout = VirtualView };

    private sealed class HorizontalSwipeViewGroup(Context context) : ContentViewGroup(context)
    {
        private readonly int _touchSlop = ViewConfiguration.Get(context)!.ScaledTouchSlop;
        private float _downX;
        private float _downY;
        private bool _isDirectionKnown;

        public override bool DispatchTouchEvent(MotionEvent? e)
        {
            switch (e?.ActionMasked)
            {
                case MotionEventActions.Down:
                    _downX = e.GetX();
                    _downY = e.GetY();
                    _isDirectionKnown = false;
                    Parent?.RequestDisallowInterceptTouchEvent(true);
                    break;

                case MotionEventActions.Move when !_isDirectionKnown:
                    var dx = Math.Abs(e.GetX() - _downX);
                    var dy = Math.Abs(e.GetY() - _downY);
                    if (dy > _touchSlop && dy > dx)
                    {
                        Parent?.RequestDisallowInterceptTouchEvent(false);
                        _isDirectionKnown = true;
                    }
                    else if (dx > _touchSlop)
                    {
                        _isDirectionKnown = true;
                    }
                    break;
            }

            return base.DispatchTouchEvent(e);
        }
    }
}
