using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.ContentIdViewer
{
    public class ContentIdViewerControlContext : INotifyPropertyChanged
    {
        private string _contentIdInformation;
        private IContentId _dbEntry;
        private StatusControlContext _statusContext;

        public ContentIdViewerControlContext(StatusControlContext statusContext, IContentId dbEntry)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(() => LoadData(dbEntry));
        }

        public string ContentIdInformation
        {
            get => _contentIdInformation;
            set
            {
                if (value == _contentIdInformation) return;
                _contentIdInformation = value;
                OnPropertyChanged();
            }
        }

        public IContentId DbEntry
        {
            get => _dbEntry;
            set
            {
                if (Equals(value, _dbEntry)) return;
                _dbEntry = value;
                OnPropertyChanged();
            }
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

        public async Task LoadData(IContentId dbEntry)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = dbEntry;

            if (dbEntry == null)
            {
                ContentIdInformation = "Id: (Db Entry Is Null) Fingerprint: (Db Entry is Null)";
                return;
            }

            ContentIdInformation = $" Fingerprint: {dbEntry.ContentId} Db Id: {dbEntry.Id}";
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}