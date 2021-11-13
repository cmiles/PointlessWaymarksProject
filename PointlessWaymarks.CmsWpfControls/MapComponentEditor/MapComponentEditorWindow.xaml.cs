﻿using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

/// <summary>
///     Interaction logic for MapComponentEditorWindow.xaml
/// </summary>
public partial class MapComponentEditorWindow
{
    private MapComponentEditorContext _mapComponentContent;
    private StatusControlContext _statusContext;

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

    public WindowAccidentalClosureHelper AccidentalCloserHelper { get; set; }

    public MapComponentEditorContext MapComponentContent
    {
        get => _mapComponentContent;
        set
        {
            if (Equals(value, _mapComponentContent)) return;
            _mapComponentContent = value;
            OnPropertyChanged();
        }
    }

    public StatusControlContext StatusContext
    {
        get => _statusContext;
        set
        {
            if (Equals(value, _statusContext)) return;
            _statusContext = value;
            OnPropertyChanged();
        }
    }

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler PropertyChanged;
}