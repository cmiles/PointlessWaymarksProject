using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Shell;
using JetBrains.Annotations;

namespace PointlessWaymarks.WpfCommon.Status
{
    /// <summary>
    ///     Provides management for the Windows Icon TaskbarItemInfo with properties to Bind and processing
    ///     of requests from multiple sources.
    /// </summary>
    public class WindowIconStatus : INotifyPropertyChanged
    {
        private readonly List<WindowIconStatusRequest> _statusList = new();
        private decimal _windowProgress;
        private TaskbarItemProgressState _windowState;

        public decimal WindowProgress
        {
            get => _windowProgress;
            set
            {
                if (value == _windowProgress) return;
                _windowProgress = value;
                OnPropertyChanged();
            }
        }

        public TaskbarItemProgressState WindowState
        {
            get => _windowState;
            private set
            {
                if (value == _windowState) return;
                _windowState = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void AddRequest(WindowIconStatusRequest request)
        {
            request.RequestedOn = DateTime.Now;

            _statusList.RemoveAll(x => x.RequestedBy == request.RequestedBy);
            _statusList.Add(request);

            if (!_statusList.Any())
            {
                WindowState = TaskbarItemProgressState.None;
                return;
            }

            if (_statusList.Any(x => x.StateRequest == TaskbarItemProgressState.Error))
            {
                WindowState = TaskbarItemProgressState.Error;
                return;
            }

            if (_statusList.Any(x => x.StateRequest == TaskbarItemProgressState.Paused))
            {
                WindowState = TaskbarItemProgressState.Paused;
                return;
            }

            if (_statusList.Any(x => x.StateRequest == TaskbarItemProgressState.Normal))
            {
                var progressEntries =
                    _statusList.Where(x => x.StateRequest == TaskbarItemProgressState.Normal).ToList();
                WindowProgress = progressEntries.Sum(x => x.Progress ?? 0) / progressEntries.Count;
                WindowState = TaskbarItemProgressState.Normal;
                return;
            }

            if (_statusList.Any(x => x.StateRequest == TaskbarItemProgressState.Indeterminate))
            {
                WindowState = TaskbarItemProgressState.Indeterminate;
                return;
            }

            WindowState = TaskbarItemProgressState.None;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public record WindowIconStatusRequest(Guid RequestedBy, TaskbarItemProgressState StateRequest,
        decimal? Progress = null)
    {
        public DateTime RequestedOn { get; set; }
    }
}