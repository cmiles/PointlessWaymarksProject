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
public partial class ActivityTypeContext : IStringWithDropdownDataEntryContext
{
    private ActivityTypeContext(StatusControlContext statusContext, LineContent? dbEntry,
        Func<Task<List<string>>> loader, List<string> initialFolderList)
    {
        StatusContext = statusContext;

        DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };

        Title = "Activity Type";
        HelpText =
            "The type of Activity - used for filtering the Activity Log.";

        GetCurrentActivityTypes = loader;

        ExistingChoices = new ObservableCollection<string>(initialFolderList);
        OnlyIncludeDataNotificationsForTypes = new List<DataNotificationContentType>
        {
            DataNotificationContentType.Line
        };

        ReferenceValue = dbEntry?.ActivityType ?? string.Empty;
        UserValue = dbEntry?.ActivityType ?? string.Empty;

        ValidationFunctions = new List<Func<string?, IsValid>>();

        PropertyChanged += OnPropertyChanged;

        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }

    public DataNotificationsWorkQueue DataNotificationsProcessor { get; set; }
    public ObservableCollection<string> ExistingChoices { get; set; }
    public Func<Task<List<string>>> GetCurrentActivityTypes { get; set; }
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

    public static async Task<ActivityTypeContext> CreateInstance(StatusControlContext? statusContext,
        LineContent dbEntry)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryContext = statusContext ?? new StatusControlContext();
        var initialFolderList = await Db.ActivityTypesFromLines();

        await ThreadSwitcher.ResumeForegroundAsync();

        var newControl = new ActivityTypeContext(factoryContext, dbEntry,
            async () => await Db.ActivityTypesFromLines(), initialFolderList);

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

        var currentDbActivityTypes = await GetCurrentActivityTypes();

        var newFolderNames = currentDbActivityTypes.Except(ExistingChoices).ToList();

        if (newFolderNames.Any())
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            ExistingChoices.Clear();
            currentDbActivityTypes.ForEach(x => ExistingChoices.Add(x));
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

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidate();
    }
}