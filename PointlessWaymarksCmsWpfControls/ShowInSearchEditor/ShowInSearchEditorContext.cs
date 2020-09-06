using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.ShowInSearchEditor
{
    public class ShowInSearchEditorContext : INotifyPropertyChanged, IHasChanges
    {
        private IShowInSearch _dbEntry;
        private readonly bool _defaultSetting;
        private bool _hasChanges;
        private bool _showInSearch;
        private bool _showInSearchHasChanges;
        private StatusControlContext _statusContext;

        private ShowInSearchEditorContext(StatusControlContext statusContext, bool defaultSetting)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            _defaultSetting = defaultSetting;
        }

        public IShowInSearch DbEntry
        {
            get => _dbEntry;
            set
            {
                if (Equals(value, _dbEntry)) return;
                _dbEntry = value;
                OnPropertyChanged();
            }
        }

        public bool HasChanges
        {
            get => _hasChanges;
            set
            {
                if (value == _hasChanges) return;
                _hasChanges = value;
                OnPropertyChanged();
            }
        }

        public bool ShowInSearch
        {
            get => _showInSearch;
            set
            {
                if (value == _showInSearch) return;
                _showInSearch = value;
                OnPropertyChanged();
            }
        }

        public bool ShowInSearchHasChanges
        {
            get => _showInSearchHasChanges;
            set
            {
                if (value == _showInSearchHasChanges) return;
                _showInSearchHasChanges = value;
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

        private void CheckForChanges()
        {
            ShowInSearchHasChanges = ShowInSearch != (DbEntry?.ShowInSearch ?? false);
            HasChanges = ShowInSearchHasChanges;
        }

        public static async Task<ShowInSearchEditorContext> CreateInstance(StatusControlContext statusContext,
            IShowInSearch dbEntry, bool defaultSetting)
        {
            var newContext = new ShowInSearchEditorContext(statusContext, defaultSetting);
            await newContext.LoadData(dbEntry);

            return newContext;
        }

        private async Task LoadData(IShowInSearch toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad;
            ShowInSearch = toLoad?.ShowInSearch ?? _defaultSetting;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges") && !propertyName.Contains("Validation")) CheckForChanges();
        }
    }
}