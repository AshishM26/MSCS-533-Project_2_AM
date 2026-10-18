using System.ComponentModel;
using System.Runtime.CompilerServices;
using LocationTracker.Data;
using LocationTracker.Models;
using LocationTracker.Services;

namespace LocationTracker.ViewModels;

/// <summary>Screen state and commands for the tracker: history, heat-map cells, and tracking lifecycle.</summary>
public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ILocationDatabase _database;
    private readonly ILocationTrackingService _tracking;
    private readonly HeatMapService _heatMap;
    private readonly List<LocationRecord> _records = [];
    private bool _isBusy;

    // False while the window is in the background, so Start cannot be tapped mid-suspend.
    private bool _isActive = true;
    private bool _initialized;
    private string _statusMessage = "Ready — tap Start Tracking to begin.";

    /// <summary>Wires commands and subscribes to tracking events.</summary>
    public MainViewModel(ILocationDatabase database, ILocationTrackingService tracking, HeatMapService heatMap)
    {
        _database = database;
        _tracking = tracking;
        _heatMap = heatMap;
        StartTrackingCommand = new AsyncCommand(() => RunBusyAsync(_tracking.StartAsync),
            () => _initialized && _isActive && !_isBusy && !IsTracking, ReportError);
        StopTrackingCommand = new AsyncCommand(() => RunBusyAsync(_tracking.StopAsync),
            () => !_isBusy && IsTracking, ReportError);
        CenterCommand = new AsyncCommand(() =>
        {
            if (LatestLocation is not null)
            {
                CenterRequested?.Invoke(this, LatestLocation);
            }

            return Task.CompletedTask;
        }, () => !_isBusy && LatestLocation is not null, ReportError);
        ClearHistoryCommand = new AsyncCommand(ClearHistoryAsync,
            () => _initialized && !_isBusy && !IsTracking && SampleCount > 0, ReportError);

        // Tracking events arrive on background threads; bound properties must change on the UI thread.
        _tracking.StateChanged += (_, _) => MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusMessage = _tracking.StatusMessage;
            OnPropertyChanged(nameof(IsTracking));
            UpdateCommands();
        });
        _tracking.LocationSaved += (_, location) => MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                bool firstLocation = _records.Count == 0;
                _records.Add(location);
                RefreshHistory();
                if (firstLocation)
                {
                    CenterRequested?.Invoke(this, location);
                }
            }
            catch (Exception exception)
            {
                ReportError(exception);
            }
        });
    }

    /// <summary>Whether location sampling is running.</summary>
    public bool IsTracking => _tracking.IsTracking;

    /// <summary>Number of saved readings, shown in the SAMPLES counter.</summary>
    public int SampleCount => _records.Count;

    /// <summary>Most recent saved reading, or <see langword="null"/> when history is empty.</summary>
    public LocationRecord? LatestLocation => _records.LastOrDefault();

    /// <summary>Aggregated density cells the page draws as map circles.</summary>
    public IReadOnlyList<HeatMapCell> HeatCells { get; private set; } = [];

    /// <summary>Local time and accuracy of the latest reading for the status area.</summary>
    public string LastReadingText => LatestLocation is { } location
        ? $"Last reading {DateTime.SpecifyKind(location.TimestampUtc, DateTimeKind.Utc).ToLocalTime():HH:mm:ss} · accuracy {location.AccuracyMeters?.ToString("0") ?? "unknown"} m"
        : "No saved locations yet";

    /// <summary>User-facing status line.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Starts sampling once saved history has loaded.</summary>
    public AsyncCommand StartTrackingCommand { get; }

    /// <summary>Stops sampling and keeps saved history.</summary>
    public AsyncCommand StopTrackingCommand { get; }

    /// <summary>Moves the map to the latest saved reading.</summary>
    public AsyncCommand CenterCommand { get; }

    /// <summary>Deletes all history after the user confirms.</summary>
    public AsyncCommand ClearHistoryCommand { get; }

    /// <summary>Confirmation dialog supplied by the page, keeping UI types out of the view model.</summary>
    public Func<Task<bool>>? ConfirmClearAsync { get; set; }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Raised when <see cref="HeatCells"/> or the latest reading changes and the map must redraw.</summary>
    public event EventHandler? HistoryChanged;

    /// <summary>Raised when the map should center on a reading.</summary>
    public event EventHandler<LocationRecord>? CenterRequested;

    /// <summary>Loads saved history once; later calls do nothing.</summary>
    public async Task InitializeAsync()
    {
        if (_initialized || _isBusy)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            _records.Clear();
            _records.AddRange(await _database.GetLocationsAsync());
            RefreshHistory();
            _initialized = true;
            StatusMessage = SampleCount == 0 ? "Ready — tap Start Tracking to begin." : "Ready — saved history restored.";
            if (LatestLocation is not null)
            {
                CenterRequested?.Invoke(this, LatestLocation);
            }
        });
    }

    /// <summary>Stops tracking when the app leaves the foreground; tracking is not resumed automatically.</summary>
    public async Task SuspendAsync()
    {
        _isActive = false;
        UpdateCommands();
        try
        {
            await _tracking.StopAsync();
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }

    /// <summary>Re-enables the Start command when the app returns to the foreground.</summary>
    public void Resume()
    {
        _isActive = true;
        UpdateCommands();
    }

    private Task ClearHistoryAsync() => RunBusyAsync(async () =>
    {
        if (ConfirmClearAsync is null || !await ConfirmClearAsync())
        {
            return;
        }

        // Drain the sampler before deleting so an in-flight write cannot recreate history.
        await _tracking.StopAsync();
        await _database.DeleteAllLocationsAsync();
        _records.Clear();
        RefreshHistory();
        StatusMessage = "Ready — history cleared.";
    });

    private void RefreshHistory()
    {
        HeatCells = _heatMap.Aggregate(_records);
        OnPropertyChanged(nameof(SampleCount));
        OnPropertyChanged(nameof(LastReadingText));
        HistoryChanged?.Invoke(this, EventArgs.Empty);
        UpdateCommands();
    }

    // Runs one user action at a time and disables the other buttons while it runs.
    private async Task RunBusyAsync(Func<Task> action)
    {
        if (_isBusy)
        {
            return;
        }

        _isBusy = true;
        UpdateCommands();
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
        finally
        {
            _isBusy = false;
            UpdateCommands();
        }
    }

    private void ReportError(Exception exception) => StatusMessage = exception is LocationAccessException
        ? exception.Message
        : "Unable to complete the action. Check device storage and permissions, then reopen the app to retry.";

    private void UpdateCommands()
    {
        StartTrackingCommand.NotifyCanExecuteChanged();
        StopTrackingCommand.NotifyCanExecuteChanged();
        CenterCommand.NotifyCanExecuteChanged();
        ClearHistoryCommand.NotifyCanExecuteChanged();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
