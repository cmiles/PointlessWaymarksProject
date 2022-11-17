using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.LoggingTools;
using PointlessWaymarks.WpfCommon.FileList;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.FeatureIntersectionTaggingGui;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
[ObservableObject]
public partial class MainWindow
{
    [ObservableProperty] private FileListViewModel _filesToTagFileList;
    [ObservableProperty] private FeatureIntersectionFilesToTagSettings _filesToTagSettings;
    [ObservableProperty] private string _infoTitle;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private WindowIconStatus _windowStatus;

    public MainWindow()
    {
        InitializeComponent();

        JotServices.Tracker.Track(this);

        if (Width < 900) Width = 900;
        if (Height < 650) Height = 650;

        WindowInitialPositionHelpers.EnsureWindowIsVisible(this);

        // ReSharper disable once ConditionIsAlwaysTrueOrFalse for generated ThisAssembly.Git.IsDirty
        // ReSharper disable once HeuristicUnreachableCode
        //.Git IsDirty can change at runtime
#pragma warning disable CS0162
        _infoTitle = WindowTitleTools.StandardAppInformationString(Assembly.GetExecutingAssembly(),
            "Pointless Waymarks Feature Intersection Tagger");
        ;
#pragma warning restore CS0162

        DataContext = this;

        _statusContext = new StatusControlContext();

        _windowStatus = new WindowIconStatus();

        FilesToTagSettings = new FeatureIntersectionFilesToTagSettings();

        StatusContext.RunBlockingTask(LoadData);
    }

    private async Task LoadData()
    {
        FilesToTagFileList =
            await FileListViewModel.CreateInstance(StatusContext, FilesToTagSettings,
                new List<ContextMenuItemData>());
    }
}