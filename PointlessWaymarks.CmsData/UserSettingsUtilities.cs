using System.Data;
using System.Globalization;
using Amazon;
using FluentMigrator.Runner;
using IniParser;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;
using PointlessWaymarks.CommonTools;
using Serilog;

namespace PointlessWaymarks.CmsData;

public static class UserSettingsUtilities
{
    public static readonly double ProjectDefaultLatitude = 32.119742;
    public static readonly double ProjectDefaultLongitude = -110.5230213;
    public static string SettingsFileFullName { get; set; } = "PointlessWaymarksCmsSettings.ini";

    private static string AddSettingsFileRootDirectoryIfNeeded(this string fileName)
    {
        if (Path.IsPathFullyQualified(fileName)) return fileName;

        return Path.Combine(SettingsFile().DirectoryName ?? string.Empty,
            fileName.TrimStart(Path.DirectorySeparatorChar));
    }

    public static string AllContentListUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/AllContentList.html";
    }

    public static string AllContentRssUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/AllContentRss.xml";
    }

    public static string AllTagsListUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/Tags/AllTagsList.html";
    }

    public static string CameraRollGalleryJavascriptUrl()
    {
        return
            $"{UserSettingsSingleton.CurrentSettings().SiteResourcesUrl()}pointless-waymarks-camera-roll-gallery.js";
    }

    public static string CameraRollGalleryUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/Photos/Galleries/CameraRoll.html";
    }

    public static string ContentGalleryJavascriptUrl()
    {
        return
            $"{UserSettingsSingleton.CurrentSettings().SiteResourcesUrl()}pointless-waymarks-content-gallery.js";
    }

    public static async Task<string> ContentUrl(this UserSettings settings, Guid toLink)
    {
        var db = await Db.Context().ConfigureAwait(false);

        //!!Content Type List!!

        var possibleFile = await db.FileContents.SingleOrDefaultAsync(x => x.ContentId == toLink)
            .ConfigureAwait(false);
        if (possibleFile != null) return settings.FilePageUrl(possibleFile);

        var possibleGeoJson = await db.GeoJsonContents.SingleOrDefaultAsync(x => x.ContentId == toLink)
            .ConfigureAwait(false);
        if (possibleGeoJson != null) return settings.GeoJsonPageUrl(possibleGeoJson);

        var possibleImage = await db.ImageContents.SingleOrDefaultAsync(x => x.ContentId == toLink)
            .ConfigureAwait(false);
        if (possibleImage != null) return settings.ImagePageUrl(possibleImage);

        var possibleLine = await db.LineContents.SingleOrDefaultAsync(x => x.ContentId == toLink)
            .ConfigureAwait(false);
        if (possibleLine != null) return settings.LinePageUrl(possibleLine);

        var possibleLink = await db.LinkContents.SingleOrDefaultAsync(x => x.ContentId == toLink)
            .ConfigureAwait(false);
        if (possibleLink != null) return settings.LinkListUrl();

        var possibleNote = await db.NoteContents.SingleOrDefaultAsync(x => x.ContentId == toLink)
            .ConfigureAwait(false);
        if (possibleNote != null) return settings.NotePageUrl(possibleNote);

        var possiblePhoto = await db.PhotoContents.SingleOrDefaultAsync(x => x.ContentId == toLink)
            .ConfigureAwait(false);
        if (possiblePhoto != null) return settings.PhotoPageUrl(possiblePhoto);

        var possiblePoint = await db.PointContents.SingleOrDefaultAsync(x => x.ContentId == toLink)
            .ConfigureAwait(false);
        if (possiblePoint != null) return settings.PointPageUrl(possiblePoint);

        var possiblePost = await db.PostContents.SingleOrDefaultAsync(x => x.ContentId == toLink)
            .ConfigureAwait(false);
        if (possiblePost != null) return settings.PostPageUrl(possiblePost);

        var possibleVideo = await db.VideoContents.SingleOrDefaultAsync(x => x.ContentId == toLink)
            .ConfigureAwait(false);
        if (possibleVideo != null) return settings.VideoPageUrl(possibleVideo);

        return string.Empty;
    }

    public static GenerationReturn CreateIfItDoesNotExist(this DirectoryInfo directoryInfo)
    {
        if (directoryInfo.Exists)
            return GenerationReturn.Success($"Create If Does Not Exists - {directoryInfo.FullName} Already Exists");

        try
        {
            directoryInfo.Create();
        }
        catch (Exception e)
        {
            GenerationReturn.Error(
                $"Trying to create Directory {directoryInfo.FullName}  resulted in an Exception.", null, e);
        }

        return GenerationReturn.Success($"Created {directoryInfo.FullName}");
    }

    public static string CssMainStyleFileUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/style.css";
    }

    public static string DailyPhotoGalleryUrl(this UserSettings settings, DateTime galleryDate)
    {
        return $"{settings.SiteUrl()}/Photos/Galleries/Daily/DailyPhotos-{galleryDate:yyyy-MM-dd}.html";
    }

    /// <summary>
    ///     This returns a fully qualified path for the Database File based on the given settings.
    /// </summary>
    /// <param name="settings"></param>
    /// <returns></returns>
    public static string DatabaseFileFullName(this UserSettings settings)
    {
        return settings.DatabaseFile.AddSettingsFileRootDirectoryIfNeeded();
    }

    public static string DefaultContentFormatChoice()
    {
        return Enum.GetNames(typeof(ContentFormatEnum)).First();
    }

    public static async Task<double> DefaultLatitudeValidated(this UserSettings settings)
    {
        var currentSetting = settings.LatitudeDefault;

        if (!(await CommonContentValidation.LatitudeValidation(currentSetting)).Valid)
            return ProjectDefaultLatitude;

        return currentSetting;
    }

    public static async Task<double> DefaultLongitudeValidated(this UserSettings settings)
    {
        var currentSetting = settings.LongitudeDefault;

        if (!(await CommonContentValidation.LongitudeValidation(currentSetting)).Valid)
            return ProjectDefaultLongitude;

        return currentSetting;
    }

    public static async Task EnsureDbIsPresent(IProgress<string>? progress = null)
    {
        var possibleDbFile = new FileInfo(UserSettingsSingleton.CurrentSettings().DatabaseFileFullName());

        //TODO: Mid-July 2023 I started having problems with this code - REVISIT
        //When compiled as a Single File Self Contained Executable an error is triggered in this code and
        //for now I have been unable to determine why. The error is listed below and occurs in the Publish
        //to S3 program and the CMS - but not in Visual Studio and not outside of a Self Contained Executable?
        //

        //       System.AggregateException: One or more errors occurred. (Value cannot be null. (Parameter 'path1'))
        //--->System.ArgumentNullException: Value cannot be null. (Parameter 'path1')
        //  at System.ArgumentNullException.Throw(String paramName)
        //  at System.IO.Path.Combine(String path1, String path2)
        //  at System.Data.SQLite.SQLiteConnection..ctor(String connectionString, Boolean parseViaFramework)
        //  at System.Data.SQLite.SQLiteConnection..ctor(String connectionString)
        //  at System.Data.SQLite.SQLiteConnection..ctor()
        //  at System.Data.SQLite.SQLiteFactory.CreateConnection()
        //  at FluentMigrator.Runner.Processors.GenericProcessorBase.<> c__DisplayClass6_1.<.ctor > b__1()
        //  at System.Lazy`1.ViaFactory(LazyThreadSafetyMode mode)
        //  at System.Lazy`1.ExecutionAndPublication(LazyHelper executionAndPublication, Boolean useDefaultConstructor)
        //  at System.Lazy`1.CreateValue()
        //  at FluentMigrator.Runner.Processors.GenericProcessorBase.get_Connection()
        //  at FluentMigrator.Runner.Processors.GenericProcessorBase.EnsureConnectionIsOpen()
        //  at FluentMigrator.Runner.Processors.SQLite.SQLiteProcessor.Exists(String template, Object[] args)
        //  at FluentMigrator.Runner.Processors.SQLite.SQLiteProcessor.TableExists(String schemaName, String tableName)
        //  at FluentMigrator.Runner.VersionLoader.get_AlreadyCreatedVersionTable()
        //  at FluentMigrator.Runner.VersionLoader.LoadVersionInfo()
        //  at FluentMigrator.Runner.VersionLoader..ctor(IProcessorAccessor processorAccessor, IConventionSet conventionSet, IMigrationRunnerConventions conventions, IVersionTableMetaData versionTableMetaData, IMigrationRunner runner)
        //  at System.RuntimeMethodHandle.InvokeMethod(Object target, Void * *arguments, Signature sig, Boolean isConstructor)
        //  at System.Reflection.ConstructorInvoker.Invoke(Object obj, IntPtr * args, BindingFlags invokeAttr)
        //  at System.Reflection.RuntimeConstructorInfo.InvokeWithManyArguments(RuntimeConstructorInfo ci, Int32 argCount, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
        //  at System.Reflection.RuntimeConstructorInfo.Invoke(BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
        //  at Microsoft.Extensions.DependencyInjection.ActivatorUtilities.ConstructorMatcher.CreateInstance(IServiceProvider provider)
        //  at Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance(IServiceProvider provider, Type instanceType, Object[] parameters)
        //  at Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance[T](IServiceProvider provider, Object[] parameters)
        //  at Microsoft.Extensions.DependencyInjection.FluentMigratorServiceCollectionExtensions.<> c.< AddFluentMigratorCore > b__0_6(IServiceProvider sp)
        //  at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSiteMain(ServiceCallSite callSite, TArgument argument)
        //  at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitRootCache(ServiceCallSite callSite, RuntimeResolverContext context)
        //  at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitScopeCache(ServiceCallSite callSite, RuntimeResolverContext context)
        //  at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(ServiceCallSite callSite, TArgument argument)
        //  at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.Resolve(ServiceCallSite callSite, ServiceProviderEngineScope scope)
        //  at Microsoft.Extensions.DependencyInjection.ServiceLookup.DynamicServiceProviderEngine.<> c__DisplayClass2_0.< RealizeService > b__0(ServiceProviderEngineScope scope)
        //  at Microsoft.Extensions.DependencyInjection.ServiceProvider.GetService(Type serviceType, ServiceProviderEngineScope serviceProviderEngineScope)
        //  at Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService(IServiceProvider provider, Type serviceType)
        //  at Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService[T](IServiceProvider provider)
        //  at System.Lazy`1.ViaFactory(LazyThreadSafetyMode mode)
        //  at System.Lazy`1.ExecutionAndPublication(LazyHelper executionAndPublication, Boolean useDefaultConstructor)
        //  at System.Lazy`1.CreateValue()
        //  at FluentMigrator.Runner.MigrationRunner.get_VersionLoader()
        //  at FluentMigrator.Runner.MigrationRunner.IsMigrationStepNeededForUpMigration(IMigrationInfo migration, Int64 targetVersion)
        //  at FluentMigrator.Runner.MigrationRunner.<> c__DisplayClass60_0.< GetUpMigrationsToApply > b__0(KeyValuePair`2 pair)
        //  at System.Linq.Enumerable.WhereSelectEnumerableIterator`2.MoveNext()
        //  at FluentMigrator.Runner.MigrationRunner.MigrateUp(Int64 targetVersion, Boolean useAutomaticTransactionManagement)
        //  at FluentMigrator.Runner.MigrationRunner.MigrateUp(Boolean useAutomaticTransactionManagement)
        //  at FluentMigrator.Runner.MigrationRunner.MigrateUp()
        //  at PointlessWaymarks.CmsData.UserSettingsUtilities.EnsureDbIsPresent(IProgress`1 progress) in C:\Code\PointlessWaymarksProject - 05\PointlessWaymarks.CmsData\UserSettingsUtilities.cs:line 153
        //  at PointlessWaymarks.CmsGui.MainWindow.LoadData() in C:\Code\PointlessWaymarksProject - 05\PointlessWaymarks.CmsGui\MainWindow.xaml.cs:line 495
        //  at PointlessWaymarks.WpfCommon.Status.StatusControlContext.<> c__DisplayClass119_0.<< RunFireAndForgetBlockingTask > b__0 > d.MoveNext() in C:\Code\PointlessWaymarksProject - 05\PointlessWaymarks.WpfCommon\Status\StatusControlContext.cs:line 338

        Log.Information($"Migration possibleDbFile {possibleDbFile.FullName}");

        if (possibleDbFile.Exists)
        {
            await using var sc = new ServiceCollection().AddFluentMigratorCore().ConfigureRunner(rb =>
                    rb.AddSQLite()
                        .WithGlobalConnectionString(
                            $"Data Source={possibleDbFile.FullName}")
                        .ScanIn(typeof(PointlessWaymarksContext).Assembly).For.Migrations())
                .AddLogging(lb => lb.AddSerilog()).BuildServiceProvider(false);

            using var scope = sc.CreateScope();
            // Instantiate the runner
            var runner = sc.GetRequiredService<IMigrationRunner>();

            // Execute the migrations
            runner.MigrateUp();
        }

        progress?.Report("Checking for database files...");

        var db = Db.Context().Result;
        await db.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }

    public static string FaviconUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/favicon.ico";
    }

    public static string FileDownloadUrl(this UserSettings settings, FileContent content)
    {
        return $"{settings.SiteUrl()}/Files/{content.Folder}/{content.Slug}/{content.OriginalFileName}";
    }

    public static string FileListUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/Files/FileList.html";
    }

    public static string FilePageUrl(this UserSettings settings, FileContent content)
    {
        return $"{settings.SiteUrl()}/Files/{content.Folder}/{content.Slug}/{content.Slug}.html";
    }

    public static string FileRssUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/Files/FileRss.xml";
    }

    public static UserSettingsGenerationValues GenerationValues(this UserSettings settings)
    {
        return (UserSettingsGenerationValues)new UserSettingsGenerationValues().InjectFrom(settings);
    }

    public static string GeoJsonJsonDownloadUrl(this UserSettings settings, GeoJsonContent content)
    {
        return $"{settings.SiteUrl()}/GeoJson/Data/GeoJson-{content.ContentId}.json";
    }

    public static string GeoJsonListUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/GeoJson/GeoJsonList.html";
    }

    public static string GeoJsonPageUrl(this UserSettings settings, GeoJsonContent content)
    {
        return $"{settings.SiteUrl()}/GeoJson/{content.Folder}/{content.Slug}/{content.Slug}.html";
    }

    public static string GeoJsonRssUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/GeoJson/GeoJsonRss.xml";
    }

    public static string ImageListUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/Images/ImageList.html";
    }

    public static string ImagePageUrl(this UserSettings settings, ImageContent content)
    {
        return $"{settings.SiteUrl()}/Images/{content.Folder}/{content.Slug}/{content.Slug}.html";
    }

    public static string ImageRssUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/Images/ImageRss.xml";
    }

    public static string IndexPageUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/index.html";
    }

    public static string LatestContentGalleryUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/LatestContent.html";
    }

    public static string LineGpxDownloadUrl(this UserSettings settings, LineContent content)
    {
        return $"{settings.SiteUrl()}/Lines/Data/Line-{content.ContentId}.gpx";
    }

    public static string LineJsonDownloadUrl(this UserSettings settings, LineContent content)
    {
        return $"{settings.SiteUrl()}/Lines/Data/Line-{content.ContentId}.json";
    }

    public static string LineMonthlyActivitySummaryUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/Lines/LineMonthlyActivitySummary.html";
    }

    public static string LinePageUrl(this UserSettings settings, LineContent content)
    {
        return $"{settings.SiteUrl()}/Lines/{content.Folder}/{content.Slug}/{content.Slug}.html";
    }

    public static string LinesListUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/Lines/LineList.html";
    }

    public static string LinesRssUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/Lines/LineRss.xml";
    }

    public static string LinkListUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/Links/LinkList.html";
    }

    public static string LinkRssUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/Links/LinkRss.xml";
    }

    public static FileInfo? LocalMediaArchiveFileContentFile(this UserSettings settings, FileContent? content)
    {
        if (string.IsNullOrWhiteSpace(content?.OriginalFileName)) return null;

        var directory = settings.LocalMediaArchiveFileDirectory();

        return new FileInfo(Path.Combine(directory.FullName, content.OriginalFileName));
    }

    public static DirectoryInfo LocalMediaArchiveFileDirectory(this UserSettings settings)
    {
        var directory =
            new DirectoryInfo(Path.Combine(settings.LocalMediaArchiveFullDirectory().FullName, "Files"));

        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static DirectoryInfo LocalMediaArchiveFullDirectory(this UserSettings settings)
    {
        var directory =
            new DirectoryInfo(settings.LocalMediaArchiveDirectory.AddSettingsFileRootDirectoryIfNeeded());

        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static FileInfo? LocalMediaArchiveImageContentFile(this UserSettings settings, ImageContent? content)
    {
        if (content?.OriginalFileName == null) return null;

        var directory = settings.LocalMediaArchiveImageDirectory();

        return new FileInfo(Path.Combine(directory.FullName, content.OriginalFileName));
    }

    public static DirectoryInfo LocalMediaArchiveImageDirectory(this UserSettings settings)
    {
        var directory =
            new DirectoryInfo(Path.Combine(settings.LocalMediaArchiveFullDirectory().FullName, "Images"));

        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static DirectoryInfo LocalMediaArchiveLogsDirectory(this UserSettings settings)
    {
        var directory = new DirectoryInfo(Path.Combine(settings.LocalMediaArchiveFullDirectory().FullName, "Logs"));

        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static FileInfo? LocalMediaArchivePhotoContentFile(this UserSettings settings, PhotoContent? content)
    {
        if (string.IsNullOrWhiteSpace(content?.OriginalFileName)) return null;

        var directory = settings.LocalMediaArchivePhotoDirectory();

        return new FileInfo(Path.Combine(directory.FullName, content.OriginalFileName));
    }

    public static DirectoryInfo LocalMediaArchivePhotoDirectory(this UserSettings settings)
    {
        var directory =
            new DirectoryInfo(Path.Combine(settings.LocalMediaArchiveFullDirectory().FullName, "Photos"));

        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static FileInfo? LocalMediaArchiveVideoContentFile(this UserSettings settings, VideoContent? content)
    {
        if (string.IsNullOrWhiteSpace(content?.OriginalFileName)) return null;

        var directory = settings.LocalMediaArchiveVideoDirectory();

        return new FileInfo(Path.Combine(directory.FullName, content.OriginalFileName));
    }

    public static DirectoryInfo LocalMediaArchiveVideoDirectory(this UserSettings settings)
    {
        var directory =
            new DirectoryInfo(Path.Combine(settings.LocalMediaArchiveFullDirectory().FullName, "Videos"));

        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static DirectoryInfo LocalScriptsDirectory(this UserSettings settings)
    {
        var directory =
            new DirectoryInfo(Path.Combine(settings.LocalMediaArchiveFullDirectory().FullName, "Scripts"));

        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static FileInfo LocalSiteAllContentListFile(this UserSettings settings)
    {
        var directory = settings.LocalSiteRootFullDirectory().FullName;
        return new FileInfo($"{Path.Combine(directory, "AllContentList")}.html");
    }

    public static FileInfo LocalSiteAllContentRssFile(this UserSettings settings)
    {
        var directory = settings.LocalSiteRootFullDirectory().FullName;
        return new FileInfo($"{Path.Combine(directory, "AllContentRss")}.xml");
    }

    public static FileInfo LocalSiteAllTagsListFileInfo(this UserSettings settings)
    {
        var directory = settings.LocalSiteTagsDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, "AllTagsList")}.html");
    }

    public static FileInfo LocalSiteCameraRollGalleryFileInfo(this UserSettings settings)
    {
        var directory = settings.LocalSitePhotoGalleryDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, "CameraRoll")}.html");
    }

    public static DirectoryInfo LocalSiteDailyPhotoGalleryDirectory(this UserSettings settings)
    {
        var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootFullDirectory().FullName, "Photos",
            "Galleries", "Daily"));
        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static FileInfo LocalSiteDailyPhotoGalleryFileInfo(this UserSettings settings, DateTime galleryDate)
    {
        var directory = settings.LocalSiteDailyPhotoGalleryDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, $"DailyPhotos-{galleryDate:yyyy-MM-dd}")}.html");
    }

    public static DateTime? LocalSiteDailyPhotoGalleryPhotoDateFromFileInfo(FileInfo? toParse)
    {
        if (toParse == null) return null;
        var name = toParse.Name;
        if (!name.StartsWith("DailyPhotos-")) return null;
        name = name.Replace("DailyPhotos-", "");
        if (!name.EndsWith(".html")) return null;
        name = name.Replace(".html", "");

        if (DateTime.TryParseExact(name, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var parsedDateTime))
            return parsedDateTime;

        return null;
    }

    public static DirectoryInfo LocalSiteDirectory(this UserSettings settings)
    {
        var directory = new DirectoryInfo(settings.LocalSiteRootFullDirectory().FullName);
        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static DirectoryInfo LocalSiteFileContentDirectory(this UserSettings settings, FileContent content,
        bool createDirectoryIfNotFound = true)
    {
        if (string.IsNullOrWhiteSpace(content.Folder))
            throw new NullReferenceException(
                $"{nameof(LocalSiteFileContentDirectory)} Null or Blank for the content.Folder of {content.Title}");

        if (string.IsNullOrWhiteSpace(content.Slug))
            throw new NullReferenceException(
                $"{nameof(LocalSiteFileContentDirectory)} Null or Blank for the content.Slug of {content.Title}");

        var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteFileDirectory().FullName, content.Folder,
            content.Slug));

        if (directory.Exists || !createDirectoryIfNotFound) return directory;

        directory.Create();
        directory.Refresh();

        return directory;
    }

    public static FileInfo? LocalSiteFileContentFile(this UserSettings settings, FileContent? content)
    {
        if (string.IsNullOrWhiteSpace(content?.OriginalFileName)) return null;

        var directory = settings.LocalSiteFileContentDirectory(content, false);

        return new FileInfo(Path.Combine(directory.FullName, content.OriginalFileName));
    }

    public static DirectoryInfo LocalSiteFileDirectory(this UserSettings settings)
    {
        var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootFullDirectory().FullName, "Files"));
        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static FileInfo? LocalSiteFileHtmlFile(this UserSettings settings, FileContent? content)
    {
        if (string.IsNullOrWhiteSpace(content?.Slug)) return null;

        var directory = settings.LocalSiteFileContentDirectory(content);
        return new FileInfo($"{Path.Combine(directory.FullName, content.Slug)}.html");
    }

    public static FileInfo LocalSiteFileListFile(this UserSettings settings)
    {
        var directory = settings.LocalSiteFileDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, "FileList")}.html");
    }

    public static FileInfo LocalSiteFileRssFile(this UserSettings settings)
    {
        var directory = settings.LocalSiteFileDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, "FileRss")}.xml");
    }

    public static DirectoryInfo LocalSiteGeoJsonContentDirectory(this UserSettings settings, GeoJsonContent content,
        bool createDirectoryIfNotFound = true)
    {
        if (string.IsNullOrWhiteSpace(content.Folder))
            throw new NullReferenceException(
                $"{nameof(LocalSiteGeoJsonContentDirectory)} Null or Blank for the content.Folder of {content.Title}");

        if (string.IsNullOrWhiteSpace(content.Slug))
            throw new NullReferenceException(
                $"{nameof(LocalSiteGeoJsonContentDirectory)} Null or Blank for the content.Slug of {content.Title}");

        var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteGeoJsonDirectory().FullName,
            content.Folder, content.Slug));

        if (directory.Exists || !createDirectoryIfNotFound) return directory;

        directory.Create();
        directory.Refresh();

        return directory;
    }

    public static DirectoryInfo LocalSiteGeoJsonDataDirectory(this UserSettings settings)
    {
        var directory = new DirectoryInfo(Path.Combine(LocalSiteGeoJsonDirectory(settings).FullName, "Data"));
        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static DirectoryInfo LocalSiteGeoJsonDirectory(this UserSettings settings)
    {
        var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootFullDirectory().FullName, "GeoJson"));
        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static FileInfo? LocalSiteGeoJsonHtmlFile(this UserSettings settings, GeoJsonContent? content)
    {
        if (string.IsNullOrWhiteSpace(content?.Slug)) return null;

        var directory = settings.LocalSiteGeoJsonContentDirectory(content);
        return new FileInfo($"{Path.Combine(directory.FullName, content.Slug)}.html");
    }

    public static FileInfo LocalSiteGeoJsonListFile(this UserSettings settings)
    {
        var directory = settings.LocalSiteGeoJsonDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, "GeoJsonList")}.html");
    }

    public static FileInfo LocalSiteGeoJsonRssFile(this UserSettings settings)
    {
        var directory = settings.LocalSiteGeoJsonDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, "GeoJsonRss")}.xml");
    }

    public static DirectoryInfo LocalSiteImageContentDirectory(this UserSettings settings, ImageContent content,
        bool createDirectoryIfNotFound = true)
    {
        if (string.IsNullOrWhiteSpace(content.Folder))
            throw new NullReferenceException(
                $"{nameof(LocalSiteImageContentDirectory)} Null or Blank for the content.Folder of {content.Title}");

        if (string.IsNullOrWhiteSpace(content.Slug))
            throw new NullReferenceException(
                $"{nameof(LocalSiteImageContentDirectory)} Null or Blank for the content.Slug of {content.Title}");

        var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteImageDirectory().FullName, content.Folder,
            content.Slug));

        if (directory.Exists || !createDirectoryIfNotFound) return directory;

        directory.Create();
        directory.Refresh();

        return directory;
    }

    public static FileInfo? LocalSiteImageContentFile(this UserSettings settings, ImageContent? content)
    {
        if (string.IsNullOrWhiteSpace(content?.OriginalFileName)) return null;

        var directory = settings.LocalSiteImageContentDirectory(content, false);

        return new FileInfo(Path.Combine(directory.FullName, content.OriginalFileName));
    }

    public static DirectoryInfo LocalSiteImageDirectory(this UserSettings settings)
    {
        var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootFullDirectory().FullName, "Images"));
        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static FileInfo? LocalSiteImageHtmlFile(this UserSettings settings, ImageContent? content)
    {
        if (string.IsNullOrWhiteSpace(content?.Slug)) return null;

        var directory = settings.LocalSiteImageContentDirectory(content);
        return new FileInfo($"{Path.Combine(directory.FullName, content.Slug)}.html");
    }

    public static FileInfo LocalSiteImageListFile(this UserSettings settings)
    {
        var directory = settings.LocalSiteImageDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, "ImageList")}.html");
    }

    public static FileInfo LocalSiteImageRssFile(this UserSettings settings)
    {
        var directory = settings.LocalSiteImageDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, "ImageRss")}.xml");
    }

    public static FileInfo LocalSiteIndexFile(this UserSettings settings)
    {
        var directory = settings.LocalSiteRootFullDirectory().FullName;
        return new FileInfo($"{Path.Combine(directory, "index")}.html");
    }

    public static FileInfo LocalSiteLatestContentGalleryFileInfo(this UserSettings settings)
    {
        var directory = settings.LocalSiteRootFullDirectory().FullName;
        return new FileInfo($"{Path.Combine(directory, "LatestContent")}.html");
    }

    public static DirectoryInfo LocalSiteLineContentDirectory(this UserSettings settings, LineContent content,
        bool createDirectoryIfNotFound = true)
    {
        if (string.IsNullOrWhiteSpace(content.Folder))
            throw new NullReferenceException(
                $"{nameof(LocalSiteLineContentDirectory)} Null or Blank for the content.Folder of {content.Title}");

        if (string.IsNullOrWhiteSpace(content.Slug))
            throw new NullReferenceException(
                $"{nameof(LocalSiteLineContentDirectory)} Null or Blank for the content.Slug of {content.Title}");

        var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteLineDirectory().FullName, content.Folder,
            content.Slug));

        if (directory.Exists || !createDirectoryIfNotFound) return directory;

        directory.Create();
        directory.Refresh();

        return directory;
    }

    public static DirectoryInfo LocalSiteLineDataDirectory(this UserSettings settings)
    {
        var directory = new DirectoryInfo(Path.Combine(LocalSiteLineDirectory(settings).FullName, "Data"));
        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static FileInfo? LocalSiteLineDataFile(this UserSettings settings, LineContent? content)
    {
        if (content is null) return null;

        var directory = settings.LocalSiteLineDataDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, $"Line-{content.ContentId.ToString()}.json")}");
    }

    public static DirectoryInfo LocalSiteLineDirectory(this UserSettings settings)
    {
        var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootFullDirectory().FullName, "Lines"));
        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static FileInfo? LocalSiteLineHtmlFile(this UserSettings settings, LineContent? content)
    {
        if (string.IsNullOrWhiteSpace(content?.Slug)) return null;

        var directory = settings.LocalSiteLineContentDirectory(content);
        return new FileInfo($"{Path.Combine(directory.FullName, content.Slug)}.html");
    }

    public static FileInfo LocalSiteLineListFile(this UserSettings settings)
    {
        var directory = settings.LocalSiteLineDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, "LineList")}.html");
    }

    public static FileInfo LocalSiteLineMonthlyActivityHtmlFile(this UserSettings settings)
    {
        var directory = settings.LocalSiteLineDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, "LineMonthlyActivitySummary")}.html");
    }

    public static FileInfo LocalSiteLineRssFile(this UserSettings settings)
    {
        var directory = settings.LocalSiteLineDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, "LineRss")}.xml");
    }

    public static DirectoryInfo LocalSiteLinkDirectory(this UserSettings settings)
    {
        var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootFullDirectory().FullName, "Links"));
        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static FileInfo LocalSiteLinkListFile(this UserSettings settings)
    {
        var directory = settings.LocalSiteLinkDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, "LinkList")}.html");
    }

    public static FileInfo LocalSiteLinkRssFile(this UserSettings settings)
    {
        var directory = settings.LocalSiteLinkDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, "LinkRss")}.xml");
    }

    public static DirectoryInfo LocalSiteMapComponentDataDirectory(this UserSettings settings)
    {
        var directory = new DirectoryInfo(Path.Combine(LocalSiteMapComponentDirectory(settings).FullName, "Data"));
        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static FileInfo LocalSiteMapComponentDataFile(this UserSettings settings, Guid contentId)
    {
        var directory = settings.LocalSiteMapComponentDataDirectory();
        return new FileInfo(
            $"{Path.Combine(directory.FullName, Names.MapComponentContentPrefix, contentId.ToString())}.json");
    }

    public static DirectoryInfo LocalSiteMapComponentDirectory(this UserSettings settings)
    {
        var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootFullDirectory().FullName, "Maps"));
        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static DirectoryInfo LocalSiteNoteContentDirectory(this UserSettings settings, NoteContent content,
        bool createDirectoryIfNotFound = true)
    {
        if (string.IsNullOrWhiteSpace(content.Folder))
            throw new NullReferenceException(
                $"{nameof(LocalSiteNoteContentDirectory)} Null or Blank for the content.Folder of {content.Title}");

        var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteNoteDirectory().FullName, content.Folder));

        if (directory.Exists || !createDirectoryIfNotFound) return directory;

        directory.Create();
        directory.Refresh();

        return directory;
    }

    public static DirectoryInfo LocalSiteNoteDirectory(this UserSettings settings)
    {
        var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootFullDirectory().FullName, "Notes"));
        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static FileInfo? LocalSiteNoteHtmlFile(this UserSettings settings, NoteContent? content)
    {
        if (string.IsNullOrWhiteSpace(content?.Slug)) return null;

        var directory = settings.LocalSiteNoteContentDirectory(content);
        return new FileInfo($"{Path.Combine(directory.FullName, content.Slug)}.html");
    }

    public static FileInfo LocalSiteNoteListFile(this UserSettings settings)
    {
        var directory = settings.LocalSiteNoteDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, "NoteList")}.html");
    }

    public static FileInfo LocalSiteNoteRssFile(this UserSettings settings)
    {
        var directory = settings.LocalSiteNoteDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, "NoteRss")}.xml");
    }

    public static DirectoryInfo LocalSitePhotoContentDirectory(this UserSettings settings, PhotoContent content,
        bool createDirectoryIfNotFound = true)
    {
        if (string.IsNullOrWhiteSpace(content.Folder))
            throw new NullReferenceException(
                $"{nameof(LocalSitePhotoContentDirectory)} Null or Blank for the content.Folder of {content.Title}");

        if (string.IsNullOrWhiteSpace(content.Slug))
            throw new NullReferenceException(
                $"{nameof(LocalSitePhotoContentDirectory)} Null or Blank for the content.Slug of {content.Title}");

        var directory = new DirectoryInfo(Path.Combine(settings.LocalSitePhotoDirectory().FullName, content.Folder,
            content.Slug));

        if (directory.Exists || !createDirectoryIfNotFound) return directory;

        directory.Create();
        directory.Refresh();

        return directory;
    }

    public static FileInfo? LocalSitePhotoContentFile(this UserSettings settings, PhotoContent? content)
    {
        if (string.IsNullOrWhiteSpace(content?.OriginalFileName)) return null;

        var directory = settings.LocalSitePhotoContentDirectory(content, false);

        return new FileInfo(Path.Combine(directory.FullName, content.OriginalFileName));
    }

    public static DirectoryInfo LocalSitePhotoDirectory(this UserSettings settings)
    {
        var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootFullDirectory().FullName, "Photos"));
        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static DirectoryInfo LocalSitePhotoGalleryDirectory(this UserSettings settings)
    {
        var directory =
            new DirectoryInfo(Path.Combine(settings.LocalSiteRootFullDirectory().FullName, "Photos", "Galleries"));
        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static FileInfo? LocalSitePhotoHtmlFile(this UserSettings settings, PhotoContent? content)
    {
        if (string.IsNullOrWhiteSpace(content?.Slug)) return null;
        var directory = settings.LocalSitePhotoContentDirectory(content);
        return new FileInfo($"{Path.Combine(directory.FullName, content.Slug)}.html");
    }

    public static FileInfo LocalSitePhotoListFile(this UserSettings settings)
    {
        var directory = settings.LocalSitePhotoDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, "PhotoList")}.html");
    }

    public static FileInfo LocalSitePhotoRssFile(this UserSettings settings)
    {
        var directory = settings.LocalSitePhotoDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, "PhotoRss")}.xml");
    }

    public static DirectoryInfo LocalSitePointContentDirectory(this UserSettings settings, PointContent content,
        bool createDirectoryIfNotFound = true)
    {
        if (string.IsNullOrWhiteSpace(content.Folder))
            throw new NullReferenceException(
                $"{nameof(LocalSitePointContentDirectory)} Null or Blank for the content.Folder of {content.Title}");

        if (string.IsNullOrWhiteSpace(content.Slug))
            throw new NullReferenceException(
                $"{nameof(LocalSitePointContentDirectory)} Null or Blank for the content.Slug of {content.Title}");

        var directory = new DirectoryInfo(Path.Combine(settings.LocalSitePointDirectory().FullName, content.Folder,
            content.Slug));

        if (directory.Exists || !createDirectoryIfNotFound) return directory;

        directory.Create();
        directory.Refresh();

        return directory;
    }

    public static DirectoryInfo LocalSitePointContentDirectory(this UserSettings settings, PointContentDto content,
        bool createDirectoryIfNotFound = true)
    {
        if (string.IsNullOrWhiteSpace(content.Folder))
            throw new NullReferenceException(
                $"{nameof(LocalSitePointContentDirectory)} Null or Blank for the content.Folder of {content.Title}");

        if (string.IsNullOrWhiteSpace(content.Slug))
            throw new NullReferenceException(
                $"{nameof(LocalSitePointContentDirectory)} Null or Blank for the content.Slug of {content.Title}");

        var directory = new DirectoryInfo(Path.Combine(settings.LocalSitePointDirectory().FullName, content.Folder,
            content.Slug));

        if (directory.Exists || !createDirectoryIfNotFound) return directory;

        directory.Create();
        directory.Refresh();

        return directory;
    }

    public static DirectoryInfo LocalSitePointDataDirectory(this UserSettings settings)
    {
        var directory = new DirectoryInfo(Path.Combine(LocalSitePointDirectory(settings).FullName, "Data"));
        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static FileInfo LocalSitePointDataFile(this UserSettings settings)
    {
        var directory = settings.LocalSitePointDataDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, "pointdata")}.json");
    }

    public static DirectoryInfo LocalSitePointDirectory(this UserSettings settings)
    {
        var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootFullDirectory().FullName, "Points"));
        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }


    public static FileInfo? LocalSitePointHtmlFile(this UserSettings settings, PointContent? content)
    {
        if (string.IsNullOrWhiteSpace(content?.Slug)) return null;

        var directory = settings.LocalSitePointContentDirectory(content);
        return new FileInfo($"{Path.Combine(directory.FullName, content.Slug)}.html");
    }

    public static FileInfo? LocalSitePointHtmlFile(this UserSettings settings, PointContentDto? content)
    {
        if (string.IsNullOrWhiteSpace(content?.Slug)) return null;

        var directory =
            settings.LocalSitePointContentDirectory(Db.PointContentDtoToPointContentAndDetails(content).content);
        return new FileInfo($"{Path.Combine(directory.FullName, content.Slug)}.html");
    }

    public static FileInfo LocalSitePointListFile(this UserSettings settings)
    {
        var directory = settings.LocalSitePointDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, "PointList")}.html");
    }

    public static FileInfo LocalSitePointRssFile(this UserSettings settings)
    {
        var directory = settings.LocalSitePointDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, "PointRss")}.xml");
    }

    public static DirectoryInfo LocalSitePostContentDirectory(this UserSettings settings, PostContent content,
        bool createDirectoryIfNotFound = true)
    {
        if (string.IsNullOrWhiteSpace(content.Folder))
            throw new NullReferenceException(
                $"{nameof(LocalSitePostContentDirectory)} Null or Blank for the content.Folder of {content.Title}");

        if (string.IsNullOrWhiteSpace(content.Slug))
            throw new NullReferenceException(
                $"{nameof(LocalSitePostContentDirectory)} Null or Blank for the content.Slug of {content.Title}");

        var directory = new DirectoryInfo(Path.Combine(settings.LocalSitePostDirectory().FullName, content.Folder,
            content.Slug));

        if (directory.Exists || !createDirectoryIfNotFound) return directory;

        directory.Create();
        directory.Refresh();

        return directory;
    }

    public static DirectoryInfo LocalSitePostDirectory(this UserSettings settings)
    {
        var localDirectory =
            new DirectoryInfo(Path.Combine(settings.LocalSiteRootFullDirectory().FullName, "Posts"));
        if (!localDirectory.Exists) localDirectory.Create();

        localDirectory.Refresh();

        return localDirectory;
    }

    public static FileInfo? LocalSitePostHtmlFile(this UserSettings settings, PostContent? content)
    {
        if (string.IsNullOrWhiteSpace(content?.Slug)) return null;

        var directory = settings.LocalSitePostContentDirectory(content);
        return new FileInfo($"{Path.Combine(directory.FullName, content.Slug)}.html");
    }

    public static FileInfo LocalSitePostListFile(this UserSettings settings)
    {
        var directory = settings.LocalSitePostDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, "PostList")}.html");
    }

    public static FileInfo LocalSitePostRssFile(this UserSettings settings)
    {
        var directory = settings.LocalSitePostDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, "PostRss")}.xml");
    }

    public static DirectoryInfo LocalSiteRootFullDirectory(this UserSettings settings)
    {
        var directory = new DirectoryInfo(settings.LocalSiteRootDirectory.AddSettingsFileRootDirectoryIfNeeded());

        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static FileInfo LocalSiteRssIndexFeedListFile(this UserSettings settings)
    {
        var directory = settings.LocalSiteRootFullDirectory().FullName;
        return new FileInfo($"{Path.Combine(directory, "RssIndexFeed")}.xml");
    }

    public static DirectoryInfo LocalSiteSiteResourcesDirectory(this UserSettings settings)
    {
        var directory =
            new DirectoryInfo(Path.Combine(settings.LocalSiteRootFullDirectory().FullName, "SiteResources"));
        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static FileInfo LocalSiteTagListFileInfo(this UserSettings settings, string tag)
    {
        var directory = settings.LocalSiteTagsDirectory();
        var sluggedTag = SlugTools.CreateSlug(true, tag, 200);
        return new FileInfo($"{Path.Combine(directory.FullName, $"TagList-{sluggedTag}")}.html");
    }


    public static DirectoryInfo LocalSiteTagsDirectory(this UserSettings settings)
    {
        var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootFullDirectory().FullName, "Tags"));
        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }


    public static DirectoryInfo LocalSiteVideoContentDirectory(this UserSettings settings, VideoContent content,
        bool createDirectoryIfNotFound = true)
    {
        if (string.IsNullOrWhiteSpace(content.Folder))
            throw new NullReferenceException(
                $"{nameof(LocalSiteVideoContentDirectory)} Null or Blank for the content.Folder of {content.Title}");

        if (string.IsNullOrWhiteSpace(content.Slug))
            throw new NullReferenceException(
                $"{nameof(LocalSiteVideoContentDirectory)} Null or Blank for the content.Slug of {content.Title}");

        var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteVideoDirectory().FullName, content.Folder,
            content.Slug));

        if (directory.Exists || !createDirectoryIfNotFound) return directory;

        directory.Create();
        directory.Refresh();

        return directory;
    }

    public static FileInfo? LocalSiteVideoContentFile(this UserSettings settings, VideoContent? content)
    {
        if (string.IsNullOrWhiteSpace(content?.OriginalFileName)) return null;

        var directory = settings.LocalSiteVideoContentDirectory(content, false);

        return new FileInfo(Path.Combine(directory.FullName, content.OriginalFileName));
    }

    public static DirectoryInfo LocalSiteVideoDirectory(this UserSettings settings)
    {
        var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootFullDirectory().FullName, "Videos"));
        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static FileInfo? LocalSiteVideoHtmlFile(this UserSettings settings, VideoContent? content)
    {
        if (string.IsNullOrWhiteSpace(content?.Slug)) return null;

        var directory = settings.LocalSiteVideoContentDirectory(content);
        return new FileInfo($"{Path.Combine(directory.FullName, content.Slug)}.html");
    }

    public static FileInfo LocalSiteVideoListVideo(this UserSettings settings)
    {
        var directory = settings.LocalSiteVideoDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, "VideoList")}.html");
    }

    public static FileInfo LocalSiteVideoRssVideo(this UserSettings settings)
    {
        var directory = settings.LocalSiteVideoDirectory();
        return new FileInfo($"{Path.Combine(directory.FullName, "VideoRss")}.xml");
    }

    public static string NoteListUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/Notes/NoteList.html";
    }

    public static string NotePageUrl(this UserSettings settings, NoteContent content)
    {
        return $"{settings.SiteUrl()}/Notes/{content.Folder}/{content.Slug}.html";
    }

    public static string NoteRssUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/Notes/NoteRss.xml";
    }

    public static async Task<string> PageUrl(this UserSettings settings, Guid contentGuid)
    {
        var db = await Db.Context().ConfigureAwait(false);
        var content = await db.ContentFromContentId(contentGuid).ConfigureAwait(false);

        //!!Content Type List!!

        return content switch
        {
            FileContent c => settings.FilePageUrl(c),
            GeoJsonContent c => settings.GeoJsonPageUrl(c),
            ImageContent c => settings.ImagePageUrl(c),
            LineContent c => settings.LinePageUrl(c),
            LinkContent => settings.LinkListUrl(),
            NoteContent c => settings.NotePageUrl(c),
            PhotoContent c => settings.PhotoPageUrl(c),
            PointContent c => settings.PointPageUrl(c),
            PointContentDto c => settings.PointPageUrl(Db.PointContentDtoToPointContentAndDetails(c).content),
            PostContent c => settings.PostPageUrl(c),
            VideoContent c => settings.VideoPageUrl(c),
            _ => throw new DataException("Content not Found")
        };
    }

    public static string PhotoListUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/Photos/PhotoList.html";
    }

    public static string PhotoPageUrl(this UserSettings settings, PhotoContent content)
    {
        return $"{settings.SiteUrl()}/Photos/{content.Folder}/{content.Slug}/{content.Slug}.html";
    }

    public static string PhotoPageUrlLocalPreviewProtocol(this UserSettings settings, PhotoContent content)
    {
        return $"{settings.SiteUrlWithLocalPreviewProtocol()}/Photos/{content.Folder}/{content.Slug}/{content.Slug}.html";
    }

    public static string PhotoRssUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/Photos/PhotoRss.xml";
    }

    public static string PicturePageUrl(this UserSettings settings, Guid contentGuid)
    {
        var db = Db.Context().Result;

        var possiblePhotoContent = db.PhotoContents.SingleOrDefault(x => x.ContentId == contentGuid);
        if (possiblePhotoContent != null) return settings.PhotoPageUrl(possiblePhotoContent);

        var possibleImageContent = db.ImageContents.SingleOrDefault(x => x.ContentId == contentGuid);
        if (possibleImageContent != null) return settings.ImagePageUrl(possibleImageContent);

        return string.Empty;
    }

    public static string PointDataUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/Points/Data/pointdata.json";
    }

    public static string PointPageUrl(this UserSettings settings, PointContent content)
    {
        return $"{settings.SiteUrl()}/Points/{content.Folder}/{content.Slug}/{content.Slug}.html";
    }

    public static string PointPageUrl(this UserSettings settings, PointContentDto content)
    {
        return $"{settings.SiteUrl()}/Points/{content.Folder}/{content.Slug}/{content.Slug}.html";
    }

    public static string PointsListUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/Points/PointList.html";
    }

    public static string PointsRssUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/Points/PointRss.xml";
    }

    public static string PostPageUrl(this UserSettings settings, PostContent content)
    {
        return $"{settings.SiteUrl()}/Posts/{content.Folder}/{content.Slug}/{content.Slug}.html";
    }

    public static string PostsListUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/Posts/PostList.html";
    }

    public static string PostsRssUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/Posts/PostRss.xml";
    }

    public static async Task<UserSettings> ReadFromCurrentSettingsFile(IProgress<string>? progress = null)
    {
        var currentFile = SettingsFile();

        return await ReadFromSettingsFile(currentFile, progress);
    }

    public static async Task<UserSettings> ReadFromSettingsFile(FileInfo fileToRead, IProgress<string>? progress = null)
    {
        if (!fileToRead.Exists)
            throw new InvalidDataException($"Settings file {fileToRead.FullName} doesn't exist?");

        if (fileToRead.Directory == null)
            throw new InvalidDataException($"Settings file {fileToRead.FullName} doesn't have a valid directory?");

        progress?.Report($"Reading and deserializing {fileToRead.FullName}");

        await using var fs = new FileStream(fileToRead.FullName, FileMode.Open, FileAccess.Read);
        var sr = new StreamReader(fs);
        var iniFileReader = new StreamIniDataParser();
        var iniResult = iniFileReader.ReadData(sr);

        var currentProperties = typeof(UserSettings).GetProperties();

        var readResult = new UserSettings();

        foreach (var loopProperties in currentProperties)
        {
            var propertyExists = iniResult.TryGetKey(loopProperties.Name, out var existingValue);

            if (!propertyExists) continue;

            if (loopProperties.PropertyType == typeof(string))
            {
                loopProperties.SetValue(readResult, existingValue.TrimNullToEmpty());
                continue;
            }

            if (loopProperties.PropertyType == typeof(bool))
            {
                var valueTranslated = bool.TryParse(existingValue, out var translated);

                if (valueTranslated)
                    loopProperties.SetValue(readResult, translated);

                continue;
            }

            if (loopProperties.PropertyType == typeof(double))
            {
                var valueTranslated = double.TryParse(existingValue, out var translated);

                if (valueTranslated)
                    loopProperties.SetValue(readResult, translated);

                continue;
            }

            if (loopProperties.PropertyType == typeof(int))
            {
                var valueTranslated = int.TryParse(existingValue, out var translated);

                if (valueTranslated)
                    loopProperties.SetValue(readResult, translated);

                continue;
            }

            if (loopProperties.PropertyType == typeof(Guid))
            {
                var valueTranslated = Guid.TryParse(existingValue, out var translated);

                if (valueTranslated)
                    loopProperties.SetValue(readResult, translated);

                continue;
            }

            throw new NotSupportedException(
                $"The use of the type {loopProperties.PropertyType} in User Settings is not supported...");
        }

        var timeStampForMissingValues = $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss-fff}";

        var hasUpdates = false;

        progress?.Report("Checking for missing values in settings...");

        if (string.IsNullOrWhiteSpace(readResult.DatabaseFile))
        {
            //This could fail for all kinds of interesting reasons but for the purposes of this program I am not sure that
            //industrial strength name collision avoidance is needed
            readResult.DatabaseFile = $"PointlessWaymarksData-{timeStampForMissingValues}.db";

            hasUpdates = true;
        }

        if (string.IsNullOrWhiteSpace(readResult.LocalSiteRootDirectory))
        {
            var newLocalSiteRoot = new DirectoryInfo(Path.Combine(fileToRead.Directory.FullName,
                timeStampForMissingValues, $"PointlessWaymarks-Site-{timeStampForMissingValues}"));

            newLocalSiteRoot.CreateIfItDoesNotExist();
            readResult.LocalSiteRootDirectory = Path.GetRelativePath(fileToRead.DirectoryName ?? string.Empty,
                newLocalSiteRoot.FullName);
            hasUpdates = true;
        }

        if (string.IsNullOrWhiteSpace(readResult.LocalMediaArchiveDirectory))
        {
            var newMediaArchive = new DirectoryInfo(Path.Combine(fileToRead.Directory.FullName,
                timeStampForMissingValues, $"PointlessWaymarks-MediaArchive-{timeStampForMissingValues}"));

            newMediaArchive.CreateIfItDoesNotExist();
            readResult.LocalMediaArchiveDirectory = Path.GetRelativePath(fileToRead.DirectoryName ?? string.Empty,
                newMediaArchive.FullName);
            hasUpdates = true;
        }

        if (string.IsNullOrWhiteSpace(readResult.DefaultCreatedBy))
        {
            readResult.DefaultCreatedBy = "Pointless Waymarks CMS";
            hasUpdates = true;
        }

        if (string.IsNullOrWhiteSpace(readResult.SiteName))
        {
            readResult.SiteName = "New Site";
            hasUpdates = true;
        }

        if (string.IsNullOrWhiteSpace(readResult.SiteDomainName))
        {
            readResult.SiteDomainName = "localhost.com";
            hasUpdates = true;
        }

        if (string.IsNullOrWhiteSpace(readResult.SiteKeywords))
        {
            readResult.SiteKeywords = "new,site";
            hasUpdates = true;
        }

        if (string.IsNullOrWhiteSpace(readResult.SiteSummary))
        {
            readResult.SiteSummary = "A new site.";
            hasUpdates = true;
        }

        if (string.IsNullOrWhiteSpace(readResult.SiteAuthors))
        {
            readResult.SiteAuthors = "Pointless Waymarks CMS";
            hasUpdates = true;
        }

        if (string.IsNullOrWhiteSpace(readResult.SiteEmailTo))
        {
            readResult.SiteEmailTo = "email.to@nowhere.com";
            hasUpdates = true;
        }

        if (string.IsNullOrWhiteSpace(readResult.SiteLangAttribute))
        {
            readResult.SiteLangAttribute = "en";
            hasUpdates = true;
        }

        if (string.IsNullOrWhiteSpace(readResult.SiteDirectionAttribute))
        {
            readResult.SiteDirectionAttribute = "ltr";
            hasUpdates = true;
        }

        if (readResult.SettingsId == Guid.Empty)
        {
            readResult.SettingsId = Guid.NewGuid();
            hasUpdates = true;
        }

        if (hasUpdates)
        {
            progress?.Report("Found missing values - writing defaults back to settings.");
            await WriteSettings(readResult).ConfigureAwait(false);
        }

        return readResult;
    }

    public static string RssIndexFeedUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/RssIndexFeed.xml";
    }

    public static string SearchListJavascriptUrl()
    {
        return
            $"{UserSettingsSingleton.CurrentSettings().SiteResourcesUrl()}pointless-waymarks-content-list-search.js";
    }

    public static FileInfo SettingsFile()
    {
        return new FileInfo(SettingsFileFullName);
    }

    /// <summary>
    /// </summary>
    /// <param name="userFilename">File Name for the </param>
    /// <param name="progress"></param>
    /// <returns></returns>
    public static async Task<UserSettings> SetupNewSite(string userFilename, IProgress<string>? progress = null)
    {
        if (!FileAndFolderTools.IsValidWindowsFileSystemFilename(userFilename))
            throw new InvalidDataException("New site input must be a valid filename.");

        var newSettings = new UserSettings();

        var rootDirectory =
            new DirectoryInfo(Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName, userFilename));

        progress?.Report("Creating new settings - looking for home...");

        var fileNumber = 1;

        while (rootDirectory.Exists)
        {
            rootDirectory =
                new DirectoryInfo(Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
                    $"{userFilename}-{fileNumber}"));
            rootDirectory.Refresh();
            progress?.Report($"Trying {rootDirectory.FullName}...");
            fileNumber++;
        }

        rootDirectory.Create();

        newSettings.LocalSiteRootDirectory = "GeneratedSite";

        progress?.Report($"Local Site Root set to {newSettings.LocalSiteRootFullDirectory().FullName}");

        newSettings.DatabaseFile = $"PointlessWaymarksCmsDatabase-{userFilename}.db";

        newSettings.LocalMediaArchiveDirectory = "MediaArchive";

        progress?.Report("Adding fake default values...");

        newSettings.DefaultCreatedBy = "Pointless Waymarks CMS";
        newSettings.SiteName = userFilename;
        newSettings.SiteDomainName = "localhost.com";
        newSettings.SiteDirectionAttribute = "ltr";
        newSettings.SiteLangAttribute = "en";
        newSettings.SiteKeywords = "new,site";
        newSettings.SiteSummary = "A new site.";
        newSettings.SiteAuthors = "Pointless Waymarks CMS";
        newSettings.SiteEmailTo = "emailto@nowhere.com";
        newSettings.LatitudeDefault = ProjectDefaultLatitude;
        newSettings.LongitudeDefault = ProjectDefaultLongitude;
        newSettings.NumberOfItemsOnMainSitePage = 4;
        newSettings.SettingsId = Guid.NewGuid();

        SettingsFileFullName =
            Path.Combine(rootDirectory.FullName, $"PointlessWaymarksCmsSettings-{userFilename}.ini");

        progress?.Report("Writing Settings");

        await WriteSettings(newSettings).ConfigureAwait(false);

        progress?.Report("Setting up directory structure.");

        newSettings.VerifyOrCreateAllTopLevelFolders();
        await EnsureDbIsPresent(progress).ConfigureAwait(false);

        await FileManagement.WriteFavIconToGeneratedSite(progress).ConfigureAwait(false);
        await FileManagement.WriteStylesCssToGeneratedSite(progress).ConfigureAwait(false);
        await FileManagement.WriteSiteResourcesToGeneratedSite(progress).ConfigureAwait(false);

        return newSettings;
    }

    public static string SiteResourcesUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/SiteResources/";
    }

    public static RegionEndpoint? SiteS3BucketEndpoint(this UserSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.SiteS3BucketRegion)) return null;

        return RegionEndpoint.EnumerableAllRegions.SingleOrDefault(x =>
            x.SystemName == settings.SiteS3BucketRegion);
    }

    public static string SiteUrl(this UserSettings settings)
    {
        return $"https://{settings.SiteDomainName}";
    }

    public static string SiteUrlWithLocalPreviewProtocol(this UserSettings settings)
    {
        return $"localpreview://{settings.SiteDomainName}";
    }

    public static string StylesCssFromLocalSiteRootDirectory()
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var possibleFile = new FileInfo(Path.Combine(settings.LocalSiteRootFullDirectory().FullName, "style.css"));

        if (!possibleFile.Exists) return string.Empty;

        return File.ReadAllText(possibleFile.FullName);
    }

    public static string TagPageUrl(this UserSettings settings, string tag)
    {
        var sluggedTag = SlugTools.CreateSlug(true, tag, 200);
        return $"{settings.SiteUrl()}/Tags/TagList-{sluggedTag}.html";
    }

    public static IsValid ValidateLocalMediaArchive()
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        if (string.IsNullOrWhiteSpace(settings.LocalMediaArchiveDirectory))
            return new IsValid(false, "No Local File Root User Setting Found");

        try
        {
            var directory = new DirectoryInfo(settings.LocalMediaArchiveFullDirectory().FullName);
            if (!directory.Exists) directory.Create();
            directory.Refresh();
        }
        catch (Exception e)
        {
            return new IsValid(false, $"Trouble with Local Media Archive Directory - {e.Message}");
        }

        return new IsValid(true, string.Empty);
    }

    public static IsValid ValidateLocalSiteRootDirectory()
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        if (string.IsNullOrWhiteSpace(settings.LocalSiteRootFullDirectory().FullName))
            return new IsValid(false, "No Local File Root User Setting Found");

        try
        {
            var directory = settings.LocalSiteRootFullDirectory();
            if (!directory.Exists) directory.Create();
            directory.Refresh();
        }
        catch (Exception e)
        {
            return new IsValid(false, $"Trouble with Local File Root Directory - {e.Message}");
        }

        return new IsValid(true, string.Empty);
    }

    public static string VideoDownloadUrl(this UserSettings settings, VideoContent content)
    {
        return $"{settings.SiteUrl()}/Videos/{content.Folder}/{content.Slug}/{content.OriginalFileName}";
    }

    public static string VideoListUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/Videos/VideoList.html";
    }

    public static string VideoPageUrl(this UserSettings settings, VideoContent content)
    {
        return $"{settings.SiteUrl()}/Videos/{content.Folder}/{content.Slug}/{content.Slug}.html";
    }

    public static string VideoRssUrl(this UserSettings settings)
    {
        return $"{settings.SiteUrl()}/Videos/VideoRss.xml";
    }

    public static Task WriteSettings(this UserSettings toWrite)
    {
        var currentFile = SettingsFile();

        if (!currentFile.Exists)
        {
            var fileStream = currentFile.Create();
            fileStream.Close();
        }

        var iniFileReader = new FileIniDataParser();
        var iniResult = iniFileReader.ReadFile(currentFile.FullName);

        var currentProperties = typeof(UserSettings).GetProperties();

        foreach (var loopProperties in currentProperties)
        {
            var propertyExists = iniResult.TryGetKey(loopProperties.Name, out _);

            if (propertyExists)
                iniResult.Global[loopProperties.Name] = loopProperties.GetValue(toWrite)?.ToString();
            else
                iniResult.Global.AddKey(loopProperties.Name, loopProperties.GetValue(toWrite)?.ToString());
        }

        iniFileReader.WriteFile(currentFile.FullName, iniResult);

        return Task.CompletedTask;
    }
}