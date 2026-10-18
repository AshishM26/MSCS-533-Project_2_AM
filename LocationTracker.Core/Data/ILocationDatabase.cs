using LocationTracker.Models;

namespace LocationTracker.Data;

public interface ILocationDatabase
{
    Task SaveLocationAsync(LocationRecord location);
    Task<IReadOnlyList<LocationRecord>> GetLocationsAsync();
    Task DeleteAllLocationsAsync();
    Task<int> GetLocationCountAsync();
}
