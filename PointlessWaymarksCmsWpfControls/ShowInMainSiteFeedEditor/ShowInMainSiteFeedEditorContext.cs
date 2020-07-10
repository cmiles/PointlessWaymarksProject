using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.ShowInMainSiteFeedEditor
{
    public class ShowInMainSiteFeedEditorContext : INotifyPropertyChanged

    {
        private IShowInSiteFeed _dbEntry;
        private readonly bool _defaultSetting;
        private bool _showInMainSite;
        private bool _showInMainSiteHasChanges;
        private StatusControlContext _statusContext;

        public ShowInMainSiteFeedEditorContext(StatusControlContext statusContext, IShowInSiteFeed dbEntry,
            bool defaultSetting)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            _defaultSetting = defaultSetting;
            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(() => LoadData(dbEntry));
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

        public bool ShowInMainSite
        {
            get => _showInMainSite;
            set
            {
                if (value == _showInMainSite) return;
                _showInMainSite = value;
                OnPropertyChanged();
            }
        }

        public bool ShowInMainSiteHasChanges
        {
            get => _showInMainSiteHasChanges;
            set
            {
                if (value == _showInMainSiteHasChanges) return;
                _showInMainSiteHasChanges = value;
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
            ShowInMainSiteHasChanges = ShowInMainSite != (DbEntry?.ShowInMainSiteFeed ?? false);
        }

        private async Task LoadData(IShowInSiteFeed toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad;
            ShowInMainSite = toLoad?.ShowInMainSiteFeed ?? _defaultSetting;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges")) CheckForChanges();
        }
    }
}