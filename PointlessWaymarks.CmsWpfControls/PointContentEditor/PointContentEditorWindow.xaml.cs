using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.PointContentEditor;

/// <summary>
///     Interaction logic for PointContentEditorWindow.xaml
/// </summary>
[ObservableObject]
public partial class PointContentEditorWindow
{
    [ObservableProperty] private WindowAccidentalClosureHelper _accidentalCloserHelper;
    [ObservableProperty] private PointContentEditorContext _pointContent;
    [ObservableProperty] private StatusControlContext _statusContext;

    public PointContentEditorWindow(PointContent toLoad)
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();

        StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            PointContent = await PointContentEditorContext.CreateInstance(StatusContext, toLoad);

            PointContent.RequestContentEditorWindowClose += (_, _) => { Dispatcher?.Invoke(Close); };
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, PointContent);

            await ThreadSwitcher.ResumeForegroundAsync();
            DataContext = this;
        });
    }
}