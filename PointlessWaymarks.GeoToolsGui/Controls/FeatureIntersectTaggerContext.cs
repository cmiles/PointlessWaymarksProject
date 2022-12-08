using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HtmlTableHelper;
using MetadataExtractor;
using MetadataExtractor.Formats.Xmp;
using NetTopologySuite.Features;
using Omu.ValueInjecter;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeatureIntersectionTags;
using PointlessWaymarks.FeatureIntersectionTags.Models;
using PointlessWaymarks.GeoToolsGui.Models;
using PointlessWaymarks.GeoToolsGui.Settings;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon.FileList;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using PointlessWaymarks.WpfCommon.WpfHtml;
using XmpCore;

namespace PointlessWaymarks.GeoToolsGui.Controls;

[ObservableObject]
public partial class FeatureIntersectTaggerContext
{
    [ObservableProperty] private bool _createBackups;
    [ObservableProperty] private bool _createBackupsInDefaultStorage;
    [ObservableProperty] private string _exifToolFullName = string.Empty;
    [ObservableProperty] private ObservableCollection<FeatureFileViewModel>? _featureFiles;
    [ObservableProperty] private FeatureFileEditorContext _featureFileToEdit;
    [ObservableProperty] private FileListViewModel _filesToTagFileList;
    [ObservableProperty] private FeatureIntersectionFilesToTagSettings _filesToTagSettings;
    [ObservableProperty] private string _infoTitle;
    [ObservableProperty] private List<IntersectFileTaggingResult> _lastResults = new();
    [ObservableProperty] private ObservableCollection<string>? _padUsAttributes;
    [ObservableProperty] private string _padUsAttributeToAdd = string.Empty;
    [ObservableProperty] private string _padUsDirectory = string.Empty;
    [ObservableProperty] private string _previewGeoJsonDto;
    [ObservableProperty] private string _previewHtml;
    [ObservableProperty] private bool _sanitizeTags;
    [ObservableProperty] private FeatureFileViewModel? _selectedFeatureFile;
    [ObservableProperty] private string? _selectedPadUsAttribute;
    [ObservableProperty] private int _selectedTab;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private bool _tagSpacesToHypens;
    [ObservableProperty] private bool _tagsToLowerCase;
    [ObservableProperty] private bool _testRunOnly;
    [ObservableProperty] private WindowIconStatus _windowStatus;

    public FeatureIntersectTaggerContext(StatusControlContext? statusContext, WindowIconStatus? windowStatus)
    {
        _statusContext = statusContext ?? new StatusControlContext();
        _windowStatus = windowStatus ?? new WindowIconStatus();

        FilesToTagSettings = new FeatureIntersectionFilesToTagSettings();
        FeatureFileToEdit = new FeatureFileEditorContext(StatusContext, new FeatureFileViewModel());
        FeatureFileToEdit.EndEdit += EndEdit;

        ChoosePadUsDirectoryCommand = StatusContext.RunBlockingTaskCommand(ChoosePadUsDirectory);
        AddPadUsAttributeCommand = StatusContext.RunNonBlockingTaskCommand(AddPadUsAttribute);
        RemovePadUsAttributeCommand = StatusContext.RunNonBlockingTaskCommand<string>(RemovePadUsAttribute);
        EditFeatureFileCommand = StatusContext.RunNonBlockingTaskCommand(EditFeatureFile);
        NewFeatureFileCommand = StatusContext.RunNonBlockingTaskCommand(NewFeatureFile);

        TagFilesCommand = StatusContext.RunBlockingTaskCommand(TagFiles);

        ImportSettingsFromFileCommand = StatusContext.RunBlockingTaskCommand(ImportSettingsFromFile);
        ExportSettingsFromFileCommand = StatusContext.RunBlockingTaskCommand(ExportSettingsToFile);
        SaveSettingsFromFileCommand = StatusContext.RunBlockingTaskCommand(WriteCurrentSettingsAndReturnSettings);

        MetadataForSelectedFilesToTagCommand = StatusContext.RunBlockingTaskCommand(MetadataForSelectedFilesToTag);
    }

