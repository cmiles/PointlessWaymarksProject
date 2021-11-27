using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.NoteContentEditor;

/// <summary>
///     Interaction logic for NoteContentEditorWindow.xaml
/// </summary>
[ObservableObject]
public partial class NoteContentEditorWindow
{
    [ObservableProperty] private WindowAccidentalClosureHelper _accidentalCloserHelper;
    [ObservableProperty] private NoteContentEditorContext _noteContent;
    [ObservableProperty] private StatusControlContext _statusContext;

    public NoteContentEditorWindow(NoteContent toLoad)
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();

        StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            NoteContent = await NoteContentEditorContext.CreateInstance(StatusContext, toLoad);

            NoteContent.RequestContentEditorWindowClose += (_, _) => { Dispatcher?.Invoke(Close); };
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, NoteContent);

            await ThreadSwitcher.ResumeForegroundAsync();
            DataContext = this;
        });
    }
}