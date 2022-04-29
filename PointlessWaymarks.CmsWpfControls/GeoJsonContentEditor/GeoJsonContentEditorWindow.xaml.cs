using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.GeoJsonContentEditor;

/// <summary>
///     Interaction logic for GeoJsonContentEditorWindow.xaml
/// </summary>
[ObservableObject]
public partial class GeoJsonContentEditorWindow
{
    [ObservableProperty] private WindowAccidentalClosureHelper _accidentalCloserHelper;
    [ObservableProperty] private GeoJsonContentEditorContext _geoJsonContent;
    [ObservableProperty] private StatusControlContext _statusContext;

    private GeoJsonContentEditorWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
    }

    public static async Task<GeoJsonContentEditorWindow> CreateInstance(GeoJsonContent toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new GeoJsonContentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.GeoJsonContent = await GeoJsonContentEditorContext.CreateInstance(window.StatusContext, toLoad);

        window.GeoJsonContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.GeoJsonContent);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}