using System.ComponentModel;
using System.Runtime.CompilerServices;
using LocationTracker.Data;
using LocationTracker.Models;
using LocationTracker.Services;

namespace LocationTracker.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ILocationDatabase _database;
    private readonly ILocationTrackingService _tracking;
    private readonly HeatMapService _heatMap;
    private readonly List<LocationRecord> _records = [];
    private bool _isBusy;
    private bool _isActive = true;
    private bool _initialized;
    private string _statusMessage = "Ready — tap Start Tracking to begin.";

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

    public bool IsTracking => _tracking.IsTracking;
    public int SampleCount => _records.Count;
    public LocationRecord? LatestLocation => _records.LastOrDefault();
    public IReadOnlyList<HeatMapCell> HeatCells { get; private set; } = [];
    public string LastReadingText => LatestLocation is { } location
        ? $"Last reading {DateTime.SpecifyKind(location.TimestampUtc, DateTimeKind.Utc).ToLocalTime():HH:mm:ss} · accuracy {location.AccuracyMeters?.ToString("0") ?? "unknown"} m"
        : "No saved locations yet";

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public AsyncCommand StartTrackingCommand { get; }
    public AsyncCommand StopTrackingCommand { get; }
    public AsyncCommand CenterCommand { get; }
    public AsyncCommand ClearHistoryCommand { get; }
    public Func<Task<bool>>? ConfirmClearAsync { get; set; }
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? HistoryChanged;
    public event EventHandler<LocationRecord>? CenterRequested;

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
