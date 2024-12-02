using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using GongSolutions.Wpf.DragDrop;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsWpfControls.ContentDropdownDataEntry;

[NotifyPropertyChanged]
public partial class ContentDropdownDataEntryContext : IDropTarget
{
    private ContentDropdownDataEntryContext(StatusControlContext statusContext, Guid? initialChoice,
        Func<Task<List<ContentDropdownDataChoice>>> loader, List<ContentDropdownDataChoice> initialContentList,
        List<DataNotificationContentType> contentTypes)
    {
        StatusContext = statusContext;

        DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };

        Title = "Content";
        HelpText = "Choose Content.";

        GetCurrentContentChoices = loader;

        ExistingChoices = new ObservableCollection<ContentDropdownDataChoice>(initialContentList);
        OnlyIncludeDataNotificationsForTypes = contentTypes;

        if (initialContentList.Any(x => x.ContentId == initialChoice))
        {
            ReferenceValue = initialChoice;
            UserValue = initialChoice;
        }

        ValidationFunctions = [];

        PropertyChanged += OnPropertyChanged;

        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }

    public DataNotificationsWorkQueue DataNotificationsProcessor { get; set; }
    public ObservableCollection<ContentDropdownDataChoice> ExistingChoices { get; set; }
    public Func<Task<List<ContentDropdownDataChoice>>> GetCurrentContentChoices { get; set; }
    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }
    public string HelpText { get; set; }
    public List<DataNotificationContentType> OnlyIncludeDataNotificationsForTypes { get; set; }
    public Guid? ReferenceValue { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string Title { get; set; }
    public Guid? UserValue { get; set; }
    public List<Func<Guid?, IsValid>> ValidationFunctions { get; set; }
    public string ValidationMessage { get; set; } = string.Empty;

    public void DragOver(IDropInfo dropInfo)
    {
        if (dropInfo.Data is string)
        {
            dropInfo.Effects = DragDropEffects.Copy;
            dropInfo.NotHandled = false;
        }
        else
        {
            dropInfo.Effects = DragDropEffects.None;
            dropInfo.NotHandled = true;
        }
    }

    public void Drop(IDropInfo dropInfo)
    {
        if (dropInfo.Data is string droppedString)
        {
            // Try to find the first Guid in the string
            var guidMatch = Regex.Match(droppedString,
                @"\b[A-Fa-f0-9]{8}\b-[A-Fa-f0-9]{4}\b-[A-Fa-f0-9]{4}\b-[A-Fa-f0-9]{4}\b-[A-Fa-f0-9]{12}\b");

            if (guidMatch.Success && Guid.TryParse(guidMatch.Value, out var foundGuid))
                StatusContext.RunNonBlockingTask(async () => await TrySelectItem(foundGuid));
        }
    }

    private void CheckForChangesAndValidate()
    {
        HasChanges = UserValue != ReferenceValue;

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

    public static async Task<ContentDropdownDataEntryContext> CreateInstance(StatusControlContext? statusContext,
        Func<Task<List<ContentDropdownDataChoice>>> loader, Guid? initialChoice,
        List<DataNotificationContentType> contentTypes)
    {
        var factoryStatusContext = await StatusControlContext.CreateInstance(statusContext);

        await ThreadSwitcher.ResumeBackgroundAsync();

        var initialList = await loader();

        await ThreadSwitcher.ResumeForegroundAsync();

        var newControl = new ContentDropdownDataEntryContext(factoryStatusContext, initialChoice,
            loader, initialList, contentTypes);

        newControl.CheckForChangesAndValidate();

        return newControl;
    }

    private async Task DataNotificationReceived(TinyMessageReceivedEventArgs e)
    {
        var translatedMessage = DataNotifications.TranslateDataNotification(e.Message.ToString());

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

        var currentContent = await GetCurrentContentChoices();

        foreach (var loopCurrentContent in currentContent)
        {
            var possibleExisting = ExistingChoices.FirstOrDefault(x => x.ContentId == loopCurrentContent.ContentId);
            if (possibleExisting is not null)
            {
                possibleExisting.DisplayString = loopCurrentContent.DisplayString;
                continue;
            }

            await ThreadSwitcher.ResumeForegroundAsync();
            ExistingChoices.Add(loopCurrentContent);
            await ThreadSwitcher.ResumeBackgroundAsync();
        }

        var currentContentIds = currentContent.Select(x => x.ContentId).ToList();
        var doesNotExist = ExistingChoices.Where(x => !currentContentIds.Contains(x.ContentId)).ToList();

        if (!doesNotExist.Any()) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        ExistingChoices.SortByDescending(x => x.DisplayString);

        foreach (var loopDoesNotExist in doesNotExist) ExistingChoices.Remove(loopDoesNotExist);
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

    public async Task TrySelectItem(Guid? toSelect)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (toSelect is not null)
            if (ExistingChoices.All(x => x.ContentId != toSelect))
            {
                await StatusContext.ToastError("The ContentId doesn't have a match - wrong type?");
                return;
            }

        UserValue = toSelect;
    }
}