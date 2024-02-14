using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsData;

[NotifyPropertyChanged]
public partial class UserSettings
{
    public string BingApiKey { get; set; } = string.Empty;
    public string CalTopoApiKey { get; set; } = string.Empty;
    /// <summary>
    ///     Database File Name Setting - this may be relative or absolute, prefer the DatabaseFileFullName in the
    ///     UserSettingsUtilities for general purpose use.
    /// </summary>
    public string DatabaseFile { get; set; } = string.Empty;
    public string DefaultCreatedBy { get; set; } = string.Empty;
    public bool FeatureIntersectionTagOnImport { get; set; }
    public string FeatureIntersectionTagSettingsFile { get; set; } = string.Empty;
    public bool ImagePagesHaveLinksToImageSizesByDefault { get; set; }
    public double LatitudeDefault { get; set; }
    public bool FilesHavePublicDownloadLinkByDefault { get; set; }
    public bool LinesHavePublicDownloadLinkByDefault { get; set; }
    public bool LinesShowContentReferencesOnMapByDefault { get; set; }
    public bool GeoJsonHasPublicDownloadLinkByDefault { get; set; }
    /// <summary>
    ///     Relative or Absolute Local Media Archive Directory - prefer the LocalMediaArchiveFullDirectory
    ///     in UserSettingsUtilities as it will always represent the full path.
    /// </summary>
    public string LocalMediaArchiveDirectory { get; set; } = string.Empty;

    /// <summary>
    ///     Relative or Absolute Local Site Root Directory (the directory html will be generated into)
    ///     - prefer the LocalSiteRootFullDirectory in UserSettingsUtilities as it will always
    ///     represent the full path.
    /// </summary>
    public string LocalSiteRootDirectory { get; set; } = string.Empty;
    public double LongitudeDefault { get; set; }
    public int NumberOfItemsOnMainSitePage { get; set; }
    public bool PhotoPagesHaveLinksToPhotoSizesByDefault { get; set; }
    public bool PhotoPagesShowPositionByDefault { get; set; }
    public string PinboardApiToken { get; set; } = string.Empty;
    public string ProgramUpdateLocation { get; set; } = string.Empty;
    public Guid SettingsId { get; set; }
    public string SiteAuthors { get; set; } = string.Empty;
    public string SiteDirectionAttribute { get; set; } = string.Empty;
    public string SiteDomainName { get; set; } = string.Empty;
    public string SiteEmailTo { get; set; } = string.Empty;
    public string SiteKeywords { get; set; } = string.Empty;
    public string SiteLangAttribute { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public string SiteS3Bucket { get; set; } = string.Empty;
    public string SiteS3BucketRegion { get; set; } = string.Empty;
    public string SiteSummary { get; set; } = string.Empty;
}