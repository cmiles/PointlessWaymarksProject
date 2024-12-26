using System.ComponentModel;
using System.IO;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeedReaderData;
using PointlessWaymarks.FeedReaderGui.Controls;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.ProgramUpdateMessage;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;

namespace PointlessWaymarks.FeedReaderGui;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
[NotifyPropertyChanged]
[GenerateStatusCommands]
[StaThreadConstructorGuard]
public partial class MainWindow
{
    public readonly string HelpText =
        """
        ## Pointless Waymarks Feed Reader

        The Pointless Waymarks Feed Reader is a Windows Desktop (only!) Feed Reader. The program uses a SQLite database to store data about Feeds and Feed Items. The emphasis in this program is NOT displaying the RSS Content in a feed, but rather displaying the URL the Feed Links to.

        There are a number of great options for Feed (RSS) Readers - so why write another one is a good question...
         - Windows Desktop Only: After many years of RSS use my strong preference is that I don't want to read feeds all the time everywhere! Also I like: sitting in front of a desktop computer with a big screen (or screens!), desktop programs, owning my own data, keeping my data local and I like that I can't sit in front of the computer all day both because of 'life' and because I know how terrible that is for me...
         - Emphasize Displaying Linked Content Not the Feed Content: Feeds are just data and can be used in an awesome number of ways - but the convention is that a Feed Item links to content and I just want to see the content, in full...
         - Simple Feed List: I wonder at this point if I have spent a full day of my life organizing and tweaking the display of Feeds/Folders in Feed Readers? Clicking/unclicking/manipulating tree like structures of Feeds... I'm interested in a simpler display of Feeds that removes the temptation to fiddle and presents fewer options.
         - Joy! I love the art and craft of writing software and I love the feeling of using software that directly addresses my needs/wants/workflow/ideas.

        While the GUI, approach, vision, scope, design and nearly every detail is different this program will always be based on my memories of using [FeedDemon](https://nick.typepad.com/blog/2013/03/the-end-of-feeddemon.html) especially in the late 2000s!
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
                "Pointless Waymarks Feed Reader Beta");

        InfoTitle = versionInfo.humanTitleString;

        var currentDateVersion = versionInfo.dateVersion;

        StatusContext = new StatusControlContext { BlockUi = false };

        BuildCommands();

        DataContext = this;

        UpdateMessageContext = new ProgramUpdateMessageContext(StatusContext);

        HelpTabContext = new HelpDisplayContext([
            HelpText,
            HelpMarkdown.CombinedAboutToolsAndPackages
        ]);

        StatusContext.RunFireAndForgetNonBlockingTask(async () =>
        {
            await CheckForProgramUpdate(currentDateVersion);

            await LoadData();
        });
    }

    public AppSettingsContext? AppSettingsTabContext { get; set; }
    public FeedItemListContext? FeedItemListTabContext { get; set; }
    public FeedListContext? FeedListTabContext { get; set; }
    public HelpDisplayContext HelpTabContext { get; set; }
    public string InfoTitle { get; set; }
    public SavedFeedItemListContext? SavedFeedItemListTabContext { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public ProgramUpdateMessageContext UpdateMessageContext { get; set; }

    public async Task CheckForProgramUpdate(string currentDateVersion)
    {
        var settings = FeedReaderGuiSettingTools.ReadSettings();

        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {settings.ProgramUpdateDirectory}");

        if (string.IsNullOrEmpty(currentDateVersion)) return;

        var (dateString, setupFile) = await ProgramInfoTools.LatestInstaller(
            settings.ProgramUpdateDirectory,
            "PointlessWaymarks-FeedReaderGui-Setup");

        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {settings.ProgramUpdateDirectory}, Installer Date Found {dateString ?? string.Empty}, Setup File Found {setupFile ?? string.Empty}");

        await UpdateMessageContext.LoadData(currentDateVersion, dateString, setupFile);
    }

    public async Task<string> DbFileExistsCheckWithUserInteraction(string dbFile)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(dbFile) || !File.Exists(dbFile))
        {
            var nextAction = await StatusContext.ShowMessage("Database Does Not Exist",
                "The database file does not exist? You can create a new database or pick another file...",
                ["New", "Choose a File"]);

            if (nextAction.Equals("New"))
                return UniqueFileTools.UniqueFile(
                               FileLocationHelpers.DefaultStorageDirectory(), "PointlessWaymarks-FeedReader.db")
                           ?.FullName ??
                       string.Empty;

            if (nextAction.Equals("Choose a File"))
            {
                await ThreadSwitcher.ResumeForegroundAsync();

                var filePicker = new VistaOpenFileDialog
                {
                    Title = "Open Database", Multiselect = false, CheckFileExists = true, ValidateNames = true,
                    Filter = "db files (*.db)|*.db|All files (*.*)|*.*",
                    FileName = $"{FeedReaderGuiSettingTools.GetLastDirectory().FullName}\\"
                };

                var result = filePicker.ShowDialog();

                if (!result ?? false) return string.Empty;

                var newFile = new FileInfo(filePicker.FileName);

                if (newFile.Directory?.Exists ?? false)
                    await FeedReaderGuiSettingTools.SetLastDirectory(newFile.Directory.FullName);

                return filePicker.FileName;
            }
        }

        return dbFile;
    }

    public async Task<string> DbIsValidCheckWithUserInteraction(string dbFile)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var invalidFile = true;
        var dbFileName = string.Empty;

        while (invalidFile)
        {
            dbFileName = await DbFileExistsCheckWithUserInteraction(dbFile);
            if (string.IsNullOrWhiteSpace(dbFileName) || !File.Exists(dbFileName)) continue;

            var dbTest = await FeedContext.TryCreateInstance(dbFileName);
            if (!dbTest.success)
                await StatusContext.ShowMessageWithOkButton("DB Not Valid?",
                    $"There was a problem with the selected db - {dbTest.message}");
            invalidFile = !dbTest.success;
        }

        return dbFileName;
    }

    private async Task LoadData(string? loadWithDatabaseFile = null)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var settings = FeedReaderGuiSettingTools.ReadSettings();

        var dbFileName = string.IsNullOrWhiteSpace(loadWithDatabaseFile)
            ? settings.LastDatabaseFile
            : loadWithDatabaseFile;

        //If the settings file has a blank db then assume this is a first run and create a db without asking
        if (string.IsNullOrWhiteSpace(dbFileName))
        {
            dbFileName = UniqueFileTools.UniqueFile(
                                 FileLocationHelpers.DefaultStorageDirectory(), "PointlessWaymarks-FeedReader.db")
                             ?.FullName ??
                         string.Empty;
            await FeedContext.CreateInstanceWithEnsureCreated(dbFileName);
        }

        dbFileName = await DbIsValidCheckWithUserInteraction(dbFileName);

        settings.LastDatabaseFile = dbFileName;

        await FeedReaderGuiSettingTools.WriteSettings(settings);

        var versionInfo =
            ProgramInfoTools.StandardAppInformationString(AppContext.BaseDirectory,
                "Pointless Waymarks Feed Reader Beta");

        InfoTitle = $"{versionInfo.humanTitleString} - {dbFileName}";

        FeedItemListTabContext = await FeedItemListContext.CreateInstance(StatusContext, dbFileName);
        FeedListTabContext = await FeedListContext.CreateInstance(StatusContext, dbFileName);
        SavedFeedItemListTabContext = await SavedFeedItemListContext.CreateInstance(StatusContext, dbFileName);
        AppSettingsTabContext = new AppSettingsContext(StatusContext);
    }

    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        Log.CloseAndFlush();
    }

    [BlockingCommand]
    public async Task NewDatabase()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var folderPicker = new VistaFolderBrowserDialog
        {
            Description = "New Db Directory", Multiselect = false,
            SelectedPath = $"{FileLocationHelpers.DefaultStorageDirectory()}\\"
        };

        var result = folderPicker.ShowDialog();

        if (!result ?? false) return;

        if (!Directory.Exists(folderPicker.SelectedPath))
        {
            await StatusContext.ToastError($"Selected Directory Does Not Exist? {folderPicker.SelectedPath}");
            return;
        }

        var userFileBase = await StatusContext.ShowStringEntry("New Db File Name", "Enter the file name for a new Db.",
            "PointlessWaymarks-FeedReader");

        if (!userFileBase.Item1) return;

        if (string.IsNullOrWhiteSpace(userFileBase.Item2))
        {
            await StatusContext.ToastError("File name is blank?");
            return;
        }

        var baseFile = Path.HasExtension(userFileBase.Item2)
            ? userFileBase.Item2.Replace(Path.GetExtension(userFileBase.Item2), string.Empty)
            : userFileBase.Item2;

        if (string.IsNullOrWhiteSpace(baseFile))
        {
            await StatusContext.ToastError("File name without extension is blank?");
            return;
        }

        var cleanedFileName = $"{FileAndFolderTools.TryMakeFilenameValid(baseFile.Trim())}.db";

        var uniqueFileName = UniqueFileTools.UniqueFile(new DirectoryInfo(folderPicker.SelectedPath), cleanedFileName);

        if (uniqueFileName == null)
        {
            await StatusContext.ToastError(
                $"Trouble creating a valid file? {folderPicker.SelectedPath} - {cleanedFileName}");
            return;
        }

        try
        {
            var dbTry = await FeedContext.CreateInstanceWithEnsureCreated(uniqueFileName.FullName);
        }
        catch (Exception e)
        {
            await StatusContext.ToastError($"Problem with File... {e.Message}");
            return;
        }

        await LoadData(uniqueFileName.FullName);
    }

    [BlockingCommand]
    public async Task PickNewDatabase()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var filePicker = new VistaOpenFileDialog
        {
            Title = "Open Database", Multiselect = false, CheckFileExists = true, ValidateNames = true,
            Filter = "db files (*.db)|*.db|All files (*.*)|*.*",
            FileName = $"{FeedReaderGuiSettingTools.GetLastDirectory().FullName}\\"
        };

        var result = filePicker.ShowDialog();

        if (!result ?? false) return;

        var newFile = new FileInfo(filePicker.FileName);

        if (newFile.Directory?.Exists ?? false)
            await FeedReaderGuiSettingTools.SetLastDirectory(newFile.Directory.FullName);

        var dbTest = await FeedContext.TryCreateInstance(newFile.FullName);
        if (!dbTest.success)
        {
            await StatusContext.ShowMessageWithOkButton("DB Not Valid?",
                $"There was a problem with the selected db - {dbTest.message}");
            return;
        }

        await LoadData(newFile.FullName);
    }
}