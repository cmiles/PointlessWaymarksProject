using System.Collections;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.FileProviders;
using PointlessWaymarks.WpfCommon.WebViewVirtualDomain;

namespace PointlessWaymarks.WpfCommon.WpfHtmlResources;

public static class WpfHtmlResourcesHelper
{
    public static List<FileBuilderCreate> AwesomeMapSvgMarkers()
    {
        var returnList = new List<FileBuilderCreate>();

        var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());

        var siteResource = embeddedProvider.GetDirectoryContents("")
            .Single(x => x.Name.Contains("leaflet.awesome-svg-markers.css"));
        var embeddedAsStream = siteResource.CreateReadStream();
        var reader = new StreamReader(embeddedAsStream);
        var resourceString = reader.ReadToEnd();

        embeddedAsStream.Dispose();

        returnList.Add(new FileBuilderCreate("leaflet.awesome-svg-markers.css", resourceString));


        siteResource = embeddedProvider.GetDirectoryContents("")
            .Single(x => x.Name.Contains("leaflet.awesome-svg-markers.js"));
        embeddedAsStream = siteResource.CreateReadStream();
        reader = new StreamReader(embeddedAsStream);
        resourceString = reader.ReadToEnd();

        embeddedAsStream.Dispose();

        returnList.Add(new FileBuilderCreate("leaflet.awesome-svg-markers.js", resourceString));


        var markerResources = embeddedProvider.GetDirectoryContents("")
            .Where(x => x.Name.Contains("markers-"));

        foreach (var markerResource in markerResources)
        {
            embeddedAsStream = markerResource.CreateReadStream();

            byte[] binaryResource;

            using (MemoryStream ms = new MemoryStream())
            {
                embeddedAsStream.CopyTo(ms);
                binaryResource = ms.ToArray();
            }

            embeddedAsStream.Dispose();

            returnList.Add(new FileBuilderCreate($@"images\{markerResource.Name.Replace("WpfHtmlResources.images.", string.Empty)}", binaryResource));
        }

        return returnList;
    }

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