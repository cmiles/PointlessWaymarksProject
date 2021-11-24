#nullable enable
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

[ObservableObject]
public partial class MapElementListLineItem :  IMapElementListItem
{
    [ObservableProperty] private LineContent? _dbEntry;
    [ObservableProperty] private bool _inInitialView;
    [ObservableProperty] private bool _isFeaturedElement;
    [ObservableProperty] private bool _showInitialDetails;
    [ObservableProperty] private string _smallImageUrl = string.Empty;

    public Guid? ContentId()
    {
        return DbEntry?.ContentId;
    }
}