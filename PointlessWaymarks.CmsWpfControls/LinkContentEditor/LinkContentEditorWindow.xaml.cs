using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.LinkContentEditor;

[ObservableObject]
public partial class LinkContentEditorWindow
{
    [ObservableProperty] private WindowAccidentalClosureHelper _accidentalCloserHelper;
    [ObservableProperty] private LinkContentEditorContext _linkContent;
    [ObservableProperty] private StatusControlContext _statusContext;

    private LinkContentEditorWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
    }

    public static async Task<LinkContentEditorWindow> CreateInstance(LinkContent toLoad, bool extractDataFromLink = false)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new LinkContentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.LinkContent = await LinkContentEditorContext.CreateInstance(window.StatusContext, toLoad, extractDataFromLink);

        window.LinkContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.LinkContent);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}