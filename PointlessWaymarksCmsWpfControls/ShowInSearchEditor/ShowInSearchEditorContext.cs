using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.ShowInSearchEditor
{
    public class ShowInSearchEditorContext : INotifyPropertyChanged

    {
        private IShowInSearch _dbEntry;
        private readonly bool _defaultSetting;
        private bool _showInSearch;
        private bool _showInSearchHasChanges;
        private StatusControlContext _statusContext;

        public ShowInSearchEditorContext(StatusControlContext statusContext, IShowInSearch dbEntry, bool defaultSetting)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            _defaultSetting = defaultSetting;
            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(() => LoadData(dbEntry));
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
            if (!propertyName?.Contains("HasChanges") ?? false) CheckForChanges();
        }
    }
}