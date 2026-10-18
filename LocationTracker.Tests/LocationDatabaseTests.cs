using LocationTracker.Data;
using LocationTracker.Models;

namespace LocationTracker.Tests;

public sealed class LocationDatabaseTests
{
    [Fact]
    public async Task HistorySurvivesReopeningAndCanBeCleared()
    {
        string path = Path.Combine(Path.GetTempPath(), $"location-test-{Guid.NewGuid():N}.db3");
        try
        {
            await using (var database = new LocationDatabase(path))
            {
                Assert.Equal(0, await database.GetLocationCountAsync());
                await Task.WhenAll(Enumerable.Range(0, 8).Select(index => database.SaveLocationAsync(new LocationRecord
                {
                    Latitude = 36.737,
                    Longitude = -84.164,
                    TimestampUtc = new DateTime(2026, 10, 18, 12, 0, index, DateTimeKind.Utc),
                    AccuracyMeters = 5
                })));
                Assert.Equal(8, await database.GetLocationCountAsync());
            }

            await using (var reopened = new LocationDatabase(path))
            {
                var records = await reopened.GetLocationsAsync();
                Assert.Equal(8, records.Count);
                Assert.Equal(8, records.Select(record => record.Id).Distinct().Count());
                Assert.Equal(records.OrderBy(record => record.TimestampUtc), records);
                Assert.All(records, record => Assert.Equal(5, record.AccuracyMeters));
                await reopened.DeleteAllLocationsAsync();
                Assert.Equal(0, await reopened.GetLocationCountAsync());
                Assert.Empty(await reopened.GetLocationsAsync());
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task InvalidCoordinatesCannotBePersisted()
    {
        await using var database = new LocationDatabase(":memory:");
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => database.SaveLocationAsync(new LocationRecord { Latitude = 100 }));
    }
}