    public RelayCommand AddPadUsAttributeCommand { get; set; }

    public RelayCommand ChoosePadUsDirectoryCommand { get; set; }

    public RelayCommand EditFeatureFileCommand { get; set; }

    public RelayCommand ExportSettingsFromFileCommand { get; set; }

    public string GeoJsonFileOverviewMarkdown => """
        Inside the USA the [USGS PAD-US Data](https://www.usgs.gov/programs/gap-analysis-project/science/pad-us-data-overview) (see previous tab) is a great resource for automatically identifying landscape ownership/management and generating tags. But there is a wide variety of other data that you might want to use or create.

        To generate tags from GeoJson files the following information has to be provided:

          - File Name - must be the full path and filename of a valid GeoJson file
          - Attributes For Tags - When an intersecting feature is found the program will add tags from the values of any attribute names listed here. This can be empty if a 'Tag All' value is provided.
          - Tag All - you may find data sets where you want any intersections tagged with a value rather than tagging based on the value of an attribute. An example is Arizona State Trust Land - in Arizona it is interesting and useful to know your hike included Arizona State Trust Land (a useful tag), but you might not care about any of the specifics list who leases the land, how is the land used, ... - in that case you could leave the 'Attributes For Tags' blank and set 'Tag All' to 'Arizona State Trust Land'.

        And you can also provide the information below which can be very useful to keep track of the data you are using: 
          - Name - Not needed for the program but can make keeping track of the data much simpler
          - Source - The source of the information - often a great choice for this field is a URL for the information
          - Downloaded On - The date you downloaded the information is suggested here. GeoJson data is not always 'versioned' in a useful way and without knowing when you downloaded a file it may be hard to know if your data is current.

        Regardless of the areas you are interested in and the availability of pre-existing data you are likely to find it useful to create your own GeoJson data to help automatically tag things like unofficial names, areas and trails that have local names that will never appear in official data and features and areas with personal significance! [geojson.io](https://geojson.io/) is one simple way to produce a reference file - for example you could draw a polygon around a local trail area, add a property that identifies its well known local name ("name": "My Special Trail Area"), save the file and create a Feature File entry for it with a 'Attributes for Tags' entry of 'name'. Official recognition and public data almost certainly don't define everything you care about on the landscape!
        """;

    public RelayCommand ImportSettingsFromFileCommand { get; set; }

    public RelayCommand MetadataForSelectedFilesToTagCommand { get; set; }

    public RelayCommand NewFeatureFileCommand { get; set; }

    public string PadUsOverviewMarkdown => """
        From the [USGS PAD-US Data Overview](https://www.usgs.gov/programs/gap-analysis-project/science/pad-us-data-overview):

        > PAD-US is America’s official national inventory of U.S. terrestrial and marine protected areas that are dedicated to the preservation of biological diversity and to other natural, recreation and cultural uses, managed for these purposes through legal or other effective means. PAD-US also includes the best available aggregation of federal land and marine areas provided directly by managing agencies, coordinated through the Federal Geographic Data Committee Federal Lands Working Group.

        The Protected Areas Database is likely the best single source for land ownership and management information for the US Landscape and forms an excellent basis for automatically generating landscape oriented tags.

        The large size of the PAD-US data is a challenge to using it efficiently. You can download State or Region files from PAD-US and enter them like you would any other GeoJson file in the next tab - but this program can use the PAD-US somewhat more efficiently if you take some time and download, setup and specifically configure the PAD-US data:
          - Create a directory dedicated to the PAD-US data - place on the Region Boundaries GeoJson file and Region GeoJson files in this directory. Enter the directory in this screen.
          - On the [U.S. Department of the Interior Unified Interior Regional Boundaries](https://www.doi.gov/employees/reorg/unified-regional-boundaries) site find and click the 'shapefiles (for mapping software)' link - this will download a zip file.
              - Extract the contents of the zip file. 
              - Use ogr2ogr (see the general help for information on this commandline program) to convert the data to GeoJson (rough template: \ogr2ogr.exe -f GeoJSON -t_srs crs:84 {path and name for destination GeoJson file} {path and name of the shapefile to convert}). 
              - Put the GeoJson output file into your PAD-US data directory
          - [PAD-US 3.0 Download data by Department of the Interior (DOI) Region GeoJSON - ScienceBase-Catalog](https://www.sciencebase.gov/catalog/item/622256afd34ee0c6b38b6bb7) - from this page click the 'Download data by Department of the Interior (DOI) Region GeoJSON' link, this will take you to a page where you can download any regions you are interested in. For each region:
            - Extract the zip file and place the GeoJson file in your PAD-US data directory
            - Ensure that the GeoJson has the expected coordinate reference system and format - for example  \ogr2ogr.exe -f GeoJSON -t_srs crs:84 C:\PointlessWaymarksPadUs\PADUS3_0Combined_Region1.geojson C:\PointlessWaymarksPadUs\PADUS3_0Combined_Region1.json.
        """;

