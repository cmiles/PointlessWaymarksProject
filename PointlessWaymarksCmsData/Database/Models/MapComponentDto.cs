#nullable enable
using System.Collections.Generic;

namespace PointlessWaymarksCmsData.Database.Models
{
    public record MapComponentDto(MapComponent Map, List<MapComponentElement> Elements);
    public record HistoricMapComponentDto(HistoricMapComponent Map, List<HistoricMapComponentElement> Elements);
}