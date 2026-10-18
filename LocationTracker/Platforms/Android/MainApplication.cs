using Android.App;
using Android.Runtime;

namespace LocationTracker;

/// <summary>Android application object that boots the shared MAUI app.</summary>
[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
