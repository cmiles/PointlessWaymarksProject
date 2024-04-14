using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor
{
    [NotifyPropertyChanged]
    public class MapElementSettings
    {
        public bool InInitialView { get; set; } = true;
        public bool IsFeaturedElement { get; set; }
        public bool ShowInitialDetails { get; set; }
        
        public static MapElementSettings CreateInstance(MapElement? mapElement)
        {
            var toReturn = new MapElementSettings();

            if (mapElement != null)
            {
                toReturn.InInitialView = mapElement.IncludeInDefaultView;
                toReturn.IsFeaturedElement = mapElement.IsFeaturedElement;
                toReturn.ShowInitialDetails = mapElement.ShowDetailsDefault;
            }

            return toReturn;
        }
    }
}
