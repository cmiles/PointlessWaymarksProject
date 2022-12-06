using System.Reflection;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.GeoToolsGui.Controls;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

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
    [ObservableProperty] private string _infoTitle;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private WindowIconStatus _windowStatus;

    public MainWindow()
    {
        InitializeComponent();

        //JotServices.Tracker.Configure<MainWindow>().Properties(x => new { x.RecentSettingsFilesNames });

        JotServices.Tracker.Track(this);

        if (Width < 900) Width = 900;
        if (Height < 650) Height = 650;

        WindowInitialPositionHelpers.EnsureWindowIsVisible(this);

        _infoTitle = ProgramInfoTools.StandardAppInformationString(Assembly.GetExecutingAssembly(),
            "Pointless Waymarks GeoTagger");
        ;

        DataContext = this;

        _statusContext = new StatusControlContext();

        _windowStatus = new WindowIconStatus();

        StatusContext.RunBlockingTask(LoadData);
    }

    private async System.Threading.Tasks.Task LoadData()
    {
        FileGeoTaggerContext = await FileBasedGeoTaggerContext.CreateInstance(StatusContext, WindowStatus);
        ConnectGeoTaggerContext = await ConnectBasedGeoTaggerContext.CreateInstance(StatusContext, WindowStatus);
        FeatureIntersectContext = await FeatureIntersectTaggerContext.CreateInstance(StatusContext, WindowStatus);
    }
}