using GalaSoft.MvvmLight.CommandWpf;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TheLemmonWorkshopWpfControls.ToastControl;
using TheLemmonWorkshopWpfControls.Utility;

namespace TheLemmonWorkshopWpfControls.ControlStatus
{
    public class ControlStatusViewModel : INotifyPropertyChanged
    {
        private readonly ManualResetEvent _messageBoxMre = new ManualResetEvent(false);
        private bool _blockUi;
        private int _countOfRunningBlockingTasks;
        private int _countOfRunningNonBlockingTasks;
        private List<string> _messageBoxButtonList;
        private string _messageBoxButtonOneText;
        private string _messageBoxButtonThreeText;
        private string _messageBoxButtonTwoText;
        private string _messageBoxMessage;
        private string _messageBoxTitle;
        private bool _messageBoxVisible;
        private bool _nonBlockingTaskAreRunning;
        private ObservableCollection<string> _statusLog;
        private ToastSource _toast;

        public ControlStatusViewModel()
        {
            Toast = new ToastSource();
            StatusLog = new ObservableCollection<string>();

            UserMessageBoxResponseCommand = new RelayCommand<string>(UserMessageBoxResponse);
        }

        public event PropertyChangedEventHandler PropertyChanged;

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

        public string MessageBoxButtonOneText
        {
            get => _messageBoxButtonOneText;
            set
            {
                if (value == _messageBoxButtonOneText) return;
                _messageBoxButtonOneText = value;
                OnPropertyChanged();
            }
        }

        public string MessageBoxButtonThreeText
        {
            get => _messageBoxButtonThreeText;
            set
            {
                if (value == _messageBoxButtonThreeText) return;
                _messageBoxButtonThreeText = value;
                OnPropertyChanged();
            }
        }

        public string MessageBoxButtonTwoText
        {
            get => _messageBoxButtonTwoText;
            set
            {
                if (value == _messageBoxButtonTwoText) return;
                _messageBoxButtonTwoText = value;
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

        public string MessageBoxResponse { get; set; }

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

        public async Task<string> ShowMessage(string title, string body, List<string> buttons)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            MessageBoxTitle = title;
            MessageBoxMessage = body;
            MessageBoxButtonList = buttons;
            MessageBoxVisible = true;

            await ThreadSwitcher.ResumeBackgroundAsync();

            _messageBoxMre.WaitOne();
            _messageBoxMre.Reset();

            await ThreadSwitcher.ResumeForegroundAsync();

            var toReturn = MessageBoxResponse;

            MessageBoxTitle = string.Empty;
            MessageBoxMessage = string.Empty;
            MessageBoxButtonList = new List<string>();
            MessageBoxResponse = string.Empty;
            MessageBoxVisible = false;

            return toReturn;
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

            if (obj.IsFaulted)
            {
                ToastError($"Error: {obj.Exception?.Message}");
            }
        }

        private void NonBlockTaskCompleted(Task obj)
        {
            CountOfRunningNonBlockingTasks--;

            if (obj.IsCanceled)
            {
                ToastWarning("Cancelled Task");
                return;
            }

            if (obj.IsFaulted)
            {
                ToastError($"Error: {obj.Exception?.Message}");
            }
        }

        private void ProgressTrackerChange(object sender, string e)
        {
            Application.Current.Dispatcher?.InvokeAsync(() =>
            {
                StatusLog.Add(e);

                if (StatusLog.Count > 20)
                {
                    StatusLog.Remove(StatusLog.First());
                }
            });
        }

        private void UserMessageBoxResponse(string responseString)
        {
            MessageBoxResponse = responseString;
            _messageBoxMre?.Set();
        }
    }
}