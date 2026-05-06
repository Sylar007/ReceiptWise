using Android.App;
using Android.Content.PM;
using Android.OS;

namespace ReceiptWise.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Request permissions at startup (Android 13+)
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            RequestPermissions(new[]
            {
                Android.Manifest.Permission.Camera,
                Android.Manifest.Permission.ReadMediaImages
            }, 0);
        }
    }
}