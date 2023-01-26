#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

public partial class MapElementListLineItem : ObservableObject, IMapElementListItem
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