using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PointlessWaymarks.WpfCommon.Utility
{
    public static class FileSystemHelpers
    {
        public static DirectoryInfo TempStorageHtmlDirectory()
        {
            var directory = new DirectoryInfo(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Pointless Waymarks Cms",
                "TemporaryFiles", "Html"));

            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
        }

        public static async Task<string> SpatialScriptsAsString()
        {
            var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());

            var siteResources = embeddedProvider.GetDirectoryContents("")
                .Single(x => x.Name.Contains("pointless-waymarks-spatial-common"));

            await using var stream = siteResources.CreateReadStream();
            using StreamReader reader = new(stream);
            var spatialScript = await reader.ReadToEndAsync().ConfigureAwait(false);

            return spatialScript;
        }
    }
}
