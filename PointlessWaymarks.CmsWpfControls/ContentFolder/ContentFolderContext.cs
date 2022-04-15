using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Toolkit.Mvvm.ComponentModel;
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

[ObservableObject]
public partial class ContentFolderContext : IHasChanges, IHasValidationIssues
{
    [ObservableProperty] private DataNotificationsWorkQueue _dataNotificationsProcessor;
    [ObservableProperty] private List<DataNotificationContentType> _dataNotificationType;
    [ObservableProperty] private ObservableCollection<string> _existingFolderChoices;
    [ObservableProperty] private Func<Task<List<string>>> _getCurrentFolderNames;
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private string _helpText;
    [ObservableProperty] private string _referenceValue;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private string _title;
    [ObservableProperty] private string _userValue;
    [ObservableProperty] private List<Func<string, IsValid>> _validationFunctions = new();
    [ObservableProperty] private string _validationMessage;

    private ContentFolderContext(StatusControlContext statusContext)
    {
        StatusContext = statusContext ?? new StatusControlContext();

        DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };

        PropertyChanged += OnPropertyChanged;
    }

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
            ValidationFunctions = new List<Func<string, IsValid>> { CommonContentValidation.ValidateFolder }
        };

        await newControl.LoadData(dbEntry);

        return newControl;
    }

    public static async Task<ContentFolderContext> CreateInstanceForAllGeoTypes(StatusControlContext statusContext)
    {
        var newControl = new ContentFolderContext(statusContext)
        {
            ValidationFunctions = new List<Func<string, IsValid>> { CommonContentValidation.ValidateFolder }
        };

        await newControl.LoadDataForAllGeoTypes();

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
            !DataNotificationType.Contains(translatedMessage.ContentType)) return;

        await ThreadSwitcher.ResumeBackgroundAsync();

        var currentDbFolders = await GetCurrentFolderNames();

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

        GetCurrentFolderNames = async () => await Db.FolderNamesFromContent(dbEntry);

        var folderChoices = await Db.FolderNamesFromContent(dbEntry);

        await ThreadSwitcher.ResumeForegroundAsync();

        ExistingFolderChoices = new ObservableCollection<string>(folderChoices);
        DataNotificationType = new List<DataNotificationContentType>
            { DataNotifications.NotificationContentTypeFromContent(dbEntry) };

        ReferenceValue = dbEntry?.Folder ?? string.Empty;
        UserValue = dbEntry?.Folder ?? string.Empty;

        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }

    public async Task LoadDataForAllGeoTypes()
    {
        DataNotifications.NewDataNotificationChannel().MessageReceived -= OnDataNotificationReceived;

        Title = "Folder";
        HelpText =
            "The Parent Folder for the Content - this will appear in the URL and allows grouping similar content together.";

        GetCurrentFolderNames = null;

        var folderChoices = await Db.FolderNamesFromGeoContent();

        await ThreadSwitcher.ResumeForegroundAsync();

        ExistingFolderChoices = new ObservableCollection<string>(folderChoices);
        DataNotificationType = new List<DataNotificationContentType>
        {
            DataNotificationContentType.GeoJson, DataNotificationContentType.Line, DataNotificationContentType.Point
        };

        ReferenceValue = string.Empty;
        UserValue = string.Empty;

        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }

    private void OnDataNotificationReceived(object sender, TinyMessageReceivedEventArgs e)
    {
        DataNotificationsProcessor.Enqueue(e);
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e == null) return;
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidate();
    }
}