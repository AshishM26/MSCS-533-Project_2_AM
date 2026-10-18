using LocationTracker.Data;
using LocationTracker.Services;
using LocationTracker.ViewModels;
using LocationTracker.Views;

namespace LocationTracker;

/// <summary>Configures MAUI, the Maps handler, fonts, and dependency injection.</summary>
public static class MauiProgram
{
    /// <summary>Builds the app; called by each platform's entry point.</summary>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>()
            .UseMauiMaps()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Singletons: one database connection, one sampling loop, and one page for the app's lifetime.
        builder.Services.AddSingleton<ILocationDatabase>(_ =>
            new LocationDatabase(Path.Combine(FileSystem.AppDataDirectory, "locations.db3")));
        builder.Services.AddSingleton<ILocationProvider, MauiLocationProvider>();
        builder.Services.AddSingleton<ILocationTrackingService, LocationTrackingService>();
        builder.Services.AddSingleton<HeatMapService>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();
        return builder.Build();
    }
}
