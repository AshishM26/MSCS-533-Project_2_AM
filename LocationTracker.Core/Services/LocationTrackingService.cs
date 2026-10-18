using LocationTracker.Data;
using LocationTracker.Models;

namespace LocationTracker.Services;

/// <summary>Owns one cancellable foreground sampling loop; StopAsync drains pending writes.</summary>
/// <param name="locationProvider">Platform permission and GPS adapter.</param>
/// <param name="database">Destination for each successful reading.</param>
/// <param name="sampleInterval">Delay between readings; defaults to 8 seconds (tests pass a shorter value).</param>
public sealed class LocationTrackingService(
    ILocationProvider locationProvider,
    ILocationDatabase database,
    TimeSpan? sampleInterval = null) : ILocationTrackingService
{
    private readonly TimeSpan _sampleInterval = sampleInterval ?? TimeSpan.FromSeconds(8);

    // Serializes Start/Stop so rapid taps or lifecycle events can never run two loops at once.
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private CancellationTokenSource? _cancellation;
    private Task _trackingTask = Task.CompletedTask;

    public bool IsTracking { get; private set; }
    public string StatusMessage { get; private set; } = "Ready";
    public event EventHandler? StateChanged;
    public event EventHandler<LocationRecord>? LocationSaved;

    public async Task StartAsync()
    {
        await _lifecycleGate.WaitAsync();
        try
        {
            if (IsTracking)
            {
                return;
            }

            // A loop that ended on its own (for example, permission denied) may still be finishing.
            await _trackingTask;
            _cancellation?.Dispose();
            _cancellation = new CancellationTokenSource();
            IsTracking = true;
            SetStatus("Requesting location permission…");
            _trackingTask = TrackAsync(_cancellation.Token);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task StopAsync()
    {
        await _lifecycleGate.WaitAsync();
        try
        {
            bool wasTracking = IsTracking;
            _cancellation?.Cancel();
            await _trackingTask;
            _cancellation?.Dispose();
            _cancellation = null;
            if (wasTracking)
            {
                SetStatus("Stopped — history saved on this device.");
            }
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    private async Task TrackAsync(CancellationToken cancellationToken)
    {
        // Yield before calling platform code so StartAsync always installs the owned task first.
        await Task.Yield();
        try
        {
            await locationProvider.EnsurePermissionAsync(cancellationToken);
            while (!cancellationToken.IsCancellationRequested)
            {
                SetStatus("Tracking — waiting for a location…");
                LocationRecord? record = await locationProvider.GetLocationAsync(cancellationToken);

                // Discard a reading that arrived after Stop was requested.
                cancellationToken.ThrowIfCancellationRequested();
                if (record is null)
                {
                    SetStatus("Location unavailable — enable GPS or set a simulator location. Retrying…");
                }
                else
                {
                    try
                    {
                        await database.SaveLocationAsync(record);
                    }
                    catch (Exception)
                    {
                        // Stop instead of retrying, so a full or broken disk does not fail every 8 seconds.
                        SetStatus("Storage error — tracking stopped. Check device storage and try again.");
                        return;
                    }

                    LocationSaved?.Invoke(this, record);
                    SetStatus("Tracking — location saved.");
                }

                // Wait after each reading, so slow GPS requests never overlap.
                await Task.Delay(_sampleInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Stop and lifecycle suspension are expected cancellation paths.
        }
        catch (LocationAccessException exception)
        {
            SetStatus(exception.Message);
        }
        catch (Exception)
        {
            SetStatus("Location unavailable — tracking stopped. Check permissions and try again.");
        }
        finally
        {
            IsTracking = false;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void SetStatus(string message)
    {
        StatusMessage = message;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
