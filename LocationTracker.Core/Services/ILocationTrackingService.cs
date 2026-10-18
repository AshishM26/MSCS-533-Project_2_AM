using LocationTracker.Models;

namespace LocationTracker.Services;

/// <summary>Controls foreground location sampling and reports its progress.</summary>
public interface ILocationTrackingService
{
    /// <summary>Whether the sampling loop is currently running.</summary>
    bool IsTracking { get; }

    /// <summary>User-facing description of the current tracking state.</summary>
    string StatusMessage { get; }

    /// <summary>Raised when <see cref="IsTracking"/> or <see cref="StatusMessage"/> changes; may fire off the UI thread.</summary>
    event EventHandler? StateChanged;

    /// <summary>Raised after a reading has been saved; may fire off the UI thread.</summary>
    event EventHandler<LocationRecord>? LocationSaved;

    /// <summary>Starts sampling; does nothing if tracking is already running.</summary>
    Task StartAsync();

    /// <summary>Cancels sampling and waits until any in-flight save has finished.</summary>
    Task StopAsync();
}
