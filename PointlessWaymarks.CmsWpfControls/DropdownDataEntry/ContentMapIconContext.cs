using System.Collections.ObjectModel;
using System.ComponentModel;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsWpfControls.DropdownDataEntry;

[NotifyPropertyChanged]
public partial class ContentMapIconContext : IDropdownDataEntryContext
{
    private ContentMapIconContext(StatusControlContext statusContext, Func<Task<List<DropDownDataChoice>>> loader,
        PointContent dbEntry, List<DropDownDataChoice> initialIconNameList)
    {
        StatusContext = statusContext;
        
        DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };
        
        Title = "Map Icon";
        HelpText =
            "A small Map Icon that will appear inside the map marker - no value is required. Map Icons can be added in the Map Icon Editor under the View menu.";
        
        GetCurrentIconNames = loader;
        
        ExistingChoices = new ObservableCollection<DropDownDataChoice>(initialIconNameList);
        ReferenceValue = dbEntry.MapIconName ?? string.Empty;
        UserValue = dbEntry.MapIconName ?? string.Empty;
        
        ValidationFunctions = [];
        
        PropertyChanged += OnPropertyChanged;
        
        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }
    
    public DataNotificationsWorkQueue DataNotificationsProcessor { get; set; }
    public Func<Task<List<DropDownDataChoice>>> GetCurrentIconNames { get; set; }
    public List<Func<string?, IsValid>> ValidationFunctions { get; set; }
    public ObservableCollection<DropDownDataChoice> ExistingChoices { get; set; }
    public string HelpText { get; set; }
    public string? ReferenceValue { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string Title { get; set; }
    public string? UserValue { get; set; }
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
    
    public static async Task<ContentMapIconContext> CreateInstance(StatusControlContext? statusContext,
        PointContent dbEntry)
    {
        var factoryContext = await StatusControlContext.ResumeForegroundAsyncAndCreateInstance(statusContext);

        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var loader = DbIconChoices;
        var initialMapIconList = await DbIconChoices();
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        var newControl = new ContentMapIconContext(factoryContext, loader, dbEntry, initialMapIconList);
        
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
        
        if (translatedMessage.ContentType != DataNotificationContentType.MapIcon) return;
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        var currentDbIcons = await GetCurrentIconNames();
        
        var tempUserValue = currentDbIcons.Any(x => x.DisplayString.Equals(UserValue)) ? UserValue : string.Empty;
        
        var toAdd = currentDbIcons.Where(x => !ExistingChoices.Contains(x)).ToList();
        var toRemove = new List<DropDownDataChoice>();
        
        foreach (var loopExisting in ExistingChoices)
            if (!currentDbIcons.Any(x => x.Equals(loopExisting)))
                toRemove.Add(loopExisting);
        
        toRemove.ForEach(x => ExistingChoices.Remove(x));
        toAdd.ForEach(x => ExistingChoices.Add(x));
        ExistingChoices.SortBy(x => x);
        
        UserValue = tempUserValue;
        
        CheckForChangesAndValidate();
    }
    
    private static async Task<List<DropDownDataChoice>> DbIconChoices()
    {
        var db = await Db.Context();
        
        var dbIcons = (await db.MapIcons.OrderBy(x => x).ToListAsync()).Select(x => new DropDownDataChoice
            { DataString = x.IconSvg ?? string.Empty, DisplayString = x.IconName ?? string.Empty });
        
        return new List<DropDownDataChoice>
            { new() { DataString = string.Empty, DisplayString = "" } }.Concat(dbIcons).ToList();
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