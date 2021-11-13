﻿using System.ComponentModel;
using System.Runtime.CompilerServices;
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
        private ConversionDataEntryContext<DateTime> _feedOnEntry;
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

        public ConversionDataEntryContext<DateTime> FeedOnEntry
        {
            get => _feedOnEntry;
            set
            {
                if (Equals(value, _feedOnEntry)) return;
                _feedOnEntry = value;
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

            FeedOnEntry =
                ConversionDataEntryContext<DateTime>.CreateInstance(ConversionDataEntryHelpers.DateTimeConversion);
            FeedOnEntry.Title = "Show in Site Feeds On";
            FeedOnEntry.HelpText =
                "Sets when (if enabled) the content will appear on the Front Page and in RSS Feeds";
            FeedOnEntry.ReferenceValue = DbEntry.FeedOn;
            FeedOnEntry.UserText = DbEntry.FeedOn.ToString("MM/dd/yyyy h:mm:ss tt");

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