    public RelayCommand<string> RemovePadUsAttributeCommand { get; set; }

    public RelayCommand SaveSettingsFromFileCommand { get; set; }

    public RelayCommand TagFilesCommand { get; set; }

    public async System.Threading.Tasks.Task AddPadUsAttribute()
    {
        if (string.IsNullOrEmpty(PadUsAttributeToAdd))
        {
            StatusContext.ToastWarning("Can't Add a Blank/Whitespace Only Attribute");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        PadUsAttributes!.Add(PadUsAttributeToAdd.Trim());

        PadUsAttributeToAdd = string.Empty;

        await FeatureIntersectionGuiSettingTools.SetPadUsAttributes(PadUsAttributes.ToList());
    }

    public async System.Threading.Tasks.Task ChoosePadUsDirectory()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        var lastDirectory = await FeatureIntersectionGuiSettingTools.GetPadUsDirectory();

        await ThreadSwitcher.ResumeForegroundAsync();
        var folderPicker = new VistaFolderBrowserDialog
            { Description = "Directory to Add", Multiselect = false };

        if (lastDirectory is { Exists: true }) folderPicker.SelectedPath = $"{lastDirectory.FullName}\\";

        var result = folderPicker.ShowDialog();

        if (!result ?? false) return;

        PadUsDirectory = folderPicker.SelectedPath;

        await FeatureIntersectionGuiSettingTools.SetPadUsDirectory(PadUsDirectory);
    }

    public static async Task<FeatureIntersectTaggerContext> CreateInstance(StatusControlContext? statusContext,
        WindowIconStatus? windowStatus)
    {
        var control = new FeatureIntersectTaggerContext(statusContext, windowStatus);
        await control.Load();
        return control;
    }

    public async System.Threading.Tasks.Task EditFeatureFile()
    {
        if (SelectedFeatureFile == null)
        {
            StatusContext.ToastWarning("Nothing Selected To Edit?");
            return;
        }

        FeatureFileToEdit.Show(SelectedFeatureFile);
    }

    private void EndEdit(object? sender, FeatureFileEditorEndEditCondition e)
    {
        if (e == FeatureFileEditorEndEditCondition.Cancelled) return;

        StatusContext.RunBlockingTask(RefreshFeatureFileList);
    }

    public async System.Threading.Tasks.Task ExportSettingsToFile()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var filePicker = new VistaSaveFileDialog
            { DefaultExt = "json", AddExtension = true, Filter = "Json Files | *.json" };

        if (!filePicker.ShowDialog() ?? false) return;

        var newFile = new FileInfo(filePicker.FileName);

        if (newFile.Exists && await StatusContext.ShowMessage("Overwrite Existing File?",
                $"The file {newFile.FullName} already exists - Overwrite that File?",
                new List<string> { "Yes", "No" }) == "No") return;

