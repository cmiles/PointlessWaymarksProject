using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using TheLemmonWorkshopData.Models;
using TheLemmonWorkshopWpfControls.ControlStatus;

namespace TheLemmonWorkshopWpfControls.UpdatesByAndOnDisplay
{
    public class CreatedAndUpdatedByAndOnDisplayContext : INotifyPropertyChanged
    {
        private string _createdAndUpdatedByAndOn;
        private ICreatedAndLastUpdateOnAndBy _dbEntry;

        public CreatedAndUpdatedByAndOnDisplayContext(StatusControlContext statusContext,
            ICreatedAndLastUpdateOnAndBy dbEntry)
        {
            StatusContext = statusContext;
            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(() => LoadData(dbEntry));
        }

        public ICreatedAndLastUpdateOnAndBy DbEntry
        {
            get => _dbEntry;
            set
            {
                if (Equals(value, _dbEntry)) return;
                _dbEntry = value;
                OnPropertyChanged();
            }
        }

        public string CreatedAndUpdatedByAndOn
        {
            get => _createdAndUpdatedByAndOn;
            set
            {
                if (value == _createdAndUpdatedByAndOn) return;
                _createdAndUpdatedByAndOn = value;
                OnPropertyChanged();
            }
        }

        public StatusControlContext StatusContext { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task LoadData(ICreatedAndLastUpdateOnAndBy toLoad)
        {
            DbEntry = toLoad;

            var newStringParts = new List<string>();

            CreatedAndUpdatedByAndOn = string.Empty;
            if (DbEntry == null) return;

            if (!string.IsNullOrWhiteSpace(DbEntry.CreatedBy))
                newStringParts.Add($"Created By {DbEntry.CreatedBy.Trim()}");
            else
                newStringParts.Add("Created");
            newStringParts.Add($"On {DbEntry.CreatedOn:g}");

            if (!string.IsNullOrWhiteSpace(DbEntry.LastUpdatedBy))
                newStringParts.Add($"Last Update By {DbEntry.LastUpdatedBy.Trim()}");
            else if (DbEntry.LastUpdatedOn != null) newStringParts.Add("Last Update");

            if (DbEntry.LastUpdatedOn != null) newStringParts.Add($"On {DbEntry.LastUpdatedOn:g}");

            CreatedAndUpdatedByAndOn = string.Join(" ", newStringParts);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}