using LocationTracker.Data;
using LocationTracker.Models;
using LocationTracker.Services;

namespace LocationTracker.Tests;

public sealed class LocationTrackingServiceTests
{
    [Fact]
    public async Task RepeatedStartDoesNotOverlapRequestsAndStopPreventsLaterWrites()
    {
        var provider = new ControlledProvider();
        var database = new FakeDatabase();
        var tracker = new LocationTrackingService(provider, database, TimeSpan.FromMilliseconds(5));
        var saved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        tracker.LocationSaved += (_, _) => saved.TrySetResult();
        await Task.WhenAll(tracker.StartAsync(), tracker.StartAsync());
        await provider.RequestEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, provider.Requests);
        provider.Reading.TrySetResult(new LocationRecord { Latitude = 36, Longitude = -84 });
        await saved.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await tracker.StopAsync();
        int savedAtStop = database.Count;
        await Task.Delay(30);
        Assert.False(tracker.IsTracking);
        Assert.Equal(savedAtStop, database.Count);
        Assert.Equal(1, provider.MaximumConcurrentRequests);
    }

    [Fact]
    public async Task StopCancelsAnOutstandingGpsRequest()
    {
        var provider = new ControlledProvider();
        var database = new FakeDatabase();
        var tracker = new LocationTrackingService(provider, database);
        await tracker.StartAsync();
        await provider.RequestEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await tracker.StopAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(tracker.IsTracking);
        Assert.Equal(0, database.Count);
    }

    [Fact]
    public async Task PermissionDenialStopsTrackingWithoutSaving()
    {
        var provider = new ControlledProvider { DenyPermission = true };
        var database = new FakeDatabase();
        var tracker = new LocationTrackingService(provider, database);
        var stopped = ObserveStop(tracker);
        await tracker.StartAsync();
        await stopped.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Contains("Permission denied", tracker.StatusMessage);
        Assert.Equal(0, database.Count);
        Assert.Equal(0, provider.Requests);
    }

    [Fact]
    public async Task DatabaseFailureStopsTrackingAndExplainsTheFailure()
    {
        var provider = new ControlledProvider();
        provider.Reading.SetResult(new LocationRecord { Latitude = 36, Longitude = -84 });
        var tracker = new LocationTrackingService(provider, new FakeDatabase { FailWrites = true });
        var stopped = ObserveStop(tracker);
        await tracker.StartAsync();
        await stopped.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Contains("Storage error", tracker.StatusMessage);
    }

    private static Task ObserveStop(LocationTrackingService tracker)
    {
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        tracker.StateChanged += (_, _) =>
        {
            if (!tracker.IsTracking)
            {
                stopped.TrySetResult();
            }
        };
        return stopped.Task;
    }

    private sealed class ControlledProvider : ILocationProvider
    {
        private int _active;
        public bool DenyPermission { get; init; }
        public int Requests { get; private set; }
        public int MaximumConcurrentRequests { get; private set; }
        public TaskCompletionSource RequestEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<LocationRecord?> Reading { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task EnsurePermissionAsync(CancellationToken cancellationToken) => DenyPermission
            ? Task.FromException(new LocationAccessException("Permission denied — enable location in Settings."))
            : Task.CompletedTask;

        public async Task<LocationRecord?> GetLocationAsync(CancellationToken cancellationToken)
        {
            Requests++;
            MaximumConcurrentRequests = Math.Max(MaximumConcurrentRequests, Interlocked.Increment(ref _active));
            RequestEntered.TrySetResult();
            try
            {
                return await Reading.Task.WaitAsync(cancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref _active);
            }
        }
    }

    private sealed class FakeDatabase : ILocationDatabase
    {
        public bool FailWrites { get; init; }
        public int Count { get; private set; }
        public Task SaveLocationAsync(LocationRecord location)
        {
            if (FailWrites)
            {
                throw new IOException("Test disk failure");
            }

            Count++;
            return Task.CompletedTask;
        }

        public Task<int> GetLocationCountAsync() => Task.FromResult(Count);
        public Task<IReadOnlyList<LocationRecord>> GetLocationsAsync() => Task.FromResult<IReadOnlyList<LocationRecord>>([]);
        public Task DeleteAllLocationsAsync()
        {
            Count = 0;
            return Task.CompletedTask;
        }
    }
}