        var settings = await WriteCurrentSettingsAndReturnSettings();

        var jsonSettings = JsonSerializer.Serialize(settings);

        await File.WriteAllTextAsync(newFile.FullName, jsonSettings, CancellationToken.None);
    }

    public async System.Threading.Tasks.Task ImportSettingsFromFile()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var filePicker = new VistaOpenFileDialog
            { CheckFileExists = true, CheckPathExists = true, DefaultExt = "json", Filter = "Json Files | *.json" };

        if (!filePicker.ShowDialog() ?? false) return;

        var selectedFile = new FileInfo(filePicker.FileName);

        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!selectedFile.Exists)
        {
            StatusContext.ToastError("File Selected for Import does not Exist?");
            return;
        }

        var settings =
            JsonSerializer.Deserialize<IntersectSettings>(await File.ReadAllTextAsync(selectedFile.FullName));

        if (settings == null)
        {
            StatusContext.ToastError($"Couldn't convert {selectedFile.Name} into a valid Settings File?");
            return;
        }

        if (!string.IsNullOrWhiteSpace(settings.PadUsDirectory)) PadUsDirectory = settings.PadUsDirectory;

        await ThreadSwitcher.ResumeForegroundAsync();

        if (settings.PadUsAttributesForTags.Any())
        {
            PadUsAttributes.Clear();
            foreach (var loopPadUsAttributes in settings.PadUsAttributesForTags.OrderBy(x => x).ToList())
                PadUsAttributes.Add(loopPadUsAttributes);
        }

        if (settings.PadUsAttributesForTags.Any())
        {
            FeatureFiles.Clear();
            foreach (var loopFeatureFile in settings.IntersectFiles.OrderBy(x => x.Name).ToList())
                FeatureFiles.Add(new FeatureFileViewModel
                {
                    Name = loopFeatureFile.Name,
                    FileName = loopFeatureFile.FileName,
                    TagAll = loopFeatureFile.TagAll,
                    AttributesForTags = loopFeatureFile.AttributesForTags.OrderBy(x => x).ToList(),
                    Source = loopFeatureFile.Source,
                    Downloaded = loopFeatureFile.Downloaded
                });
        }

        await WriteCurrentSettingsAndReturnSettings();
    }

    private async System.Threading.Tasks.Task Load()
    {
        FilesToTagFileList =
            await FileListViewModel.CreateInstance(StatusContext, FilesToTagSettings,
                new List<ContextMenuItemData>
                {
                    new()
                    {
                        ItemCommand = MetadataForSelectedFilesToTagCommand, ItemName = "Metadata Report for Selected"
                    }
                });

        var settings = await FeatureIntersectionGuiSettingTools.ReadSettings();
        PadUsDirectory = settings.PadUsDirectory;

        var featureFiles = settings.FeatureIntersectFiles.Select(x => new FeatureFileViewModel().InjectFrom(x))
            .Cast<FeatureFileViewModel>().ToList();

        await LoadTaggerSetting();

        await ThreadSwitcher.ResumeForegroundAsync();

        PadUsAttributes = new ObservableCollection<string>();
        settings.PadUsAttributes.OrderBy(x => x).ToList().ForEach(x => PadUsAttributes.Add(x));

        FeatureFiles = new ObservableCollection<FeatureFileViewModel>(featureFiles);

        PreviewHtml = WpfHtmlDocument.ToHtmlLeafletBasicGeoJsonDocument("Tagged Features and Intersect Features",
            32.12063, -110.52313, string.Empty);
    }

    public async System.Threading.Tasks.Task LoadTaggerSetting()
    {
        var settings = await FeatureIntersectionGuiSettingTools.ReadSettings();
        ExifToolFullName = settings.ExifToolFullName;
        CreateBackups = settings.CreateBackups;
        CreateBackupsInDefaultStorage = settings.CreateBackupsInDefaultStorage;
        TestRunOnly = settings.TestRunOnly;
        TagsToLowerCase = settings.TagsToLowerCase;
        SanitizeTags = settings.SanitizeTags;
        TagSpacesToHypens = settings.TagSpacesToHyphens;
        await FeatureIntersectionGuiSettingTools.WriteSettings(settings);
    }

    public async System.Threading.Tasks.Task MetadataForSelectedFilesToTag()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (FilesToTagFileList.SelectedFiles == null)
        {
            StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        var frozenSelected = FilesToTagFileList.SelectedFiles.ToList();

        if (!frozenSelected.Any())
        {
            StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        if (frozenSelected.Count > 10)
        {
            StatusContext.ToastWarning("Sorry - dumping metadata limited to 10 files at once...");
            return;
        }

        foreach (var loopFile in frozenSelected)
        {
            loopFile.Refresh();
            if (!loopFile.Exists)
            {
                StatusContext.ToastWarning($"File {loopFile.FullName} no longer exists?");
                continue;
            }

            var htmlParts = new List<string>();

            if (loopFile.Extension.Equals(".xmp", StringComparison.OrdinalIgnoreCase))
            {
                IXmpMeta xmp;
                await using (var stream = File.OpenRead(loopFile.FullName))
                {
                    xmp = XmpMetaFactory.Parse(stream);
                }

                htmlParts.Add(xmp.Properties.OrderBy(x => x.Namespace).ThenBy(x => x.Path)
                    .Select(x => new { x.Namespace, x.Path, x.Value })
                    .ToHtmlTable(new { @class = "pure-table pure-table-striped" }));
            }
            else
            {
                var photoMetaTags = ImageMetadataReader.ReadMetadata(loopFile.FullName);

                htmlParts.Add(photoMetaTags.SelectMany(x => x.Tags).OrderBy(x => x.DirectoryName).ThenBy(x => x.Name)
                    .ToList().Select(x => new
                    {
                        DataType = x.Type.ToString(),
                        x.DirectoryName,
                        Tag = x.Name,
                        TagValue = x.Description?.SafeObjectDump()
                    }).ToHtmlTable(new { @class = "pure-table pure-table-striped" }));

                var xmpDirectory = ImageMetadataReader.ReadMetadata(loopFile.FullName).OfType<XmpDirectory>()
                    .FirstOrDefault();

                var xmpMetadata = xmpDirectory?.GetXmpProperties()
                    .Select(x => new { XmpKey = x.Key, XmpValue = x.Value })
                    .ToHtmlTable(new { @class = "pure-table pure-table-striped" });

                if (!string.IsNullOrWhiteSpace(xmpMetadata)) htmlParts.Add(xmpMetadata);
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            var file = new FileInfo(Path.Combine(UserSettingsUtilities.TempStorageDirectory().FullName,
                $"PhotoMetadata-{Path.GetFileNameWithoutExtension(loopFile.Name)}-{DateTime.Now:yyyy-MM-dd---HH-mm-ss}.htm"));

            var htmlString =
                await
                    $"<h1>Metadata Report:</h1><h1>{HttpUtility.HtmlEncode(loopFile.FullName)}</h1><br><h1>Metadata</h1><br>{string.Join("<br><br>", htmlParts)}"
                        .ToHtmlDocumentWithPureCss("File Metadata", "body {margin: 12px;}");

            await File.WriteAllTextAsync(file.FullName, htmlString);

            var ps = new ProcessStartInfo(file.FullName) { UseShellExecute = true, Verb = "open" };
            Process.Start(ps);
        }
    }

    public async System.Threading.Tasks.Task NewFeatureFile()
    {
        FeatureFileToEdit.Show(new FeatureFileViewModel());
    }

    public async System.Threading.Tasks.Task RefreshFeatureFileList()
    {
        var currentList = (await FeatureIntersectionGuiSettingTools.ReadSettings()).FeatureIntersectFiles;

        await ThreadSwitcher.ResumeForegroundAsync();

        FeatureFiles!.Clear();
        currentList.ForEach(x => FeatureFiles.Add(x));
    }

    public async System.Threading.Tasks.Task RemovePadUsAttribute(string toRemove)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        if (PadUsAttributes!.Contains(toRemove))
        {
            PadUsAttributes.Remove(toRemove);
            await FeatureIntersectionGuiSettingTools.SetPadUsAttributes(PadUsAttributes.ToList());
        }
    }

    public async System.Threading.Tasks.Task TagFiles()
    {
        await WriteTaggerSetting();

        var settings = await FeatureIntersectionGuiSettingTools.ReadSettings();

        var featureFiles = settings.FeatureIntersectFiles
            .Select(x => new FeatureFile(x.Source, x.Name, x.AttributesForTags, x.TagAll, x.FileName, x.Downloaded))
            .ToList();

        var intersectSettings = new IntersectSettings(featureFiles, settings.PadUsDirectory, settings.PadUsAttributes);

        var fileTags = await FilesToTagFileList.Files.ToList()
            .FileIntersectionTags(intersectSettings, CancellationToken.None, StatusContext.ProgressTracker());

        LastResults = await fileTags.WriteTagsToFiles(
            settings.TestRunOnly, settings.CreateBackups, settings.CreateBackupsInDefaultStorage,
            settings.TagsToLowerCase, settings.SanitizeTags, settings.TagSpacesToHyphens,
            settings.ExifToolFullName, CancellationToken.None, 1024, StatusContext.ProgressTracker());

        SelectedTab = 4;

        var allFeatures = LastResults.Where(x => x.IntersectInformation?.Features != null)
            .SelectMany(x => x.IntersectInformation.Features)
            .Union(LastResults.Where(x => x.IntersectInformation?.IntersectsWith != null)
                .SelectMany(x => x.IntersectInformation.IntersectsWith)).Distinct(new FeatureComparer()).ToList();

        var bounds = GeoJsonTools.GeometryBoundingBox(allFeatures.Select(x => x.Geometry).ToList());

        var featureCollection = new FeatureCollection();
        allFeatures.ForEach(x => featureCollection.Add(x));

        var jsonDto = new GeoJsonData.GeoJsonSiteJsonData(Guid.NewGuid().ToString(),
            new GeoJsonData.SpatialBounds(bounds.MaxY, bounds.MaxX, bounds.MinY, bounds.MinX), featureCollection);

        PreviewGeoJsonDto = await GeoJsonTools.SerializeWithGeoJsonSerializer(jsonDto);
    }

    private async Task<IntersectSettings> WriteCurrentSettingsAndReturnSettings()
    {
        await WriteTaggerSetting();

        var settings = await FeatureIntersectionGuiSettingTools.ReadSettings();

        var featureFiles = settings.FeatureIntersectFiles
            .Select(x => new FeatureFile(x.Source, x.Name, x.AttributesForTags, x.TagAll, x.FileName, x.Downloaded))
            .ToList();

        return new IntersectSettings(featureFiles, settings.PadUsDirectory, settings.PadUsAttributes);
    }

    public async System.Threading.Tasks.Task WriteTaggerSetting()
    {
        var settings = await FeatureIntersectionGuiSettingTools.ReadSettings();
        settings.ExifToolFullName = ExifToolFullName;
        settings.CreateBackups = CreateBackups;
        settings.CreateBackupsInDefaultStorage = CreateBackupsInDefaultStorage;
        settings.TestRunOnly = TestRunOnly;
        settings.TagsToLowerCase = TagsToLowerCase;
        settings.SanitizeTags = SanitizeTags;
        settings.FeatureIntersectFiles = FeatureFiles?.ToList() ?? new List<FeatureFileViewModel>();
        await FeatureIntersectionGuiSettingTools.WriteSettings(settings);
    }
}