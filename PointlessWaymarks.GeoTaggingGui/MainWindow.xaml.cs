using System.Reflection;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.GeoTaggingGui;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
[ObservableObject]
public partial class MainWindow : Window
{
    [ObservableProperty] private ConnectBasedTaggerContext? _connectTaggerContext;
    [ObservableProperty] private DirectoryBasedTaggerContext? _directoryTaggerContext;
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

        // ReSharper disable once ConditionIsAlwaysTrueOrFalse for generated ThisAssembly.Git.IsDirty
        // ReSharper disable once HeuristicUnreachableCode
        //.Git IsDirty can change at runtime
#pragma warning disable CS0162
        _infoTitle =
            $"Pointless Waymarks GeoTagger - Built On {GetBuildDate(Assembly.GetEntryAssembly())} - Commit {ThisAssembly.Git.Commit} {(ThisAssembly.Git.IsDirty ? "(Has Local Changes)" : string.Empty)}";
#pragma warning restore CS0162

        DataContext = this;

        _statusContext = new StatusControlContext();

        _windowStatus = new WindowIconStatus();

        StatusContext.RunBlockingTask(LoadData);
    }

    private static DateTime? GetBuildDate(Assembly assembly)
    {
        var attribute = assembly.GetCustomAttribute<BuildDateAttribute>();
        return attribute?.DateTime;
    }

    private async System.Threading.Tasks.Task LoadData()
    {
        DirectoryTaggerContext = await DirectoryBasedTaggerContext.CreateInstance(StatusContext, WindowStatus);
        ConnectTaggerContext = await ConnectBasedTaggerContext.CreateInstance(StatusContext, WindowStatus);
    }
}