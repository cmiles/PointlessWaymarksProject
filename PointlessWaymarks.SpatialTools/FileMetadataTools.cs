using MetadataExtractor;
using XmpCore;

namespace PointlessWaymarks.SpatialTools;

public static class FileMetadataTools
{
    /// <summary>
    ///     From https://exiftool.org/exiftool_pod.html on 10/30/2022 with processing and manual filtering
    ///     in Excel to pick 'File Types' from the list with r/w or r/w/c support. .JPG added in addition to .JPEG.
    /// </summary>
    public static List<string> ExifToolWriteSupportedExtensions => new List<string>
    {
        // ReSharper disable StringLiteralTypo
        ".360",
        ".3G2",
        ".3GP",
        ".AAX",
        ".AI",
        ".ARQ",
        ".ARW",
        ".AVIF",
        ".CR2",
        ".CR3",
        ".CRM",
        ".CRW",
        ".CS1",
        ".DCP",
        ".DNG",
        ".DR4",
        ".DVB",
        ".EPS",
        ".ERF",
        ".EXIF",
        ".EXV",
        ".F4A/V",
        ".FFF",
        ".FLIF",
        ".GIF",
        ".GPR",
        ".HDP",
        ".HEIC",
        ".HEIF",
        ".ICC",
        ".IIQ",
        ".IND",
        ".INSP",
        ".JNG",
        ".JP2",
        ".JPEG",
        ".JPG",
        ".LRV",
        ".M4A/V",
        ".MEF",
        ".MIE",
        ".MNG",
        ".MOS",
        ".MOV",
        ".MP4",
        ".MPO",
        ".MQV",
        ".MRW",
        ".NEF",
        ".NKSC",
        ".NRW",
        ".ORF",
        ".ORI",
        ".PBM",
        ".PDF",
        ".PEF",
        ".PGM",
        ".PNG",
        ".PPM",
        ".PS",
        ".PSB",
        ".PSD",
        ".QTIF",
        ".RAF",
        ".RAW",
        ".RW2",
        ".RWL",
        ".SR2",
        ".SRW",
        ".THM",
        ".TIFF",
        ".VRD",
        ".WDP",
        ".WEBP",
        ".X3F",
        ".XMP"
        // ReSharper restore StringLiteralTypo
    }.Select(x => x.ToUpperInvariant()).OrderBy(x => x).ToList();

    public static List<string> TagSharpAndExifToolSupportedExtensions => TagSharpSupportedExtensions
        .Union(ExifToolWriteSupportedExtensions).Select(x => x.ToUpperInvariant())
        .OrderBy(x => x).ToList();

    /// <summary>
    ///     From the supported Images list on https://github.com/mono/taglib-sharp on 10/30/2022.
    ///     Dots, upper case and JPG manually changed/added.
    /// </summary>
    public static List<string> TagSharpSupportedExtensions => new List<string>
    {
        ".BMP", ".GIF", ".JPEG", ".JPG", ".PBM", ".PGM", ".PPM", ".PNM", ".PCX", ".PNG", ".SVG"
    }.Select(x => x.ToUpperInvariant()).OrderBy(x => x).ToList();

    public static (bool isPresent, FileInfo? exifToolFile) ExifToolExecutable(string? exifToolFullName)
    {
        if (string.IsNullOrEmpty(exifToolFullName)) return (false, null);

        var possibleExifToolFile = new FileInfo(exifToolFullName);

        if (!possibleExifToolFile.Exists) return (false, null);

        return (true, possibleExifToolFile);
    }

    public static async Task<bool> FileHasLatLong(FileInfo loopFile, IProgress<string>? progress)
    {
        if (loopFile.Extension.Equals(".xmp", StringComparison.InvariantCultureIgnoreCase))
        {
            await using var stream = File.OpenRead(loopFile.FullName);
            var xmp = XmpMetaFactory.Parse(stream);

            var xmpLocation = await FileMetadataXmpSidecarTools.LocationFromXmpSidecar(xmp, false, progress);

            return xmpLocation.HasValidLocation();
        }

        var metadataDirectories = ImageMetadataReader.ReadMetadata(loopFile.FullName);

        var metaLocation =
            await FileMetadataEmbeddedTools.LocationFromExif(metadataDirectories,
                false, progress);

        return metaLocation.HasValidLocation();
    }

    public static async Task<List<string>> FileKeywords(FileInfo fileToProcess, bool splitOnCommaAndSemiColon)
    {
        if (fileToProcess.Extension.Equals(".xmp", StringComparison.InvariantCultureIgnoreCase))
        {
            await using var stream = File.OpenRead(fileToProcess.FullName);
            var xmp = XmpMetaFactory.Parse(stream);

            return FileMetadataXmpSidecarTools.KeywordsFromXmpSidecar(xmp, splitOnCommaAndSemiColon);
        }

        var metadataDirectories = ImageMetadataReader.ReadMetadata(fileToProcess.FullName);

        return FileMetadataEmbeddedTools.KeywordsFromExif(metadataDirectories, splitOnCommaAndSemiColon);
    }

    public static async Task<DateTime?> FileUtcCreatedOn(FileInfo fileToProcess, IProgress<string>? progress)
    {
        if (fileToProcess.Extension.Equals(".xmp", StringComparison.InvariantCultureIgnoreCase))
        {
            await using var stream = File.OpenRead(fileToProcess.FullName);
            var xmp = XmpMetaFactory.Parse(stream);

            var xmpCreatedOn = await FileMetadataXmpSidecarTools.CreatedOnLocalAndUtc(xmp);

            if (xmpCreatedOn.createdOnUtc is null)
            {
                progress?.Report(
                    $"No UTC Date/Time found in xmp sidecar file {fileToProcess.FullName} - skipping");
                return null;
            }

            progress?.Report(
                $"{fileToProcess.FullName} Found UTC Time {xmpCreatedOn.createdOnUtc} from xmp sidecar file");
            return xmpCreatedOn.createdOnUtc;
        }

        var metadataDirectories = ImageMetadataReader.ReadMetadata(fileToProcess.FullName);

        var metaCreatedOn =
            await FileMetadataEmbeddedTools.CreatedOnLocalAndUtc(metadataDirectories);

        if (metaCreatedOn.createdOnUtc is null)
        {
            progress?.Report(
                $"No UTC Date/Time found in file {fileToProcess.FullName} - skipping");
            return null;
        }

        progress?.Report(
            $"{fileToProcess.FullName} Found UTC Time {metaCreatedOn.createdOnUtc} from file metadata");
        return metaCreatedOn.createdOnUtc;
    }

    public static async Task<MetadataLocation> Location(FileInfo file, bool tryGetElevationIfNotInMetadata,
        IProgress<string>? progress)
    {
        if (!file.Exists) return new MetadataLocation();

        if (file.Extension.Equals(".xmp", StringComparison.InvariantCultureIgnoreCase))
        {
            await using var stream = File.OpenRead(file.FullName);
            var xmp = XmpMetaFactory.Parse(stream);

            var xmpLocation =
                await FileMetadataXmpSidecarTools.LocationFromXmpSidecar(xmp, tryGetElevationIfNotInMetadata,
                    progress);

            return xmpLocation;
        }

        var metadataDirectories = ImageMetadataReader.ReadMetadata(file.FullName);

        var metaLocation =
            await FileMetadataEmbeddedTools.LocationFromExif(metadataDirectories,
                tryGetElevationIfNotInMetadata, progress);

        return metaLocation;
    }
}