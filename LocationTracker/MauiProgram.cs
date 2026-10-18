using LocationTracker.Data;
using LocationTracker.Services;
using LocationTracker.ViewModels;
using LocationTracker.Views;

namespace LocationTracker;

public static class MauiProgram
{
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
