﻿using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.NoteContentEditor;

/// <summary>
///     Interaction logic for NoteContentEditorWindow.xaml
/// </summary>
[ObservableObject]
#pragma warning disable MVVMTK0033
public partial class NoteContentEditorWindow
#pragma warning restore MVVMTK0033
{
    [ObservableProperty] private WindowAccidentalClosureHelper? _accidentalCloserHelper;
    [ObservableProperty] private NoteContentEditorContext? _noteContent;
    [ObservableProperty] private StatusControlContext _statusContext;

    /// <summary>
    /// DO NOT USE - Use CreateInstance instead - using the constructor directly will result in
    /// core functionality being uninitialized.
    /// </summary>
    private NoteContentEditorWindow()
    {
        InitializeComponent();
        _statusContext = new StatusControlContext();
        DataContext = this;
    }

    /// <summary>
    /// Creates a new instance - this method can be called from any thread and will
    /// switch to the UI thread as needed. Does not show the window - consider using
    /// PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<NoteContentEditorWindow> CreateInstance(NoteContent? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new NoteContentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.NoteContent = await NoteContentEditorContext.CreateInstance(window.StatusContext, toLoad);

        window.NoteContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.NoteContent);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}