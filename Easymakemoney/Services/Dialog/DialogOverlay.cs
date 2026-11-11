using Easymakemoney.Components;
#if ANDROID || IOS || MACCATALYST
using Microsoft.Maui.Platform;
#endif

namespace Easymakemoney.Services.Dialog;

/// <summary>
/// Pose une vue MAUI par-dessus tout l'écran, au niveau natif, sans passer par la navigation.
/// Rend l'action qui la retire, ou null si la plateforme ne le permet pas (repli : boîte native).
/// </summary>
internal static class DialogOverlay
{
    public static Action? Attach(UiDialogView view)
    {
#if ANDROID || IOS || MACCATALYST
        var mauiContext = Shell.Current?.Handler?.MauiContext
                          ?? Application.Current?.Windows.FirstOrDefault()?.Handler?.MauiContext;
        if (mauiContext == null)
            return null;
#endif

#if ANDROID
        // Au-dessus de la fenêtre qui est devant : celle de l'activité, ou celle d'une page modale (popup) ouverte.
        var top = Shell.Current?.Navigation.ModalStack.LastOrDefault() ?? (Page?)Shell.Current;
        var root = (top?.Handler?.PlatformView as Android.Views.View)?.RootView as Android.Views.ViewGroup
                   ?? Platform.CurrentActivity?.Window?.DecorView as Android.Views.ViewGroup;
        if (root == null)
            return null;

        var platformView = view.ToPlatform(mauiContext);
        root.AddView(platformView, new Android.Views.ViewGroup.LayoutParams(
            Android.Views.ViewGroup.LayoutParams.MatchParent, Android.Views.ViewGroup.LayoutParams.MatchParent));
        platformView.Focusable = true;
        platformView.FocusableInTouchMode = true;
        platformView.RequestFocus();
        platformView.KeyPress += (_, e) =>
        {
            if (e.KeyCode != Android.Views.Keycode.Back)
            {
                e.Handled = false;
                return;
            }
            e.Handled = true;
            if (e.Event?.Action == Android.Views.KeyEventActions.Up)
                view.OnBack();
        };

        return () =>
        {
            root.RemoveView(platformView);
            view.DisconnectHandlers();
        };
#elif IOS || MACCATALYST
        var window = Microsoft.Maui.ApplicationModel.WindowStateManager.Default.GetCurrentUIWindow();
        if (window == null)
            return null;

        var platformView = view.ToPlatform(mauiContext);
        platformView.Frame = window.Bounds;
        platformView.AutoresizingMask = UIKit.UIViewAutoresizing.FlexibleWidth | UIKit.UIViewAutoresizing.FlexibleHeight;
        // VoiceOver reste dans la boîte tant qu'elle est ouverte.
        platformView.AccessibilityViewIsModal = true;
        window.AddSubview(platformView);

        return () =>
        {
            platformView.RemoveFromSuperview();
            view.DisconnectHandlers();
        };
#else
        return null;
#endif
    }
}
