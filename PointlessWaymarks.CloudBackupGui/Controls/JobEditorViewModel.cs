using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.ConversionDataEntry;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CloudBackupGui.Controls;

public partial class JobEditorViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<DirectoryInfo> _excludedDirectories;
    [ObservableProperty] private ObservableCollection<string> _excludedDirectoryPatterns;
    [ObservableProperty] private ObservableCollection<string> _excludedFilePatterns;
    [ObservableProperty] private DirectoryInfo? _initialDirectory;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private StringDataEntryContext _userDirectoryPatternEntry;
    [ObservableProperty] private StringDataEntryContext _userFilePatternEntry;
    [ObservableProperty] private ConversionDataEntryContext<int> _userMaximumRuntimeHoursEntry;
    [ObservableProperty] private StringDataEntryContext _userNameEntry;


    private JobEditorViewModel(StatusControlContext statusContext, StringDataEntryContext userNameEntry,
        StringDataEntryContext userFilePatternEntry, StringDataEntryContext userDirectoryPatternEntry,
        ConversionDataEntryContext<int> userMaximumRuntimeHoursEntry,
        ObservableCollection<DirectoryInfo> excludedDirectories,
        ObservableCollection<string> excludedDirectoryPatterns, ObservableCollection<string> excludedFilePatterns)
    {
        _statusContext = statusContext;
        _userNameEntry = userNameEntry;
        _userFilePatternEntry = userFilePatternEntry;
        _userDirectoryPatternEntry = userDirectoryPatternEntry;
        _excludedDirectories = excludedDirectories;
        _excludedDirectoryPatterns = excludedDirectoryPatterns;
        _excludedFilePatterns = excludedFilePatterns;
        _userMaximumRuntimeHoursEntry = userMaximumRuntimeHoursEntry;
    }

    public static async Task<JobEditorViewModel> CreateInstance(StatusControlContext? context, BackupJob initialJob)
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
                return Task.FromResult(new IsValid(true, string.Empty));
            }
        };


        return new JobEditorViewModel(statusContext, nameEntry, filePatternEntry, directoryPatternEntry,
            maximumRuntimeHoursEntry, new ObservableCollection<DirectoryInfo>(), new ObservableCollection<string>(),
            new ObservableCollectionListSource<string>());
    }
}