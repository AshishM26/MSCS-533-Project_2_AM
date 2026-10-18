using UIKit;

namespace LocationTracker;

/// <summary>Native entry point that hands control to <see cref="AppDelegate"/>.</summary>
public class Program
{
    private static void Main(string[] args) => UIApplication.Main(args, null, typeof(AppDelegate));
}
