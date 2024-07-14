using System.ComponentModel;
using System.Windows;
using CronExpressionDescriptor;
using Cronos;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.PowerShellRunnerData.Models;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.BoolDataEntry;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.ConversionDataEntry;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;
using Console = System.Console;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
[StaThreadConstructorGuard]
[GenerateStatusCommands]
public partial class ScriptJobEditorContext : IHasChanges, IHasValidationIssues,
    ICheckForChangesAndValidation
{
    private readonly PeriodicTimer _cronNextTimer = new(TimeSpan.FromSeconds(30));
    private string _databaseFile = string.Empty;
    private Guid _dbId = Guid.Empty;

    public ScriptJobEditorContext()
    {
    }

    public BoolDataEntryContext AllowSimultaneousRunsEntry { get; set; }
    public string? CronDescription { get; set; }
    public DateTime? CronNextRun { get; set; }
    public required string DatabaseFile { get; set; }
    public required ScriptJob DbEntry { get; set; }
    public required ConversionDataEntryContext<int> DeleteRunsAfterMonthsEntry { get; set; }
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
        await ThreadSwitcher.ResumeBackgroundAsync();

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

        var deleteAfterEntry =
            await ConversionDataEntryContext<int>.CreateInstance(
                ConversionDataEntryHelpers.IntGreaterThanZeroConversion);
        deleteAfterEntry.Title = "Delete Script Job Run Information After ______ Months";
        deleteAfterEntry.HelpText =
            "A name for this Scheduled Job.";

        var descriptionEntry = StringDataEntryContext.CreateInstance();
        descriptionEntry.Title = "Description";
        descriptionEntry.HelpText =
            "Description, notes or other information about the Job.";

        var allowSimultaneousRunsEntry = await BoolDataEntryContext.CreateInstance();
        allowSimultaneousRunsEntry.Title = "Allow Simultaneous Run Entry";
        descriptionEntry.HelpText =
            "If set multiple instances of this job may run at the same time.";

        var cronEntry = StringDataEntryContext.CreateInstance();
        cronEntry.Title = "Schedule (Cron Expression)";
        cronEntry.HelpText =
            "A Cron Expression or a blank if the job is only run on demand.";
        cronEntry.ValidationFunctions =
        [
            x =>
            {
                if (string.IsNullOrWhiteSpace(x))
                    return Task.FromResult(new IsValid(true, string.Empty));

                try
                {
                    CronExpression.Parse(x);
                    return Task.FromResult(new IsValid(true, string.Empty));
                }
                catch (Exception)
                {
                    return Task.FromResult(new IsValid(false, "Invalid Cron Expression"));
                }
            }
        ];

        var scriptEntry = StringDataEntryContext.CreateInstance();
        scriptEntry.Title = "Script";
        scriptEntry.HelpText =
            "A PowerShell script to run.";
        scriptEntry.ValidationFunctions =
        [
            x =>
            {
                if (string.IsNullOrWhiteSpace(x))
                    return Task.FromResult(new IsValid(false, "A Script is required for the Job"));
                return Task.FromResult(new IsValid(true, string.Empty));
            }
        ];

        var enabledEntry = await BoolDataEntryContext.CreateInstance();
        enabledEntry.Title = "Enabled";
        enabledEntry.HelpText =
            "If checked the job will run on schedule, if not it will only run on demand.";

        var dbId = await PowerShellRunnerDbQuery.DbId(databaseFile);

        await ThreadSwitcher.ResumeForegroundAsync();

        var newContext = new ScriptJobEditorContext
        {
            DatabaseFile = databaseFile,
            DbEntry = initialScriptJob,
            DeleteRunsAfterMonthsEntry = deleteAfterEntry,
            StatusContext = statusContext ?? new StatusControlContext(),
            NameEntry = nameEntry,
            DescriptionEntry = descriptionEntry,
            AllowSimultaneousRunsEntry = allowSimultaneousRunsEntry,
            ScheduleEntry = cronEntry,
            ScriptEntry = scriptEntry,
            EnabledEntry = enabledEntry,
            _databaseFile = databaseFile,
            _dbId = dbId
        };

        cronEntry.PropertyChanged += newContext.CronExpressionChanged;

        await ThreadSwitcher.ResumeBackgroundAsync();

        await newContext.Setup(initialScriptJob);

        newContext.UpdateCronNextRun();

        return newContext;
    }

    private void CronExpressionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScheduleEntry.UserValue)) UpdateCronExpressionInformation();
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

        DeleteRunsAfterMonthsEntry.ReferenceValue = toLoad.DeleteScriptJobRunsAfterMonths;
        DeleteRunsAfterMonthsEntry.UserText = toLoad.DeleteScriptJobRunsAfterMonths.ToString();

        AllowSimultaneousRunsEntry.ReferenceValue = toLoad.AllowSimultaneousRuns;
        AllowSimultaneousRunsEntry.UserValue = toLoad.AllowSimultaneousRuns;

        ScriptEntry.ReferenceValue = toLoad.Script.Decrypt(obfuscationKey);
        ScriptEntry.UserValue = toLoad.Script.Decrypt(obfuscationKey);

        EnabledEntry.ReferenceValue = toLoad.ScheduleEnabled;
        EnabledEntry.UserValue = toLoad.ScheduleEnabled;
    }

    [BlockingCommand]
    public async Task Save()
    {
        await SaveChanges(false);
    }

    [BlockingCommand]
    public async Task SaveAndClose()
    {
        await SaveChanges(true);
    }

    public async Task SaveChanges(bool closeAfterSave)
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

        var db = await PowerShellRunnerDbContext.CreateInstance(DatabaseFile);

        var newEntry = false;

        var toSave = db.ScriptJobs.SingleOrDefault(x => x.PersistentId == DbEntry.PersistentId);
        if (toSave == null)
        {
            newEntry = true;
            toSave = new ScriptJob
            {
                PersistentId = Guid.NewGuid()
            };
            db.ScriptJobs.Add(toSave);
        }

        toSave.Name = NameEntry.UserValue;
        toSave.Description = DescriptionEntry.UserValue;
        toSave.CronExpression = ScheduleEntry.UserValue;
        toSave.DeleteScriptJobRunsAfterMonths = DeleteRunsAfterMonthsEntry.UserValue;
        toSave.AllowSimultaneousRuns = AllowSimultaneousRunsEntry.UserValue;
        toSave.Script = ScriptEntry.UserValue.Encrypt(obfuscationKey);
        toSave.ScheduleEnabled = EnabledEntry.UserValue;
        toSave.LastEditOn = DateTime.Now;

        await db.SaveChangesAsync();

        DataNotifications.PublishJobDataNotification("Script Job Editor",
            newEntry
                ? DataNotifications.DataNotificationUpdateType.New
                : DataNotifications.DataNotificationUpdateType.Update, _dbId, toSave.PersistentId);

        await LoadData(toSave);

        if (closeAfterSave) RequestContentEditorWindowClose?.Invoke(this, EventArgs.Empty);
    }

    [NonBlockingCommand]
    public async Task ScriptToClipboard()
    {
        if (string.IsNullOrWhiteSpace(ScriptEntry.UserValue))
        {
            StatusContext.ToastError("No Script to Copy?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();
        Clipboard.SetText(ScriptEntry.UserValue);

        StatusContext.ToastSuccess("Script Copied to Clipboard");
    }

    public async Task Setup(ScriptJob initialEntry)
    {
        BuildCommands();

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this,
            CheckForChangesAndValidationIssues);

        await LoadData(initialEntry);
    }

    private void UpdateCronExpressionInformation()
    {
        try
        {
            var expression = CronExpression.Parse(ScheduleEntry.UserValue);
            var nextRun = expression.GetNextOccurrence(DateTimeOffset.Now, TimeZoneInfo.Local);
            if (nextRun != null) CronNextRun = nextRun.Value.LocalDateTime;
            CronDescription = ExpressionDescriptor.GetDescription(ScheduleEntry.UserValue);
        }
        catch (Exception)
        {
            CronNextRun = null;
            CronDescription = string.Empty;
        }
    }

    private async Task UpdateCronNextRun()
    {
        try
        {
            while (await _cronNextTimer.WaitForNextTickAsync())
                if (!StatusContext.BlockUi)
                    UpdateCronExpressionInformation();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}