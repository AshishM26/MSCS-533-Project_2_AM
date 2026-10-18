using LocationTracker.Models;

namespace LocationTracker.Services;

/// <summary>Translates platform permission/GPS results into actionable user messages.</summary>
public sealed class MauiLocationProvider : ILocationProvider
{
    private const string PermissionDeniedMessage =
        "Permission denied — allow location in device Settings, then tap Start Tracking.";

    /// <inheritdoc />
    public async Task EnsurePermissionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Permission prompts must be shown from the UI thread on every platform.
        PermissionStatus status = await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            PermissionStatus permission = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (permission != PermissionStatus.Granted)
            {
                permission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            return permission;
        });
        cancellationToken.ThrowIfCancellationRequested();
        if (status != PermissionStatus.Granted)
        {
            throw new LocationAccessException(PermissionDeniedMessage);
        }
    }

    /// <inheritdoc />
    public async Task<LocationRecord?> GetLocationAsync(CancellationToken cancellationToken)
    {
        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(15));
            Location? location = await Geolocation.Default.GetLocationAsync(request, cancellationToken);
            if (location is null)
            {
                return null;
            }

            return new LocationRecord
            {
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                TimestampUtc = location.Timestamp.UtcDateTime,
                AccuracyMeters = location.Accuracy
            };
        }
        catch (FeatureNotEnabledException)
        {
            throw new LocationAccessException("GPS disabled — enable Location Services, then tap Start Tracking.");
        }
        catch (FeatureNotSupportedException)
        {
            throw new LocationAccessException("Location is not supported on this device.");
        }
        catch (PermissionException)
        {
            throw new LocationAccessException(PermissionDeniedMessage);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // A platform-side timeout surfaces as cancellation; treat it as "no fix yet" and retry.
            return null;
        }
        catch (TimeoutException)
        {
            return null;
        }
    }
}
