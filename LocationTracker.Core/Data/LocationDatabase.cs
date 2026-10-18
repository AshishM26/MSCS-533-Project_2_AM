using LocationTracker.Models;
using SQLite;

namespace LocationTracker.Data;

/// <summary>Serializes initialization and operations on the app's private SQLite file.</summary>
public sealed class LocationDatabase(string databasePath) : ILocationDatabase, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private SQLiteAsyncConnection? _connection;

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
        {
            return _connection;
        }

        var connection = new SQLiteAsyncConnection(databasePath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
        try
        {
            await connection.CreateTableAsync<LocationRecord>();
            _connection = connection;
            return connection;
        }
        catch
        {
            await connection.CloseAsync();
            throw;
        }
    }

    private async Task<T> WithConnectionAsync<T>(Func<SQLiteAsyncConnection, Task<T>> operation)
    {
        await _gate.WaitAsync();
        try
        {
            return await operation(await GetConnectionAsync());
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveLocationAsync(LocationRecord location)
    {
        ArgumentNullException.ThrowIfNull(location);
        if (!location.HasValidCoordinates)
        {
            throw new ArgumentOutOfRangeException(nameof(location), "Coordinates must be finite and within geographic bounds.");
        }

        await WithConnectionAsync(connection => connection.InsertAsync(location));
    }

    public async Task<IReadOnlyList<LocationRecord>> GetLocationsAsync() =>
        await WithConnectionAsync(connection => connection.Table<LocationRecord>()
            .OrderBy(record => record.TimestampUtc).ThenBy(record => record.Id).ToListAsync());

    public async Task DeleteAllLocationsAsync() =>
        await WithConnectionAsync(connection => connection.DeleteAllAsync<LocationRecord>());

    public Task<int> GetLocationCountAsync() =>
        WithConnectionAsync(connection => connection.Table<LocationRecord>().CountAsync());

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_connection is not null)
            {
                await _connection.CloseAsync();
                _connection = null;
            }
        }
        finally
        {
            _gate.Release();
        }
    }
}
