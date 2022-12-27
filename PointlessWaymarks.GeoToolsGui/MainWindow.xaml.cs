#region

using System.Reflection;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.GeoToolsGui.Controls;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

#endregion

namespace PointlessWaymarks.GeoToolsGui;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
[ObservableObject]
public partial class MainWindow : Window
{
    [ObservableProperty] private ConnectBasedGeoTaggerContext? _connectGeoTaggerContext;
    [ObservableProperty] private FeatureIntersectTaggerContext? _featureIntersectContext;
    [ObservableProperty] private FileBasedGeoTaggerContext? _fileGeoTaggerContext;
    [ObservableProperty] private ConnectDownloadContext? _garminConnectDownloadContext;
    [ObservableProperty] private string _infoTitle;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private WindowIconStatus _windowStatus;
    [ObservableProperty] private HelpDisplayContext _softwareComponentsHelpContext;

    public MainWindow()
    {
        InitializeComponent();

        JotServices.Tracker.Track(this);

        if (Width < 900) Width = 900;
        if (Height < 650) Height = 650;

        WindowInitialPositionHelpers.EnsureWindowIsVisible(this);

        _infoTitle = ProgramInfoTools.StandardAppInformationString(Assembly.GetExecutingAssembly(),
            "Pointless Waymarks GeoTools Beta").humanTitleString;

        DataContext = this;

        _statusContext = new StatusControlContext();

        _windowStatus = new WindowIconStatus();

        StatusContext.RunBlockingTask(LoadData);
    }

    private async Task LoadData()
    {
        FileGeoTaggerContext = await FileBasedGeoTaggerContext.CreateInstance(StatusContext, WindowStatus);
        ConnectGeoTaggerContext = await ConnectBasedGeoTaggerContext.CreateInstance(StatusContext, WindowStatus);
        FeatureIntersectContext = await FeatureIntersectTaggerContext.CreateInstance(StatusContext, WindowStatus);
        GarminConnectDownloadContext = await ConnectDownloadContext.CreateInstance(StatusContext, WindowStatus);
        SoftwareComponentsHelpContext = new HelpDisplayContext(new List<string> { SoftwareUsedHelpMarkdown.HelpBlock });
    }
}