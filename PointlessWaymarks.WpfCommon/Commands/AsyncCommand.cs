#nullable enable
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using PointlessWaymarks.WpfCommon.Utility;

// Lovely code from our good friend John Tririet
// https://johnthiriet.com/mvvm-going-async-with-async-command


namespace PointlessWaymarks.WpfCommon.Commands
{
    /// <summary>
    ///     Implementation of an Async Command
    /// </summary>
    public class AsyncCommand : IAsyncCommand
    {
        private readonly Func<object?, bool>? _canExecute;
        private readonly bool _continueOnCapturedContext;
        private readonly Func<Task> _execute;
        private readonly Action<Exception>? _onException;
        private readonly WeakEventManager _weakEventManager = new();

        /// <summary>
        ///     Create a new AsyncCommand
        /// </summary>
        /// <param name="execute">Function to execute</param>
        /// <param name="canExecute">Function to call to determine if it can be executed</param>
        /// <param name="onException">Action callback when an exception occurs</param>
        /// <param name="continueOnCapturedContext">If the context should be captured on exception</param>
        public AsyncCommand(Func<Task> execute, Func<object?, bool>? canExecute = null,
            Action<Exception>? onException = null, bool continueOnCapturedContext = false)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _onException = onException;
            _continueOnCapturedContext = continueOnCapturedContext;
        }

        /// <summary>
        ///     Execute the command async.
        /// </summary>
        /// <returns>Task of action being executed that can be awaited.</returns>
        public Task ExecuteAsync()
        {
            return _execute();
        }

        /// <summary>
        ///     Raise a CanExecute change event.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            _weakEventManager.HandleEvent(this, EventArgs.Empty, nameof(CanExecuteChanged));
        }

        /// <summary>
        ///     Invoke the CanExecute method and return if it can be executed.
        /// </summary>
        /// <param name="parameter">Parameter to pass to CanExecute.</param>
        /// <returns>If it can be executed.</returns>
        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        /// <summary>
        ///     Event triggered when Can Execute changes.
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add => _weakEventManager.AddEventHandler(value);
            remove => _weakEventManager.RemoveEventHandler(value);
        }

        public void Execute(object? parameter)
        {
            ExecuteAsync().SafeFireAndForget(_onException, _continueOnCapturedContext);
        }
    }

    /// <summary>
    ///     Implementation of a generic Async Command
    /// </summary>
    public class AsyncCommand<T> : IAsyncCommand<T>
    {
        private readonly Func<object?, bool>? _canExecute;
        private readonly bool _continueOnCapturedContext;
        private readonly Func<T?, Task> _execute;
        private readonly Action<Exception>? _onException;
        private readonly WeakEventManager _weakEventManager = new();

        /// <summary>
        ///     Create a new AsyncCommand
        /// </summary>
        /// <param name="execute">Function to execute</param>
        /// <param name="canExecute">Function to call to determine if it can be executed</param>
        /// <param name="onException">Action callback when an exception occurs</param>
        /// <param name="continueOnCapturedContext">If the context should be captured on exception</param>
        public AsyncCommand(Func<T?, Task> execute, Func<object?, bool>? canExecute = null,
            Action<Exception>? onException = null, bool continueOnCapturedContext = false)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _onException = onException;
            _continueOnCapturedContext = continueOnCapturedContext;
        }

        /// <summary>
        ///     Execute the command async.
        /// </summary>
        /// <returns>Task that is executing and can be awaited.</returns>
        public Task ExecuteAsync(T? parameter)
        {
            return _execute(parameter);
        }

        /// <summary>
        ///     Raise a CanExecute change event.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            _weakEventManager.HandleEvent(this, EventArgs.Empty, nameof(CanExecuteChanged));
        }

        /// <summary>
        ///     Invoke the CanExecute method and return if it can be executed.
        /// </summary>
        /// <param name="parameter">Parameter to pass to CanExecute.</param>
        /// <returns>If it can be executed</returns>
        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        /// <summary>
        ///     Event triggered when Can Execute changes.
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add => _weakEventManager.AddEventHandler(value);
            remove => _weakEventManager.RemoveEventHandler(value);
        }

        public void Execute(object? parameter)
        {
            if (CommandUtils.IsValidCommandParameter<T?>(parameter))
                ExecuteAsync(parameter == null ? default : (T) parameter)
                    .SafeFireAndForget(_onException, _continueOnCapturedContext);
        }
    }

    public interface IAsyncCommand : ICommand
    {
        /// <summary>
        ///     Execute the command async.
        /// </summary>
        /// <returns>Task to be awaited on.</returns>
        Task ExecuteAsync();

        /// <summary>
        ///     Raise a CanExecute change event.
        /// </summary>
        void RaiseCanExecuteChanged();
    }

    /// <summary>
    ///     Interface for Async Command with parameter
    /// </summary>
    public interface IAsyncCommand<T> : ICommand
    {
        /// <summary>
        ///     Execute the command async.
        /// </summary>
        /// <param name="parameter">Parameter to pass to command</param>
        /// <returns>Task to be awaited on.</returns>
        Task ExecuteAsync(T parameter);

        /// <summary>
        ///     Raise a CanExecute change event.
        /// </summary>
        void RaiseCanExecuteChanged();
    }
}