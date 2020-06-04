using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AngleSharp.Text;

namespace PointlessWaymarksCmsData.Pictures
{
    public interface IPictureResizer
    {
        FileInfo ResizeTo(FileInfo toResize, int width, int quality, string imageTypeString, bool addSizeString,
            IProgress<string> progress);
    }
}