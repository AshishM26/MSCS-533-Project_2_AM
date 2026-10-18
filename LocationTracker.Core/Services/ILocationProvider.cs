using LocationTracker.Models;

namespace LocationTracker.Services;

/// <summary>Abstracts platform permission and GPS access so tracking logic can be tested without a device.</summary>
public interface ILocationProvider
{
    /// <summary>Requests while-in-use location permission if it has not been granted.</summary>
    /// <exception cref="LocationAccessException">The user denied location access.</exception>
    Task EnsurePermissionAsync(CancellationToken cancellationToken);

    /// <summary>Reads the current position, or returns <see langword="null"/> when no fix is available yet.</summary>
    /// <exception cref="LocationAccessException">Location is disabled, unsupported, or no longer permitted.</exception>
    Task<LocationRecord?> GetLocationAsync(CancellationToken cancellationToken);
}

/// <summary>A location failure whose message is safe and actionable to show the user.</summary>
public sealed class LocationAccessException(string message) : Exception(message);
