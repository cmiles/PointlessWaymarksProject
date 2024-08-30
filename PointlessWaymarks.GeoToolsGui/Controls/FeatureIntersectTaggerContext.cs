using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Web;
using CommunityToolkit.Mvvm.Messaging;
using HtmlTableHelper;
using MetadataExtractor;
using MetadataExtractor.Formats.Xmp;
using NetTopologySuite.Features;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeatureIntersectionTags;
using PointlessWaymarks.FeatureIntersectionTags.Models;
using PointlessWaymarks.GeoToolsGui.Messages;
using PointlessWaymarks.GeoToolsGui.Models;
using PointlessWaymarks.GeoToolsGui.Settings;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.FileList;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using PointlessWaymarks.WpfCommon.WpfHtml;
using XmpCore;
using Directory = System.IO.Directory;

namespace PointlessWaymarks.GeoToolsGui.Controls;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class FeatureIntersectTaggerContext
{
    public FeatureIntersectTaggerContext(StatusControlContext? statusContext, WindowIconStatus? windowStatus)
    {
        StatusContext = statusContext ?? new StatusControlContext();
        WindowStatus = windowStatus ?? new WindowIconStatus();

        BuildCommands();

        Settings = new FeatureIntersectTaggerSettings();

        FeatureFileToEdit =
            new FeatureFileEditorContext(StatusContext, new FeatureFileContext(), []);
        FeatureFileToEdit.EndEdit += EndEdit;
    }

    public bool ExifToolExists { get; set; }
    public FeatureFileEditorContext? FeatureFileToEdit { get; set; }
    public FileListContext? FilesToTagFileList { get; set; }
    public FeatureIntersectTaggerFilesToTagSettings? FilesToTagSettings { get; set; }

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

    public string PadUsAttributeToAdd { get; set; } = string.Empty;

    public string PadUsOverviewMarkdown => """
        From the [USGS PAD-US Data Overview](https://www.usgs.gov/programs/gap-analysis-project/science/pad-us-data-overview):

        > PAD-US is America’s official national inventory of U.S. terrestrial and marine protected areas that are dedicated to the preservation of biological diversity and to other natural, recreation and cultural uses, managed for these purposes through legal or other effective means. PAD-US also includes the best available aggregation of federal land and marine areas provided directly by managing agencies, coordinated through the Federal Geographic Data Committee Federal Lands Working Group.

        The Protected Areas Database is likely the best single source for land ownership and management information for the US Landscape and forms an excellent basis for automatically generating landscape oriented tags.

        The large size of the PAD-US data is a challenge to using it efficiently. You can download State or Region files from PAD-US and enter them like you would any other GeoJson file in the next tab - but this program can use the PAD-US somewhat more efficiently if you take some time and download, setup and specifically configure the PAD-US data:
          - Create a directory dedicated to the PAD-US data - place on the Region Boundaries GeoJson file and Region GeoJson files in this directory. Enter the directory in this screen.
          - On the [U.S. Department of the Interior Unified Interior Regional Boundaries](https://www.doi.gov/employees/reorg/unified-regional-boundaries) site find and click the 'shapefiles (for mapping software)' link - this will download a zip file.
              - Extract the contents of the zip file. 
              - Use ogr2ogr (see the general help for information on this command-line program) to convert the data to GeoJson (rough template: \ogr2ogr.exe -f GeoJSON -t_srs crs:84 {path and name for destination GeoJson file} {path and name of the shapefile to convert}). 
              - Put the GeoJson output file into your PAD-US data directory
          - [PAD-US 3.0 Download data by Department of the Interior (DOI) Region GeoJSON - ScienceBase-Catalog](https://www.sciencebase.gov/catalog/item/622256afd34ee0c6b38b6bb7) - from this page click the 'Download data by Department of the Interior (DOI) Region GeoJSON' link, this will take you to a page where you can download any regions you are interested in. For each region:
            - Extract the zip file and place the GeoJson file in your PAD-US data directory
            - Ensure that the GeoJson has the expected coordinate reference system and format - for example  \ogr2ogr.exe -f GeoJSON -t_srs crs:84 C:\PointlessWaymarksPadUs\PADUS3_0Combined_Region1.geojson C:\PointlessWaymarksPadUs\PADUS3_0Combined_Region1.json.
        """;

