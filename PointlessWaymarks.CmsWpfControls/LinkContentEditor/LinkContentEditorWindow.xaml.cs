using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.LinkContentEditor;

[ObservableObject]
public partial class LinkContentEditorWindow
{
    [ObservableProperty] private LinkContentEditorContext _editorContent;
    [ObservableProperty] private StatusControlContext _statusContext;

    public LinkContentEditorWindow(LinkContent toLoad, bool extractDataFromLink = false)
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();

        StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            EditorContent =
                await LinkContentEditorContext.CreateInstance(StatusContext, toLoad, extractDataFromLink);

            EditorContent.RequestContentEditorWindowClose += (_, _) => { Dispatcher?.Invoke(Close); };
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, EditorContent);

            await ThreadSwitcher.ResumeForegroundAsync();
            DataContext = this;
        });
    }

    public WindowAccidentalClosureHelper AccidentalCloserHelper { get; set; }
}