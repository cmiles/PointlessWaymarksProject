using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Web.WebView2.Core;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsWpfControls.SitePreview;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.ProgramUpdateMessage;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;

namespace PointlessWaymarks.SiteViewerGui;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class MainWindow
{
    public MainWindow(string? localFolder, string? siteUrl, string? siteName, string? initialPage)
    {
        InitializeComponent();

        JotServices.Tracker.Configure<MainWindow>().Properties(x => new { x.RecentSettingsFilesNames });

        JotServices.Tracker.Track(this);

        if (Width < 900) Width = 900;
        if (Height < 650) Height = 650;

        WindowInitialPositionHelpers.EnsureWindowIsVisible(this);

        var versionInfo =
            ProgramInfoTools.StandardAppInformationString(AppContext.BaseDirectory,
                "Pointless Waymarks Site Viewer Beta");

        InfoTitle = versionInfo.humanTitleString;

        var currentDateVersion = versionInfo.dateVersion;

        StatusContext = new StatusControlContext { BlockUi = false };

        SiteUrl = siteUrl ?? string.Empty;
        InitialPage = initialPage ?? string.Empty;
        LocalFolder = localFolder ?? string.Empty;
        SiteName = siteName ?? string.Empty;

        DataContext = this;

        NewTab = NewTabFunction;

        UpdateMessageContext = new ProgramUpdateMessageContext();

        if (string.IsNullOrWhiteSpace(localFolder))
        {
            ShowSettingsFileChooser = true;

            StatusContext.RunFireAndForgetBlockingTask(async () =>
            {
                await CheckForProgramUpdate(currentDateVersion);

                SettingsFileChooser =
                    await SiteChooserContext.CreateInstance(StatusContext, RecentSettingsFilesNames);

                SettingsFileChooser.SiteSettingsFileChosen += SiteChooserOnSiteSettingsFileChosenEvent;
                SettingsFileChooser.SiteDirectoryChosen += SettingsFileChooserOnSiteDirectoryChosenEvent;
            });
        }
        else
        {
            StatusContext.RunFireAndForgetBlockingTask(async () =>
            {
                await CheckForProgramUpdate(currentDateVersion);

                await LoadData();
            });
        }
    }

    public string InfoTitle { get; set; }
    public string InitialPage { get; set; }
    public string LocalFolder { get; set; }
    public Func<object> NewTab { get; set; }
    public SitePreviewContext? PreviewContext { get; set; }
    public string PreviewServerHost { get; set; } = string.Empty;
    public string RecentSettingsFilesNames { get; set; } = string.Empty;
    public SiteChooserContext? SettingsFileChooser { get; set; }
    public bool ShowSettingsFileChooser { get; set; }
    public string SiteName { get; set; }
    public string SiteUrl { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public ProgramUpdateMessageContext UpdateMessageContext { get; set; }


    public async Task CheckForProgramUpdate(string currentDateVersion)
    {
        var settings = SiteViewerGuiSettingTools.ReadSettings();

        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {settings.ProgramUpdateDirectory}");

        if (string.IsNullOrEmpty(currentDateVersion)) return;

        var (dateString, setupFile) = ProgramInfoTools.LatestInstaller(
            settings.ProgramUpdateDirectory,
            "PointlessWaymarksSiteViewerSetup");

        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {settings.ProgramUpdateDirectory}, Installer Date Found {dateString ?? string.Empty}, Setup File Found {setupFile?.FullName ?? string.Empty}");

        if (string.IsNullOrWhiteSpace(dateString) || setupFile is not { Exists: true }) return;

        if (string.Compare(currentDateVersion, dateString, StringComparison.OrdinalIgnoreCase) >= 0) return;

        await UpdateMessageContext.LoadData(currentDateVersion, dateString, setupFile);
    }

    private async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        ShowSettingsFileChooser = false;

        if (string.IsNullOrWhiteSpace(LocalFolder)) LocalFolder = Environment.CurrentDirectory;

        if (string.IsNullOrWhiteSpace(SiteUrl) || string.IsNullOrWhiteSpace(SiteName))
        {
            var possibleFile = Directory.EnumerateFiles(LocalFolder, "index.htm*").MinBy(x => x.Length);

            if (!string.IsNullOrWhiteSpace(possibleFile))
            {
                var urlFound = !string.IsNullOrWhiteSpace(SiteUrl);
                var siteNameFound = !string.IsNullOrWhiteSpace(SiteName);

                foreach (var loopLine in File.ReadLines(possibleFile))
                {
                    if (!urlFound)
                    {
                        var urlString = Regex
                            .Match(loopLine, "<meta property=\"og:url\" content=\"(?<contentUrl>.*)\"",
                                RegexOptions.IgnoreCase).Groups["contentUrl"].Value;

                        if (!string.IsNullOrWhiteSpace(urlString))
                        {
                            urlFound = true;
                            SiteUrl = new Uri(urlString).Host;
                        }
                    }

                    if (!siteNameFound)
                    {
                        var siteNameString = Regex.Match(loopLine,
                            "<meta property=\"og:site_name\" content=\"(?<contentUrl>.*)\"",
                            RegexOptions.IgnoreCase).Groups["contentUrl"].Value;

                        if (!string.IsNullOrWhiteSpace(siteNameString))
                        {
                            siteNameFound = true;
                            SiteName = siteNameString;
                        }
                    }

                    if (urlFound && siteNameFound) break;

                    if (loopLine.Contains("</head>", StringComparison.OrdinalIgnoreCase)) break;
                }
            }
        }

        var freePort = PreviewServer.FreeTcpPort();

        var server = PreviewServer.CreateHostBuilder(
            SiteUrl, LocalFolder, freePort).Build();

        StatusContext.RunFireAndForgetWithToastOnError(async () =>
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            await server.RunAsync();
        });

