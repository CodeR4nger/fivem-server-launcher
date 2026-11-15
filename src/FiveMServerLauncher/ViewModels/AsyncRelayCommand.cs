using System.Windows.Input;

namespace FiveMServerLauncher.ViewModels;

internal sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Func<bool>? _canExecute;
    private readonly Action<Exception>? _onError;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null, Action<Exception>? onError = null)
        : this(_ => execute(), canExecute, onError)
    {
    }

    public AsyncRelayCommand(Func<object?, Task> execute, Func<bool>? canExecute = null, Action<Exception>? onError = null)
    {
        _execute = execute;
        _canExecute = canExecute;
        _onError = onError;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke() ?? true;
    }

    public async void Execute(object? parameter)
    {
        try
        {
            await _execute(parameter);
        }
        catch (Exception exception)
        {
            // async void must never throw (it would crash the dispatcher): the failure is
            // only reported, and a failing report is swallowed as well.
            try
            {
                _onError?.Invoke(exception);
            }
            catch
            {
            }
        }
    }
}