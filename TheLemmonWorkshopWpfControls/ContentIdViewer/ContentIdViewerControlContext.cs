using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using TheLemmonWorkshopData.Models;
using TheLemmonWorkshopWpfControls.ControlStatus;
using TheLemmonWorkshopWpfControls.Utility;

namespace TheLemmonWorkshopWpfControls.ContentIdViewer
{
    public class ContentIdViewerControlContext : INotifyPropertyChanged
    {
        private StatusControlContext _statusContext;
        private string _contentIdInformation;
        private IContentId _dbEntry;

        public ContentIdViewerControlContext(StatusControlContext statusContext,
            IContentId dbEntry)
        {
            StatusContext = statusContext;
            
            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(() => LoadData(dbEntry));
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

        public async Task LoadData(IContentId dbEntry)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            
            DbEntry = dbEntry;

            if (dbEntry == null) 
            {
                ContentIdInformation = "Id: (Db Entry Is Null) Fingerprint: (Db Entry is Null)";
                return;
            }

            
            ContentIdInformation = $"Id: {dbEntry.Id} Fingerprint: {dbEntry.Fingerprint}";
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

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}