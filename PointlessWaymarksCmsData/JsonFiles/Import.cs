using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PointlessWaymarksCmsData.JsonFiles
{
    public static class Import
    {
        public static List<string> GetAllJsonFiles(DirectoryInfo rootDirectory)
        {
            var listOfPrefixes = new List<string> {"Photo---"};

            return Directory.GetFiles(rootDirectory.FullName, "*.xml", SearchOption.AllDirectories).ToList();
        }
    }
}