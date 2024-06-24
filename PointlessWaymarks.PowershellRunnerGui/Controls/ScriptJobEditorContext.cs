using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.PowerShellRunnerData.Models;
using PointlessWaymarks.WpfCommon.BoolDataEntry;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
[StaThreadConstructorGuard]
[GenerateStatusCommands]
public partial class ScriptJobEditorContext : IHasChanges, IHasValidationIssues,
    ICheckForChangesAndValidation
{
    public ScriptJobEditorContext()
    {
    }

    public required string DatabaseFile { get; set; }
    public required ScriptJob DbEntry { get; set; }
    public required StringDataEntryContext DescriptionEntry { get; set; }
    public required BoolDataEntryContext EnabledEntry { get; set; }
    public required StringDataEntryContext NameEntry { get; set; }
    public EventHandler? RequestContentEditorWindowClose { get; set; }
    public required StringDataEntryContext ScheduleEntry { get; set; }
    public required StringDataEntryContext ScriptEntry { get; set; }
    public required StatusControlContext StatusContext { get; set; }

    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
        HasValidationIssues =
            PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }

    public bool HasChanges { get; set; }

    public bool HasValidationIssues { get; set; }

    public static async Task<ScriptJobEditorContext> CreateInstance(StatusControlContext? statusContext,
        ScriptJob initialScriptJob, string databaseFile)
    {
        var nameEntry = StringDataEntryContext.CreateInstance();
        nameEntry.Title = "Name";
        nameEntry.HelpText =
            "A name for this Scheduled Job.";
        nameEntry.ValidationFunctions =
        [
            x =>
            {
                if (string.IsNullOrWhiteSpace(x))
                    return Task.FromResult(new IsValid(false, "A name is required for the Job"));
                return Task.FromResult(new IsValid(true, string.Empty));
            }
        ];

        var descriptionEntry = StringDataEntryContext.CreateInstance();
        descriptionEntry.Title = "Description";
        descriptionEntry.HelpText =
            "Description, notes or other information about the  Job.";

        var cronEntry = StringDataEntryContext.CreateInstance();
        cronEntry.Title = "Schedule (Cron Expression)";
        cronEntry.HelpText =
            "A Cron Expression or a blank if the job is only run on demand.";

        var scriptEntry = StringDataEntryContext.CreateInstance();
        scriptEntry.Title = "Schedule (Cron Expression)";
        scriptEntry.HelpText =
            "A Cron Expression or a blank if the job is only run on demand.";

        var enabledEntry = await BoolDataEntryContext.CreateInstance();
        enabledEntry.Title = "Enabled";
        enabledEntry.HelpText =
            "If checked the job will run on schedule, if not it will only run on demand.";

        var newContext = new ScriptJobEditorContext
        {
            DatabaseFile = databaseFile,
            DbEntry = initialScriptJob,
            StatusContext = statusContext ?? new StatusControlContext(),
            NameEntry = nameEntry,
            DescriptionEntry = descriptionEntry,
            ScheduleEntry = cronEntry,
            ScriptEntry = scriptEntry,
            EnabledEntry = enabledEntry
        };

        await newContext.Setup(initialScriptJob);

        return newContext;
    }

    public async Task LoadData(ScriptJob toLoad)
    {
        var obfuscationKey = await ObfuscationKeyHelpers.GetObfuscationKey(DatabaseFile);

        DbEntry = toLoad;

        NameEntry.ReferenceValue = toLoad.Name;
        NameEntry.UserValue = toLoad.Name;

        DescriptionEntry.ReferenceValue = toLoad.Description;
        DescriptionEntry.UserValue = toLoad.Description;

        ScheduleEntry.ReferenceValue = toLoad.CronExpression;
        ScheduleEntry.UserValue = toLoad.CronExpression;

        ScriptEntry.ReferenceValue = toLoad.Script.Decrypt(obfuscationKey);
        ScriptEntry.UserValue = toLoad.Script.Decrypt(obfuscationKey);

        EnabledEntry.ReferenceValue = toLoad.ScheduleEnabled;
        EnabledEntry.UserValue = toLoad.ScheduleEnabled;
    }

    [BlockingCommand]
    public async Task Save()
    {
        if (!HasChanges)
        {
            StatusContext.ToastError("No Changes to Save?");
            return;
        }

        if (HasValidationIssues)
        {
            StatusContext.ToastError("Can't Save - Validation Issues Exist...");
            return;
        }

        var obfuscationKey = await ObfuscationKeyHelpers.GetObfuscationKey(DatabaseFile);

        var db = await PowerShellRunnerContext.CreateInstance(DatabaseFile, false);

        var toSave = db.Jobs.SingleOrDefault(x => x.Id == DbEntry.Id);
        if (toSave == null)
        {
            toSave = new ScriptJob();
            db.Jobs.Add(toSave);
        }

        toSave.Name = NameEntry.UserValue;
        toSave.Description = DescriptionEntry.UserValue;
        toSave.CronExpression = ScheduleEntry.UserValue;
        toSave.Script = ScriptEntry.UserValue.Encrypt(obfuscationKey);
        toSave.ScheduleEnabled = EnabledEntry.UserValue;
        toSave.LastEditOn = DateTime.Now;

        await db.SaveChangesAsync();

        await LoadData(toSave);
    }

    public async Task Setup(ScriptJob initialEntry)
    {
        BuildCommands();

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this,
            CheckForChangesAndValidationIssues);

        await LoadData(initialEntry);
    }
}