using Amazon;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.S3;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.UserSettingsEditor;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class UserSettingsEditorContext
{
    private UserSettingsEditorContext(StatusControlContext statusContext, UserSettings toLoad)
    {
        StatusContext = statusContext;
        CommonCommands = new CmsCommonCommands(StatusContext);

        BuildCommands();

        RegionChoices = RegionEndpoint.EnumerableAllRegions.Select(x => x.SystemName).ToList();
        EditorSettings = toLoad;
    }

    public CmsCommonCommands CommonCommands { get; set; }
    public UserSettings EditorSettings { get; set; }

    public static string HelpMarkdownBingMapsApiKey =>
        "If you have a Bing Maps API key you can enter it here - this will allow access to some Bing layers in the maps. This is NOT required for maps to be functional.";

    public static string HelpMarkdownCalTopoMapsApiKey =>
        "If you have a CalTopo Maps API key you can enter it here - this will allow access to some CalTopo layers in the maps. This is NOT required for maps to be functional.";

    public static string HelpMarkdownDefaultCreatedByName =>
        "Set this to fill in a Default Created By when creating new content. Example 'Charles Miles'.";

    public static string HelpMarkdownDefaultLatitudeLongitude =>
        "The default Latitude and Longitude (in dd.dddd format) are used as the default starting point for maps. Example Latitude '32.4432', Longitude '-110.7577'.";

    public static string HelpMarkdownDomain =>
        "This is the subdomain + domain and optionally port - for example 'PointlessWaymarks.com'. This software will " +
        "prepend protocol and append paths to this.";

    public static string HelpMarkdownFeatureIntersectionSettingsFile =>
        "This program can check a Point or Line against a set of GeoJson files to generate tags. The settings file for that feature must be specified here.";

    public static string HelpMarkdownFeatureIntersectionTagOnImport =>
        "If checked - and the Feature Intersection Settings File is set/valid - newly imported content that has position information will have feature intersect tags added.";

    public static string HelpMarkdownFilesHavePublicDownloadLinkByDefault =>
        "Default setting for whether File Content has a download link. All Content is ALWAYS sent to the site!!! Controls like this only determine if there is an obvious link to the content - private content should not be added to this program.";

    public static string HelpMarkdownLinesHavePublicDownloadLinkByDefault =>
        "Default setting for whether Line Content has a download link. All Content is ALWAYS sent to the site!!! Controls like this only determine if there is an obvious link to the content - private content should not be added to this program.";
    
    public static string HelpMarkdownLinesShowContentReferencesOnMapByDefault =>
        "Default setting for whether spatial content referenced in the Line Body are shown on the map by default.";

    public static string HelpMarkdownGeoJsonHasPublicDownloadLinkByDefault =>
        "Default setting for whether GeoJson Content has a download link. All Content is ALWAYS sent to the site!!! Controls like this only determine if there is an obvious link to the content - private content should not be added to this program.";

    public static string HelpMarkdownLocalMediaArchive =>
        "The original/source media files are stored separately from the generated site - this (local) directory is very " +
        "important because the generating the site depends on the settings file, database and the contents of this " +
        "directory. Ideally you should backup this directory.";

    public static string HelpMarkdownLocalSiteRootDirectory =>
        "This is the directory where the local generated site will be placed - this should be a local directory, the " +
        "intention is that this program will create a local generated site to this directory and provide tools " +
        "to help you sync that to a server if you want to publish a public version of the site.";

    public static string HelpMarkdownNumberOfItemsOnTheMainPage =>
        "Determines the maximum number of items that will be displayed on the main/home/index page of the site.";

    public static string HelpMarkdownPinboardApiKey =>
        "Sites, pages and links on the internet are constantly disappearing - [Pinboard](https://pinboard.in/) is a bookmarking site that has options to archive links for your personal use and this software has some functions that help you send links to Pinboard if you enter your Api Key. This is OPTIONAL - nothing in this software requires Pinboard.";

    public static string HelpMarkdownProgramUpdateLocation =>
        "The location the program should check for updates.";

    public static string HelpMarkdownS3Information =>
        "This is NOT required. Amazon S3 - especially behind a service like Cloudflare - can be an excellent way to host a static site like this program generates. This program can help you upload files and maintain files on S3, but to do so you must provide some information - S3 Bucket Name (this will often match your domain name), S3 Bucket Region and AWS Site Credentials (these are not shown and are stored securely by windows - these are NOT stored in the database or in the settings file).";

    public static string HelpMarkdownShowImageSizesByDefault =>
        "Used as the default value for a Photo's or Image's 'Show Sizes' setting - if this is checked by default image pages will have links to every size available. ALL IMAGE FILES are 'public', but unless this is checked the user is never shown a direct link to any image file.";

    public static string HelpMarkdownShowPhotoPositionByDefault =>
        "Used as the default value for a Photo's 'Show Position' setting - if this is checked by default photo pages will show and link the position of a photo if the photo's latitude and longitude have values. ALL PHOTO FILES are 'public' so a determined user can examine the source of a page, download the image and extract metadata present in the photo, but unless 'Show Position' is checked a photographs position will never be displayed.";

    public static string HelpMarkdownSiteAuthors =>
        "A value for the site creators/authors - for example " + "'Pointless Waymarks Team'.";

    public static string HelpMarkdownSiteDirAttribute =>
        "Dir attribute indicating text direction for the site - see the [dir attribute on MDN](https://developer.mozilla.org/en-US/docs/Web/HTML/Global_attributes/dir) for more information.";

    public static string HelpMarkdownSiteEmailTo => "An Email To for the site - example 'PointlessWaymarks@gmail.com'.";

    public static string HelpMarkdownSiteKeywords =>
        "Used in as the tags for the overall/entire site - for example " +
        "'outdoors,hiking,running,landscape,photography,history'.";

    public static string HelpMarkdownSiteLangAttribute =>
        "Lang attribute indicating the default language for the site - see [lang attribute on MDN](https://developer.mozilla.org/en-US/docs/Web/HTML/Global_attributes/lang) for more information.";

    public static string HelpMarkdownSiteName => "The 'human readable' Site Name - for example 'Pointless Waymarks'.";

    public static string HelpMarkdownSubtitleSummary =>
        "Used as a sub-title and site summary - example 'Ramblings, Questionable Geographics, Photographic Half-truths'.";

    public List<string> RegionChoices { get; set; }
    public StatusControlContext StatusContext { get; set; }

    public static async Task<UserSettingsEditorContext> CreateInstance(StatusControlContext? statusContext,
        UserSettings toLoad)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        return new UserSettingsEditorContext(statusContext ?? new StatusControlContext(), toLoad);
    }

    [BlockingCommand]
    public async Task DeleteAwsCredentials()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        AwsCredentials.RemoveAwsSiteCredentials();
    }


    [BlockingCommand]
    public async Task SaveSettings()
    {
        await EditorSettings.WriteSettings();

        UserSettingsSingleton.CurrentSettings().InjectFrom(EditorSettings);
    }

    [BlockingCommand]
    public async Task UserAwsKeyAndSecretEntry()
    {
        var newKeyEntry = await StatusContext.ShowStringEntry("AWS Access Key",
            "Enter the AWS Access Key", string.Empty);

        if (!newKeyEntry.Item1)
        {
            StatusContext.ToastWarning("Amazon Credential Entry Cancelled");
            return;
        }

        var cleanedKey = StringTools.TrimNullToEmpty(newKeyEntry.Item2);

        if (string.IsNullOrWhiteSpace(cleanedKey)) return;

        var newSecretEntry = await StatusContext.ShowStringEntry("AWS Secret Access Key",
            "Enter the AWS Secret Access Key", string.Empty);

        if (!newSecretEntry.Item1) return;

        var cleanedSecret = StringTools.TrimNullToEmpty(newSecretEntry.Item2);

        if (string.IsNullOrWhiteSpace(cleanedSecret))
        {
            StatusContext.ToastError("AWS Credential Entry Canceled - secret can not be blank");
            return;
        }

        AwsCredentials.SaveAwsSiteCredential(cleanedKey, cleanedSecret);
    }
}