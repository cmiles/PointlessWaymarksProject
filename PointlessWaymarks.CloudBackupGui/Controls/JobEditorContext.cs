using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CloudBackupData;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.ConversionDataEntry;
using PointlessWaymarks.WpfCommon.ExistingDirectoryDataEntry;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CloudBackupGui.Controls;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class JobEditorContext : IHasChanges, IHasValidationIssues,
    ICheckForChangesAndValidation
{
    public required string DatabaseFile { get; set; }
    public required ObservableCollection<DirectoryInfo> ExcludedDirectories { get; set; }

    public bool ExcludedDirectoriesHasChanges { get; set; }

    public string ExcludedDirectoriesHasChangesMessage { get; set; } = string.Empty;
    public required List<DirectoryInfo> ExcludedDirectoriesOriginal { get; set; } = new();
    public required ObservableCollection<string> ExcludedDirectoryPatterns { get; set; }

    public bool ExcludedDirectoryPatternsHasChanges { get; set; }

    public string ExcludedDirectoryPatternsHasChangesMessage { get; set; } = string.Empty;

    public required List<string> ExcludedDirectoryPatternsOriginal { get; set; } = new();
    public required ObservableCollection<string> ExcludedFilePatterns { get; set; }

    public bool ExcludedFilePatternsHasChanges { get; set; }

    public string ExcludedFilePatternsHasChangesMessage { get; set; } = string.Empty;

    public required List<string> ExcludedFilePatternsOriginal { get; set; } = new();
    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }
    public required BackupJob LoadedJob { get; set; }
    public EventHandler? RequestContentEditorWindowClose { get; set; }
    public DirectoryInfo? SelectedExcludedDirectory { get; set; }
    public string? SelectedExcludedDirectoryPattern { get; set; }
    public string? SelectedExcludedFilePattern { get; set; }
    public required StatusControlContext StatusContext { get; set; }
    public required StringDataEntryContext UserCloudDirectoryEntry { get; set; }
    public required StringDataEntryContext UserDirectoryPatternEntry { get; set; }
    public required StringDataEntryContext UserFilePatternEntry { get; set; }
    public required ExistingDirectoryDataEntryContext UserInitialDirectoryEntry { get; set; }
    public required ConversionDataEntryContext<int> UserMaximumRuntimeHoursEntry { get; set; }
    public required StringDataEntryContext UserNameEntry { get; set; }

    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this) || ExcludedDirectoriesHasChanges ||
                     ExcludedDirectoryPatternsHasChanges || ExcludedFilePatternsHasChanges;
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }

    [BlockingCommand]
    public async Task AddExcludedDirectory()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var selectedDirectory = await ChooseDirectory();

        if (selectedDirectory == null) return;

        if (ExcludedDirectories.Any(x =>
                x.FullName.Equals(selectedDirectory.FullName, StringComparison.InvariantCultureIgnoreCase)))
        {
            StatusContext.ToastError("Directory already exists in the list.");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        ExcludedDirectories.Add(selectedDirectory);
    }

    [BlockingCommand]
    public async Task AddExcludedDirectoryPattern()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(UserDirectoryPatternEntry.UserValue))
        {
            StatusContext.ToastError("A blank pattern, or a pattern with only white space is not valid");
            return;
        }

        if (ExcludedDirectoryPatterns.Contains(UserDirectoryPatternEntry.UserValue.Trim(),
                StringComparer.InvariantCultureIgnoreCase))
        {
            StatusContext.ToastError("Pattern already exists - patterns are case insensitive.");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        ExcludedDirectoryPatterns.Add(UserDirectoryPatternEntry.UserValue);
        UserDirectoryPatternEntry.UserValue = string.Empty;
    }

    [BlockingCommand]
    public async Task AddExcludedFilePattern()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(UserFilePatternEntry.UserValue))
        {
            StatusContext.ToastError("A blank pattern, or a pattern with only white space is not valid");
            return;
        }

        if (ExcludedFilePatterns.Contains(UserFilePatternEntry.UserValue.Trim(),
                StringComparer.InvariantCultureIgnoreCase))
        {
            StatusContext.ToastError("Pattern already exists - patterns are case insensitive.");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        ExcludedFilePatterns.Add(UserFilePatternEntry.UserValue);
        UserFilePatternEntry.UserValue = string.Empty;
    }

    [BlockingCommand]
    public async Task<DirectoryInfo?> ChooseDirectory()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var initialDirectoryString = CloudBackupGuiSettingTools.ReadSettings().LastDirectory;

        DirectoryInfo? initialDirectory = null;

        try
        {
            initialDirectory = new DirectoryInfo(initialDirectoryString);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var folderPicker = new VistaFolderBrowserDialog
            { Description = "Initial Directory", Multiselect = false };

        if (initialDirectory != null) folderPicker.SelectedPath = $"{initialDirectory.FullName}\\";

        if (folderPicker.ShowDialog() != true) return null;

        var selectedDirectory = new DirectoryInfo(folderPicker.SelectedPath);

        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!selectedDirectory.Exists) return null;

        var currentSettings = CloudBackupGuiSettingTools.ReadSettings();
        currentSettings.LastDirectory = selectedDirectory.Parent?.FullName ?? selectedDirectory.FullName;
        await CloudBackupGuiSettingTools.WriteSettings(currentSettings);

        return selectedDirectory;
    }

    public static async Task<JobEditorContext> CreateInstance(StatusControlContext? context, BackupJob initialJob,
        string databaseFile)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var statusContext = context ?? new StatusControlContext();

        var nameEntry = StringDataEntryContext.CreateInstance();
        nameEntry.Title = "Job Name";
        nameEntry.HelpText =
            "A name for the job - you must give the job a name, but there are no requirements - just use anything that will help you remember what you are backing up!";
        nameEntry.ReferenceValue = initialJob.Name;
        nameEntry.UserValue = initialJob.Name;
        nameEntry.ValidationFunctions = new List<Func<string?, Task<IsValid>>>
        {
            x =>
            {
                if (string.IsNullOrWhiteSpace(x))
                    return Task.FromResult(new IsValid(false, "A name is required for the job"));
                return Task.FromResult(new IsValid(true, string.Empty));
            }
        };

        var cloudDirectoryEntry = StringDataEntryContext.CreateInstance();
        cloudDirectoryEntry.Title = "Job Cloud Directory";
        cloudDirectoryEntry.HelpText =
            "A Cloud Directory for the job - for simplicity can be as simple as a single descriptive folder name";
        cloudDirectoryEntry.ReferenceValue = initialJob.CloudDirectory;
        cloudDirectoryEntry.UserValue = initialJob.CloudDirectory;
        cloudDirectoryEntry.ValidationFunctions = new List<Func<string?, Task<IsValid>>>
        {
            x =>
            {
                if (string.IsNullOrWhiteSpace(x))
                    return Task.FromResult(new IsValid(false, "A Cloud Directory is required for the job"));
                if (!Regex.IsMatch(x, @"^[a-zA-Z0-9-/]+$"))
                    return Task.FromResult(new IsValid(false,
                        "To keep easy compatibility with cloud storage only a-z, A-Z, 0-9, - and / are allowed in the Cloud Directory Name."));
                var pathSeparatorTestString = x.StartsWith("//") ? x.Substring(2) : x;
                if (pathSeparatorTestString.Contains("//"))
                    return Task.FromResult(new IsValid(false,
                        "// should only appear at the start of the Cloud Directory"));
                return Task.FromResult(new IsValid(true, string.Empty));
            }
        };

        var initialDirectoryEntry = ExistingDirectoryDataEntryContext.CreateInstance(statusContext);
        initialDirectoryEntry.Title = "Initial Local Directory";
        initialDirectoryEntry.HelpText = "Pick a single starting directory for the backup";
        initialDirectoryEntry.ReferenceValue = initialJob.LocalDirectory;
        initialDirectoryEntry.UserValue = initialJob.LocalDirectory;
        initialDirectoryEntry.ValidationFunctions = new List<Func<string?, Task<IsValid>>>
        {
            x =>
            {
                if (string.IsNullOrWhiteSpace(x))
                    return Task.FromResult(new IsValid(false, "A Local Directory is required for the job"));
                if (!Path.Exists(x))
                    return Task.FromResult(new IsValid(false, "Directory is not valid?"));
                if (!File.GetAttributes(x).HasFlag(FileAttributes.Directory))
                    return Task.FromResult(new IsValid(false, "Please choose a directory not a file..."));
                return Task.FromResult(new IsValid(true, string.Empty));
            }
        };
        initialDirectoryEntry.GetInitialDirectory =
            () => Task.FromResult(CloudBackupGuiSettingTools.ReadSettings().LastDirectory);
        initialDirectoryEntry.AfterDirectoryChoice = async x =>
        {
            var currentSettings = CloudBackupGuiSettingTools.ReadSettings();
            var newDirectory = new DirectoryInfo(x);
            if (!newDirectory.Exists) return;

            currentSettings.LastDirectory = newDirectory.Parent?.FullName ?? newDirectory.FullName;
            await CloudBackupGuiSettingTools.WriteSettings(currentSettings);
        };

        var filePatternEntry = StringDataEntryContext.CreateInstance();
        filePatternEntry.Title = "New File Pattern Exclusion";
        filePatternEntry.HelpText =
            "Each file name will be compared to the File Exclusion Patterns and if there is a match it will be excluded. You can use * (match anything) and ? (match any single character) in your patterns.";
        filePatternEntry.ReferenceValue = string.Empty;
        filePatternEntry.UserValue = string.Empty;
        filePatternEntry.ValidationFunctions = new List<Func<string?, Task<IsValid>>>
        {
            _ => Task.FromResult(new IsValid(true, string.Empty))
        };

        var directoryPatternEntry = StringDataEntryContext.CreateInstance();
        directoryPatternEntry.Title = "New File Pattern Exclusion";
        directoryPatternEntry.HelpText =
            "Each directory name will be compared to the Directory Exclusion Patterns and if there is a match it will be excluded. You can use * (match anything) and ? (match any single character) in your patterns.";
        directoryPatternEntry.ReferenceValue = string.Empty;
        directoryPatternEntry.UserValue = string.Empty;
        directoryPatternEntry.ValidationFunctions = new List<Func<string?, Task<IsValid>>>
        {
            _ => Task.FromResult(new IsValid(true, string.Empty))
        };

        var maximumRuntimeHoursEntry =
            await ConversionDataEntryContext<int>.CreateInstance(ConversionDataEntryHelpers.IntConversion);
        maximumRuntimeHoursEntry.Title = "Default Maximum Runtime Hours";
        maximumRuntimeHoursEntry.HelpText =
            "The maximum number of hours during which new uploads will be started. This is a default value - you can override it for each run of the job.";
        maximumRuntimeHoursEntry.ReferenceValue = initialJob.DefaultMaximumRunTimeInHours;
        maximumRuntimeHoursEntry.UserText = initialJob.DefaultMaximumRunTimeInHours.ToString();
        maximumRuntimeHoursEntry.ValidationFunctions = new List<Func<int, Task<IsValid>>>
        {
            x =>
            {
                if (x < 1)
                    return Task.FromResult(new IsValid(false, "The maximum runtime hours must be greater than 0"));
                if (x > 23)
                    return Task.FromResult(new IsValid(false, "The maximum runtime hours must be less than 24"));
                return Task.FromResult(new IsValid(true, string.Empty));
            }
        };

        var dbExcludedDirectory = initialJob.ExcludedDirectories
            .Select(x => new DirectoryInfo(x.Directory)).ToList();
        var excludedDirectory = new ObservableCollection<DirectoryInfo>(dbExcludedDirectory);

        var dbExcludedDirectoryPatterns = initialJob.ExcludedDirectoryNamePatterns.Select(x => x.Pattern).ToList();
        var excludedDirectoryPatterns =
            new ObservableCollection<string>(dbExcludedDirectoryPatterns);

        var dbExcludedFilePatterns = initialJob.ExcludedFileNamePatterns.Select(x => x.Pattern).ToList();
        var excludedFilePatterns =
            new ObservableCollection<string>(dbExcludedFilePatterns);

        var toReturn = new JobEditorContext
        {
            LoadedJob = initialJob,
            ExcludedDirectories = excludedDirectory,
            ExcludedDirectoriesOriginal = dbExcludedDirectory,
            ExcludedDirectoryPatterns = excludedDirectoryPatterns,
            ExcludedDirectoryPatternsOriginal = dbExcludedDirectoryPatterns,
            ExcludedFilePatterns = excludedFilePatterns,
            ExcludedFilePatternsOriginal = dbExcludedFilePatterns,
            StatusContext = statusContext,
            UserInitialDirectoryEntry = initialDirectoryEntry,
            UserCloudDirectoryEntry = cloudDirectoryEntry,
            UserDirectoryPatternEntry = directoryPatternEntry,
            UserFilePatternEntry = filePatternEntry,
            UserMaximumRuntimeHoursEntry = maximumRuntimeHoursEntry,
            UserNameEntry = nameEntry,
            DatabaseFile = databaseFile
        };

        await toReturn.Setup();

        return toReturn;
    }

    private void ExcludedDirectoriesChangeCheck()
    {
        var frozenList = ExcludedDirectories.Select(x => x.FullName).ToList();
        var originalList = ExcludedDirectoriesOriginal.Select(x => x.FullName).ToList();
        var added = frozenList.Except(originalList).ToList();
        var removed = originalList.Except(frozenList).ToList();

        ExcludedDirectoriesHasChanges = added.Any() || removed.Any();

        ExcludedDirectoriesHasChangesMessage = string.Empty;
        if (added.Any()) ExcludedDirectoriesHasChangesMessage += $"Added: {string.Join(",", added)}";

        if (added.Any() && removed.Any()) ExcludedDirectoriesHasChangesMessage += Environment.NewLine;

        if (removed.Any()) ExcludedDirectoriesHasChangesMessage += $"Removed: {string.Join(",", removed)}";
    }

    private void ExcludedDirectoriesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ExcludedDirectoriesChangeCheck();
        CheckForChangesAndValidationIssues();
    }

    private void ExcludedDirectoryPatternsChangeCheck()
    {
        var frozenList = ExcludedDirectoryPatterns.ToList();

        var added = frozenList.Except(ExcludedDirectoryPatternsOriginal).ToList();
        var removed = ExcludedDirectoryPatternsOriginal.Except(frozenList).ToList();

        ExcludedDirectoryPatternsHasChanges = added.Any() || removed.Any();

        ExcludedDirectoryPatternsHasChangesMessage = string.Empty;
        if (added.Any()) ExcludedDirectoryPatternsHasChangesMessage += $"Added: {string.Join(",", added)}";

        if (added.Any() && removed.Any()) ExcludedDirectoryPatternsHasChangesMessage += Environment.NewLine;

        if (removed.Any()) ExcludedDirectoryPatternsHasChangesMessage += $"Removed: {string.Join(",", removed)}";
    }

    private void ExcludedDirectoryPatternsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ExcludedDirectoryPatternsChangeCheck();
        CheckForChangesAndValidationIssues();
    }

    private void ExcludedFilePatternChangeCheck()
    {
        var frozenList = ExcludedFilePatterns.ToList();

        var added = frozenList.Except(ExcludedFilePatternsOriginal).ToList();
        var removed = ExcludedFilePatternsOriginal.Except(frozenList).ToList();

        ExcludedFilePatternsHasChanges = added.Any() || removed.Any();

        ExcludedFilePatternsHasChangesMessage = string.Empty;
        if (added.Any()) ExcludedFilePatternsHasChangesMessage += $"Added: {string.Join(",", added)}";

        if (added.Any() && removed.Any()) ExcludedFilePatternsHasChangesMessage += Environment.NewLine;

        if (removed.Any()) ExcludedFilePatternsHasChangesMessage += $"Removed: {string.Join(",", removed)}";
    }

    private void ExcludedFilePatternsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ExcludedFilePatternChangeCheck();
        CheckForChangesAndValidationIssues();
    }

    [BlockingCommand]
    public async Task RemoveSelectedExcludedDirectory()
    {
        var frozenSelection = SelectedExcludedDirectory;

        if (frozenSelection == null)
        {
            StatusContext.ToastError("Nothing selected to Remove?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        ExcludedDirectories.Remove(frozenSelection);
    }

    [BlockingCommand]
    public async Task RemoveSelectedExcludedDirectoryPattern()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var frozenSelection = SelectedExcludedDirectoryPattern;

        if (string.IsNullOrWhiteSpace(frozenSelection))
        {
            StatusContext.ToastError("Nothing selected to Remove?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        ExcludedDirectoryPatterns.Remove(frozenSelection);
    }

    [BlockingCommand]
    public async Task RemoveSelectedExcludedFilePattern()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var frozenSelection = SelectedExcludedFilePattern;

        if (string.IsNullOrWhiteSpace(frozenSelection))
        {
            StatusContext.ToastError("Nothing selected to Remove?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        ExcludedFilePatterns.Remove(frozenSelection);
    }

    [BlockingCommand]
    public async Task SaveAndClose()
    {
        await SaveChanges(true);
    }

    [BlockingCommand]
    public async Task SaveAndStayOpen()
    {
        await SaveChanges(false);
    }

    public async Task SaveChanges(bool closeAfterSave)
    {
        if (HasValidationIssues)
        {
            StatusContext.ToastError("Please correct all issues before saving.");
            return;
        }

        if (!HasChanges)
        {
            StatusContext.ToastWarning("No Changes to Save?");
            return;
        }

        //Todo: Track changes in the Observable Collections

        var toSave = new BackupJob();

        var frozenNow = DateTime.Now;

        var db = await CloudBackupContext.CreateInstance(DatabaseFile, false);

        if (LoadedJob.Id > 0)
        {
            var item = await db.BackupJobs.SingleOrDefaultAsync(x => x.Id == LoadedJob.Id);
            if (item != null) toSave = item;
        }

        toSave.Name = UserNameEntry.UserValue;
        toSave.LocalDirectory = UserInitialDirectoryEntry.UserValue.Trim();
        toSave.CloudDirectory = UserCloudDirectoryEntry.UserValue;
        toSave.CreatedOn = LoadedJob.CreatedOn;
        toSave.DefaultMaximumRunTimeInHours = UserMaximumRuntimeHoursEntry.UserValue;

        var directoriesToRemove = new List<ExcludedDirectory>();
        var directoriesToAdd = new List<string>();
        foreach (var loopDbExcluded in toSave.ExcludedDirectories)
            if (ExcludedDirectories.All(x => x.FullName != loopDbExcluded.Directory))
                directoriesToRemove.Add(loopDbExcluded);

        foreach (var loopGuiExcluded in ExcludedDirectories)
            if (toSave.ExcludedDirectories.All(x => x.Directory != loopGuiExcluded.FullName))
                directoriesToAdd.Add(loopGuiExcluded.FullName);

        directoriesToRemove.ForEach(x => toSave.ExcludedDirectories.Remove(x));

        directoriesToAdd.ForEach(x => toSave.ExcludedDirectories.Add(new ExcludedDirectory
            { CreatedOn = frozenNow, Job = toSave, Directory = x }));

        var directoryPatternsToRemove = toSave.ExcludedDirectoryNamePatterns
            .Where(x => !ExcludedDirectoryPatterns.Contains(x.Pattern)).ToList();
        var directoryPatternsToAdd = new List<string>();

        foreach (var loopGuiExcluded in ExcludedDirectoryPatterns)
            if (toSave.ExcludedDirectoryNamePatterns.All(x => x.Pattern != loopGuiExcluded))
                directoryPatternsToAdd.Add(loopGuiExcluded);

        directoryPatternsToRemove.ForEach(x => toSave.ExcludedDirectoryNamePatterns.Remove(x));

        directoryPatternsToAdd.ForEach(x =>
            toSave.ExcludedDirectoryNamePatterns.Add(new ExcludedDirectoryNamePattern
                { CreatedOn = frozenNow, Job = toSave, Pattern = x }));

        var filePatternsToRemove = toSave.ExcludedFileNamePatterns
            .Where(x => !ExcludedFilePatterns.Contains(x.Pattern)).ToList();
        var filePatternsToAdd = new List<string>();

        foreach (var loopGuiFilePattern in ExcludedFilePatterns)
            if (toSave.ExcludedFileNamePatterns.All(x => x.Pattern != loopGuiFilePattern))
                filePatternsToAdd.Add(loopGuiFilePattern);

        filePatternsToRemove.ForEach(x => toSave.ExcludedFileNamePatterns.Remove(x));

        filePatternsToAdd.ForEach(x => toSave.ExcludedFileNamePatterns.Add(new ExcludedFileNamePattern
            { CreatedOn = frozenNow, Job = toSave, Pattern = x }));

        if (toSave.Id < 1) db.BackupJobs.Add(toSave);

        await db.SaveChangesAsync();

        UserNameEntry.ReferenceValue = toSave.Name;
        UserCloudDirectoryEntry.ReferenceValue = toSave.CloudDirectory;
        UserMaximumRuntimeHoursEntry.ReferenceValue = toSave.DefaultMaximumRunTimeInHours;
        UserInitialDirectoryEntry.ReferenceValue = toSave.LocalDirectory;
        ExcludedDirectoriesOriginal = toSave.ExcludedDirectories.Select(x => new DirectoryInfo(x.Directory)).ToList();
        ExcludedDirectoryPatternsOriginal = toSave.ExcludedDirectoryNamePatterns.Select(x => x.Pattern).ToList();
        ExcludedFilePatternsOriginal = toSave.ExcludedFileNamePatterns.Select(x => x.Pattern).ToList();

        ExcludedDirectoriesChangeCheck();
        ExcludedDirectoryPatternsChangeCheck();
        ExcludedFilePatternChangeCheck();

        CheckForChangesAndValidationIssues();
    }

    public Task Setup()
    {
        BuildCommands();

        ExcludedDirectories.CollectionChanged += ExcludedDirectoriesOnCollectionChanged;
        ExcludedDirectoryPatterns.CollectionChanged += ExcludedDirectoryPatternsOnCollectionChanged;
        ExcludedFilePatterns.CollectionChanged += ExcludedFilePatternsOnCollectionChanged;

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this,
            CheckForChangesAndValidationIssues);

        CheckForChangesAndValidationIssues();

        return Task.CompletedTask;
    }
}