using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.CmsData;

[ObservableObject]
public partial class UserSettings
{
    [ObservableProperty] private string _bingApiKey = string.Empty;
    [ObservableProperty] private string _calTopoApiKey = string.Empty;

    /// <summary>
    ///     Database File Name Setting - this may be relative or absolute, prefer the DatabaseFileFullName in the
    ///     UserSettingsUtilities for general purpose use.
    /// </summary>
    [ObservableProperty] private string _databaseFile = string.Empty;

    [ObservableProperty] private string _defaultCreatedBy = string.Empty;
    [ObservableProperty] private double _latitudeDefault;

    /// <summary>
    ///     Relative or Absolute Local Media Archive Directory - prefer the LocalMediaArchiveFullDirectory
    ///     in UserSettingsUtilities as it will always represent the full path.
    /// </summary>
    [ObservableProperty] private string _localMediaArchiveDirectory = string.Empty;

    /// <summary>
    ///     Relative or Absolute Local Site Root Directory (the directory html will be generated into)
    ///     - prefer the LocalSiteRootFullDirectory in UserSettingsUtilities as it will always
    ///     represent the full path.
    /// </summary>
    [ObservableProperty] private string _localSiteRootDirectory = string.Empty;

    [ObservableProperty] private double _longitudeDefault;
    [ObservableProperty] private string _pdfToCairoExeDirectory = string.Empty;

    [ObservableProperty] private bool _photoPagesHaveLinksToPhotoSizesByDefault;
    [ObservableProperty] private bool _imagePagesHaveLinksToImageSizesByDefault;
    [ObservableProperty] private string _pinboardApiToken = string.Empty;
    [ObservableProperty] private Guid _settingsId;
    [ObservableProperty] private string _siteAuthors = string.Empty;
    [ObservableProperty] private string _siteDirectionAttribute = string.Empty;
    [ObservableProperty] private string _siteEmailTo = string.Empty;
    [ObservableProperty] private string _siteKeywords = string.Empty;
    [ObservableProperty] private string _siteLangAttribute = string.Empty;
    [ObservableProperty] private string _siteName = string.Empty;
    [ObservableProperty] private string _siteS3Bucket = string.Empty;
    [ObservableProperty] private string _siteS3BucketRegion = string.Empty;
    [ObservableProperty] private string _siteSummary = string.Empty;
    [ObservableProperty] private string _siteDomainName = string.Empty;
}