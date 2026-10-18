using LocationTracker.Models;

namespace LocationTracker.Data;

/// <summary>Persists location history so it survives app restarts.</summary>
public interface ILocationDatabase
{
    /// <summary>Saves one reading; rejects records with out-of-range coordinates.</summary>
    Task SaveLocationAsync(LocationRecord location);

    /// <summary>Returns all saved readings ordered from oldest to newest.</summary>
    Task<IReadOnlyList<LocationRecord>> GetLocationsAsync();

    /// <summary>Permanently removes every saved reading.</summary>
    Task DeleteAllLocationsAsync();

    /// <summary>Returns the number of saved readings.</summary>
    Task<int> GetLocationCountAsync();
}
