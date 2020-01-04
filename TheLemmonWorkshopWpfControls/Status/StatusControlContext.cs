using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight.CommandWpf;
using JetBrains.Annotations;
using TheLemmonWorkshopWpfControls.ToastControl;
using TheLemmonWorkshopWpfControls.Utility;

namespace TheLemmonWorkshopWpfControls.ControlStatus
{
    public class StatusControlContext : INotifyPropertyChanged
    {
        private bool _blockUi;
        private int _countOfRunningBlockingTasks;
        private int _countOfRunningNonBlockingTasks;
        private List<string> _messageBoxButtonList;
        private string _messageBoxMessage;
        private string _messageBoxTitle;
        private bool _messageBoxVisible;
        private bool _nonBlockingTaskAreRunning;
        private ObservableCollection<string> _statusLog;
        private bool _stringEntryApproved;
        private string _stringEntryMessage;
        private string _stringEntryTitle;
        private string _stringEntryUserText;
        private bool _stringEntryVisible;
        private ToastSource _toast;

        public StatusControlContext()
        {
            Toast = new ToastSource();
            StatusLog = new ObservableCollection<string>();

            UserMessageBoxResponseCommand = new RelayCommand<string>(UserMessageBoxResponse);
            UserStringEntryApprovedResponseCommand = new RelayCommand(UserStringEntryApprovedResponse);
            UserStringEntryCancelledResponseCommand = new RelayCommand(UserStringEntryCancelledResponse);
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

        public int CountOfRunningBlockingTasks
        {
            get => _countOfRunningBlockingTasks;
            set
            {
                if (value == _countOfRunningBlockingTasks) return;
                _countOfRunningBlockingTasks = value;
                OnPropertyChanged();

                BlockUi = CountOfRunningBlockingTasks > 0;
            }
        }

        public int CountOfRunningNonBlockingTasks
        {
            get => _countOfRunningNonBlockingTasks;
            set
            {
                if (value == _countOfRunningNonBlockingTasks) return;
                _countOfRunningNonBlockingTasks = value;
                OnPropertyChanged();

                if (CountOfRunningBlockingTasks > 0) NonBlockingTaskAreRunning = true;
            }
        }

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

        public string ShowMessageResponse { get; set; }

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

        public RelayCommand<string> UserMessageBoxResponseCommand { get; set; }
        public RelayCommand UserStringEntryApprovedResponseCommand { get; set; }
        public RelayCommand UserStringEntryCancelledResponseCommand { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public IProgress<string> ProgressTracker()
        {
            var toReturn = new Progress<string>();
            toReturn.ProgressChanged += ProgressTrackerChange;
            return toReturn;
        }

        public void RunBlockingAction(Action toRun)
        {
            RunBlockingTask(() => Task.Run(toRun));
        }

        public void RunBlockingAction<T>(Action<T> toRun, T parameter)
        {
            RunBlockingTask(() => Task.Run(() => toRun(parameter)));
        }

        public void RunBlockingTask(Func<Task> toRun)
        {
            CountOfRunningBlockingTasks++;
            Task.Run(toRun).ContinueWith(BlockTaskCompleted);
        }

        public void RunNonBlockingAction(Action toRun)
        {
            RunNonBlockingTask(() => Task.Run(toRun));
        }

        public void RunNonBlockingTask(Func<Task> toRun)
        {
            CountOfRunningNonBlockingTasks++;
            Task.Run(toRun).ContinueWith(NonBlockTaskCompleted);
        }

        public void RunFireAndForgetTaskWithUiToastErrorReturn(Func<Task> toRun)
        {
            Task.Run(toRun).ContinueWith(FireAndForgetTaskWithToastErrorReturnCompleted);
        }
        
        public void RunFireAndForgetBlockingTaskWithUiMessageReturn(Func<Task> toRun)
        {
            BlockUi = true;
            Task.Run(toRun).ContinueWith(FireAndForgetBlockingTaskWithUiMessageReturnCompleted);
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
                await Task.Delay(TimeSpan.FromMinutes(3), _currentFullScreenCancellationSource.Token);
            }
            catch (Exception e)
            {
                if (!(e is OperationCanceledException))
                {
                    Progress($"ShowMessage Exception {e.Message}");
                }
            }
            finally
            {
                _currentFullScreenCancellationSource = null;
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

        private CancellationTokenSource _currentFullScreenCancellationSource;


        public void StateForceDismissFullScreenMessage()
        {
            _currentFullScreenCancellationSource?.Cancel();
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
                await Task.Delay(TimeSpan.FromMinutes(3), _currentFullScreenCancellationSource.Token);
            }
            catch (Exception e)
            {
                if (!(e is OperationCanceledException))
                {
                    Progress($"ShowMessage Exception {e.Message}");
                }
            }
            finally
            {
                _currentFullScreenCancellationSource = null;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            var toReturn = StringEntryUserText;
            var approved = StringEntryApproved;

            StringEntryTitle = string.Empty;
            StringEntryMessage = string.Empty;
            ;
            StringEntryUserText = string.Empty;
            ;
            StringEntryVisible = false;
            StringEntryApproved = false;

            return (approved, toReturn);
        }

        public void ToastError(string toastText)
        {
            Application.Current.Dispatcher?.InvokeAsync(() => Toast.Show(toastText, ToastType.Error));
        }

        public void ToastSuccess(string toastText)
        {
            Application.Current.Dispatcher?.InvokeAsync(() => Toast.Show(toastText, ToastType.Success));
        }

        public void ToastWarning(string toastText)
        {
            Application.Current.Dispatcher?.InvokeAsync(() => Toast.Show(toastText, ToastType.Warning));
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void BlockTaskCompleted(Task obj)
        {
            CountOfRunningBlockingTasks--;

            if (obj.IsCanceled)
            {
                ToastWarning("Cancelled Task");
                return;
            }

            if (obj.IsFaulted) ToastError($"Error: {obj.Exception?.Message}");
        }

        private void NonBlockTaskCompleted(Task obj)
        {
            CountOfRunningNonBlockingTasks--;

            if (obj.IsCanceled)
            {
                ToastWarning("Cancelled Task");
                return;
            }

            if (obj.IsFaulted) ToastError($"Error: {obj.Exception?.Message}");
        }

        private void FireAndForgetTaskWithToastErrorReturnCompleted(Task obj)
        {
            if (obj.IsCanceled) return;

            if (obj.IsFaulted) ToastError($"Error: {obj.Exception?.Message}");
        }

        private async void FireAndForgetBlockingTaskWithUiMessageReturnCompleted(Task obj)
        {
            if (obj.IsCanceled) return;

            if (obj.IsFaulted) await ShowMessage("Error", obj.Exception.ToString(), new List<string> {"Ok"});
            else BlockUi = false;
        }

        private void ProgressTrackerChange(object sender, string e)
        {
            Progress(e);
        }

        public void Progress(string e)
        {
            Application.Current.Dispatcher?.InvokeAsync(() =>
            {
                StatusLog.Add(e);

                if (StatusLog.Count > 20) StatusLog.Remove(StatusLog.First());
            });
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

        private void UserStringEntryCancelledResponse()
        {
            StringEntryApproved = false;
            _currentFullScreenCancellationSource?.Cancel();
        }
    }
}