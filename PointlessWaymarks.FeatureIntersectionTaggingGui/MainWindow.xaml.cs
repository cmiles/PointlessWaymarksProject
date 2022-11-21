﻿using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Omu.ValueInjecter;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.FeatureIntersectionTaggingGui.Models;
using PointlessWaymarks.FeatureIntersectionTags;
using PointlessWaymarks.FeatureIntersectionTags.Models;
using PointlessWaymarks.LoggingTools;
using PointlessWaymarks.WpfCommon.FileList;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.FeatureIntersectionTaggingGui;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
[ObservableObject]
public partial class MainWindow
{
    [ObservableProperty] private bool _createBackups;
    [ObservableProperty] private string _exifToolFullName = string.Empty;
    [ObservableProperty] private ObservableCollection<FeatureFileViewModel>? _featureFiles;
    [ObservableProperty] private FeatureFileEditorViewModel _featureFileToEdit;
    [ObservableProperty] private FileListViewModel _filesToTagFileList;
    [ObservableProperty] private FeatureIntersectionFilesToTagSettings _filesToTagSettings;
    [ObservableProperty] private string _infoTitle;
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
    [ObservableProperty] private bool _tagsToLowerCase;
    [ObservableProperty] private bool _testRunOnly;
    [ObservableProperty] private WindowIconStatus _windowStatus;

    public MainWindow()
    {
        InitializeComponent();

        JotServices.Tracker.Track(this);

        if (Width < 900) Width = 900;
        if (Height < 650) Height = 650;

        WindowInitialPositionHelpers.EnsureWindowIsVisible(this);

        // ReSharper disable once ConditionIsAlwaysTrueOrFalse for generated ThisAssembly.Git.IsDirty
        // ReSharper disable once HeuristicUnreachableCode
        //.Git IsDirty can change at runtime
#pragma warning disable CS0162
        _infoTitle = WindowTitleTools.StandardAppInformationString(Assembly.GetExecutingAssembly(),
            "Pointless Waymarks Feature Intersection Tagger");
        ;
#pragma warning restore CS0162

        DataContext = this;

        _statusContext = new StatusControlContext();

        _windowStatus = new WindowIconStatus();

        FilesToTagSettings = new FeatureIntersectionFilesToTagSettings();
        FeatureFileToEdit = new FeatureFileEditorViewModel(StatusContext, new FeatureFileViewModel());
        FeatureFileToEdit.EndEdit += EndEdit;

        ChoosePadUsDirectoryCommand = StatusContext.RunBlockingTaskCommand(ChoosePadUsDirectory);
        AddPadUsAttributeCommand = StatusContext.RunNonBlockingTaskCommand(AddPadUsAttribute);
        RemovePadUsAttributeCommand = StatusContext.RunNonBlockingTaskCommand<string>(RemovePadUsAttribute);
        EditFeatureFileCommand = StatusContext.RunNonBlockingTaskCommand(EditFeatureFile);
        NewFeatureFileCommand = StatusContext.RunNonBlockingTaskCommand(NewFeatureFile);

        TagFilesCommand = StatusContext.RunBlockingTaskCommand(TagFiles);

        StatusContext.RunBlockingTask(Load);
    }

    public RelayCommand AddPadUsAttributeCommand { get; set; }

    public RelayCommand ChoosePadUsDirectoryCommand { get; set; }

    public RelayCommand EditFeatureFileCommand { get; set; }

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

    public RelayCommand TagFilesCommand { get; set; }

    public async Task AddPadUsAttribute()
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

    public async Task ChoosePadUsDirectory()
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

    public async Task EditFeatureFile()
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

    private async Task Load()
    {
        FilesToTagFileList =
            await FileListViewModel.CreateInstance(StatusContext, FilesToTagSettings,
                new List<ContextMenuItemData>());

        var settings = await FeatureIntersectionGuiSettingTools.ReadSettings();
        PadUsDirectory = settings.PadUsDirectory;

        var featureFiles = settings.FeatureIntersectFiles.Select(x => new FeatureFileViewModel().InjectFrom(x))
            .Cast<FeatureFileViewModel>().ToList();

        await LoadTaggerSetting();

        await ThreadSwitcher.ResumeForegroundAsync();

        PadUsAttributes = new ObservableCollection<string>();
        settings.PadUsAttributes.OrderBy(x => x).ToList().ForEach(x => PadUsAttributes.Add(x));

        FeatureFiles = new ObservableCollection<FeatureFileViewModel>(featureFiles);
    }

    public async Task LoadTaggerSetting()
    {
        var settings = await FeatureIntersectionGuiSettingTools.ReadSettings();
        ExifToolFullName = settings.ExifToolFullName;
        CreateBackups = settings.CreateBackups;
        TestRunOnly = settings.TestRunOnly;
        TagsToLowerCase = settings.TagsToLowerCase;
        SanitizeTags = settings.SanitizeTags;
        await FeatureIntersectionGuiSettingTools.WriteSettings(settings);
    }

    public async Task NewFeatureFile()
    {
        FeatureFileToEdit.Show(new FeatureFileViewModel());
    }

    public async Task RefreshFeatureFileList()
    {
        var currentList = (await FeatureIntersectionGuiSettingTools.ReadSettings()).FeatureIntersectFiles;

        await ThreadSwitcher.ResumeForegroundAsync();

        FeatureFiles!.Clear();
        currentList.ForEach(x => FeatureFiles.Add(x));
    }

    public async Task RemovePadUsAttribute(string toRemove)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        if (PadUsAttributes!.Contains(toRemove))
        {
            PadUsAttributes.Remove(toRemove);
            await FeatureIntersectionGuiSettingTools.SetPadUsAttributes(PadUsAttributes.ToList());
        }
    }

    public async Task TagFiles()
    {
        await WriteTaggerSetting();

        var settings = await FeatureIntersectionGuiSettingTools.ReadSettings();

        var featureFiles = settings.FeatureIntersectFiles
            .Select(x => new FeatureFile(x.Source, x.Name, x.AttributesForTags, x.TagAll, x.FileName)).ToList();

        var intersectSettings = new IntersectSettings(featureFiles, settings.PadUsDirectory, settings.PadUsAttributes);

        var fileTags = await FilesToTagFileList.Files.ToList()
            .FileIntersectionTags(intersectSettings, CancellationToken.None, StatusContext.ProgressTracker());

        var rawResults = fileTags.WriteTagsToFiles(
            settings.TestRunOnly, settings.CreateBackups, settings.TagsToLowerCase, settings.SanitizeTags,
            settings.ExifToolFullName, CancellationToken.None, 1024, StatusContext.ProgressTracker());
    }

    public async Task WriteTaggerSetting()
    {
        var settings = await FeatureIntersectionGuiSettingTools.ReadSettings();
        settings.ExifToolFullName = ExifToolFullName;
        settings.CreateBackups = CreateBackups;
        settings.TestRunOnly = TestRunOnly;
        settings.TagsToLowerCase = TagsToLowerCase;
        settings.SanitizeTags = SanitizeTags;
        await FeatureIntersectionGuiSettingTools.WriteSettings(settings);
    }
}