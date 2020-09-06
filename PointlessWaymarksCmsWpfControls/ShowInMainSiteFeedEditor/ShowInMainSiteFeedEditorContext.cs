using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.ShowInMainSiteFeedEditor
{
    public class ShowInMainSiteFeedEditorContext : INotifyPropertyChanged, IHasChanges

    {
        private IShowInSiteFeed _dbEntry;
        private readonly bool _defaultSetting;
        private bool _hasChanges;
        private bool _showInMainSiteFeed;
        private bool _showInMainSiteFeedHasChanges;
        private StatusControlContext _statusContext;

        private ShowInMainSiteFeedEditorContext(StatusControlContext statusContext, bool defaultSetting)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            _defaultSetting = defaultSetting;
        }

        public IShowInSiteFeed DbEntry
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

        public bool ShowInMainSiteFeed
        {
            get => _showInMainSiteFeed;
            set
            {
                if (value == _showInMainSiteFeed) return;
                _showInMainSiteFeed = value;
                OnPropertyChanged();
            }
        }

        public bool ShowInMainSiteFeedHasChanges
        {
            get => _showInMainSiteFeedHasChanges;
            set
            {
                if (value == _showInMainSiteFeedHasChanges) return;
                _showInMainSiteFeedHasChanges = value;
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
            ShowInMainSiteFeedHasChanges = ShowInMainSiteFeed != (DbEntry?.ShowInMainSiteFeed ?? false);
            HasChanges = ShowInMainSiteFeedHasChanges;
        }

        public static async Task<ShowInMainSiteFeedEditorContext> CreateInstance(StatusControlContext statusContext,
            IShowInSiteFeed dbEntry, bool defaultSetting)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var newContext = new ShowInMainSiteFeedEditorContext(statusContext, defaultSetting);
            await newContext.LoadData(dbEntry);
            return newContext;
        }

        private async Task LoadData(IShowInSiteFeed toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad;
            ShowInMainSiteFeed = toLoad?.ShowInMainSiteFeed ?? _defaultSetting;
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