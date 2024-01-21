using System.Reflection;
using System.Text.Encodings.Web;
using Microsoft.Extensions.FileProviders;

namespace PointlessWaymarks.CommonTools;

public static class HtmlTools
{
    public static string FavIconIco()
    {
        var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());

        var siteResource = embeddedProvider.GetDirectoryContents("")
            .Single(x => x.Name.Contains("favicon"));
        using var embeddedAsStream = siteResource.CreateReadStream();
        var reader = new StreamReader(embeddedAsStream);
        var resourceString = reader.ReadToEnd();

        return resourceString;
    }

    public static async Task<string> MinimalCssAsString()
    {
        var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());

        var siteResources = embeddedProvider.GetDirectoryContents("")
            .Single(x => x.Name.Contains("sakura"));

        await using var stream = siteResources.CreateReadStream();
        using StreamReader reader = new(stream);
        var picoCss = await reader.ReadToEndAsync().ConfigureAwait(false);

        return picoCss;
    }

    public static async Task<string> PureCssAsString()
    {
        var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());

        var siteResources = embeddedProvider.GetDirectoryContents("")
            .Single(x => x.Name.Contains("pure-min"));

        await using var stream = siteResources.CreateReadStream();
        using StreamReader reader = new(stream);
        var pureCss = await reader.ReadToEndAsync().ConfigureAwait(false);

        return pureCss;
    }

    public static async Task<string> ToHtmlDocumentWithMinimalCss(this string body, string title, string styleBlock)
    {
        var minimalCss = await MinimalCssAsString();

        var htmlDoc = $"""

                       <!doctype html>
                       <html lang=en>
                       <head>
                           <meta http-equiv="X-UA-Compatible" content="IE=edge" />
                           <meta name="viewport" content="width=device-width, initial-scale=1.0">
                           <meta charset="utf-8">
                           <title>{HtmlEncoder.Default.Encode(title)}</title>
                           <style>
                            {minimalCss}
                            {styleBlock}
                            </style>
                       </head>
                       <body>
                           {body}
                       </body>
                       </html>
                       """;

        return htmlDoc;
    }

    public static async Task<string> ToHtmlDocumentWithPureCss(this string body, string title, string styleBlock)
    {
        var pureCss = await PureCssAsString();

        var htmlDoc = $"""

                       <!doctype html>
                       <html lang=en>
                       <head>
                           <meta http-equiv="X-UA-Compatible" content="IE=edge" />
                           <meta name="viewport" content="width=device-width, initial-scale=1.0">
                           <meta charset="utf-8">
                           <title>{HtmlEncoder.Default.Encode(title)}</title>
                           <style>
                            {pureCss}
                            {styleBlock}
                            </style>
                       </head>
                       <body>
                           {body}
                       </body>
                       </html>
                       """;

        return htmlDoc;
    }
}