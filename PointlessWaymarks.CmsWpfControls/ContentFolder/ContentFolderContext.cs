using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsWpfControls.ContentFolder;

public partial class ContentFolderContext : ObservableObject, IHasChanges, IHasValidationIssues
{
    [ObservableProperty] private DataNotificationsWorkQueue _dataNotificationsProcessor;
    [ObservableProperty] private List<DataNotificationContentType> _dataNotificationType;
    [ObservableProperty] private ObservableCollection<string> _existingFolderChoices;
    [ObservableProperty] private Func<Task<List<string>>> _getCurrentFolderNames;
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private string _helpText;
    [ObservableProperty] private string? _referenceValue;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private string _title;
    [ObservableProperty] private string?_userValue;
    [ObservableProperty] private List<Func<string?, IsValid>> _validationFunctions;
    [ObservableProperty] private string _validationMessage = string.Empty;

    private ContentFolderContext(StatusControlContext statusContext, ITitleSummarySlugFolder? dbEntry, Func<Task<List<string>>> loader, List<string> initialFolderList)
    {
        _statusContext = statusContext;

        _dataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };

        PropertyChanged += OnPropertyChanged;

        _title = "Folder";
        _helpText =
            "The Parent Folder for the Content - this will appear in the URL and allows grouping similar content together.";

        _getCurrentFolderNames = loader;

        _existingFolderChoices = new ObservableCollection<string>(initialFolderList);
        _dataNotificationType = new List<DataNotificationContentType>
        {
            DataNotificationContentType.GeoJson, DataNotificationContentType.Line, DataNotificationContentType.Point
        };

        _referenceValue = dbEntry?.Folder ?? string.Empty;
        _userValue = dbEntry?.Folder ?? string.Empty;

        _validationFunctions = new List<Func<string?, IsValid>> { CommonContentValidation.ValidateFolder };

        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }

    private void CheckForChangesAndValidate()
    {
        HasChanges = UserValue.TrimNullToEmpty() != ReferenceValue.TrimNullToEmpty();

        if (ValidationFunctions.Any())
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

    public static async Task<ContentFolderContext> CreateInstance(StatusControlContext? statusContext,
        ITitleSummarySlugFolder dbEntry)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryContext = statusContext ?? new StatusControlContext();
        var initialFolderList = await Db.FolderNamesFromContent(dbEntry);

        await ThreadSwitcher.ResumeForegroundAsync();

        var newControl = new ContentFolderContext(factoryContext, dbEntry, async () => await Db.FolderNamesFromContent(dbEntry), initialFolderList);

        return newControl;
    }

    public static async Task<ContentFolderContext> CreateInstanceForAllGeoTypes(StatusControlContext? statusContext)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryContext = statusContext ?? new StatusControlContext();
        var loader = Db.FolderNamesFromGeoContent;
        var initialFolderList = await loader();

        await ThreadSwitcher.ResumeForegroundAsync();

        var newControl = new ContentFolderContext(factoryContext, null, loader, initialFolderList);

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

    private void OnDataNotificationReceived(object? sender, TinyMessageReceivedEventArgs e)
    {
        DataNotificationsProcessor.Enqueue(e);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidate();
    }
}