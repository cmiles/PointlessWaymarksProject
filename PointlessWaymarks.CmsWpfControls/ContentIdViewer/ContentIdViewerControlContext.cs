using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.ContentIdViewer
{
    public class ContentIdViewerControlContext : INotifyPropertyChanged
    {
        private string _contentIdInformation;
        private IContentId _dbEntry;
        private StatusControlContext _statusContext;

        private ContentIdViewerControlContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();
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

        public event PropertyChangedEventHandler PropertyChanged;

        public static async Task<ContentIdViewerControlContext> CreateInstance(StatusControlContext statusContext,
            IContentId dbEntry)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var newContext = new ContentIdViewerControlContext(statusContext);
            await newContext.LoadData(dbEntry);
            return newContext;
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
    }
}