        PreviewServerHost = $"localhost:{freePort}";

        PreviewContext = new SitePreviewContext(SiteUrl,
            LocalFolder,
            SiteName, PreviewServerHost, StatusContext);

        InfoTitle += $" - {PreviewContext.SiteMappingNote}";

        PreviewContext.NewWindowRequestedAction = NewWindowRequestedAction;
    }

    private async Task NewAdditionalTab(string requestedAddress)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newTab = await NewTabFromAddress(requestedAddress);

        ViewTabs.AddToSource(newTab);
        ViewTabs.SelectedItem = newTab;
    }

    private async Task<TabItem> NewTabFromAddress(string requestedAddress)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newTabContext = new SitePreviewContext(SiteUrl,
            LocalFolder,
            SiteName, PreviewServerHost, StatusContext, requestedAddress);

        newTabContext.NewWindowRequestedAction = NewWindowRequestedAction;

        var newSitePreviewControl = new SitePreviewControl
        {
            DataContext = newTabContext
        };

        var myBinding = new Binding
        {
            Source = newTabContext,
            Path = new PropertyPath("CurrentDocumentTitle"),
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };

        var newTab = new TabItem
        {
            Content = newSitePreviewControl
        };

        BindingOperations.SetBinding(newTab, HeaderedContentControl.HeaderProperty, myBinding);

        return newTab;
    }

    public object NewTabFunction()
    {
        return NewTabFromAddress($"http://{SiteUrl}").Result;
    }

    private async void NewWindowRequestedAction(CoreWebView2NewWindowRequestedEventArgs navigationArgs)
    {
        if (string.IsNullOrWhiteSpace(navigationArgs.Uri)) return;

        if (navigationArgs.Uri.Contains(SiteUrl) || navigationArgs.Uri.Contains(PreviewServerHost))
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            navigationArgs.Handled = true;

            var uri = navigationArgs.Uri;

            StatusContext.RunFireAndForgetBlockingTask(async () => await NewAdditionalTab(uri));
        }
    }

    private async Task SettingsFileChooserOnDirectoryUpdated(
        (string userInput, List<string> fileList) settingReturn)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(settingReturn.userInput))
        {
            StatusContext.ToastError("Error with Directory? No name?");
            return;
        }

        var directoryInfo = new DirectoryInfo(settingReturn.userInput);

        if (!directoryInfo.Exists)
        {
            StatusContext.ToastError("Error with Directory? Does not exist?");
            return;
        }

        StatusContext.Progress($"Using {directoryInfo.FullName}");

        var fileList = settingReturn.fileList;

        if (fileList.Contains(directoryInfo.FullName))
            fileList.Remove(directoryInfo.FullName);

        fileList = new List<string> { directoryInfo.FullName }.Concat(fileList).ToList();

        if (fileList.Count > 10)
            fileList = fileList.Take(10).ToList();

        RecentSettingsFilesNames = string.Join("|", fileList);

        LocalFolder = settingReturn.userInput;

        StatusContext.RunFireAndForgetBlockingTask(LoadData);
    }

    private async Task SettingsFileChooserOnSettingsFileUpdated(
        (string userInput, List<string> fileList) settingReturn)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(settingReturn.userInput))
        {
            StatusContext.ToastError("Error - Nothing Selected?");
            return;
        }

        UserSettingsUtilities.SettingsFileFullName = settingReturn.userInput;

        StatusContext.Progress($"Using {UserSettingsUtilities.SettingsFileFullName}");

        var fileList = settingReturn.fileList;

        if (fileList.Contains(UserSettingsUtilities.SettingsFileFullName))
            fileList.Remove(UserSettingsUtilities.SettingsFileFullName);

        fileList = new List<string> { UserSettingsUtilities.SettingsFileFullName }.Concat(fileList).ToList();

        if (fileList.Count > 10)
            fileList = fileList.Take(10).ToList();

        RecentSettingsFilesNames = string.Join("|", fileList);

        LocalFolder = UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName;
        SiteUrl = new Uri(UserSettingsSingleton.CurrentSettings().SiteUrl()).Host;
        SiteName = UserSettingsSingleton.CurrentSettings().SiteName;

        StatusContext.RunFireAndForgetBlockingTask(LoadData);
    }


    private void SettingsFileChooserOnSiteDirectoryChosenEvent(object? sender,
        (string userString, List<string> recentFiles) e)
    {
        StatusContext.RunFireAndForgetBlockingTask(async () => await SettingsFileChooserOnDirectoryUpdated(e));
    }

    private void SiteChooserOnSiteSettingsFileChosenEvent(object? sender,
        (string userString, List<string> recentFiles) e)
    {
        StatusContext.RunFireAndForgetBlockingTask(async () => await SettingsFileChooserOnSettingsFileUpdated(e));
    }
}