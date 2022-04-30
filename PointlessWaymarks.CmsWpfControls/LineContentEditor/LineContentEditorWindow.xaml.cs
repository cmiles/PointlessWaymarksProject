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

    private LineContentEditorWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
    }

    public static async Task<LineContentEditorWindow> CreateInstance(LineContent toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new LineContentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.LineContent = await LineContentEditorContext.CreateInstance(window.StatusContext, toLoad);

        window.LineContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.LineContent);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}