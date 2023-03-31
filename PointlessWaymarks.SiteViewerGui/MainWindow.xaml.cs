﻿using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Hosting;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsWpfControls.SitePreview;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.ProgramUpdateMessage;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;

namespace PointlessWaymarks.SiteViewerGui;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
[ObservableObject]
#pragma warning disable MVVMTK0033 - Main Window Exception
public partial class MainWindow
#pragma warning restore MVVMTK0033
{
    [ObservableProperty] private string _infoTitle;
    [ObservableProperty] private string _initialPage;
    [ObservableProperty] private string _localFolder;
    [ObservableProperty] private SitePreviewContext? _previewContext;
    [ObservableProperty] private string _recentSettingsFilesNames = string.Empty;
    [ObservableProperty] private ProjectChooserContext? _settingsFileChooser;
    [ObservableProperty] private bool _showSettingsFileChooser;
    [ObservableProperty] private string _siteName;
    [ObservableProperty] private string _siteUrl;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private ProgramUpdateMessageContext _updateMessageContext;


    public MainWindow(string? siteUrl, string? localFolder, string? siteName, string? initialPage)
    {
        InitializeComponent();

        JotServices.Tracker.Configure<MainWindow>().Properties(x => new { x.RecentSettingsFilesNames });

        JotServices.Tracker.Track(this);

        if (Width < 900) Width = 900;
        if (Height < 650) Height = 650;

        WindowInitialPositionHelpers.EnsureWindowIsVisible(this);

        var versionInfo =
            ProgramInfoTools.StandardAppInformationString(Assembly.GetExecutingAssembly(),
                "Pointless Waymarks CMS Beta");

        _infoTitle = versionInfo.humanTitleString;

        var currentDateVersion = versionInfo.dateVersion;

        _statusContext = new StatusControlContext { BlockUi = false };

        _siteUrl = siteUrl ?? string.Empty;
        _initialPage = initialPage ?? string.Empty;
        _localFolder = localFolder ?? string.Empty;
        _siteName = siteName ?? string.Empty;

        DataContext = this;

        _updateMessageContext = new ProgramUpdateMessageContext();

        if (string.IsNullOrWhiteSpace(localFolder))
        {
            ShowSettingsFileChooser = true;

            StatusContext.RunFireAndForgetBlockingTask(async () =>
            {
                await CheckForProgramUpdate(currentDateVersion);

                SettingsFileChooser =
                    await ProjectChooserContext.CreateInstance(StatusContext, RecentSettingsFilesNames);

                SettingsFileChooser.SettingsFileUpdated += SettingsFileChooserOnSettingsFileUpdatedEvent;
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

    public async Task CheckForProgramUpdate(string currentDateVersion)
    {
        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {SiteViewerGuiAppSettings.Default.ProgramUpdateLocation}");

        if (string.IsNullOrEmpty(currentDateVersion)) return;

        var (dateString, setupFile) = ProgramInfoTools.LatestInstaller(
            SiteViewerGuiAppSettings.Default.ProgramUpdateLocation,
            "PointlessWaymarksSiteViewerSetup");

        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {SiteViewerGuiAppSettings.Default.ProgramUpdateLocation}, Installer Date Found {dateString ?? string.Empty}, Setup File Found {setupFile?.FullName ?? string.Empty}");

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
                            .Match(loopLine, "<meta property=\"og:url\" content=\"(?<contentUrl>.*)\">",
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
                            "<meta property=\"og:site_name\" content=\"(?<contentUrl>.*)\">",
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

        PreviewContext = new SitePreviewContext(SiteUrl,
            LocalFolder,
            SiteName, $"localhost:{freePort}", StatusContext);
    }

    private async Task SettingsFileChooserOnSettingsFileUpdated(
        (bool isNew, string userInput, List<string> fileList) settingReturn)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(settingReturn.userInput))
        {
            StatusContext.ToastError("Error with Settings File? No name?");
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

    private void SettingsFileChooserOnSettingsFileUpdatedEvent(object? sender,
        (bool isNew, string userString, List<string> recentFiles) e)
    {
        StatusContext.RunFireAndForgetBlockingTask(async () => await SettingsFileChooserOnSettingsFileUpdated(e));
    }
}