using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    public static class FileHelpers
    {

        public static bool ImageFileTypeIsSupported(FileInfo toCheck)
        {
            if (toCheck == null) return false;
            if (!toCheck.Exists) return false;
            return toCheck.Extension.ToLower().Contains("jpg") || toCheck.Extension.ToLower().Contains("jpeg");
        }

        public static bool PhotoFileTypeIsSupported(FileInfo toCheck)
        {
            if (toCheck == null) return false;
            if (!toCheck.Exists) return false;
            return toCheck.Extension.ToLower().Contains("jpg") || toCheck.Extension.ToLower().Contains("jpeg");
        }
    }
}
