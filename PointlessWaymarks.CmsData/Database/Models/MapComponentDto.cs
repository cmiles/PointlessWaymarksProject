using System.Collections.Generic;

namespace PointlessWaymarks.CmsData.Database.Models
{
    public record MapComponentDto(MapComponent Map, List<MapElement> Elements);

    public record HistoricMapComponentDto(HistoricMapComponent Map, List<HistoricMapElement> Elements);
}