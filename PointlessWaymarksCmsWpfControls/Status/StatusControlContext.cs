using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsWpfControls.ToastControl;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.Status
{
    public class StatusControlContext : INotifyPropertyChanged
    {
        private bool _blockUi;
        private ObservableCollection<UserCancellations> _cancellationList;
        private int _countOfRunningBlockingTasks;
        private int _countOfRunningNonBlockingTasks;

        private CancellationTokenSource _currentFullScreenCancellationSource;
        private List<string> _messageBoxButtonList;
        private string _messageBoxMessage;
        private string _messageBoxTitle;
        private bool _messageBoxVisible;
        private bool _nonBlockingTaskAreRunning;
        private bool _showCancellations;
        private ObservableCollection<string> _statusLog;
        private bool _stringEntryApproved;
        private string _stringEntryMessage;
        private string _stringEntryTitle;
        private string _stringEntryUserText;
        private bool _stringEntryVisible;
        private ToastSource _toast;

        public StatusControlContext()
        {
            ContextDispatcher = Application.Current?.Dispatcher ??
                                ThreadSwitcher.PinnedDispatcher ?? Dispatcher.CurrentDispatcher;

            Toast = new ToastSource(ContextDispatcher);
            StatusLog = new ObservableCollection<string>();
            CancellationList = new ObservableCollection<UserCancellations>();

            UserMessageBoxResponseCommand = new Command<string>(UserMessageBoxResponse);
            UserStringEntryApprovedResponseCommand = new Command(UserStringEntryApprovedResponse);
            UserStringEntryCancelledResponseCommand = new Command(UserStringEntryCanceledResponse);
        }

        public bool BlockUi
        {
            get => _blockUi;
            set
            {
                if (value == _blockUi) return;
                _blockUi = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<UserCancellations> CancellationList
        {
            get => _cancellationList;
            set
            {
                if (Equals(value, _cancellationList)) return;
                _cancellationList = value;
                OnPropertyChanged();
            }
        }

        public Dispatcher ContextDispatcher { get; set; }

        public List<string> MessageBoxButtonList
        {
            get => _messageBoxButtonList;
            set
            {
                if (Equals(value, _messageBoxButtonList)) return;
                _messageBoxButtonList = value;
                OnPropertyChanged();
            }
        }

        public string MessageBoxMessage
        {
            get => _messageBoxMessage;
            set
            {
                if (value == _messageBoxMessage) return;
                _messageBoxMessage = value;
                OnPropertyChanged();
            }
        }

        public string MessageBoxTitle
        {
            get => _messageBoxTitle;
            set
            {
                if (value == _messageBoxTitle) return;
                _messageBoxTitle = value;
                OnPropertyChanged();
            }
        }

        public bool MessageBoxVisible
        {
            get => _messageBoxVisible;
            set
            {
                if (value == _messageBoxVisible) return;
                _messageBoxVisible = value;
                OnPropertyChanged();
            }
        }

        public bool NonBlockingTaskAreRunning
        {
            get => _nonBlockingTaskAreRunning;
            set
            {
                if (value == _nonBlockingTaskAreRunning) return;
                _nonBlockingTaskAreRunning = value;
                OnPropertyChanged();
            }
        }

        public bool ShowCancellations
        {
            get => _showCancellations;
            set
            {
                if (value == _showCancellations) return;
                _showCancellations = value;
                OnPropertyChanged();
            }
        }

        public string ShowMessageResponse { get; set; }

        public Guid StatusControlContextId { get; } = Guid.NewGuid();

        public ObservableCollection<string> StatusLog
        {
            get => _statusLog;
            set
            {
                if (Equals(value, _statusLog)) return;
                _statusLog = value;
                OnPropertyChanged();
            }
        }

        public bool StringEntryApproved
        {
            get => _stringEntryApproved;
            set
            {
                if (value == _stringEntryApproved) return;
                _stringEntryApproved = value;
                OnPropertyChanged();
            }
        }

        public string StringEntryMessage
        {
            get => _stringEntryMessage;
            set
            {
                if (value == _stringEntryMessage) return;
                _stringEntryMessage = value;
                OnPropertyChanged();
            }
        }

        public string StringEntryTitle
        {
            get => _stringEntryTitle;
            set
            {
                if (value == _stringEntryTitle) return;
                _stringEntryTitle = value;
                OnPropertyChanged();
            }
        }

        public string StringEntryUserText
        {
            get => _stringEntryUserText;
            set
            {
                if (value == _stringEntryUserText) return;
                _stringEntryUserText = value;
                OnPropertyChanged();
            }
        }

        public bool StringEntryVisible
        {
            get => _stringEntryVisible;
            set
            {
                if (value == _stringEntryVisible) return;
                _stringEntryVisible = value;
                OnPropertyChanged();
            }
        }

        public ToastSource Toast
        {
            get => _toast;
            set
            {
                if (Equals(value, _toast)) return;
                _toast = value;
                OnPropertyChanged();
            }
        }

        public Command<string> UserMessageBoxResponseCommand { get; set; }
        public Command UserStringEntryApprovedResponseCommand { get; set; }
        public Command UserStringEntryCancelledResponseCommand { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void BlockTaskCompleted(Task obj)
        {
            DecrementBlockingTasks();

            if (obj.IsCanceled)
            {
                ToastWarning("Canceled Task");
                return;
            }

            if (obj.IsFaulted)
            {
                ToastError($"Error: {obj.Exception?.Message}");
                Task.Run(async () => await EventLogContext.TryWriteExceptionToLog(obj.Exception,
                    StatusControlContextId.ToString(), await GetStatusLogEntriesString(10)));
            }
        }

        private void BlockTaskCompleted(Task obj, CancellationTokenSource cancellationSource)
        {
            DecrementBlockingTasks();

            var toRemove = CancellationList.Where(x => x.CancelSource == cancellationSource).ToList();

            if (toRemove.Any())
                ContextDispatcher?.InvokeAsync(() =>
                {
                    toRemove.ForEach(x => CancellationList.Remove(x));
                    ShowCancellations = CancellationList.Any();
                });

            cancellationSource?.Dispose();

            if (obj.IsCanceled)
            {
                ToastWarning("Canceled Task");
                return;
            }

            if (obj.IsFaulted)
            {
                ToastError($"Error: {obj.Exception?.Message}");
                Task.Run(async () => await EventLogContext.TryWriteExceptionToLog(obj.Exception,
                    StatusControlContextId.ToString(), await GetStatusLogEntriesString(10)));
            }
        }

        private void DecrementBlockingTasks()
        {
            Interlocked.Decrement(ref _countOfRunningBlockingTasks);
            BlockUi = _countOfRunningBlockingTasks > 0;
        }

        private void DecrementNonBlockingTasks()
        {
            Interlocked.Decrement(ref _countOfRunningNonBlockingTasks);
            NonBlockingTaskAreRunning = _countOfRunningNonBlockingTasks > 0;
        }

        private async void FireAndForgetBlockingTaskWithUiMessageReturnCompleted(Task obj)
        {
            DecrementBlockingTasks();

            if (obj.IsCanceled) return;

            if (obj.IsFaulted)
            {
                await ShowMessage("Error", obj.Exception?.ToString() ?? "Error with no information?!?!",
                    new List<string> {"Ok"});

#pragma warning disable 4014
                Task.Run(async () => await EventLogContext.TryWriteExceptionToLog(obj.Exception,
                    StatusControlContextId.ToString(), await GetStatusLogEntriesString(10)));
#pragma warning restore 4014
            }
        }

        private void FireAndForgetTaskWithToastErrorReturnCompleted(Task obj)
        {
            DecrementNonBlockingTasks();

            if (obj.IsCanceled) return;

            if (obj.IsFaulted)
            {
                ToastError($"Error: {obj.Exception?.Message}");

                Task.Run(async () => await EventLogContext.TryWriteExceptionToLog(obj.Exception,
                    StatusControlContextId.ToString(), await GetStatusLogEntriesString(10)));
            }
        }

        private async Task<string> GetStatusLogEntriesString(int maxLastEntries)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            if (StatusLog == null || !StatusLog.Any()) return string.Empty;

            var toReturn = string.Join(Environment.NewLine, StatusLog.Take(maxLastEntries));

            await ThreadSwitcher.ResumeBackgroundAsync();

            return toReturn;
        }

        private void IncrementBlockingTasks()
        {
            Interlocked.Increment(ref _countOfRunningBlockingTasks);
            BlockUi = _countOfRunningBlockingTasks > 0;
        }

        private void IncrementNonBlockingTasks()
        {
            Interlocked.Increment(ref _countOfRunningNonBlockingTasks);
            NonBlockingTaskAreRunning = _countOfRunningNonBlockingTasks > 0;
        }

        private void NonBlockTaskCompleted(Task obj)
        {
            DecrementNonBlockingTasks();
            DecrementNonBlockingTasks();

            if (obj.IsCanceled)
            {
                ToastWarning("Canceled Task");
                return;
            }

            if (obj.IsFaulted)
            {
                ToastError($"Error: {obj.Exception?.Message}");

                Task.Run(async () => await EventLogContext.TryWriteExceptionToLog(obj.Exception,
                    StatusControlContextId.ToString(), await GetStatusLogEntriesString(10)));
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Progress(string e)
        {
            ContextDispatcher?.InvokeAsync(() =>
            {
                StatusLog.Add(e);

                if (StatusLog.Count > 100) StatusLog.Remove(StatusLog.First());

                if (UserSettingsSingleton.LogDiagnosticEvents)
                    Task.Run(() =>
                        EventLogContext.TryWriteDiagnosticMessageToLog(e, StatusControlContextId.ToString()));
            });
        }

        public IProgress<string> ProgressTracker()
        {
            var toReturn = new Progress<string>();
            toReturn.ProgressChanged += ProgressTrackerChange;
            return toReturn;
        }

        private void ProgressTrackerChange(object sender, string e)
        {
            Progress(e);
        }

        public void RunBlockingAction(Action toRun)
        {
            RunBlockingTask(() => Task.Run(toRun));
        }

        public void RunBlockingAction<T>(Action<T> toRun, T parameter)
        {
            RunBlockingTask(() => Task.Run(() => toRun(parameter)));
        }

        public Command RunBlockingActionCommand(Action toRun)
        {
            return new(() => RunBlockingAction(toRun));
        }

        public void RunBlockingTask(Func<Task> toRun)
        {
            IncrementBlockingTasks();
            Task.Run(toRun).ContinueWith(BlockTaskCompleted);
        }

        public void RunBlockingTask<T>(Func<T, Task> toRun, T parameter)
        {
            IncrementBlockingTasks();
            Task.Run(async () => await toRun(parameter)).ContinueWith(BlockTaskCompleted);
        }

        public Command<T> RunBlockingTaskCommand<T>(Func<T, Task> toRun)
        {
            return new(x => RunBlockingTask(async () => await toRun(x)));
        }

        public Command RunBlockingTaskCommand(Func<Task> toRun)
        {
            return new(() => RunBlockingTask(toRun));
        }

        public void RunBlockingTaskWithCancellation(Func<CancellationToken, Task> toRun, string cancelDescription)
        {
            IncrementBlockingTasks();
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            ContextDispatcher?.InvokeAsync(() =>
            {
                CancellationList.Add(
                    new UserCancellations {CancelSource = tokenSource, Description = cancelDescription});
                ShowCancellations = CancellationList.Any();
            });


            // ReSharper disable once MethodSupportsCancellation No token for final cancellation
            Task.Run(async () => await toRun(token), token).ContinueWith(x => BlockTaskCompleted(x, tokenSource));
        }

        public Command RunBlockingTaskWithCancellationCommand(Func<CancellationToken, Task> toRun,
            string cancelDescription)
        {
            return new(() => RunBlockingTaskWithCancellation(toRun, cancelDescription));
        }

        public void RunFireAndForgetBlockingTaskWithUiMessageReturn(Func<Task> toRun)
        {
            try
            {
                IncrementBlockingTasks();
                Task.Run(async () => await toRun()).ContinueWith(FireAndForgetBlockingTaskWithUiMessageReturnCompleted);
            }
            catch (Exception e)
            {
                ShowMessage("Error", e.ToString(), new List<string> {"Ok"}).Wait();
                DecrementBlockingTasks();

                Task.Run(async () => await EventLogContext.TryWriteExceptionToLog(e, StatusControlContextId.ToString(),
                    await GetStatusLogEntriesString(10)));
            }
        }

        public void RunFireAndForgetTaskWithUiToastErrorReturn(Func<Task> toRun)
        {
            try
            {
                IncrementNonBlockingTasks();
                Task.Run(async () => await toRun()).ContinueWith(FireAndForgetTaskWithToastErrorReturnCompleted);
            }
            catch (Exception e)
            {
                DecrementNonBlockingTasks();
                ToastError($"Error: {e.Message}");

                Task.Run(async () => await EventLogContext.TryWriteExceptionToLog(e, StatusControlContextId.ToString(),
                    await GetStatusLogEntriesString(10)));
            }
        }

        public void RunNonBlockingAction(Action toRun)
        {
            RunNonBlockingTask(() => Task.Run(toRun));
        }

        public Command RunNonBlockingActionCommand(Action toRun)
        {
            return new(() => RunNonBlockingAction(toRun));
        }

        public void RunNonBlockingTask(Func<Task> toRun)
        {
            IncrementNonBlockingTasks();
            Task.Run(toRun).ContinueWith(NonBlockTaskCompleted);
        }

        public Command<T> RunNonBlockingTaskCommand<T>(Func<T, Task> toRun)
        {
            return new(x => RunNonBlockingTask(async () => await toRun(x)));
        }

        public Command RunNonBlockingTaskCommand(Func<Task> toRun)
        {
            return new(() => RunNonBlockingTask(toRun));
        }

        public async Task<string> ShowMessage(string title, string body, List<string> buttons)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            MessageBoxTitle = title;
            MessageBoxMessage = body;
            MessageBoxButtonList = buttons;
            MessageBoxVisible = true;

            await ThreadSwitcher.ResumeBackgroundAsync();

            _currentFullScreenCancellationSource = new CancellationTokenSource();

            try
            {
                await _currentFullScreenCancellationSource.Token.WhenCancelled();
            }
            catch (Exception e)
            {
                if (!(e is OperationCanceledException)) Progress($"ShowMessage Exception {e.Message}");
            }
            finally
            {
                _currentFullScreenCancellationSource.Dispose();
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            var toReturn = ShowMessageResponse ?? string.Empty;

            MessageBoxTitle = string.Empty;
            MessageBoxMessage = string.Empty;
            MessageBoxButtonList = new List<string>();
            ShowMessageResponse = string.Empty;
            MessageBoxVisible = false;

            return toReturn;
        }

        public async Task<string> ShowMessageWithOkButton(string title, string body)
        {
            return await ShowMessage(title, body, new List<string> {"Ok"});
        }

        public async Task<(bool, string)> ShowStringEntry(string title, string body, string initialUserString)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            StringEntryTitle = title;
            StringEntryMessage = body;
            StringEntryUserText = initialUserString;
            StringEntryVisible = true;
            StringEntryApproved = false;

            _currentFullScreenCancellationSource = new CancellationTokenSource();

            try
            {
                await _currentFullScreenCancellationSource.Token.WhenCancelled();
            }
            catch (Exception e)
            {
                if (!(e is OperationCanceledException)) Progress($"ShowMessage Exception {e.Message}");
#pragma warning disable 4014
                Task.Run(async () => await EventLogContext.TryWriteExceptionToLog(e, StatusControlContextId.ToString(),
                    await GetStatusLogEntriesString(10)));
#pragma warning restore 4014
            }
            finally
            {
                _currentFullScreenCancellationSource.Dispose();
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            var toReturn = StringEntryUserText;
            var approved = StringEntryApproved;

            StringEntryTitle = string.Empty;
            StringEntryMessage = string.Empty;

            StringEntryUserText = string.Empty;

            StringEntryVisible = false;
            StringEntryApproved = false;

            return (approved, toReturn);
        }

        public void StateForceDismissFullScreenMessage()
        {
            _currentFullScreenCancellationSource?.Cancel();
        }

        public void ToastError(string toastText)
        {
            ContextDispatcher?.InvokeAsync(() => Toast.Show(toastText, ToastType.Error));
            if (UserSettingsSingleton.LogDiagnosticEvents)
                Task.Run(() =>
                    EventLogContext.TryWriteDiagnosticMessageToLog($"Toast Error - {toastText}",
                        StatusControlContextId.ToString()));
        }

        public void ToastSuccess(string toastText)
        {
            ContextDispatcher?.InvokeAsync(() => Toast.Show(toastText, ToastType.Success));
            if (UserSettingsSingleton.LogDiagnosticEvents)
                Task.Run(() =>
                    EventLogContext.TryWriteDiagnosticMessageToLog($"Toast Error - {toastText}",
                        StatusControlContextId.ToString()));
        }

        public void ToastWarning(string toastText)
        {
            ContextDispatcher?.InvokeAsync(() => Toast.Show(toastText, ToastType.Warning));
            if (UserSettingsSingleton.LogDiagnosticEvents)
                Task.Run(() =>
                    EventLogContext.TryWriteDiagnosticMessageToLog($"Toast Error - {toastText}",
                        StatusControlContextId.ToString()));
        }

        private void UserMessageBoxResponse(string responseString)
        {
            ShowMessageResponse = responseString;
            Progress($"Show Message Response {responseString}");
            _currentFullScreenCancellationSource?.Cancel();
        }

        private void UserStringEntryApprovedResponse()
        {
            StringEntryApproved = true;
            _currentFullScreenCancellationSource?.Cancel();
        }

        private void UserStringEntryCanceledResponse()
        {
            StringEntryApproved = false;
            _currentFullScreenCancellationSource?.Cancel();
        }
    }
}