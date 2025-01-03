using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using Amazon;
using Microsoft.EntityFrameworkCore;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CloudBackupData;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CloudBackupData.Reports;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.CommonTools.S3;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WindowsTools;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.ConversionDataEntry;
using PointlessWaymarks.WpfCommon.ExistingDirectoryDataEntry;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;
using PointlessWaymarks.WpfCommon.StringDropdownDataEntry;

namespace PointlessWaymarks.CloudBackupGui.Controls;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class JobEditorContext : IHasChanges, IHasValidationIssues,
    ICheckForChangesAndValidation
{
    public readonly string HelpText = """
                                      ## Cloud Backup Editor
                                      
                                      The Cloud Backup Editor is used to create and update Backup Jobs.
                                      
                                      Fields:
                                       - Name: The program displays, but does not 'use' this value and this is intended for you to have an easy way to identify a job.
                                       - Initial Local Directory: A Backup Job must start with a single local directory. Unless excluded (see Exclusions below) all subdirectories are included.
                                       - Cloud Bucket: The Amazon S3 'bucket'.
                                       - Cloud Directory: The initial Amazon S3 directory for the job.
                                       - Maximum Runtime in Hours: A focus for this program is scheduled (thru the Windows Task Scheduler) runs with a maximum duration - this allows scheduling your backup runs in time periods where it won't use bandwidth better used for meetings, remote work, streaming and games.
                                       - Cloud Credentials: This will allow you to enter your S3 Access and Secret Keys. These are stored in the Windows Credential Manager - this does mean that your credentials could potentially be exposed to malicious programs running as you! You will need to decide for yourself if this is secure enough...
                                       - Cloud Region: The Region of your Bucket.
                                       - Excluded Directories: This is a list of full directory paths that are excluded. Once a directory is excluded all of its content and subdirectories are excluded also! This list lets you exclude specific directories but will almost certainly require you to reset/rework this list if you Initial Local Directory changes...
                                       - Excluded Directory Patterns: In directories matching any of the given patterns are excluded - once a directory is excluded all contents and subdirectories are also excluded. Matching by patterns like temp* or *data can sometimes make temporary directories easier to excluded. * and ? are the accepted wildcards.
                                       - Excluded File Patterns: Files that match any of these patterns will be excluded from your backup. * and ? are the accepted wildcards.
                                      
                                      Hover over field names for some additional help and look for indicators in the UI that will show you changes and problems.
                                      
                                      Jobs in the Backup Job List have a 'Included/Excluded Files Report' - this report can take a long time to run, but it is suggested that before you run a backup you let that report run and examine the results in order not to be surprised about what is included and what is excluded.
                                      
                                      ### Progress
                                      
                                      Jobs in the list will have the last progress message from any backups running on your local machine. This can make it easy to quickly see which jobs are running especially if a Task Runner like Windows Scheduler is set to hide the console window the backup is running in. To see more progress use the 'Progress to Window' button. This progress display only tracks progress for processes on the local machine.
                                      """;
    
    
    public bool CloudCredentialsHaveValidationIssues { get; set; }
    public required string DatabaseFile { get; set; }
    public required ObservableCollection<DirectoryInfo> ExcludedDirectories { get; set; }
    public bool ExcludedDirectoriesHasChanges { get; set; }
    public string ExcludedDirectoriesHasChangesMessage { get; set; } = string.Empty;
    public required List<DirectoryInfo> ExcludedDirectoriesOriginal { get; set; } = [];
    public required ObservableCollection<string> ExcludedDirectoryPatterns { get; set; }
    public bool ExcludedDirectoryPatternsHasChanges { get; set; }
    public string ExcludedDirectoryPatternsHasChangesMessage { get; set; } = string.Empty;
    public required List<string> ExcludedDirectoryPatternsOriginal { get; set; } = [];
    public required ObservableCollection<string> ExcludedFilePatterns { get; set; }
    public bool ExcludedFilePatternsHasChanges { get; set; }
    public string ExcludedFilePatternsHasChangesMessage { get; set; } = string.Empty;
    public required List<string> ExcludedFilePatternsOriginal { get; set; } = [];
    public HelpDisplayContext? HelpContext { get; set; }
    public required BackupJob LoadedJob { get; set; }
    public EventHandler? RequestContentEditorWindowClose { get; set; }
    public DirectoryInfo? SelectedExcludedDirectory { get; set; }
    public string? SelectedExcludedDirectoryPattern { get; set; }
    public string? SelectedExcludedFilePattern { get; set; }
    public required StatusControlContext StatusContext { get; set; }
    public required StringDropdownDataEntryContext UserAwsRegionEntry { get; set; }
    public required StringDataEntryContext UserCloudBucketEntry { get; set; }
    public required StringDataEntryContext UserCloudDirectoryEntry { get; set; }
    public required StringDropdownDataEntryContext UserCloudProviderEntry { get; set; }
    public required StringDataEntryContext UserDirectoryPatternEntry { get; set; }
    public required StringDataEntryContext UserFilePatternEntry { get; set; }
    public required ExistingDirectoryDataEntryContext UserInitialDirectoryEntry { get; set; }
    public required ConversionDataEntryContext<int> UserMaximumRuntimeHoursEntry { get; set; }
    public required StringDataEntryContext UserNameEntry { get; set; }
    
    
    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this) || ExcludedDirectoriesHasChanges ||
                     ExcludedDirectoryPatternsHasChanges || ExcludedFilePatternsHasChanges;
        HasValidationIssues =
            PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }
    
    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }
    
    [BlockingCommand]
    public async Task AddExcludedDirectory()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var selectedDirectory = await ChooseDirectory();
        
        if (selectedDirectory == null) return;
        
        if (ExcludedDirectories.Any(x =>
                x.FullName.Equals(selectedDirectory.FullName, StringComparison.InvariantCultureIgnoreCase)))
        {
            await StatusContext.ToastError("Directory already exists in the list.");
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
            await StatusContext.ToastError("A blank pattern, or a pattern with only white space is not valid");
            return;
        }
        
        if (ExcludedDirectoryPatterns.Contains(UserDirectoryPatternEntry.UserValue.Trim(),
                StringComparer.InvariantCultureIgnoreCase))
        {
            await StatusContext.ToastError("Pattern already exists - patterns are case insensitive.");
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
            await StatusContext.ToastError("A blank pattern, or a pattern with only white space is not valid");
            return;
        }
        
        if (ExcludedFilePatterns.Contains(UserFilePatternEntry.UserValue.Trim(),
                StringComparer.InvariantCultureIgnoreCase))
        {
            await StatusContext.ToastError("Pattern already exists - patterns are case insensitive.");
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
            if (!string.IsNullOrWhiteSpace(initialDirectoryString))
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
    
    private void CloudCredentialsCheckForValidationIssues()
    {
        var currentCredentials = PasswordVaultTools.GetCredentials(LoadedJob.VaultS3CredentialsIdentifier);
        CloudCredentialsHaveValidationIssues = string.IsNullOrWhiteSpace(currentCredentials.username) ||
                                               string.IsNullOrWhiteSpace(currentCredentials.password);
        
        if (UserCloudProviderEntry.UserValue != S3Providers.Amazon.ToString())
        {
            var currentCloudServiceUrl =
                PasswordVaultTools.GetCredentials(LoadedJob.VaultServiceUrlIdentifier);
            CloudCredentialsHaveValidationIssues = CloudCredentialsHaveValidationIssues ||
                                                   string.IsNullOrWhiteSpace(currentCloudServiceUrl.password);
        }
    }
    
    public static async Task<JobEditorContext> CreateInstance(StatusControlContext? statusContext, BackupJob initialJob,
        string databaseFile)
    {
        var factoryStatusContext = await StatusControlContext.CreateInstance(statusContext);

        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var db = await CloudBackupContext.CreateInstance();
        
        await db.Entry(initialJob).Collection(x => x.ExcludedDirectories).LoadAsync();
        await db.Entry(initialJob).Collection(x => x.ExcludedDirectoryNamePatterns).LoadAsync();
        await db.Entry(initialJob).Collection(x => x.ExcludedFileNamePatterns).LoadAsync();

        var nameEntry = StringDataEntryContext.CreateInstance();
        nameEntry.Title = "Job Name";
        nameEntry.HelpText =
            "A name for the job - you must give the job a name, but there are no requirements - just use anything that will help you remember what you are backing up!";
        nameEntry.ReferenceValue = initialJob.Name;
        nameEntry.UserValue = initialJob.Name;
        nameEntry.ValidationFunctions =
        [
            x =>
            {
                if (string.IsNullOrWhiteSpace(x))
                    return Task.FromResult(new IsValid(false, "A name is required for the job"));
                return Task.FromResult(new IsValid(true, string.Empty));
            }
        ];
        
        var cloudBucketEntry = StringDataEntryContext.CreateInstance();
        cloudBucketEntry.Title = "Cloud Bucket";
        cloudBucketEntry.HelpText =
            "The S3/Cloud Bucket for the job.";
        cloudBucketEntry.ReferenceValue = initialJob.CloudBucket;
        cloudBucketEntry.UserValue = initialJob.CloudBucket;
        cloudBucketEntry.ValidationFunctions =
        [
            x =>
            {
                if (string.IsNullOrWhiteSpace(x))
                    return Task.FromResult(new IsValid(false, "A Cloud Bucket is required for the job"));
                if (!Regex.IsMatch(x, @"\A^[a-z0-9.-]+$\z"))
                    return Task.FromResult(new IsValid(false, "S3 Bucket names can only consist of a-z, 0-9, . and -"));
                return Task.FromResult(new IsValid(true, string.Empty));
            }
        ];
        
        var cloudDirectoryEntry = StringDataEntryContext.CreateInstance();
        cloudDirectoryEntry.Title = "Cloud Directory";
        cloudDirectoryEntry.HelpText =
            "The S3/Cloud Directory for the job - for simplicity can be as simple as a single descriptive folder name";
        cloudDirectoryEntry.ReferenceValue = initialJob.CloudDirectory;
        cloudDirectoryEntry.UserValue = initialJob.CloudDirectory;
        cloudDirectoryEntry.ValidationFunctions =
        [
            x =>
            {
                if (string.IsNullOrWhiteSpace(x))
                    return Task.FromResult(new IsValid(false, "A Cloud Directory is required for the job"));
                if (!Regex.IsMatch(x, "^[a-zA-Z0-9-/]+$"))
                    return Task.FromResult(new IsValid(false,
                        "To keep easy compatibility with cloud storage only a-z, A-Z, 0-9, - and / are allowed in the Cloud Directory Name."));
                if (x.StartsWith("/"))
                    return Task.FromResult(new IsValid(false,
                        "Cloud Directory can not start with / or // - directory must be from the bucket root which does not need an identifier."));
                if (x.Contains("//"))
                    return Task.FromResult(new IsValid(false,
                        "// should not appear in a Cloud Directory - root path starts without / or //."));
                return Task.FromResult(new IsValid(true, string.Empty));
            }
        ];
        
        var initialDirectoryEntry = await ExistingDirectoryDataEntryContext.CreateInstance(factoryStatusContext);
        initialDirectoryEntry.Title = "Initial Local Directory";
        initialDirectoryEntry.HelpText = "Pick a single starting directory for the backup";
        initialDirectoryEntry.ReferenceValue = initialJob.LocalDirectory;
        initialDirectoryEntry.UserValue = initialJob.LocalDirectory;
        initialDirectoryEntry.ValidationFunctions =
        [
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
        ];
        
        initialDirectoryEntry.GetInitialDirectory =
            () => Task.FromResult(CloudBackupGuiSettingTools.ReadSettings().LastDirectory ?? string.Empty);
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
        filePatternEntry.ValidationFunctions = [_ => Task.FromResult(new IsValid(true, string.Empty))];
        
        var directoryPatternEntry = StringDataEntryContext.CreateInstance();
        directoryPatternEntry.Title = "New File Pattern Exclusion";
        directoryPatternEntry.HelpText =
            "Each directory name will be compared to the Directory Exclusion Patterns and if there is a match it will be excluded. You can use * (match anything) and ? (match any single character) in your patterns.";
        directoryPatternEntry.ReferenceValue = string.Empty;
        directoryPatternEntry.UserValue = string.Empty;
        directoryPatternEntry.ValidationFunctions = [_ => Task.FromResult(new IsValid(true, string.Empty))];
        
        var maximumRuntimeHoursEntry =
            await ConversionDataEntryContext<int>.CreateInstance(ConversionDataEntryHelpers.IntConversion);
        maximumRuntimeHoursEntry.Title = "Maximum Runtime in Hours";
        maximumRuntimeHoursEntry.HelpText =
            "The maximum number of hours during which new uploads will be started.";
        maximumRuntimeHoursEntry.ReferenceValue = initialJob.MaximumRunTimeInHours;
        maximumRuntimeHoursEntry.UserText = initialJob.MaximumRunTimeInHours.ToString();
        maximumRuntimeHoursEntry.ValidationFunctions =
        [
            x =>
            {
                if (x < 1)
                    return Task.FromResult(new IsValid(false, "The maximum runtime hours must be greater than 0"));
                if (x > 23)
                    return Task.FromResult(new IsValid(false, "The maximum runtime hours must be less than 24"));
                return Task.FromResult(new IsValid(true, string.Empty));
            }
        ];
        
        var cloudProviderDataEntry = StringDropdownDataEntryContext.CreateInstance();
        cloudProviderDataEntry.Title = "Cloud Provider";
        cloudProviderDataEntry.HelpText = "The cloud provider for the job.";
        cloudProviderDataEntry.ReferenceValue = initialJob.CloudProvider;
        cloudProviderDataEntry.Choices = new List<string> { string.Empty }.Concat(Enum.GetNames(typeof(S3Providers)))
            .Select(x => new DropDownDataChoice { DisplayString = x, DataString = x }).ToList();
        cloudProviderDataEntry.TrySetUserValue(initialJob.CloudProvider);
        
        
        var regionsDataEntry = StringDropdownDataEntryContext.CreateInstance();
        regionsDataEntry.Title = "Cloud Region";
        regionsDataEntry.HelpText = "The region of the S3 Bucket.";
        regionsDataEntry.ReferenceValue = initialJob.CloudRegion;
        regionsDataEntry.Choices = new DropDownDataChoice { DataString = "", DisplayString = "" }.AsList().Concat(
            RegionEndpoint.EnumerableAllRegions.Select(x => new DropDownDataChoice()
                { DisplayString = x.SystemName, DataString = x.SystemName })).ToList();
        regionsDataEntry.TrySetUserValue(initialJob.CloudRegion);
        regionsDataEntry.ValidationFunctions =
        [
            x =>
            {
                if (cloudProviderDataEntry.UserValue == S3Providers.Amazon.ToString())
                    if (string.IsNullOrWhiteSpace(x))
                        return new IsValid(false, "A Cloud Region is required for the job");
                
                return new IsValid(true, string.Empty);
            }
        ];
        
        
        var dbExcludedDirectory = initialJob.ExcludedDirectories
            .Select(x => new DirectoryInfo(x.Directory)).ToList();
        var excludedDirectory = new ObservableCollection<DirectoryInfo>(dbExcludedDirectory.GroupBy(x => x.FullName)
            .Select(x => x.First()).OrderBy(x => x.FullName).ToList());
        
        var dbExcludedDirectoryPatterns = initialJob.ExcludedDirectoryNamePatterns.Select(x => x.Pattern).ToList();
        var excludedDirectoryPatterns =
            new ObservableCollection<string>(dbExcludedDirectoryPatterns.Distinct().OrderBy(x => x).ToList());
        
        var dbExcludedFilePatterns = initialJob.ExcludedFileNamePatterns.Select(x => x.Pattern).ToList();
        var excludedFilePatterns =
            new ObservableCollection<string>(dbExcludedFilePatterns.Distinct().OrderBy(x => x).ToList());
        
        var toReturn = new JobEditorContext
        {
            LoadedJob = initialJob,
            ExcludedDirectories = excludedDirectory,
            ExcludedDirectoriesOriginal = dbExcludedDirectory,
            ExcludedDirectoryPatterns = excludedDirectoryPatterns,
            ExcludedDirectoryPatternsOriginal = dbExcludedDirectoryPatterns,
            ExcludedFilePatterns = excludedFilePatterns,
            ExcludedFilePatternsOriginal = dbExcludedFilePatterns,
            StatusContext = factoryStatusContext,
            UserInitialDirectoryEntry = initialDirectoryEntry,
            UserAwsRegionEntry = regionsDataEntry,
            UserCloudProviderEntry = cloudProviderDataEntry,
            UserCloudBucketEntry = cloudBucketEntry,
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
    
    [BlockingCommand]
    public async Task EnterCloudCredentials()
    {
        var newKeyEntry = await StatusContext.ShowStringEntry("Cloud Access Key",
            "Enter the Cloud Access Key", string.Empty);
        
        if (!newKeyEntry.Item1)
        {
            await StatusContext.ToastWarning("Cloud Credential Entry Cancelled");
            return;
        }
        
        var cleanedKey = newKeyEntry.Item2.TrimNullToEmpty();
        
        if (string.IsNullOrWhiteSpace(cleanedKey)) return;
        
        var newSecretEntry = await StatusContext.ShowStringEntry("Cloud Secret Key",
            "Enter the Secret Key", string.Empty);
        
        if (!newSecretEntry.Item1) return;
        
        var cleanedSecret = newSecretEntry.Item2.TrimNullToEmpty();
        
        if (string.IsNullOrWhiteSpace(cleanedSecret))
        {
            await StatusContext.ToastError("Cloud Credential Entry Canceled - secret can not be blank");
            return;
        }
        
        PasswordVaultTools.SaveCredentials(LoadedJob.VaultS3CredentialsIdentifier, cleanedKey, cleanedSecret);
        
        if (UserCloudProviderEntry.UserValue != S3Providers.Amazon.ToString())
        {
            var serviceUrl = await StatusContext.ShowStringEntry("Service URL",
                "Enter the S3 service URL. For Cloudflare this will be https://{accountId}.r2.cloudflarestorage.com - other providers, like Wasabi, will have a Service URL based on region (for example s3.ca-central-1.wasabisys.com for Wasabi-Toronto)",
                string.Empty);
            
            if (!serviceUrl.Item1) return;
            
            var cleanedServiceUrl = serviceUrl.Item2.TrimNullToEmpty();
            
            if (string.IsNullOrWhiteSpace(cleanedServiceUrl))
            {
                await StatusContext.ToastError("Cloud Credential Entry Canceled - Service URL can not be blank");
                return;
            }
            
            PasswordVaultTools.SaveCredentials(LoadedJob.VaultServiceUrlIdentifier, "Service Url",
                cleanedServiceUrl);
        }
        
        CloudCredentialsCheckForValidationIssues();
    }
    
    private void ExcludedDirectoriesChangeCheck()
    {
        var frozenList = ExcludedDirectories.Select(x => x.FullName).ToList();
        var originalList = ExcludedDirectoriesOriginal.Select(x => x.FullName).ToList();
        var added = frozenList.Except(originalList).ToList();
        var removed = originalList.Except(frozenList).ToList();
        
        ExcludedDirectoriesHasChanges =
            added.Any() || removed.Any() || frozenList.Count != ExcludedDirectoriesOriginal.Count;
        
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
        
        ExcludedDirectoryPatternsHasChanges = added.Any() || removed.Any() ||
                                              frozenList.Count != ExcludedDirectoryPatternsOriginal.Count;
        
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
        
        ExcludedFilePatternsHasChanges =
            added.Any() || removed.Any() || frozenList.Count != ExcludedFilePatternsOriginal.Count;
        
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
    public async Task IncludedAndExcludedFilesReport()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (UserInitialDirectoryEntry.HasValidationIssues)
        {
            await StatusContext.ToastError("The initial directory has validation issues - please correct before continuing.");
            return;
        }
        
        var frozenInitialLocalDirectory = new DirectoryInfo(UserInitialDirectoryEntry.UserValue);
        
        if (!frozenInitialLocalDirectory.Exists)
        {
            await StatusContext.ToastError("The initial directory does not exist - please correct before continuing.");
            return;
        }
        
        var frozenName = string.IsNullOrWhiteSpace(UserNameEntry.UserValue) ? "No Name" : UserNameEntry.UserValue;
        var frozenExcludedDirectories = ExcludedDirectories.Select(x => x.FullName).OrderBy(x => x).ToList();
        var frozenExcludedDirectoryPatterns = ExcludedDirectoryPatterns.OrderBy(x => x).ToList();
        var frozenExcludedFilePatterns = ExcludedFilePatterns.OrderBy(x => x).ToList();
        
        await IncludedAndExcludedFilesToExcel.Run(frozenName, frozenInitialLocalDirectory.FullName,
            frozenExcludedDirectories, frozenExcludedDirectoryPatterns,
            frozenExcludedFilePatterns, StatusContext.ProgressTracker());
    }
    
    [BlockingCommand]
    public async Task RemoveSelectedExcludedDirectory()
    {
        var frozenSelection = SelectedExcludedDirectory;
        
        if (frozenSelection == null)
        {
            await StatusContext.ToastError("Nothing selected to Remove?");
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
            await StatusContext.ToastError("Nothing selected to Remove?");
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
            await StatusContext.ToastError("Nothing selected to Remove?");
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
            await StatusContext.ToastError("Please correct all issues before saving.");
            return;
        }
        
        if (!HasChanges)
        {
            await StatusContext.ToastWarning("No Changes to Save?");
            return;
        }
        
        var toSave = new BackupJob();
        
        var frozenNow = DateTime.Now;
        
        var db = await CloudBackupContext.CreateInstance(DatabaseFile, false);
        
        if (LoadedJob.Id > 0)
        {
            var item = await db.BackupJobs.Include(backupJob => backupJob.ExcludedDirectories)
                .Include(backupJob => backupJob.ExcludedDirectoryNamePatterns)
                .Include(backupJob => backupJob.ExcludedFileNamePatterns)
                .SingleOrDefaultAsync(x => x.Id == LoadedJob.Id);
            if (item != null) toSave = item;
        }
        
        var translatedCloudRegion = UserCloudProviderEntry.UserValue == S3Providers.Amazon.ToString()
            ? UserAwsRegionEntry.UserValue ?? string.Empty
            : string.Empty;
        
        toSave.Name = UserNameEntry.UserValue;
        toSave.LocalDirectory = UserInitialDirectoryEntry.UserValue.Trim();
        toSave.CloudRegion = translatedCloudRegion;
        toSave.CloudProvider = UserCloudProviderEntry.UserValue!;
        toSave.CloudBucket = UserCloudBucketEntry.UserValue;
        if (!UserCloudDirectoryEntry.UserValue.EndsWith("/"))
            UserCloudDirectoryEntry.UserValue = $"{UserCloudDirectoryEntry.UserValue}/";
        toSave.CloudDirectory = UserCloudDirectoryEntry.UserValue;
        toSave.PersistentId = LoadedJob.PersistentId;
        toSave.CreatedOn = LoadedJob.CreatedOn;
        toSave.MaximumRunTimeInHours = UserMaximumRuntimeHoursEntry.UserValue;
        
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
        
        var directoryToRemoveForDuplicates =
            await db.ExcludedDirectories.Where(x => x.BackupJobId == toSave.Id).ToListAsync();
        var duplicateDirectoryGroups = directoryToRemoveForDuplicates.GroupBy(x => x.Directory);
        foreach (var loopDuplicateDirectoryGroup in duplicateDirectoryGroups)
            db.ExcludedDirectories.RemoveRange(loopDuplicateDirectoryGroup.Skip(1));
        
        var directoryPatternsToRemoveForDuplicates =
            await db.ExcludedDirectoryNamePatterns.Where(x => x.BackupJobId == toSave.Id).ToListAsync();
        var duplicateDirectoryPatternGroups = directoryPatternsToRemoveForDuplicates.GroupBy(x => x.Pattern);
        foreach (var loopDuplicateDirectoryPatternGroup in duplicateDirectoryPatternGroups)
            db.ExcludedDirectoryNamePatterns.RemoveRange(loopDuplicateDirectoryPatternGroup.Skip(1));
        
        var filePatternsToRemoveForDuplicates =
            await db.ExcludedFileNamePatterns.Where(x => x.BackupJobId == toSave.Id).ToListAsync();
        var duplicateFilePatternGroups = filePatternsToRemoveForDuplicates.GroupBy(x => x.Pattern);
        foreach (var loopDuplicateFilePatternGroup in duplicateFilePatternGroups)
            db.ExcludedFileNamePatterns.RemoveRange(loopDuplicateFilePatternGroup.Skip(1));
        
        await db.SaveChangesAsync();
        
        //Make sure we have the latest from the db
        toSave = await db.BackupJobs.Include(backupJob => backupJob.ExcludedDirectories)
            .Include(backupJob => backupJob.ExcludedDirectoryNamePatterns)
            .Include(backupJob => backupJob.ExcludedFileNamePatterns).SingleAsync(x => x.Id == toSave.Id);
        
        DataNotifications.PublishDataNotification(StatusContext.StatusControlContextId.ToString(),
            DataNotificationContentType.BackupJob, DataNotificationUpdateType.Update, toSave.PersistentId, null);
        
        UserNameEntry.ReferenceValue = toSave.Name;
        UserAwsRegionEntry.ReferenceValue = toSave.CloudRegion;
        UserAwsRegionEntry.TrySetUserValue(toSave.CloudRegion);
        UserCloudProviderEntry.ReferenceValue = toSave.CloudProvider;
        UserCloudBucketEntry.ReferenceValue = toSave.CloudBucket;
        UserCloudDirectoryEntry.ReferenceValue = toSave.CloudDirectory;
        UserMaximumRuntimeHoursEntry.ReferenceValue = toSave.MaximumRunTimeInHours;
        UserInitialDirectoryEntry.ReferenceValue = toSave.LocalDirectory;
        ExcludedDirectoriesOriginal = toSave.ExcludedDirectories.Select(x => new DirectoryInfo(x.Directory)).ToList();
        ExcludedDirectoryPatternsOriginal = toSave.ExcludedDirectoryNamePatterns.Select(x => x.Pattern).ToList();
        ExcludedFilePatternsOriginal = toSave.ExcludedFileNamePatterns.Select(x => x.Pattern).ToList();
        
        LoadedJob = toSave;
        
        ExcludedDirectoriesChangeCheck();
        ExcludedDirectoryPatternsChangeCheck();
        ExcludedFilePatternChangeCheck();
        CloudCredentialsCheckForValidationIssues();
        
        CheckForChangesAndValidationIssues();
        
        if (closeAfterSave) RequestContentEditorWindowClose?.Invoke(this, EventArgs.Empty);
    }
    
    public Task Setup()
    {
        BuildCommands();
        
        ExcludedDirectories.CollectionChanged += ExcludedDirectoriesOnCollectionChanged;
        ExcludedDirectoryPatterns.CollectionChanged += ExcludedDirectoryPatternsOnCollectionChanged;
        ExcludedFilePatterns.CollectionChanged += ExcludedFilePatternsOnCollectionChanged;
        
        HelpContext = new HelpDisplayContext([HelpText]);
        
        CloudCredentialsCheckForValidationIssues();
        
        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this,
            CheckForChangesAndValidationIssues);
        
        CheckForChangesAndValidationIssues();
        
        UserCloudProviderEntry.PropertyChanged += UserCloudProviderEntry_PropertyChanged;
        
        return Task.CompletedTask;
    }
    
    private void UserCloudProviderEntry_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UserCloudProviderEntry.SelectedItem))
        {
            CloudCredentialsCheckForValidationIssues();
            
            if (UserCloudProviderEntry.UserValue == S3Providers.Amazon.ToString())
                UserAwsRegionEntry.ValidationFunctions =
                [
                    x =>
                    {
                        if (string.IsNullOrWhiteSpace(x))
                            return new IsValid(false, "A Cloud Region is required for the job");
                        return new IsValid(true, string.Empty);
                    }
                ];
            else
                UserAwsRegionEntry.ValidationFunctions = [];
        }
    }
}