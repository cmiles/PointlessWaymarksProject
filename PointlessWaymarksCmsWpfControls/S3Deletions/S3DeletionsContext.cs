#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ABI.System.Collections.Specialized;
using JetBrains.Annotations;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.S3Deletions
{
    public class S3DeletionsContext : INotifyPropertyChanged
    {
        public S3DeletionsContext(StatusControlContext? statusContext)
        {
            _statusContext = statusContext ?? new StatusControlContext();
        }

        public StatusControlContext StatusContext
        {
            get => _statusContext;
            set
            {
                if (Equals(value, _statusContext)) return;
                _statusContext = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<string>? _items;
        private StatusControlContext _statusContext;

        public ObservableCollection<string>? Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged();
            }
        }

        public async Task LoadData(List<string> keysToDelete)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            keysToDelete ??= new List<string>();

            await ThreadSwitcher.ResumeForegroundAsync();

            Items = new ObservableCollection<string>(keysToDelete);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
