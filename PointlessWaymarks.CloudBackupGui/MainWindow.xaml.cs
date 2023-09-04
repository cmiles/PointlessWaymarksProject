using PointlessWaymarks.CloudBackupGui.Controls;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.ProgramUpdateMessage;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;

namespace PointlessWaymarks.CloudBackupGui;

[NotifyPropertyChanged]
public partial class MainWindow
{
    public readonly string HelpText =
        """
        ## Pointless Waymarks Cloud Backup

        **This program uses Amazon S3 - Amazon S3 is not free and Amazon charges you for nearly EVERYTHING related to S3 - bandwidth, storage, requests... Using this program WILL INCUR CHARGES on your Amazon AWS Account and it WILL NOT help you calculate, estimate, limit or manage those costs!! In general this is a well understood part of setting up and using Amazon S3 but be warned...**

        **This program stores your AWS Keys in the Windows Credential Manager. This could expose your keys to malicious programs running under your login - it is up to you to decide if this is too much of a security risk. Unfortunately it is difficult to create a system where a autonomous program runs without your interaction and with complete security...**

        The Pointless Waymarks Cloud Backup is designed to create a backup of your local files on Amazon S3. Core details include a focus on data files (system/OS/whole disk backups are not the target of this program), time limited backup runs, Excel reporting and easy scheduling via the Windows Task Scheduler.

        The Pointless Waymarks Cloud Backup consists of two parts - this program, the Cloud Backup Editor, and the Cloud Backup Runner.

        The Cloud Backup Editor is used to create, monitor and report on backup jobs.

        The Cloud Backup Runner is a command line program that is used to run the backup jobs created by the Cloud Backup Editor.

        This project DOES NOT manage, check, test or create resources for you on Amazon AWS! This program can not help you setup your Amazon S3 resources correctly or securely. If you are not a current Amazon AWS/S3 user the good news is that there is quite a bit of information online about setting up and managing AWS resources. The bad news is that it can be quite complex to get the right setup and mistakes can be costly both in terms of money and the privacy and security of your data!

        To use this program you will need to create an Amazon S3 Bucket and an IAM User with access to that bucket. The program will need the Access Key and Secret Key for that IAM User to run.

        ## How it Works

        Backup Jobs are created in the Cloud Backup Editor and saved into a database. Each Backup must start from a single directory and can exclude directories and files as needed. The Cloud Backup Editor does not run backup jobs - that is left to the command line Cloud Backup Runner - it is used to create, monitor and report on Backup Jobs.

        The Cloud Backup Runner is used to run Backup Jobs. While it might sometimes be more convenient to run Backup Jobs directly from the editor having the Cloud Backup Runner as a separate command line program makes is easy to schedule nightly/periodic runs via something as simple as the Windows Task Scheduler. Combined with the Max Runtime setting that all backup jobs have it is easy to schedule a nightly run that will stop before you are awake and using your computer.
                        
        To determine what uploads and deletions need to take place the program will use the setting in the Backup Job (starting directory, excluded directories and files...) and scan all the local files and all of the Amazon S3 files to create a Transfer Batch that is saved in the database. File comparisons are done using the MD5 hash of the file. You can preview the files that will be included and excluded by a Backup Job by using the 'Included/Excluded Files Report' - this can take awhile to run but can be useful before you start a backup job and realize you have excluded something important and included temporary files that you don't want to upload...

        In addition to creating a new Transfer Batch when it is run the Cloud Backup Runner can resume existing batches. Resuming a batch will mean that the backup does NOT know about file changes after the batch was created and is NOT suitable for all backups... But for backup jobs with a large number of files, or where the majority of files only have infrequent changes, resuming a batch will mean that the program spends more time uploading/deleting rather than scanning files.

        The Cloud Backup Editor can generate Excel Reports for both Backup Jobs and Transfer Batches.

        ## Getting Started

        Use the Cloud Backup Editor to create a job - you will need to know or decide on:
          - Bucket, Region and the Access Key and Secret Key for your Amazon S3 account.
          - The starting directory for the backup.
          - The directory on S3 where the backup should be stored.
          - The maximum runtime for the backup job.
          
        Once you have created a Backup Job use the 'Command Line Command to Clipboard' button to get the basic command to execute the job.

        Start a Windows Terminal/PowerShell session in the directory where the Cloud Backup Runner is located and paste the command into the terminal.

        Progress will show both in the terminal and in the Cloud Backup Editor.

        From here you can explore additional Cloud Backup Runner options and scheduling runs via the Windows Task Scheduler.

        ## Cloud Backup Runner Command Line Options

        Options for running the Cloud Backup Runner:
          - No Arguments: Displays the help text
          - Database File: Lists the Backup Jobs and Recent Transfer Batches
          - Database File and Backup Job Id: Runs the Backup Job and creates a new Transfer Batch. This will ensure that the backup picks up recent file changes - but creating a new Transfer Batch for a large set of files can take a significant amount of time.
          - Database File, Backup Job Id and Transfer Batch Id: Resumes the Transfer Batch. This will NOT pick up recent file changes but will spend less time scanning files. If the Transfer Batch Id is not found the program will create a new Transfer Batch.
          - Database File, Backup Job Id and 'last': The program will resume the last Transfer Batch for the Backup Job. If the Backup Job has not been run before the program will create a new Transfer Batch.
          - Database File, Backup Job Id and 'auto': The program will use a number of metrics including when the Backup Job was created, % complete and number of errors to guess about whether it makes more sense to resume a batch or create a new batch. This may create a new Transfer Batch.
        """;

