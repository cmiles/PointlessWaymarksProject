using System.ComponentModel;
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
[StaThreadConstructorGuard]
public partial class MainWindow
{
    public readonly string HelpText =
        """
        ## Pointless Waymarks Cloud Backup

        **This program uses either Amazon S3 or Cloudflare R2 - these are NOT free and while they have different pricing models they both charge based on what you use (bandwidth, storage, requests...) without simple fixed cost or cost limit options! This means that incorrect configurations, user errors or not fully understanding their cost models can quickly generate unexpected HUGE costs!!**

        **Using this program WILL ABSOLUTELY INCUR CHARGES on your S3 Account and it WILL NOT help you calculate, estimate, limit or manage those costs!!! In general this is a well understood part of setting up and using S3 but be warned...**

        **This program stores your S3 Access Keys in the Windows Credential Manager. This could expose your keys to malicious programs running under your login - it is up to you to decide if this is too much of a security risk. Unfortunately it is difficult to create a system where a autonomous program runs without your interaction and with complete security...**

        The Pointless Waymarks Cloud Backup is designed to create a backup of your local files on S3. Core details include a focus on data files (system/OS/whole disk backups are not the target of this program), time limited backup runs, Excel reporting and easy scheduling via a program like the Windows Task Scheduler.

        The Pointless Waymarks Cloud Backup consists of two parts - this program and the command line Cloud Backup Runner.

        Use this program to is used to create, run and monitor and report on backup jobs.

        Use the Cloud Backup Runner to run the backup jobs created by this program from the command line or via a scheduling program.

        This project DOES NOT manage, check, test or create S3 buckets/resources for you and can not help you setup your S3 resources correctly or securely. If you are not a current S3 user the good news is that there is quite a bit of information online about setting up and managing S3 accounts. The bad news is that it can be quite complex to get the right setup and mistakes can be very costly both in terms of money and the privacy and security of your data!

        To use this program you will need to create an S3 Bucket and the related keys that allow access.

        ## How it Works

        Backup Jobs are saved into a database. Each Backup must start from a single directory and can exclude directories and files as needed.

        You can run jobs from this program or use the Cloud Backup Runner to run Backup Jobs from the Command Line or in a scheduling program like Windows Task Scheduler. Use the Max Runtime setting to ensure that the job doesn't run longer than expected - for example you could schedule an 11pm run with a 6 hour runtime to ensure the job doesn't use your bandwidth during normal working hours.
                        
        To determine what uploads and deletions need to take place the program will use the setting in the Backup Job (starting directory, excluded directories and files...), scan all the local files and, depending on runtime settings, compare those files to either db information or a scan of the S3 Files. The needed uploads/copies/deletes will be saved as a Transfer Batch in the database. File comparisons are done using the MD5 hash of the file.
          
        You can preview the files that will be included and excluded by a Backup Job by using the 'Included/Excluded Files Report' - this can take awhile to run but can be useful before you start a backup job and realize you have excluded something important or included temporary files that you don't want to upload...

        In addition to creating a new Transfer Batch when it is run the Cloud Backup Runner can resume existing batches. Resuming a batch will mean that the backup does NOT know about file changes after the batch was created and is NOT suitable for all backups... But for backup jobs with a large number of files, or where the majority of files only have infrequent changes, resuming a batch will mean that the program spends more time uploading/deleting rather than scanning files.

        There are Excel Reports available for both Backup Jobs and Transfer Batches.

        ## Getting Started

        Create a new job - you will need to know or decide on:
          - Bucket, Region, Account ID, Access Key and Secret Key -> some or all of these will be needed depending on whether you are using Amazon S3 or Cloudflare R2.
          - The starting directory for the backup.
          - The directory on S3 where the backup should be stored.
          - The maximum runtime for the backup job.
          
        Click 'Run' to run the job or use the 'Command Line Command to Clipboard' button to get get the Command Line Command to execute or schedule the job.

        From here you can explore additional Cloud Backup Runner options and scheduling runs via the Windows Task Scheduler.

        ## Cloud Backup Runner Command Line Options

        Options for running the Cloud Backup Runner:
          - No Arguments: Displays the help text
          - Database File: Lists the Backup Jobs and Recent Transfer Batches
          - Database File and Backup Job Id: Runs the Backup Job and creates a new Transfer Batch. This will ensure that the backup picks up recent file changes - but creating a new Transfer Batch for a large set of files can take a significant amount of time.
          - Database File, Backup Job Id and Transfer Batch Id: Resumes the Transfer Batch. This will NOT pick up recent file changes but will spend less time scanning files. If the Transfer Batch Id is not found the program will create a new Transfer Batch.
          - Database File, Backup Job Id and 'last': The program will resume the last Transfer Batch for the Backup Job. If the Backup Job has not been run before the program will create a new Transfer Batch.
          - Database File, Backup Job Id and 'new': The program will rescan the S3 files and create a new batch.
          - Database File, Backup Job Id and 'auto': The program will use a number of metrics including when the Backup Job was created, % complete and number of errors to guess about whether it makes more sense to resume a batch or create a new batch and whether to rescan S3 files. This may create a new Transfer Batch.
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

        UpdateMessageContext = new ProgramUpdateMessageContext(StatusContext);

        HelpContext = new HelpDisplayContext([
            HelpText,
            HelpMarkdown.CombinedAboutToolsAndPackages
        ]);

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

        var (dateString, setupFile) = await ProgramInfoTools.LatestInstaller(
            settings.ProgramUpdateDirectory,
            "PointlessWaymarks-CloudBackupGui-Setup");

        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {settings.ProgramUpdateDirectory}, Installer Date Found {dateString ?? string.Empty}, Setup File Found {setupFile ?? string.Empty}");

        await UpdateMessageContext.LoadData(currentDateVersion, dateString, setupFile);
    }

    private async Task LoadData()
    {
        ListContext = await JobListContext.CreateInstance(StatusContext);
    }

    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        Log.CloseAndFlush();
    }
}