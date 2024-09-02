using System.Collections.ObjectModel;
using System.ComponentModel;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsWpfControls.StringWithDropdownDataEntry;

[NotifyPropertyChanged]
public partial class ContentFolderContext : IStringWithDropdownDataEntryContext
{
    private ContentFolderContext(StatusControlContext statusContext, ITitleSummarySlugFolder? dbEntry,
        Func<Task<List<string>>> loader, List<string> initialFolderList)
    {
        StatusContext = statusContext;

        DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };

        Title = "Folder";
        HelpText =
            "The Parent Folder for the Content - this will appear in the URL and allows grouping similar content together.";

        GetCurrentFolderNames = loader;

        ExistingChoices = new ObservableCollection<string>(initialFolderList);
        OnlyIncludeDataNotificationsForTypes = [];
        ReferenceValue = dbEntry?.Folder ?? string.Empty;
        UserValue = dbEntry?.Folder ?? string.Empty;

        ValidationFunctions = [CommonContentValidation.ValidateFolder];

        PropertyChanged += OnPropertyChanged;

        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }

    public DataNotificationsWorkQueue DataNotificationsProcessor { get; set; }
    public ObservableCollection<string> ExistingChoices { get; set; }
    public Func<Task<List<string>>> GetCurrentFolderNames { get; set; }
    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }
    public string HelpText { get; set; }
    public List<DataNotificationContentType> OnlyIncludeDataNotificationsForTypes { get; set; }
    public string? ReferenceValue { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string Title { get; set; }
    public string? UserValue { get; set; }
    public List<Func<string?, IsValid>> ValidationFunctions { get; set; }
    public string ValidationMessage { get; set; } = string.Empty;

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
        var factoryContext = await StatusControlContext.ResumeForegroundAsyncAndCreateInstance(statusContext);

        await ThreadSwitcher.ResumeBackgroundAsync();

        var initialFolderList = await Db.FolderNamesFromContent(dbEntry);

        await ThreadSwitcher.ResumeForegroundAsync();

        var newControl = new ContentFolderContext(factoryContext, dbEntry,
            async () => await Db.FolderNamesFromContent(dbEntry), initialFolderList);

        newControl.CheckForChangesAndValidate();

        return newControl;
    }

    public static async Task<ContentFolderContext> CreateInstanceForAllGeoTypes(StatusControlContext? statusContext)
    {
        var factoryContext = await StatusControlContext.ResumeForegroundAsyncAndCreateInstance(statusContext);

        await ThreadSwitcher.ResumeBackgroundAsync();

        var loader = Db.FolderNamesFromGeoContent;
        var initialFolderList = await loader();

        await ThreadSwitcher.ResumeForegroundAsync();

        var newControl = new ContentFolderContext(factoryContext, null, loader, initialFolderList);

        newControl.OnlyIncludeDataNotificationsForTypes =
            [DataNotificationContentType.GeoJson, DataNotificationContentType.Line, DataNotificationContentType.Point];

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
            (OnlyIncludeDataNotificationsForTypes.Any() &&
             !OnlyIncludeDataNotificationsForTypes.Contains(translatedMessage.ContentType))) return;

        await ThreadSwitcher.ResumeBackgroundAsync();

        var currentDbFolders = await GetCurrentFolderNames();

        var newFolderNames = currentDbFolders.Except(ExistingChoices).ToList();

        if (newFolderNames.Any())
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            ExistingChoices.Clear();
            currentDbFolders.ForEach(x => ExistingChoices.Add(x));
            ExistingChoices.SortBy(x => x);
        }
    }

    private void OnDataNotificationReceived(object? sender, TinyMessageReceivedEventArgs e)
    {
        DataNotificationsProcessor.Enqueue(e);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;
        
        if (e.PropertyName.Equals(nameof(HasChanges)) || e.PropertyName.Equals(nameof(HasValidationIssues)) ||
            e.PropertyName.Equals(nameof(ValidationMessage))) return;
        
        CheckForChangesAndValidate();
    }
}