    public MainWindow()
    {
        InitializeComponent();

        JotServices.Tracker.Track(this);

        if (Width < 900) Width = 900;
        if (Height < 650) Height = 650;

        WindowInitialPositionHelpers.EnsureWindowIsVisible(this);

        var versionInfo =
            ProgramInfoTools.StandardAppInformationString(AppContext.BaseDirectory,
                "Pointless Waymarks Cloud Backup Beta");

        InfoTitle = versionInfo.humanTitleString;

        var currentDateVersion = versionInfo.dateVersion;

        StatusContext = new StatusControlContext { BlockUi = false };

        DataContext = this;

        UpdateMessageContext = new ProgramUpdateMessageContext();

        HelpContext = new HelpDisplayContext(new List<string>
        {
            HelpText,
            HelpMarkdown.SoftwareUsedBlock
        });

        SettingsContext = new AppSettingsContext();

        StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            await CheckForProgramUpdate(currentDateVersion);

            await LoadData();
        });
    }

    public HelpDisplayContext HelpContext { get; set; }
    public string InfoTitle { get; set; }
    public JobListContext? ListContext { get; set; }
    public AppSettingsContext SettingsContext { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public ProgramUpdateMessageContext UpdateMessageContext { get; set; }

    public async Task CheckForProgramUpdate(string currentDateVersion)
    {
        var settings = CloudBackupGuiSettingTools.ReadSettings();

        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {settings.ProgramUpdateDirectory}");

        if (string.IsNullOrEmpty(currentDateVersion)) return;

        var (dateString, setupFile) = ProgramInfoTools.LatestInstaller(
            settings.ProgramUpdateDirectory,
            "PointlessWaymarksCloudBackupEditorSetup");

        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {settings.ProgramUpdateDirectory}, Installer Date Found {dateString ?? string.Empty}, Setup File Found {setupFile?.FullName ?? string.Empty}");

        if (string.IsNullOrWhiteSpace(dateString) || setupFile is not { Exists: true }) return;

        if (string.Compare(currentDateVersion, dateString, StringComparison.OrdinalIgnoreCase) >= 0) return;

        await UpdateMessageContext.LoadData(currentDateVersion, dateString, setupFile);
    }

    private async Task LoadData()
    {
        ListContext = await JobListContext.CreateInstance(StatusContext);
    }
}