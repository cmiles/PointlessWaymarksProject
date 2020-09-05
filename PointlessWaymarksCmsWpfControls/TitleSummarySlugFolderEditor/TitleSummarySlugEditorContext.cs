using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.StringDataEntry;
using PointlessWaymarksCmsWpfControls.Utility;
using TinyIpc.Messaging;

namespace PointlessWaymarksCmsWpfControls.TitleSummarySlugFolderEditor
{
    public class TitleSummarySlugEditorContext : INotifyPropertyChanged, IHasChanges, IHasValidationIssues
    {
        private DataNotificationsWorkQueue _dataNotificationsProcessor;
        private DataNotificationContentType _dataNotificationType;
        private ITitleSummarySlugFolder _dbEntry;
        private ObservableCollection<string> _existingFolderChoices;
        private string _folder = string.Empty;
        private bool _folderHasChanges;
        private bool _folderHasValidationIssues;
        private string _folderValidationMessage;
        private bool _hasChanges;
        private bool _hasValidationIssues;
        private StringDataEntryContext _slugEntry;
        private StatusControlContext _statusContext;
        private StringDataEntryContext _summaryEntry;
        private StringDataEntryContext _titleEntry;
        private Command _titleToSlugCommand;

        private TitleSummarySlugEditorContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            DataNotificationsProcessor = new DataNotificationsWorkQueue {Processor = DataNotificationReceived};
        }

