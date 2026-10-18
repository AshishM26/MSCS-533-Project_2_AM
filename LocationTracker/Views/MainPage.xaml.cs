using LocationTracker.Models;
using LocationTracker.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Map = Microsoft.Maui.Controls.Maps.Map;

namespace LocationTracker.Views;

/// <summary>Hosts the native map and draws the heat-map circles produced by <see cref="MainViewModel"/>.</summary>
public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private Map? _map;

    // Remembers the latest center request so it can be applied once the native map has loaded.
    private LocationRecord? _pendingCenter;

    /// <summary>Creates the page and connects view-model events to map updates.</summary>
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        _viewModel.ConfirmClearAsync = () => DisplayAlertAsync("Clear location history?",
            "Delete all saved locations from this device? This cannot be undone.", "Delete", "Cancel");
        _viewModel.HistoryChanged += (_, _) => RenderHeatMap();
        _viewModel.CenterRequested += (_, location) => CenterMap(location);

        // The map is created in code so a native initialization failure leaves the rest of the page usable.
        try
        {
            _map = new Map { MapType = MapType.Street, IsShowingUser = false };
            _map.Loaded += (_, _) =>
            {
                RenderHeatMap();
                if (_pendingCenter is not null)
                {
                    CenterMap(_pendingCenter);
                }
            };
            MapHost.Content = _map;
        }
        catch (Exception)
        {
            ShowMapNotice("The map could not initialize. Reopen the app to retry; saved history is retained.");
        }

#if ANDROID
        // Google Maps on Android renders a blank grid without a real key; explain that instead.
        Android.Content.Context context = Android.App.Application.Context;
        Android.OS.Bundle? metadata = context.PackageManager?.GetApplicationInfo(context.PackageName!, Android.Content.PM.PackageInfoFlags.MetaData)?.MetaData;
        string? key = metadata?.GetString("com.google.android.geo.API_KEY");
        if (string.IsNullOrWhiteSpace(key) || key == "YOUR_GOOGLE_MAPS_API_KEY")
        {
            ShowMapNotice("Map setup is incomplete. Tracking and saved history are available; see the README to enable the map.");
        }
#endif
    }

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Resume();
        await _viewModel.InitializeAsync();
    }

    private void RenderHeatMap()
    {
        if (_map is null)
        {
            return;
        }

        try
        {
            _map.MapElements.Clear();
            _map.Pins.Clear();
            foreach (HeatMapCell cell in _viewModel.HeatCells)
            {
                // Relative-density bands: low (blue) up to 33%, medium (amber) up to 66%, high (red) above.
                Color color = cell.Density <= 0.33 ? Color.FromArgb("#1477DF")
                    : cell.Density <= 0.66 ? Color.FromArgb("#F4AA24") : Color.FromArgb("#E33455");
                _map.MapElements.Add(new Circle
                {
                    Center = new Location(cell.Latitude, cell.Longitude),
                    Radius = Distance.FromMeters(cell.RadiusMeters),
                    StrokeColor = color.WithAlpha(0.8f),
                    StrokeWidth = 1,
                    // Opacity rises from 0.2 to 0.65 with density, so street names stay readable underneath.
                    FillColor = color.WithAlpha((float)(0.2 + 0.45 * cell.Density))
                });
            }

            if (_viewModel.LatestLocation is { } latest)
            {
                _map.Pins.Add(new Pin
                {
                    Label = "Latest recorded location",
                    Location = new Location(latest.Latitude, latest.Longitude),
                    Type = PinType.Place
                });
            }
        }
        catch (Exception)
        {
            ShowMapNotice("The map could not display history. Your samples remain saved; reopen the app to retry.");
        }
    }

    private void CenterMap(LocationRecord record)
    {
        _pendingCenter = record;

        // MoveToRegion needs the native handler; the Loaded handler retries with _pendingCenter.
        if (_map?.Handler is null)
        {
            return;
        }

        try
        {
            _map.MoveToRegion(MapSpan.FromCenterAndRadius(
                new Location(record.Latitude, record.Longitude), Distance.FromMeters(650)));
        }
        catch (Exception)
        {
            ShowMapNotice("Map centering is unavailable. Try again after the map loads.");
        }
    }

    private void ShowMapNotice(string message)
    {
        MapNoticeText.Text = message;
        MapNotice.IsVisible = true;
    }
}
