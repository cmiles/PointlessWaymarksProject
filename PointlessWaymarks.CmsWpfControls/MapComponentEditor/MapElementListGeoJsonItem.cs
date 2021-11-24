#nullable enable
using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

[ObservableObject]
public partial class MapElementListGeoJsonItem : IMapElementListItem
{
    [ObservableProperty] private GeoJsonContent? _dbEntry;
    [ObservableProperty] private bool _inInitialView;
    [ObservableProperty] private bool _isFeaturedElement;
    [ObservableProperty] private bool _showInitialDetails = true;
    [ObservableProperty] private string _smallImageUrl = string.Empty;

    public Guid? ContentId()
    {
        return DbEntry?.ContentId;
    }
}