    public List<IntersectFileTaggingResult> PreviewResults { get; set; } = [];
    public FeatureFileContext? SelectedFeatureFile { get; set; }
    public string? SelectedPadUsAttribute { get; set; }
    public int SelectedTab { get; set; }
    public FeatureIntersectTaggerSettings Settings { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public WindowIconStatus WindowStatus { get; set; }
    public List<IntersectFileTaggingResult> WriteToFileResults { get; set; } = [];

    [NonBlockingCommand]
    public async Task AddPadUsAttribute()
    {
        if (string.IsNullOrWhiteSpace(PadUsAttributeToAdd))
        {
            await StatusContext.ToastWarning("Can't Add a Blank/Whitespace Only Attribute");
            return;
        }

        PadUsAttributeToAdd = PadUsAttributeToAdd.Trim();

        Debug.Assert(Settings != null, nameof(Settings) + " != null");

        if (Settings.PadUsAttributes.Any(x => x.Equals(PadUsAttributeToAdd)))
        {
            await StatusContext.ToastWarning($"Can't Add {PadUsAttributeToAdd} - already exists.");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();
        Settings.PadUsAttributes.Add(PadUsAttributeToAdd.Trim());
        Settings.PadUsAttributes.SortBy(x => x);

        await ThreadSwitcher.ResumeBackgroundAsync();
        PadUsAttributeToAdd = string.Empty;

        await FeatureIntersectTaggerSettingTools.WriteSettings(Settings);
    }

    public async Task CheckThatExifToolExists(bool saveSettings)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(Settings.ExifToolFullName))
        {
            ExifToolExists = false;
            return;
        }

        var exists = File.Exists(Settings.ExifToolFullName.Trim());

        if (exists && saveSettings)
        {
            await FeatureIntersectTaggerSettingTools.WriteSettings(Settings);
            WeakReferenceMessenger.Default.Send(new ExifToolSettingsUpdateMessage((this, Settings.ExifToolFullName)));
        }

        ExifToolExists = exists;
    }

    [BlockingCommand]
    public async Task ChooseExifFile()
    {
        var newFile = await ExifFilePicker.ChooseExifFile(StatusContext, Settings.ExifToolFullName);

        if (!newFile.validFileFound) return;

        Debug.Assert(Settings != null, nameof(Settings) + " != null");

        if (Settings.ExifToolFullName.Equals(newFile.pickedFileName)) return;

        Settings.ExifToolFullName = newFile.pickedFileName;
    }

    [BlockingCommand]
    public async Task ChoosePadUsDirectory()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var folderPicker = new VistaFolderBrowserDialog
            { Description = "Directory to Add", Multiselect = false };

        Debug.Assert(Settings != null, nameof(Settings) + " != null");

        if (!string.IsNullOrWhiteSpace(Settings.PadUsDirectory) && Directory.Exists(Settings.PadUsDirectory))
        {
            var lastDirectory = new DirectoryInfo(Settings.PadUsDirectory);
            folderPicker.SelectedPath = $"{lastDirectory.FullName}\\";
        }

        var result = folderPicker.ShowDialog();

        if (!result ?? false) return;

        Settings.PadUsDirectory = folderPicker.SelectedPath;

