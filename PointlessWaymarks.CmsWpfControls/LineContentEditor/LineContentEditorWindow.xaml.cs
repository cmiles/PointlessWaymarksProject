using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.LineContentEditor;

/// <summary>
///     Interaction logic for LineContentEditorWindow.xaml
/// </summary>
[ObservableObject]
public partial class LineContentEditorWindow
{
    [ObservableProperty] private WindowAccidentalClosureHelper _accidentalCloserHelper;
    [ObservableProperty] private LineContentEditorContext _lineContent;
    [ObservableProperty] private StatusControlContext _statusContext;

    public LineContentEditorWindow(LineContent toLoad)
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();

        StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            LineContent = await LineContentEditorContext.CreateInstance(StatusContext, toLoad);

            LineContent.RequestContentEditorWindowClose += (_, _) => { Dispatcher?.Invoke(Close); };
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, LineContent);

            await ThreadSwitcher.ResumeForegroundAsync();
            DataContext = this;
        });
    }
}