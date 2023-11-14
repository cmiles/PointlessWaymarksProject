using System.Diagnostics;
using System.IO;
using System.Web;
using HtmlTableHelper;
using MetadataExtractor;
using MetadataExtractor.Formats.Xmp;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.PhotoList;

public static class PhotoMetadataReport
{
    public static async Task AllPhotoMetadataToHtml(FileInfo? selectedFile, StatusControlContext statusContext)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (selectedFile == null)
        {
            statusContext.ToastError("No photo...");
            return;
        }

        selectedFile.Refresh();

        if (!selectedFile.Exists)
        {
            statusContext.ToastError($"File {selectedFile.FullName} doesn't exist?");
            return;
        }

        var photoMetaTags = ImageMetadataReader.ReadMetadata(selectedFile.FullName);

        var tagHtml = photoMetaTags.SelectMany(x => x.Tags).OrderBy(x => x.DirectoryName).ThenBy(x => x.Name)
            .ToList().Select(x => new
            {
                DataType = x.Type.ToString(),
                x.DirectoryName,
                Tag = x.Name,
                TagValue = x.Description?.SafeObjectDump()
            }).ToHtmlTable(new {@class = "pure-table pure-table-striped"});

        var xmpDirectory = ImageMetadataReader.ReadMetadata(selectedFile.FullName).OfType<XmpDirectory>()
            .FirstOrDefault();

        var xmpMetadata = xmpDirectory?.GetXmpProperties().Select(x => new {XmpKey = x.Key, XmpValue = x.Value})
            .ToHtmlTable(new {@class = "pure-table pure-table-striped"});

        await ThreadSwitcher.ResumeForegroundAsync();

        var file = new FileInfo(Path.Combine(FileLocationTools.TempStorageDirectory().FullName,
            $"PhotoMetadata-{Path.GetFileNameWithoutExtension(selectedFile.Name)}-{DateTime.Now:yyyy-MM-dd---HH-mm-ss}.htm"));

        var htmlString =
            await ($"<h1>Metadata Report:</h1><h1>{HttpUtility.HtmlEncode(selectedFile.FullName)}</h1><br><h1>Metadata - Part 1</h1><br>" +
             tagHtml + "<br><br><h1>XMP - Part 2</h1><br>" + xmpMetadata)
            .ToHtmlDocumentWithPureCss("Photo Metadata", "body {margin: 12px;}");

        await File.WriteAllTextAsync(file.FullName, htmlString);

        var ps = new ProcessStartInfo(file.FullName) {UseShellExecute = true, Verb = "open"};
        Process.Start(ps);
    }
}