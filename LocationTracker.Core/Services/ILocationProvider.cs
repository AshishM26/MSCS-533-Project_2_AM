using LocationTracker.Models;

namespace LocationTracker.Services;

public interface ILocationProvider
{
    Task EnsurePermissionAsync(CancellationToken cancellationToken);
    Task<LocationRecord?> GetLocationAsync(CancellationToken cancellationToken);
}

public sealed class LocationAccessException(string message) : Exception(message);
