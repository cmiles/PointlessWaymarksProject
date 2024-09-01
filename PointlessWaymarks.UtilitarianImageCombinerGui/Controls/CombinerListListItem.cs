using System.ComponentModel;
using System.IO;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Iptc;
using MetadataExtractor.Formats.Xmp;
using Metalama.Patterns.Observability;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using SkiaSharp;
using XmpCore;

namespace PointlessWaymarks.UtilitarianImageCombinerGui.Controls;

[Observable]
public partial class CombinerListListItem
{
    public CombinerListListItem(string fileName, StatusControlContext statusContext)
    {
        StatusContext = statusContext;
        PropertyChanged += CombinerListListItem_PropertyChanged;
        FileFullName = fileName;
    }

    public string? Tags { get; set; }
    public string? Summary { get; set; }
    public string? Title { get; set; }
    public double? Elevation { get; set; }
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
    public DateTime? CreatedOnUtc { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? CreatedBy { get; set; }
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string FileFullName { get; set; }

    private void CombinerListListItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;
        if (e.PropertyName.Equals(nameof(FileFullName))) StatusContext.RunNonBlockingTask(GetFileInfo);
    }

    private async Task GetFileInfo()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!File.Exists(FileFullName))
        {
            ImageHeight = 0;
            ImageWidth = 0;
        }

        using var codec = SKCodec.Create(FileFullName);
        ImageHeight = codec?.Info.Height ?? 0;
        ImageWidth = codec?.Info.Width ?? 0;

        var metadataDirectories = ImageMetadataReader.ReadMetadata(FileFullName);
        var exifIfdDirectory = ImageMetadataReader.ReadMetadata(FileFullName).OfType<ExifIfd0Directory>()
            .FirstOrDefault();
        var iptcDirectory = ImageMetadataReader.ReadMetadata(FileFullName).OfType<IptcDirectory>()
            .FirstOrDefault();
        var xmpDirectory = ImageMetadataReader.ReadMetadata(FileFullName).OfType<XmpDirectory>()
            .FirstOrDefault();

        CreatedBy = exifIfdDirectory?.GetDescription(ExifDirectoryBase.TagArtist) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(CreatedBy))
            CreatedBy = xmpDirectory?.XmpMeta?.GetArrayItem(XmpConstants.NsDC, "creator", 1)?.Value ??
                        string.Empty;

        if (string.IsNullOrWhiteSpace(CreatedBy))
            CreatedBy = iptcDirectory?.GetDescription(IptcDirectory.TagByLine) ?? string.Empty;

        var createdOn =
            await FileMetadataEmbeddedTools.CreatedOnLocalAndUtc(metadataDirectories);

        CreatedOn = createdOn.createdOnLocal ?? DateTime.Now;
        CreatedOnUtc = createdOn.createdOnUtc;

        var locationInformation =
            await FileMetadataEmbeddedTools.LocationFromExif(metadataDirectories, true,
                StatusContext.ProgressTracker());

        Latitude = locationInformation.Latitude;
        Longitude = locationInformation.Longitude;
        Elevation = locationInformation.Elevation?.MetersToFeet();

        Title = xmpDirectory?.XmpMeta?.GetArrayItem(XmpConstants.NsDC, "title", 1)?.Value;

        if (string.IsNullOrWhiteSpace(Title))
            Title = iptcDirectory?.GetDescription(IptcDirectory.TagObjectName) ?? string.Empty;

        Summary = exifIfdDirectory?.GetDescription(ExifDirectoryBase.TagImageDescription) ?? string.Empty;

        var tagList = FileMetadataEmbeddedTools.KeywordsFromExif(metadataDirectories, true);

        Tags = tagList.Any() ? string.Join(",", tagList) : string.Empty;
    }

    public static async Task<CombinerListListItem> CreateInstance(string fileName, StatusControlContext statusContext)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        return new CombinerListListItem(fileName, statusContext);
    }
}