        private TitleSummarySlugEditorContext(StatusControlContext statusContext, ITitleSummarySlugFolder dbEntry)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            DataNotificationsProcessor = new DataNotificationsWorkQueue {Processor = DataNotificationReceived};

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(() => LoadData(dbEntry));
        }

        public DataNotificationsWorkQueue DataNotificationsProcessor
        {
            get => _dataNotificationsProcessor;
            set
            {
                if (Equals(value, _dataNotificationsProcessor)) return;
                _dataNotificationsProcessor = value;
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

        public string Folder
        {
            get => _folder;
            set
            {
                if (value == _folder) return;
                _folder = value;
                OnPropertyChanged();
            }
        }

        public bool FolderHasChanges
        {
            get => _folderHasChanges;
            set
            {
                if (value == _folderHasChanges) return;
                _folderHasChanges = value;
                OnPropertyChanged();
            }
        }

        public bool FolderHasValidationIssues
        {
            get => _folderHasValidationIssues;
            set
            {
                if (value == _folderHasValidationIssues) return;
                _folderHasValidationIssues = value;
                OnPropertyChanged();
            }
        }

        public string FolderValidationMessage
        {
            get => _folderValidationMessage;
            set
            {
                if (value == _folderValidationMessage) return;
                _folderValidationMessage = value;
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

        public event PropertyChangedEventHandler PropertyChanged;

        private void CheckForChangesAndValidate()
        {
            // ReSharper disable InvokeAsExtensionMethod - in this case TrimNullToEmpty - which returns an
            //Empty string from null will not be invoked as an extension if DbEntry is null...
            FolderHasChanges = StringHelpers.TrimNullToEmpty(DbEntry?.Folder) != Folder.TrimNullToEmpty();
            // ReSharper restore InvokeAsExtensionMethod

            HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this) || FolderHasChanges;

            ValidateFolder();

            HasValidationIssues =
                PropertyScanners.ChildPropertiesHaveValidationIssues(this) || FolderHasValidationIssues;
        }

        public static async Task<TitleSummarySlugEditorContext> CreateInstance(StatusControlContext statusContext,
            ITitleSummarySlugFolder dbEntry)
        {
            var newItem = new TitleSummarySlugEditorContext(statusContext);
            await newItem.LoadData(dbEntry);
            return newItem;
        }

        private async Task DataNotificationReceived(TinyMessageReceivedEventArgs e)
        {
            var translatedMessage = DataNotifications.TranslateDataNotification(e.Message);

            if (translatedMessage.HasError)
            {
                await EventLogContext.TryWriteDiagnosticMessageToLog(
                    $"Data Notification Failure in PostListContext - {translatedMessage.ErrorNote}",
                    StatusContext.StatusControlContextId.ToString());
                return;
            }

            if (translatedMessage.UpdateType == DataNotificationUpdateType.LocalContent ||
                translatedMessage.ContentType != _dataNotificationType) return;

            await ThreadSwitcher.ResumeBackgroundAsync();

            var currentDbFolders = await Db.FolderNamesFromContent(DbEntry);

            var newFolderNames = ExistingFolderChoices.Except(currentDbFolders).ToList();

            if (newFolderNames.Any())
            {
                await ThreadSwitcher.ResumeForegroundAsync();
                ExistingFolderChoices.Clear();
                currentDbFolders.ForEach(x => ExistingFolderChoices.Add(x));
            }
        }

        private void OnDataNotificationReceived(object sender, TinyMessageReceivedEventArgs e)
        {
            DataNotificationsProcessor.Enqueue(e);
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


        public async Task LoadData(ITitleSummarySlugFolder dbEntry)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DataNotifications.DataNotificationChannel().MessageReceived -= OnDataNotificationReceived;

            TitleToSlugCommand = StatusContext.RunBlockingActionCommand(TitleToSlug);

            DbEntry = dbEntry;

            TitleEntry = new StringDataEntryContext
            {
                Title = "Title",
                HelpText = "Title Text",
                ReferenceValue = DbEntry?.Title ?? string.Empty,
                UserValue = StringHelpers.NullToEmptyTrim(DbEntry?.Title),
                ValidationFunctions = new List<Func<string, (bool passed, string validationMessage)>>
                {
                    CommonContentValidation.ValidateTitle
                }
            };

            SlugEntry = new StringDataEntryContext
            {
                Title = "Slug",
                HelpText = "This will be the Folder and File Name used in URLs - limited to a-z 0-9 _ -",
                ReferenceValue = DbEntry?.Slug ?? string.Empty,
                UserValue = StringHelpers.NullToEmptyTrim(DbEntry?.Slug),
                ValidationFunctions = new List<Func<string, (bool passed, string validationMessage)>>
                {
                    CommonContentValidation.ValidateSlugLocal
                }
            };

            SummaryEntry = new StringDataEntryContext
            {
                Title = "Summary",
                HelpText = "A short text entry that will show in Search and short references to the content",
                ReferenceValue = DbEntry?.Summary ?? string.Empty,
                UserValue = StringHelpers.NullToEmptyTrim(DbEntry?.Summary),
                ValidationFunctions = new List<Func<string, (bool passed, string validationMessage)>>
                {
                    CommonContentValidation.ValidateSummary
                }
            };

            Folder = DbEntry?.Folder ?? string.Empty;

            await ThreadSwitcher.ResumeForegroundAsync();

            ExistingFolderChoices = new ObservableCollection<string>(await Db.FolderNamesFromContent(DbEntry));
            _dataNotificationType = DataNotifications.NotificationContentTypeFromContent(DbEntry);

            DataNotifications.DataNotificationChannel().MessageReceived += OnDataNotificationReceived;
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

        public ObservableCollection<string> ExistingFolderChoices
        {
            get => _existingFolderChoices;
            set
            {
                if (Equals(value, _existingFolderChoices)) return;
                _existingFolderChoices = value;
                OnPropertyChanged();
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges") && !propertyName.Contains("Validation"))
                CheckForChangesAndValidate();
        }

        public void TitleToSlug()
        {
            SlugEntry.UserValue = SlugUtility.Create(true, TitleEntry.UserValue);
        }

        public void ValidateFolder()
        {
            var validationResult = CommonContentValidation.ValidateFolder(Folder);

            FolderHasValidationIssues = !validationResult.isValid;
            FolderValidationMessage = validationResult.explanation;
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
    }
}