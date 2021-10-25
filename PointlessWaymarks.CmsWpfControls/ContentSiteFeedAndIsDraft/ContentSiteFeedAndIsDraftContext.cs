using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.BoolDataEntry;
using PointlessWaymarks.CmsWpfControls.ConversionDataEntry;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.ContentSiteFeedAndIsDraft
{
    public class ContentSiteFeedAndIsDraftContext : INotifyPropertyChanged, IHasChanges, IHasValidationIssues,
        ICheckForChangesAndValidation
    {
        private IMainSiteFeed _dbEntry;
        private bool _hasChanges;
        private bool _hasValidationIssues;
        private BoolDataEntryContext _isDraftEntry;
        private BoolDataEntryContext _showInMainSiteFeedEntry;
        private ConversionDataEntryContext<DateTime> _showInMainSiteFeedOnEntry;
        private StatusControlContext _statusContext;

        public ContentSiteFeedAndIsDraftContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new();
        }

        public IMainSiteFeed DbEntry
        {
            get => _dbEntry;
            set
            {
                if (Equals(value, _dbEntry)) return;
                _dbEntry = value;
                OnPropertyChanged();
            }
        }

        public BoolDataEntryContext IsDraftEntry
        {
            get => _isDraftEntry;
            set
            {
                if (Equals(value, _isDraftEntry)) return;
                _isDraftEntry = value;
                OnPropertyChanged();
            }
        }

        public BoolDataEntryContext ShowInMainSiteFeedEntry
        {
            get => _showInMainSiteFeedEntry;
            set
            {
                if (Equals(value, _showInMainSiteFeedEntry)) return;
                _showInMainSiteFeedEntry = value;
                OnPropertyChanged();
            }
        }

        public ConversionDataEntryContext<DateTime> ShowInMainSiteFeedOnEntry
        {
            get => _showInMainSiteFeedOnEntry;
            set
            {
                if (Equals(value, _showInMainSiteFeedOnEntry)) return;
                _showInMainSiteFeedOnEntry = value;
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

        public void CheckForChangesAndValidationIssues()
        {
            HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);

            HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
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

        public bool HasValidationIssues
        {
            get => _hasValidationIssues;
            set
            {
                if (value == _hasValidationIssues) return;
                _hasValidationIssues = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static async Task<ContentSiteFeedAndIsDraftContext> CreateInstance(StatusControlContext statusContext,
            IMainSiteFeed dbEntry)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var newItem = new ContentSiteFeedAndIsDraftContext(statusContext);
            await newItem.LoadData(dbEntry);

            return newItem;
        }

        public async Task LoadData(IMainSiteFeed dbEntry)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = dbEntry;

            ShowInMainSiteFeedEntry = BoolDataEntryContext.CreateInstanceForShowInMainSiteFeed(DbEntry, false);

            ShowInMainSiteFeedOnEntry =
                ConversionDataEntryContext<DateTime>.CreateInstance(ConversionDataEntryHelpers.DateTimeConversion);
            ShowInMainSiteFeedOnEntry.Title = "Show in Site Feeds On";
            ShowInMainSiteFeedOnEntry.HelpText =
                "Sets when (if enabled) the content will appear on the Front Page and in RSS Feeds";
            ShowInMainSiteFeedOnEntry.ReferenceValue = DbEntry.FeedOn;
            ShowInMainSiteFeedOnEntry.UserText = DbEntry.FeedOn.ToString("MM/dd/yyyy h:mm:ss tt");

            IsDraftEntry = BoolDataEntryContext.CreateInstanceForIsDraft(DbEntry, false);

            PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges") && !propertyName.Contains("Validation"))
                CheckForChangesAndValidationIssues();
        }
    }
}