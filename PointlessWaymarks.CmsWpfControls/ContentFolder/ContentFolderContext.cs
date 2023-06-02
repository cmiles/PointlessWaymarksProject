using System.Collections.ObjectModel;
using System.ComponentModel;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsWpfControls.ContentFolder;

[NotifyPropertyChanged]
public partial class ContentFolderContext : IHasChanges, IHasValidationIssues
{
    private ContentFolderContext(StatusControlContext statusContext, ITitleSummarySlugFolder? dbEntry,
        Func<Task<List<string>>> loader, List<string> initialFolderList)
    {
        StatusContext = statusContext;

        DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };

        PropertyChanged += OnPropertyChanged;

        Title = "Folder";
        HelpText =
            "The Parent Folder for the Content - this will appear in the URL and allows grouping similar content together.";

        GetCurrentFolderNames = loader;

        ExistingFolderChoices = new ObservableCollection<string>(initialFolderList);
        DataNotificationType = new List<DataNotificationContentType>
        {
            DataNotificationContentType.GeoJson, DataNotificationContentType.Line, DataNotificationContentType.Point
        };

        ReferenceValue = dbEntry?.Folder ?? string.Empty;
        UserValue = dbEntry?.Folder ?? string.Empty;

        ValidationFunctions = new List<Func<string?, IsValid>> { CommonContentValidation.ValidateFolder };

        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }

    public DataNotificationsWorkQueue DataNotificationsProcessor { get; set; }
    public List<DataNotificationContentType> DataNotificationType { get; set; }
    public ObservableCollection<string> ExistingFolderChoices { get; set; }
    public Func<Task<List<string>>> GetCurrentFolderNames { get; set; }
    public string HelpText { get; set; }
    public string? ReferenceValue { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string Title { get; set; }
    public string? UserValue { get; set; }
    public List<Func<string?, IsValid>> ValidationFunctions { get; set; }
    public string ValidationMessage { get; set; } = string.Empty;
    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }

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

        var newControl = new ContentFolderContext(factoryContext, dbEntry,
            async () => await Db.FolderNamesFromContent(dbEntry), initialFolderList);

        newControl.CheckForChangesAndValidate();

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

        newControl.CheckForChangesAndValidate();

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