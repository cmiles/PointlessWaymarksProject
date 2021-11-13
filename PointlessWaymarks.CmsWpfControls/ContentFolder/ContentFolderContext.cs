using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsWpfControls.ContentFolder;

public class ContentFolderContext : INotifyPropertyChanged, IHasChanges, IHasValidationIssues
{
    private DataNotificationsWorkQueue _dataNotificationsProcessor;
    private DataNotificationContentType _dataNotificationType;
    private ITitleSummarySlugFolder _dbEntry;
    private ObservableCollection<string> _existingFolderChoices;
    private bool _hasChanges;
    private bool _hasValidationIssues;
    private string _helpText;
    private string _referenceValue;
    private StatusControlContext _statusContext;
    private string _title;
    private string _userValue;

    private List<Func<string, IsValid>> _validationFunctions = new();

    private string _validationMessage;

    private ContentFolderContext(StatusControlContext statusContext)
    {
        StatusContext = statusContext ?? new StatusControlContext();

        DataNotificationsProcessor = new DataNotificationsWorkQueue {Processor = DataNotificationReceived};
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

    public string HelpText
    {
        get => _helpText;
        set
        {
            if (value == _helpText) return;
            _helpText = value;
            OnPropertyChanged();
        }
    }

    public string ReferenceValue
    {
        get => _referenceValue;
        set
        {
            if (value == _referenceValue) return;
            _referenceValue = value;
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

    public string Title
    {
        get => _title;
        set
        {
            if (value == _title) return;
            _title = value;
            OnPropertyChanged();
        }
    }

    public string UserValue
    {
        get => _userValue;
        set
        {
            if (value == _userValue) return;
            _userValue = value;
            OnPropertyChanged();
        }
    }

    public List<Func<string, IsValid>> ValidationFunctions
    {
        get => _validationFunctions;
        set
        {
            if (Equals(value, _validationFunctions)) return;
            _validationFunctions = value;
            OnPropertyChanged();
        }
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        set
        {
            if (value == _validationMessage) return;
            _validationMessage = value;
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

    private void CheckForChangesAndValidate()
    {
        HasChanges = UserValue.TrimNullToEmpty() != ReferenceValue.TrimNullToEmpty();

        if (ValidationFunctions != null && ValidationFunctions.Any())
            foreach (var loopValidations in ValidationFunctions)
            {
                var (passed, validationMessage) = loopValidations(UserValue);
                if (!passed)
                {
                    HasValidationIssues = true;
                    ValidationMessage = validationMessage;
                    return;
                }
            }

        HasValidationIssues = false;
        ValidationMessage = string.Empty;
    }

    public static async Task<ContentFolderContext> CreateInstance(StatusControlContext statusContext,
        ITitleSummarySlugFolder dbEntry)
    {
        var newControl = new ContentFolderContext(statusContext)
        {
            ValidationFunctions = new List<Func<string, IsValid>> {CommonContentValidation.ValidateFolder}
        };

        await newControl.LoadData(dbEntry);

        return newControl;
    }

    private async Task DataNotificationReceived(TinyMessageReceivedEventArgs e)
    {
        var translatedMessage = DataNotifications.TranslateDataNotification(e.Message);

        if (translatedMessage.HasError)
        {
            Log.Error("Data Notification Failure. Error Note {0}. Status Control Context Id {1}",
                translatedMessage.ErrorNote, StatusContext.StatusControlContextId);

            return;
        }

        if (translatedMessage.UpdateType == DataNotificationUpdateType.LocalContent ||
            translatedMessage.ContentType != _dataNotificationType) return;

        await ThreadSwitcher.ResumeBackgroundAsync();

        var currentDbFolders = await Db.FolderNamesFromContent(DbEntry);

        var newFolderNames = currentDbFolders.Except(ExistingFolderChoices).ToList();

        if (newFolderNames.Any())
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            ExistingFolderChoices.Clear();
            currentDbFolders.ForEach(x => ExistingFolderChoices.Add(x));
            ExistingFolderChoices.SortBy(x => x);
        }
    }

    public async Task LoadData(ITitleSummarySlugFolder dbEntry)
    {
        DataNotifications.NewDataNotificationChannel().MessageReceived -= OnDataNotificationReceived;

        Title = "Folder";
        HelpText =
            "The Parent Folder for the Content - this will appear in the URL and allows grouping similar content together.";

        DbEntry = dbEntry;

        var folderChoices = await Db.FolderNamesFromContent(DbEntry);

        await ThreadSwitcher.ResumeForegroundAsync();

        ExistingFolderChoices = new ObservableCollection<string>(folderChoices);
        _dataNotificationType = DataNotifications.NotificationContentTypeFromContent(DbEntry);

        ReferenceValue = dbEntry?.Folder ?? string.Empty;
        UserValue = dbEntry?.Folder ?? string.Empty;

        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }

    private void OnDataNotificationReceived(object sender, TinyMessageReceivedEventArgs e)
    {
        DataNotificationsProcessor.Enqueue(e);
    }


    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        if (string.IsNullOrWhiteSpace(propertyName)) return;

        if (!propertyName.Contains("HasChanges") && !propertyName.Contains("Validation"))
            CheckForChangesAndValidate();
    }
}