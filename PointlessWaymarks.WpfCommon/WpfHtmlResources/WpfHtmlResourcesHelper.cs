using System.IO;
using System.Reflection;
using Microsoft.Extensions.FileProviders;

namespace PointlessWaymarks.WpfCommon.WpfHtmlResources;

public static class WpfHtmlResourcesHelper
{
    public static string LeafletBingLayerJs()
    {
        var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());

        var siteResource = embeddedProvider.GetDirectoryContents("")
            .Single(x => x.Name.Contains("leafletBingLayer"));
        using var embeddedAsStream = siteResource.CreateReadStream();
        var reader = new StreamReader(embeddedAsStream);
        var resourceString = reader.ReadToEnd();

        return resourceString;
    }

    public static string LocalMapCommonJs()
    {
        var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());

        var siteResource = embeddedProvider.GetDirectoryContents("")
            .Single(x => x.Name.Contains("localMapCommon"));
        using var embeddedAsStream = siteResource.CreateReadStream();
        var reader = new StreamReader(embeddedAsStream);
        var resourceString = reader.ReadToEnd();

        return resourceString;
    }
}