using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsWpfControls.Status;

namespace PointlessWaymarksCmsWpfControls.FileContentEditor
{
    public class FileContentEditorContext : INotifyPropertyChanged
    {
        private StatusControlContext _statusContext;

        public FileContentEditorContext(StatusControlContext statusContext, FileContent toLoad)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () => await LoadData(toLoad));
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

        private async Task LoadData(FileContent toLoad)
        {
            throw new NotImplementedException();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}