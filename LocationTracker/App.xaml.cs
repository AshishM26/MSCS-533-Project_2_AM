using LocationTracker.ViewModels;
using LocationTracker.Views;

namespace LocationTracker;

public partial class App : Application
{
    private readonly MainPage _page;
    private readonly MainViewModel _viewModel;

    public App(MainPage page, MainViewModel viewModel)
    {
        InitializeComponent();
        _page = page;
        _viewModel = viewModel;
        UserAppTheme = AppTheme.Light;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(_page);
        // Never resume tracking automatically after the app goes into the background.
        window.Stopped += async (_, _) => await _viewModel.SuspendAsync();
        window.Destroying += async (_, _) => await _viewModel.SuspendAsync();
        window.Resumed += (_, _) => _viewModel.Resume();
        return window;
    }
}
