using System.Collections.ObjectModel;
using System.IO;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.ConversionDataEntry;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CloudBackupGui.Controls;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class JobEditorContext
{
    private JobEditorContext()
    {
        BuildCommands();
    }

    public required ObservableCollection<DirectoryInfo> ExcludedDirectories { get; set; }
    public required ObservableCollection<string> ExcludedDirectoryPatterns { get; set; }
    public required ObservableCollection<string> ExcludedFilePatterns { get; set; }
    public DirectoryInfo? InitialDirectory { get; set; }
    public DirectoryInfo? SelectedExcludedDirectory { get; set; }
    public string? SelectedExcludedDirectoryPattern { get; set; }
    public string? SelectedExcludedFilePattern { get; set; }
    public required StatusControlContext StatusContext { get; set; }
    public required StringDataEntryContext UserDirectoryPatternEntry { get; set; }
    public required StringDataEntryContext UserFilePatternEntry { get; set; }
    public required ConversionDataEntryContext<int> UserMaximumRuntimeHoursEntry { get; set; }
    public required StringDataEntryContext UserNameEntry { get; set; }

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
            { Description = "Initial Directory", Multiselect = true };

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

    [BlockingCommand]
    public async Task ChooseInitialDirectory()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var selectedDirectory = await ChooseDirectory();

        if (selectedDirectory == null) return;

        InitialDirectory = selectedDirectory;
    }

    public static async Task<JobEditorContext> CreateInstance(StatusControlContext? context, BackupJob initialJob)
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

        var filePatternEntry = StringDataEntryContext.CreateInstance();
        filePatternEntry.Title = "New File Pattern Exclusion";
        filePatternEntry.HelpText =
            "Each file name will be compared to the File Exclusion Patterns and if there is a match it will be excluded. You can use * (match anything) and ? (match any single character) in your patterns.";
        filePatternEntry.ReferenceValue = string.Empty;
        filePatternEntry.UserValue = string.Empty;
        filePatternEntry.ValidationFunctions = new List<Func<string?, Task<IsValid>>>
        {
            x =>
            {
                if (string.IsNullOrWhiteSpace(x))
                    return Task.FromResult(new IsValid(false, "A name is required for the job"));
                return Task.FromResult(new IsValid(true, string.Empty));
            }
        };

        var directoryPatternEntry = StringDataEntryContext.CreateInstance();
        directoryPatternEntry.Title = "New File Pattern Exclusion";
        directoryPatternEntry.HelpText =
            "Each directory name will be compared to the Directory Exclusion Patterns and if there is a match it will be excluded. You can use * (match anything) and ? (match any single character) in your patterns.";
        directoryPatternEntry.ReferenceValue = string.Empty;
        directoryPatternEntry.UserValue = string.Empty;
        directoryPatternEntry.ValidationFunctions = new List<Func<string?, Task<IsValid>>>
        {
            x =>
            {
                if (string.IsNullOrWhiteSpace(x))
                    return Task.FromResult(new IsValid(false, "A name is required for the job"));
                return Task.FromResult(new IsValid(true, string.Empty));
            }
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

        var excludedDirectory = new ObservableCollection<DirectoryInfo>(initialJob.ExcludedDirectories.Select(x => new DirectoryInfo(x.Directory)).ToList());
        var excludedDirectoryPatterns =
            new ObservableCollection<string>(initialJob.ExcludedDirectoryNamePatterns.Select(x => x.Pattern));
        var excludedFilePatterns =
            new ObservableCollection<string>(initialJob.ExcludedFileNamePatterns.Select(x => x.Pattern));

        return new JobEditorContext
        {
            ExcludedDirectories = excludedDirectory,
            ExcludedDirectoryPatterns = excludedDirectoryPatterns,
            ExcludedFilePatterns = excludedFilePatterns,
            StatusContext = statusContext,
            UserDirectoryPatternEntry = directoryPatternEntry,
            UserFilePatternEntry = filePatternEntry,
            UserMaximumRuntimeHoursEntry = maximumRuntimeHoursEntry,
            UserNameEntry = nameEntry
        };
    }

    [BlockingCommand]
    public async Task RemoveSelectedExcludedDirectory()
    {
        var frozenSelection = SelectedExcludedDirectoryPattern;

        if (frozenSelection == null)
        {
            StatusContext.ToastError("Nothing selected to Remove?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        ExcludedDirectoryPatterns.Remove(frozenSelection);
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
}