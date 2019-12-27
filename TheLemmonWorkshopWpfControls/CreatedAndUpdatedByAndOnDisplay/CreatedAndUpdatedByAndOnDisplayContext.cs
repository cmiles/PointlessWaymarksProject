using JetBrains.Annotations;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TheLemmonWorkshopData.Models;
using TheLemmonWorkshopWpfControls.ControlStatus;
using TheLemmonWorkshopWpfControls.Utility;

namespace TheLemmonWorkshopWpfControls.UpdatesByAndOnDisplay
{
    public class CreatedAndUpdatedByAndOnDisplayContext : INotifyPropertyChanged
    {
        private string _createdAndUpdatedByAndOn;
        private ICreatedAndLastUpdateOnAndBy _dbEntry;

        public CreatedAndUpdatedByAndOnDisplayContext(StatusControlContext statusContext,
            ICreatedAndLastUpdateOnAndBy dbEntry)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(() => LoadData(dbEntry));
        }

        public event PropertyChangedEventHandler PropertyChanged;

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

        public StatusControlContext StatusContext { get; set; }

        public async Task LoadData(ICreatedAndLastUpdateOnAndBy toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad;

            var newStringParts = new List<string>();

            CreatedAndUpdatedByAndOn = string.Empty;
            if (DbEntry == null)
            {
                CreatedAndUpdatedByAndOn = "New Entry";
                return;
            }

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