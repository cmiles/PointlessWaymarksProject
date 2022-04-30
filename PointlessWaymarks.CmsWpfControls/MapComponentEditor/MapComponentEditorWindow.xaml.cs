using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

/// <summary>
///     Interaction logic for MapComponentEditorWindow.xaml
/// </summary>
[ObservableObject]
public partial class MapComponentEditorWindow
{
    [ObservableProperty] private WindowAccidentalClosureHelper _accidentalCloserHelper;
    [ObservableProperty] private MapComponentEditorContext _mapComponentContent;
    [ObservableProperty] private StatusControlContext _statusContext;

    private MapComponentEditorWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
    }

    public static async Task<MapComponentEditorWindow> CreateInstance(MapComponent toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new MapComponentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.MapComponentContent = await MapComponentEditorContext.CreateInstance(window.StatusContext, toLoad);

        window.MapComponentContent.RequestContentEditorWindowClose += (_, _) =>
        {
            window.Dispatcher?.Invoke(window.Close);
        };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.MapComponentContent);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}