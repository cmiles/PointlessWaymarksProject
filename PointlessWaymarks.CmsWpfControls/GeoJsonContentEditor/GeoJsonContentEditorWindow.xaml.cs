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
    [ObservableProperty] private GeoJsonContentEditorContext _geoJsonContent;
    [ObservableProperty] private StatusControlContext _statusContext;

    public GeoJsonContentEditorWindow(GeoJsonContent toLoad)
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();

        StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            GeoJsonContent = await GeoJsonContentEditorContext.CreateInstance(StatusContext, toLoad);

            GeoJsonContent.RequestContentEditorWindowClose += (_, _) => { Dispatcher?.Invoke(Close); };
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, GeoJsonContent);

            await ThreadSwitcher.ResumeForegroundAsync();
            DataContext = this;
        });
    }

    public WindowAccidentalClosureHelper AccidentalCloserHelper { get; set; }
}