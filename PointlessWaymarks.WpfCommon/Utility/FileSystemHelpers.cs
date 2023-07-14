using System.IO;
using System.Reflection;
using Microsoft.Extensions.FileProviders;

namespace PointlessWaymarks.WpfCommon.Utility;

public static class FileSystemHelpers
{
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