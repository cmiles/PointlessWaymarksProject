using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsData.Html.MapComponentData
{
    public static class MapData
    {
        public static async Task WriteLocalJsonData(MapComponentDto dto)
        {
            var mapDtoJson = System.Text.Json.JsonSerializer.Serialize(dto);

            var settings = UserSettingsSingleton.CurrentSettings();

            var dataFileInfo = new FileInfo($"{settings.LocalSiteMapComponentDataFile(dto.Map.ContentId)}");

            if (dataFileInfo.Exists)
            {
                dataFileInfo.Delete();
                dataFileInfo.Refresh();
            }

            await FileManagement.WriteAllTextToFileAndLogAsync(dataFileInfo.FullName, mapDtoJson);
        }
    }
}
