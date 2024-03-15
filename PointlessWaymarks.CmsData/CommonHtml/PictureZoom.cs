using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PointlessWaymarks.CmsData.CommonHtml
{
    public static class PictureZoom
    {
        /// <summary>
        /// This is only setup to handle the first image on a page - it is a simple way to add a zoom effect to a single photo/image.
        /// </summary>
        /// <param name="photo"></param>
        /// <returns></returns>
        public static string SwipeZoomHeader(PictureSiteInformation photo)
        {
            if(photo.Pictures?.LargePicture == null) return string.Empty;

            return $$"""
                     <link rel="stylesheet" href="{{UserSettingsSingleton.CurrentSettings().SiteResourcesUrl()}}photoswipe.css">

                     <script type="module">
                         import PhotoSwipe from '{{UserSettingsSingleton.CurrentSettings().SiteResourcesUrl()}}photoswipe.esm.js';
                     
                         const options = {
                             dataSource: [{
                                 src: '{{photo.Pictures.LargePicture.SiteUrl}}',
                               width: {{photo.Pictures.LargePicture.Width}},
                               height: {{photo.Pictures.LargePicture.Height}}
                             }
                           ],
                             showHideAnimationType: 'fade',
                             initialZoomLevel: 1,
                             secondaryZoomLevel: 3,
                             maxZoomLevel: 8,
                         };
                     
                         document.querySelector('img').onclick = () => {
                             options.index = 0;// defines start slide index
                             const pswp = new PhotoSwipe(options);
                             pswp.init(); // initializing PhotoSwipe core adds it to DOM
                         };
                     </script>
                     """;
        }
    }
}
