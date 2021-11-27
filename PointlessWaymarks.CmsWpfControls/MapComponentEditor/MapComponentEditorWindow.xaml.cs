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

    public MapComponentEditorWindow(MapComponent toLoad)
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();

        StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            MapComponentContent = await MapComponentEditorContext.CreateInstance(StatusContext, toLoad);

            MapComponentContent.RequestContentEditorWindowClose += (_, _) => { Dispatcher?.Invoke(Close); };
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, MapComponentContent);

            await ThreadSwitcher.ResumeForegroundAsync();
            DataContext = this;
        });
    }
}