using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsWpfControls.MapIconList
{
    public static class MapIconDefaultLibrary
    {
        public static List<MapIcon> DefaultIcons()
        {
            var frozenNow = DateTime.Now;

            return
            [
                new MapIcon
                {
                    ContentId = Guid.NewGuid(),
                    IconName = "campfire",
                    IconSource = "https://github.com/nationalparkservice/symbol-library/blob/gh-pages/src/standalone/campfire-black-30.svg",
                    IconSvg = """
                              <svg version="1.1" id="Icons" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" x="0px" y="0px"
                              	 width="30px" height="30px" viewBox="0 0 30 30" style="enable-background:new 0 0 30 30;" xml:space="preserve">
                              <g>
                              	<path  d="M10,15.2c-0.6-0.7-4.4-5.1-0.5-8.9C12.7,3,13.1,1.3,12,0c0,0,8.3,2.4,3.5,10.2c-0.8,1.3-2.6,2.5-1.5,5.8L10,15.2z"/>
                              	<path  d="M16,16c0-1.2,0.1-3.1,1.7-4.7c1.3-1.5,1.8-3.8,1.9-4.6c0,0,4.3,2.4,0,8.4L16,16z"/>
                              	<polygon  points="0,21 3,21 3,26 4,27 26,27 27,26 27,21 30,21 30,28 28,30 2,30 0,28 	"/>
                              	<polygon  points="6,16 24,22 24,26 6,20 	"/>
                              	<polygon  points="18,18 24,20 24,16 	"/>
                              	<polygon  points="6,26 11.7,24 6,22 	"/>
                              </g>
                              </svg>
                              """,
                    LastUpdatedBy = "Default Icon Library",
                    LastUpdatedOn = frozenNow,
                    ContentVersion = Db.ContentVersionDateTime()
                },
                new MapIcon
                {
                    ContentId = Guid.NewGuid(),
                    IconName = "campground",
                    IconSource = "https://github.com/nationalparkservice/symbol-library/blob/gh-pages/src/standalone/campground-black-30.svg",
                    IconSvg = """
                              <svg version="1.1" id="Icons" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" x="0px" y="0px"
                              	 width="30px" height="30px" viewBox="0 0 30 30" style="enable-background:new 0 0 30 30;" xml:space="preserve">
                              <g>
                              	<path d="M23,28L13,8L3,28H0v2h26v-2H23z M7,28l6-11.3L19,28H7z"/>
                              </g>
                              <polygon points="17,3.9 15.5,6.9 25,26 28,26 "/>
                              <polygon points="20.7,0 19.4,2.7 29,22 30,22 30,18.1 "/>
                              </svg>
                              """,
                    LastUpdatedBy = "Default Icon Library",
                    LastUpdatedOn = frozenNow,
                    ContentVersion = Db.ContentVersionDateTime()
                },
                new MapIcon
                {
                    ContentId = Guid.NewGuid(),
                    IconName = "campsite",
                    IconSource = "https://github.com/nationalparkservice/symbol-library/blob/gh-pages/src/standalone/campsite-black-30.svg",
                    IconSvg = """
                              <svg version="1.1" id="Layer_1" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" x="0px" y="0px"
                              	 width="30px" height="30px" viewBox="0 0 30 30" style="enable-background:new 0 0 30 30;" xml:space="preserve">
                              <g>
                              	<path  d="M8,26l7.1-12.9L22,26H8z M27,26L15.1,2L3,26H0v2h30v-2H27z"/>
                              </g>
                              </svg>
                              """,
                    LastUpdatedBy = "Default Icon Library",
                    LastUpdatedOn = frozenNow,
                    ContentVersion = Db.ContentVersionDateTime()
                },
                new MapIcon
                {
                    ContentId = Guid.NewGuid(),
                    IconName = "dam",
                    IconSource = "https://github.com/nationalparkservice/symbol-library/blob/gh-pages/src/standalone/dam-black-30.svg",
                    IconSvg = """
                              <svg version="1.1" id="Layer_1" xmlns:sketch="http://www.bohemiancoding.com/sketch/ns"
                              	 xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" x="0px" y="0px" width="30px" height="30px"
                              	 viewBox="0 0 30 30" style="enable-background:new 0 0 30 30;" xml:space="preserve">
                              <rect  y="7" width="10" height="2"/>
                              <rect  y="10" width="10" height="2"/>
                              <rect  y="13" width="10" height="2"/>
                              <rect  y="16" width="10" height="2"/>
                              <rect  y="19" width="10" height="2"/>
                              <rect  y="22" width="10" height="2"/>
                              <rect  y="25" width="10" height="2"/>
                              <polygon  points="11,5 13,5 18,27 11,27 "/>
                              <g>
                              	<path  d="M30,27c-0.7,0-1.4-0.3-1.9-0.6C27.7,26.2,27.3,26,27,26c-0.2,0-0.4,0.1-0.7,0.3c-0.4,0.3-1,0.7-1.8,0.7s-1.4-0.4-1.8-0.7
                              		C22.4,26.1,22.2,26,22,26c-0.3,0-0.7,0.2-1.1,0.4C20.4,26.7,19.7,27,19,27v-2c0.3,0,0.7-0.2,1.1-0.4c0.6-0.3,1.2-0.6,1.9-0.6
                              		c0.8,0,1.4,0.4,1.8,0.7c0.6,0.4,0.8,0.4,1.4,0c0.4-0.3,1-0.7,1.8-0.7c0.7,0,1.4,0.3,1.9,0.6c0.4,0.2,0.8,0.4,1.1,0.4V27z"/>
                              </g>
                              <g>
                              	<path  d="M30,23c-0.7,0-1.4-0.3-1.9-0.6C27.7,22.2,27.3,22,27,22c-0.2,0-0.4,0.1-0.7,0.3c-0.4,0.3-1,0.7-1.8,0.7s-1.4-0.4-1.8-0.7
                              		C22.4,22.1,22.2,22,22,22c-0.3,0-0.7,0.2-1.1,0.4C20.4,22.7,19.7,23,19,23v-2c0.3,0,0.7-0.2,1.1-0.4c0.6-0.3,1.2-0.6,1.9-0.6
                              		c0.8,0,1.4,0.4,1.8,0.7c0.6,0.4,0.8,0.4,1.4,0c0.4-0.3,1-0.7,1.8-0.7c0.7,0,1.4,0.3,1.9,0.6c0.4,0.2,0.8,0.4,1.1,0.4V23z"/>
                              </g>
                              </svg>
                              """,
                    LastUpdatedBy = "Default Icon Library",
                    LastUpdatedOn = frozenNow,
                    ContentVersion = Db.ContentVersionDateTime()
                },
                new MapIcon
                {
                    ContentId = Guid.NewGuid(),
                    IconName = "parking",
                    IconSource = "https://github.com/nationalparkservice/symbol-library/blob/gh-pages/src/standalone/parking-black-30.svg",
                    IconSvg = """
                              <svg version="1.1" id="Icons" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" x="0px" y="0px"
                              	 width="30px" height="30px" viewBox="0 0 30 30" enable-background="new 0 0 30 30" xml:space="preserve">
                              <g>
                              	<path  d="M16.5,14.6c2.5,0,4.5-2,4.5-4.5s-2-4.5-4.5-4.5L10,5.5l-0.1,9L16.5,14.6L16.5,14.6z M16.4,0C22,0,27,4.6,27,10.2
                              		S21.6,20,16,20h-6v10H4V0H16.4z"/>
                              </g>
                              </svg>
                              """,
                    LastUpdatedBy = "Default Icon Library",
                    LastUpdatedOn = frozenNow,
                    ContentVersion = Db.ContentVersionDateTime()
                },
                new MapIcon
                {
                    ContentId = Guid.NewGuid(),
                    IconName = "peak",
                    IconSource = "https://pictogrammers.com/library/mdi/icon/summit/",
                    IconSvg = """
                              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><title>summit</title><path d="M15,3H17L22,5L17,7V10.17L22,21H2L8,13L11.5,17.7L15,10.17V3Z" /></svg>
                              """,
                    LastUpdatedBy = "Default Icon Library",
                    LastUpdatedOn = frozenNow,
                    ContentVersion = Db.ContentVersionDateTime()
                },
                new MapIcon
                {
                    ContentId = Guid.NewGuid(),
                    IconName = "pit-toilet",
                    IconSource = "https://github.com/nationalparkservice/symbol-library/blob/gh-pages/src/standalone/campsite-black-30.svg",
                    IconSvg = """
                              <svg version="1.1" id="Layer_1" xmlns:sketch="http://www.bohemiancoding.com/sketch/ns"
                              	 xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" x="0px" y="0px" width="30px" height="30px"
                              	 viewBox="0 0 30 30" enable-background="new 0 0 30 30" xml:space="preserve">
                              <path  id="symbol-copy-2" sketch:type="MSShapeGroup" d="M5.2,17.1c1.8,0,3.2-1.4,3.2-3.1c0-1.7-1.4-3.1-3.2-3.1
                              	C3.4,10.8,2,12.2,2,14C2,15.7,3.4,17.1,5.2,17.1z M23,22.5c0.1-0.1,0.3-0.1,0.5,0.1l0.6,0.8c0.1,0.1,0.1,0.3-0.1,0.4l-4.3,3.1
                              	c-0.1,0.1-0.3,0.1-0.5-0.1l-0.6-0.8c-0.1-0.1-0.1-0.3,0.1-0.4L23,22.5z M18.1,18.5l-1.7-3l-0.9,0.6l1.7,3L18.1,18.5z M14.1,11.6
                              	L7.5,0L6.2,0l7,12.2L14.1,11.6z M20.4,17.7c0.4-0.3,0.8-0.5,1.3-0.5l6.2,0c1.2,0,2.2,1,2.2,2.1c0,1.2-1,2.1-2.2,2.1l-5.2,0l-6.6,4.6
                              	c-2.3,1.7-3.7-0.2-3.7-0.2l-5.2-5.1c-0.9-1-0.7-3,0.4-3.8l7.1-5c0.7-0.6,1.8-0.5,2.4,0.2c0.6,0.7,0.5,1.8-0.2,2.3l-4.6,3.3l3.3,3.2
                              	L20.4,17.7z"/>
                              </svg>
                              """,
                    LastUpdatedBy = "Default Icon Library",
                    LastUpdatedOn = frozenNow,
                    ContentVersion = Db.ContentVersionDateTime()
                },
                new MapIcon
                {
                    ContentId = Guid.NewGuid(),
                    IconName = "restrooms",
                    IconSource = "https://github.com/nationalparkservice/symbol-library/blob/gh-pages/src/standalone/campsite-black-30.svg",
                    IconSvg = """
                              <svg version="1.1" id="Icons" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" x="0px" y="0px"
                              	 width="30px" height="30px" viewBox="0 0 30 30" enable-background="new 0 0 30 30" xml:space="preserve">
                              <g>
                              	<rect  x="14" y="3" width="2" height="24"/>
                              	<polygon  points="20,8 29,8 29,9 30,9 30,15 29,17 28,17 28,10 27,10 27,26 26,27 25,27 25,17 24,17 24,27 23,27 22,26 22,10 21,10 
                              		21,17 20,17 19,15 19,9 20,9 	"/>
                              	<polygon  points="8,8 3,8 0,14 0,15 1,15 4,9 4,11 1,19 3,19 3,26 5,27 5,19 6,19 6,27 8,26 8,19 10,19 7,11 7,9 10,15 11,15 11,14 
                              			"/>
                              	<path  d="M6,7H5C4.4,7,4,6.5,4,6V5c0-0.6,0.4-1,1-1h1c0.6,0,1,0.4,1,1v1C7,6.5,6.6,7,6,7z"/>
                              	<path  d="M25,7h-1c-0.6,0-1-0.5-1-1V5c0-0.6,0.4-1,1-1h1c0.6,0,1,0.4,1,1v1C26,6.5,25.6,7,25,7z"/>
                              </g>
                              </svg>
                              """,
                    LastUpdatedBy = "Default Icon Library",
                    LastUpdatedOn = frozenNow,
                    ContentVersion = Db.ContentVersionDateTime()
                },
                new MapIcon
                {
                    ContentId = Guid.NewGuid(),
                    IconName = "trailhead",
                    IconSource = "https://github.com/nationalparkservice/symbol-library/blob/gh-pages/src/standalone/trailhead-black-30.svg",
                    IconSvg = """
                              <svg version="1.1" id="Icons" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" x="0px" y="0px"
                              	 width="30px" height="30px" viewBox="0 0 30 30" enable-background="new 0 0 30 30" xml:space="preserve">
                              <g>
                              	<path  d="M11,4.9c0.1-0.4-0.2-0.4-0.5-0.4L8.5,4C8.1,3.9,7.8,4.1,7.7,4.5L6,11.3c-0.1,0.4,0.1,0.7,0.5,0.8l2,0.5
                              		c0.4,0.1,0.5,0.2,0.6-0.2L11,4.9"/>
                              	<path  d="M20,29.4L20,29.4c0,0.3,0.2,0.6,0.5,0.6s0.5-0.2,0.5-0.5L25,8l0,0c0-0.3-0.2-0.5-0.5-0.5S24,7.7,24,8L20,29.4L20,29.4z"/>
                              	<path  d="M7.1,28c0,0.1,0,0.2,0,0.3c0,0.9,0.7,1.7,1.7,1.7c0.8,0,1.4-0.5,1.6-1.2L13.2,17l2.7,11.7c0.2,0.7,0.8,1.3,1.6,1.3
                              		c0.9,0,1.7-0.7,1.7-1.7c0-0.1,0-0.2,0-0.4l-3.7-15.6l0.3-1.4L16,12c0.2,0.7,0.9,0.8,0.9,0.8l3.8,1h0.2c0.6,0,1.1-0.5,1.1-1.1
                              		c0-0.5-0.4-1-0.9-1.1l-3.3-0.8l-0.9-3.7C16.6,5.8,15,6,15,6c-2.6,0-2.9,1.1-2.9,1.1L7.1,28z"/>
                              	<circle  cx="15.5" cy="2.5" r="2.5"/>
                              </g>
                              </svg>
                              """,
                    LastUpdatedBy = "Default Icon Library",
                    LastUpdatedOn = frozenNow,
                    ContentVersion = Db.ContentVersionDateTime()
                },
            ];
        }
    }
}
