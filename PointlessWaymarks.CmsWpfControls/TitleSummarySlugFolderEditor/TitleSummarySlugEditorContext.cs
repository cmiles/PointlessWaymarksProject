using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentFolder;
using PointlessWaymarks.CmsWpfControls.StringDataEntry;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.TitleSummarySlugFolderEditor
{
    public class TitleSummarySlugEditorContext : INotifyPropertyChanged, IHasChanges, IHasValidationIssues,
        ICheckForChangesAndValidation
    {
        private Func<TitleSummarySlugEditorContext, bool> _customTitleCheckToEnable;
        private Command _customTitleCommand;
        private bool _customTitleFunctionEnabled;
        private string _customTitleFunctionText;
        private bool _customTitleFunctionVisible;
        private ITitleSummarySlugFolder _dbEntry;
        private ContentFolderContext _folderEntry;
        private bool _hasChanges;
        private bool _hasValidationIssues;
        private StringDataEntryContext _slugEntry;
        private StatusControlContext _statusContext;
        private StringDataEntryContext _summaryEntry;
        private StringDataEntryContext _titleEntry;
        private Command _titleToSlugCommand;
        private bool _titleToSlugEnabled = true;
        private Command _titleToSummaryCommand;
        private bool _titleToSummaryEnabled = true;

        private TitleSummarySlugEditorContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();
        }

        private TitleSummarySlugEditorContext(StatusControlContext statusContext, string customTitleCommandText,
            Command customTitleCommand, Func<TitleSummarySlugEditorContext, bool> customTitleCheckToEnable)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            CustomTitleFunctionText = customTitleCommandText;
            CustomTitleCommand = customTitleCommand;
            CustomTitleCheckToEnable = customTitleCheckToEnable;
            CustomTitleFunctionVisible = true;
        }

        public Func<TitleSummarySlugEditorContext, bool> CustomTitleCheckToEnable
        {
            get => _customTitleCheckToEnable;
            set
            {
                if (Equals(value, _customTitleCheckToEnable)) return;
                _customTitleCheckToEnable = value;
                OnPropertyChanged();
            }
        }

        public Command CustomTitleCommand
        {
            get => _customTitleCommand;
            set
            {
                if (Equals(value, _customTitleCommand)) return;
                _customTitleCommand = value;
                OnPropertyChanged();
            }
        }

        public bool CustomTitleFunctionEnabled
        {
            get => _customTitleFunctionEnabled;
            set
            {
                if (value == _customTitleFunctionEnabled) return;
                _customTitleFunctionEnabled = value;
                OnPropertyChanged();
            }
        }

        public string CustomTitleFunctionText
        {
            get => _customTitleFunctionText;
            set
            {
                if (value == _customTitleFunctionText) return;
                _customTitleFunctionText = value;
                OnPropertyChanged();
            }
        }

        public bool CustomTitleFunctionVisible
        {
            get => _customTitleFunctionVisible;
            set
            {
                if (value == _customTitleFunctionVisible) return;
                _customTitleFunctionVisible = value;
                OnPropertyChanged();
            }
        }

        public ITitleSummarySlugFolder DbEntry
        {
            get => _dbEntry;
            set
            {
                if (Equals(value, _dbEntry)) return;
                _dbEntry = value;
                OnPropertyChanged();
            }
        }

        public ContentFolderContext FolderEntry
        {
            get => _folderEntry;
            set
            {
                if (Equals(value, _folderEntry)) return;
                _folderEntry = value;
                OnPropertyChanged();
            }
        }

        public StringDataEntryContext SlugEntry
        {
            get => _slugEntry;
            set
            {
                if (Equals(value, _slugEntry)) return;
                _slugEntry = value;
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

        public StringDataEntryContext SummaryEntry
        {
            get => _summaryEntry;
            set
            {
                if (Equals(value, _summaryEntry)) return;
                _summaryEntry = value;
                OnPropertyChanged();
            }
        }

        public StringDataEntryContext TitleEntry
        {
            get => _titleEntry;
            set
            {
                if (Equals(value, _titleEntry)) return;
                _titleEntry = value;
                OnPropertyChanged();
            }
        }

        public Command TitleToSlugCommand
        {
            get => _titleToSlugCommand;
            set
            {
                if (Equals(value, _titleToSlugCommand)) return;
                _titleToSlugCommand = value;
                OnPropertyChanged();
            }
        }

        public bool TitleToSlugEnabled
        {
            get => _titleToSlugEnabled;
            set
            {
                if (value == _titleToSlugEnabled) return;
                _titleToSlugEnabled = value;
                OnPropertyChanged();
            }
        }


        public Command TitleToSummaryCommand
        {
            get => _titleToSummaryCommand;
            set
            {
                if (Equals(value, _titleToSummaryCommand)) return;
                _titleToSummaryCommand = value;
                OnPropertyChanged();
            }
        }

        public bool TitleToSummaryEnabled
        {
            get => _titleToSummaryEnabled;
            set
            {
                if (value == _titleToSummaryEnabled) return;
                _titleToSummaryEnabled = value;
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

        public void CheckForChangesToTitleToFunctionStates()
        {
            try
            {
                TitleToSlugEnabled = SlugUtility.Create(true, TitleEntry.UserValue) != SlugEntry.UserValue;
                TitleToSummaryEnabled =
                    !(SummaryEntry.UserValue.Equals(TitleEntry.UserValue, StringComparison.OrdinalIgnoreCase) ||
                      (SummaryEntry.UserValue.Length - 1 == TitleEntry.UserValue.Length &&
                       char.IsPunctuation(SummaryEntry.UserValue[^1]) && SummaryEntry.UserValue[..^1]
                           .Equals(TitleEntry.UserValue, StringComparison.OrdinalIgnoreCase)));

                CustomTitleFunctionEnabled = CustomTitleCheckToEnable?.Invoke(this) ?? false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static async Task<TitleSummarySlugEditorContext> CreateInstance(StatusControlContext statusContext,
            ITitleSummarySlugFolder dbEntry)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var newItem = new TitleSummarySlugEditorContext(statusContext);
            await newItem.LoadData(dbEntry);

            return newItem;
        }

        public static async Task<TitleSummarySlugEditorContext> CreateInstance(StatusControlContext statusContext,
            string customTitleCommandText, Command customTitleCommand,
            Func<TitleSummarySlugEditorContext, bool> customTitleCheckToEnable, ITitleSummarySlugFolder dbEntry)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var newItem = new TitleSummarySlugEditorContext(statusContext, customTitleCommandText, customTitleCommand,
                customTitleCheckToEnable);
            await newItem.LoadData(dbEntry);

            return newItem;
        }

        public async Task LoadData(ITitleSummarySlugFolder dbEntry)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            TitleToSlugCommand = StatusContext.RunBlockingActionCommand(TitleToSlug);
            TitleToSummaryCommand = StatusContext.RunBlockingActionCommand(TitleToSummary);

            DbEntry = dbEntry;

            TitleEntry = StringDataEntryContext.CreateTitleInstance(DbEntry);
            TitleEntry.PropertyChanged += TitleChangedMonitor;

            SlugEntry = StringDataEntryContext.CreateSlugInstance(DbEntry);
            SlugEntry.PropertyChanged += TitleChangedMonitor;

            SummaryEntry = StringDataEntryContext.CreateSummaryInstance(DbEntry);
            SummaryEntry.PropertyChanged += TitleChangedMonitor;

            FolderEntry = await ContentFolderContext.CreateInstance(StatusContext, DbEntry);

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

        private void TitleChangedMonitor(object sender, PropertyChangedEventArgs e)
        {
            if (!e?.PropertyName?.Equals("UserValue") ?? true) return;

            CheckForChangesToTitleToFunctionStates();
        }

        public void TitleToSlug()
        {
            SlugEntry.UserValue = SlugUtility.Create(true, TitleEntry.UserValue);
        }


        public void TitleToSummary()
        {
            SummaryEntry.UserValue = TitleEntry.UserValue;

            if (!char.IsPunctuation(SummaryEntry.UserValue[^1]))
                SummaryEntry.UserValue += ".";
        }
    }
}