using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

using Microsoft.Xna.Framework;

using ___SafeGameName___.Core;

namespace ___SafeGameName___.Android;

[Activity(
    Label = "___SafeGameName___",
    MainLauncher = true,
    Icon = "@drawable/icon",
    Theme = "@style/Theme.Splash",
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleInstance,
    ScreenOrientation = ScreenOrientation.SensorLandscape,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden
)]
public class MainActivity : AndroidGameActivity
{
    private ___SafeGameName___Game _game;
    private View _view;

    /// <summary>
    /// Called when the activity is first created. Initializes the game instance,
    /// retrieves its rendering view, and sets it as the content view of the activity.
    /// Finally, starts the game loop.
    /// </summary>
    /// <param name="bundle">A Bundle containing the activity's previously saved state, if any.</param>
    protected override void OnCreate(Bundle bundle)
    {
        base.OnCreate(bundle);

        _game = new ___SafeGameName___Game();
        _view = _game.Services.GetService(typeof(View)) as View;

        SetContentView(_view);
        _game.Run();
    }
}