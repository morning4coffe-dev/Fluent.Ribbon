using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;

namespace FluentRibbon.Uno.Showcase.Droid;

[Activity(
    MainLauncher = true,
    ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
    WindowSoftInputMode = SoftInput.AdjustNothing | SoftInput.StateHidden
)]
public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        global::AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);

        base.OnCreate(savedInstanceState);

        if (FindViewById(global::Android.Resource.Id.Content) is { } content)
        {
            ViewCompat.SetOnApplyWindowInsetsListener(
                content,
                new SystemBarInsetsListener(
                    content.PaddingLeft,
                    content.PaddingTop,
                    content.PaddingRight,
                    content.PaddingBottom));
            ViewCompat.RequestApplyInsets(content);
        }
    }

    private sealed class SystemBarInsetsListener(
        int initialLeft,
        int initialTop,
        int initialRight,
        int initialBottom)
        : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        public WindowInsetsCompat? OnApplyWindowInsets(
            global::Android.Views.View? view,
            WindowInsetsCompat? insets)
        {
            if (view is null || insets is null)
            {
                return insets;
            }

            var systemBars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());
            if (systemBars is null)
            {
                return insets;
            }

            view.SetPadding(
                initialLeft + systemBars.Left,
                initialTop + systemBars.Top,
                initialRight + systemBars.Right,
                initialBottom + systemBars.Bottom);
            return insets;
        }
    }
}
