using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    public static class ImageConstants
    {
        public static BitmapSource BlankImage = BitmapSource.Create(1, 1, 1, 1, PixelFormats.BlackWhite, null, new byte[] { 1 }, 1);
    }
}
