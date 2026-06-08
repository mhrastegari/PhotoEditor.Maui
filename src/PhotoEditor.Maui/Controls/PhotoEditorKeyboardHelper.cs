#if ANDROID
using Android.OS;
using Android.Views.InputMethods;
using View = Android.Views.View;
#endif

#if IOS || MACCATALYST
using UIKit;
#endif

namespace PhotoEditor.Maui.Controls;

internal static class PhotoEditorKeyboardHelper
{
    public static void HideKeyboardAndClearFocus(VisualElement? element = null)
    {
        try
        {
            element?.Unfocus();

#if ANDROID
            var activity = Platform.CurrentActivity;
            if (activity is null)
                return;

            if (activity.GetSystemService(Android.Content.Context.InputMethodService) is not InputMethodManager imm)
                return;

            IBinder? token = null;
            if (element?.Handler?.PlatformView is View nativeView)
                token = nativeView.WindowToken;

            token ??= activity.CurrentFocus?.WindowToken;
            token ??= activity.Window?.DecorView?.WindowToken;

            if (token is not null)
                imm.HideSoftInputFromWindow(token, HideSoftInputFlags.None);

            activity.Window?.DecorView?.ClearFocus();
#elif IOS || MACCATALYST
            if (element?.Handler?.PlatformView is UIView nativeView)
            {
                nativeView.EndEditing(true);
            }
            else
            {
                var window = UIApplication.SharedApplication
                    .ConnectedScenes
                    .OfType<UIWindowScene>()
                    .SelectMany(s => s.Windows)
                    .FirstOrDefault(w => w.IsKeyWindow);

                window?.EndEditing(true);
            }
#endif
        }
        catch
        {
            // ignored
        }
    }
}
