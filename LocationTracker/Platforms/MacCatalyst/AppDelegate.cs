using Foundation;

namespace LocationTracker;

/// <summary>Application delegate that boots the shared MAUI app.</summary>
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
