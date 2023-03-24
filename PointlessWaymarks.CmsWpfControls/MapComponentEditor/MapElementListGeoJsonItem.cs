#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

public partial class MapElementListGeoJsonItem : ObservableObject, IMapElementListItem
{
    [ObservableProperty] private GeoJsonContent? _dbEntry;
    [ObservableProperty] private bool _inInitialView;
    [ObservableProperty] private bool _isFeaturedElement;
    [ObservableProperty] private bool _showInitialDetails = true;
    [ObservableProperty] private string _smallImageUrl = string.Empty;
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _elementType = "geojson";

    public Guid? ContentId()
    {
        return DbEntry?.ContentId;
    }
}