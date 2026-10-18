using LocationTracker.Models;

namespace LocationTracker.Services;

public interface ILocationTrackingService
{
    bool IsTracking { get; }
    string StatusMessage { get; }
    event EventHandler? StateChanged;
    event EventHandler<LocationRecord>? LocationSaved;
    Task StartAsync();
    Task StopAsync();
}
