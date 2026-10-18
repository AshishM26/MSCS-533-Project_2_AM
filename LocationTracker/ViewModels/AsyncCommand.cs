using System.Windows.Input;

namespace LocationTracker.ViewModels;

/// <summary>Prevents double taps from executing an asynchronous action concurrently.</summary>
/// <param name="execute">Work to run when the command is invoked.</param>
/// <param name="canExecute">Extra condition checked in addition to "not already running".</param>
/// <param name="onError">Receives exceptions, because <see cref="Execute"/> is <c>async void</c> and cannot rethrow safely.</param>
public sealed class AsyncCommand(Func<Task> execute, Func<bool> canExecute, Action<Exception> onError) : ICommand
{
    private bool _isExecuting;

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => !_isExecuting && canExecute();

    /// <inheritdoc />
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        _isExecuting = true;
        NotifyCanExecuteChanged();
        try
        {
            await execute();
        }
        catch (Exception exception)
        {
            onError(exception);
        }
        finally
        {
            _isExecuting = false;
            NotifyCanExecuteChanged();
        }
    }

    /// <summary>Asks bound buttons to re-evaluate <see cref="CanExecute"/>.</summary>
    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
