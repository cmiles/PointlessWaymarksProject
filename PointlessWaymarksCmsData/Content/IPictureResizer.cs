using System;
using System.IO;

namespace PointlessWaymarksCmsData.Content
{
    public interface IPictureResizer
    {
        FileInfo ResizeTo(FileInfo toResize, int width, int quality, string imageTypeString, bool addSizeString,
            IProgress<string> progress);
    }
}