#nullable enable
using System.Collections.Generic;

namespace PointlessWaymarksCmsData.Database.Models
{
    public record MapComponentDto(MapComponent Map, List<MapElement> Elements);
    public record HistoricMapComponentDto(HistoricMapComponent Map, List<HistoricMapElement> Elements);
}