        await FeatureIntersectTaggerSettingTools.WriteSettings(Settings);
    }

    public static async Task<FeatureIntersectTaggerContext> CreateInstance(StatusControlContext? statusContext,
        WindowIconStatus? windowStatus)
    {
        var control = new FeatureIntersectTaggerContext(statusContext, windowStatus);
        await control.Load();
        return control;
    }

    [BlockingCommand]
    private Task<IntersectSettings> CurrentSettingsAsIntersectSettings()
    {
        Debug.Assert(Settings != null, nameof(Settings) + " != null");

        var featureFiles = Settings.FeatureIntersectFiles
            .Select(x => new FeatureFile(x.Source, x.Name, x.AttributesForTags, x.TagAll, x.FileName, x.Downloaded, x.Note))
            .ToList();

        return Task.FromResult(new IntersectSettings(featureFiles, Settings.PadUsDirectory, Settings.PadUsAttributes));
    }

    [BlockingCommand]
    public async Task DeleteFeatureFile()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedFeatureFile == null)
        {
            await StatusContext.ToastWarning("Nothing Selected To Delete?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        Debug.Assert(Settings != null, nameof(Settings) + " != null");

        Settings.FeatureIntersectFiles.Remove(SelectedFeatureFile);

        await ThreadSwitcher.ResumeBackgroundAsync();

        await FeatureIntersectTaggerSettingTools.WriteSettings(Settings);
    }

    [NonBlockingCommand]
    public async Task EditFeatureFile()
    {
        if (SelectedFeatureFile == null)
        {
            await StatusContext.ToastWarning("Nothing Selected To Edit?");
            return;
        }

        FeatureFileToEdit!.Show(SelectedFeatureFile, Settings.FeatureIntersectFiles.ToList());

        return;
    }

    private void EndEdit(object? sender,
        (FeatureFileEditorEndEditCondition endCondition, FeatureFileContext model) e)
    {
        if (e.endCondition == FeatureFileEditorEndEditCondition.Cancelled) return;

        StatusContext.RunBlockingTask(async () => await ProcessEditedFeatureFileViewModel(e.model));
    }

    [BlockingCommand]
    public async Task ExportSettingsToFile()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var filePicker = new VistaSaveFileDialog
            { DefaultExt = "json", AddExtension = true, Filter = "Json Files | *.json" };

        if (!filePicker.ShowDialog() ?? false) return;

        var newFile = new FileInfo(filePicker.FileName);

        if (newFile.Exists && await StatusContext.ShowMessage("Overwrite Existing File?",
                $"The file {newFile.FullName} already exists - Overwrite that File?",
                ["Yes", "No"]) == "No") return;

        var settings = await CurrentSettingsAsIntersectSettings();

        var jsonSettings = JsonSerializer.Serialize(settings);

        await File.WriteAllTextAsync(newFile.FullName, jsonSettings, CancellationToken.None);
    }

    [BlockingCommand]
    public async Task GeneratePreview()
    {
        await FeatureIntersectTaggerSettingTools.WriteSettings(Settings);

        if (FilesToTagFileList?.Files == null || !FilesToTagFileList.Files.Any())
        {
            await StatusContext.ToastError("No Files to Tag Selected?");
            return;
        }

        Debug.Assert(Settings != null, nameof(Settings) + " != null");

        var featureFiles = Settings.FeatureIntersectFiles
            .Select(x => new FeatureFile(x.Source, x.Name, x.AttributesForTags, x.TagAll, x.FileName, x.Downloaded, x.Note))
            .ToList();

        var intersectSettings =
            new IntersectSettings(featureFiles, Settings.PadUsDirectory, Settings.PadUsAttributes);

        PreviewResults = await FilesToTagFileList.Files.ToList()
            .FileIntersectionTags(intersectSettings, Settings.TagsToLowerCase, Settings.SanitizeTags,
                Settings.TagSpacesToHyphens, CancellationToken.None, 1024, StatusContext.ProgressTracker());

        SelectedTab = 4;
    }

    [BlockingCommand]
    public async Task ImportSettingsFromFile()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var filePicker = new VistaOpenFileDialog
            { CheckFileExists = true, CheckPathExists = true, DefaultExt = "json", Filter = "Json Files | *.json" };

        if (!filePicker.ShowDialog() ?? false) return;

        var selectedFile = new FileInfo(filePicker.FileName);

        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!selectedFile.Exists)
        {
            await StatusContext.ToastError("File Selected for Import does not Exist?");
            return;
        }

        var settings =
            JsonSerializer.Deserialize<IntersectSettings>(await File.ReadAllTextAsync(selectedFile.FullName));

        if (settings == null)
        {
            await StatusContext.ToastError($"Couldn't convert {selectedFile.Name} into a valid Settings File?");
            return;
        }

        Debug.Assert(Settings != null, nameof(Settings) + " != null");

        if (!string.IsNullOrWhiteSpace(settings.PadUsDirectory)) Settings.PadUsDirectory = settings.PadUsDirectory;

        await ThreadSwitcher.ResumeForegroundAsync();

        if (settings.PadUsAttributesForTags.Any())
        {
            Settings.PadUsAttributes.Clear();
            settings.PadUsAttributesForTags.OrderBy(x => x).ToList().ForEach(x => Settings.PadUsAttributes.Add(x));
        }

        if (settings.PadUsAttributesForTags.Any())
        {
            Settings.FeatureIntersectFiles.Clear();
            settings.IntersectFiles.OrderBy(x => x.Name).Select(loopFeatureFile =>
                new FeatureFileContext
                {
                    Name = loopFeatureFile.Name,
                    FileName = loopFeatureFile.FileName,
                    TagAll = loopFeatureFile.TagAll,
                    AttributesForTags = loopFeatureFile.AttributesForTags.OrderBy(x => x).ToList(),
                    Source = loopFeatureFile.Source,
                    Downloaded = loopFeatureFile.Downloaded
                }).ToList().ForEach(x => Settings.FeatureIntersectFiles.Add(x));
        }

        await FeatureIntersectTaggerSettingTools.WriteSettings(Settings);
    }

    private async Task Load()
    {
        Settings = await FeatureIntersectTaggerSettingTools.ReadSettings();

        FilesToTagSettings = new FeatureIntersectTaggerFilesToTagSettings(this);

        FilesToTagFileList =
            await FileListContext.CreateInstance(StatusContext, FilesToTagSettings,
            [
                new()
                {
                    ItemCommand = MetadataForSelectedFilesToTagCommand,
                    ItemName = "Metadata Report for Selected"
                }
            ]);
        
        Debug.Assert(Settings != null, nameof(Settings) + " != null");

        Settings.PropertyChanged += OnSettingsPropertyChanged;

        await CheckThatExifToolExists(false);
    }

    [BlockingCommand]
    public async Task MetadataForSelectedFilesToTag()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (FilesToTagFileList?.SelectedFiles == null)
        {
            await StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        var frozenSelected = FilesToTagFileList.SelectedFiles.ToList();

        if (!frozenSelected.Any())
        {
            await StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        if (frozenSelected.Count > 10)
        {
            await StatusContext.ToastWarning("Sorry - dumping metadata limited to 10 files at once...");
            return;
        }

        foreach (var loopFile in frozenSelected)
        {
            loopFile.Refresh();
            if (!loopFile.Exists)
            {
                await StatusContext.ToastWarning($"File {loopFile.FullName} no longer exists?");
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

                htmlParts.Add(photoMetaTags.SelectMany(x => x.Tags).OrderBy(x => x.DirectoryName)
                    .ThenBy(x => x.Name)
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

            var file = new FileInfo(Path.Combine(FileLocationTools.TempStorageDirectory().FullName,
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

    [NonBlockingCommand]
    public Task NewFeatureFile()
    {
        Debug.Assert(FeatureFileToEdit != null, nameof(FeatureFileToEdit) + " != null");

        Debug.Assert(Settings != null, nameof(Settings) + " != null");

        FeatureFileToEdit.Show(new FeatureFileContext(), Settings.FeatureIntersectFiles.ToList());
        return Task.CompletedTask;
    }

    [NonBlockingCommand]
    public async Task NextTab()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        SelectedTab++;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName == nameof(Settings))
            StatusContext.RunNonBlockingTask(async () => await CheckThatExifToolExists(false));
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName == nameof(Settings.ExifToolFullName))
            StatusContext.RunNonBlockingTask(async () => await CheckThatExifToolExists(true));
    }

    public async Task ProcessEditedFeatureFileViewModel(FeatureFileContext model)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        Debug.Assert(Settings != null, nameof(Settings) + " != null");

        if (Settings.FeatureIntersectFiles.All(x => x.ContentId != model.ContentId))
            Settings.FeatureIntersectFiles.Add(model);

        Settings.FeatureIntersectFiles.SortBy(x => x.Name);

        await ThreadSwitcher.ResumeBackgroundAsync();

        await FeatureIntersectTaggerSettingTools.WriteSettings(Settings);
    }

    [NonBlockingCommand]
    public async Task RemovePadUsAttribute(string? toRemove)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        Debug.Assert(Settings != null, nameof(Settings) + " != null");

        if (Settings.PadUsAttributes.Contains(toRemove ?? string.Empty))
            Settings.PadUsAttributes.Remove(toRemove ?? string.Empty);

        Settings.PadUsAttributes.SortBy(x => x);

        await ThreadSwitcher.ResumeBackgroundAsync();

        await FeatureIntersectTaggerSettingTools.WriteSettings(Settings);
    }

    [BlockingCommand]
    public async Task WriteResultsToFile()
    {
        await FeatureIntersectTaggerSettingTools.WriteSettings(Settings);

        if (PreviewResults.Count == 0)
        {
            await StatusContext.ToastError("No Files to Write To?");
            return;
        }

        if (PreviewResults.Count(x => !string.IsNullOrWhiteSpace(x.NewTagsString)) == 0)
        {
            await StatusContext.ToastError("None of the files have New Tags - nothing to Write?");
            return;
        }

        Debug.Assert(Settings != null, nameof(Settings) + " != null");

        WriteToFileResults = await PreviewResults.Where(x => !string.IsNullOrWhiteSpace(x.NewTagsString)).ToList()
            .WriteTagsToFiles(
                false, Settings.CreateBackups, Settings.CreateBackupsInDefaultStorage,
                Settings.TagsToLowerCase, Settings.SanitizeTags, Settings.TagSpacesToHyphens,
                Settings.ExifToolFullName, CancellationToken.None, 1024, StatusContext.ProgressTracker());

        SelectedTab = 5;
    }
}