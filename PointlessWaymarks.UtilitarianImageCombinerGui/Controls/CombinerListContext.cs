using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using GongSolutions.Wpf.DragDrop;
using Metalama.Patterns.Observability;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.UtilitarianImage;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ConversionDataEntry;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using SkiaSharp;
using Path = System.IO.Path;

namespace PointlessWaymarks.UtilitarianImageCombinerGui.Controls;

[Observable]
[GenerateStatusCommands]
public partial class CombinerListContext : IDropTarget
{
    public required List<ColorNameAndSkColor> BackgroundColors { get; set; }
    public required ConversionDataEntryNoChangeIndicatorContext<int> FinalImageJpegQuality { get; set; }
    public required ConversionDataEntryNoChangeIndicatorContext<int> ItemMaxHeightEntryContext { get; set; }
    public required ConversionDataEntryNoChangeIndicatorContext<int> ItemMaxWidthEntryContext { get; set; }
    public required ObservableCollection<CombinerListListItem> Items { get; set; }
    public ColorNameAndSkColor? SelectedBackgroundColor { get; set; }
    public CombinerListListItem? SelectedItem { get; set; }
    public List<CombinerListListItem> SelectedItems { get; set; } = [];
    public required StatusControlContext StatusContext { get; set; }

    public void DragOver(IDropInfo dropInfo)
    {
        if (dropInfo.Data is CombinerListListItem)
        {
            dropInfo.Effects = DragDropEffects.Move;
            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
        }
        else if (HasFileSystemData(dropInfo.Data))
        {
            if (dropInfo.Data is DataObject dataObject && dataObject.ContainsFileDropList())
            {
                var files = dataObject.GetFileDropList().Cast<string>().ToList();
                var allFilesSupported =
                    files.All(file => Combiner.SupportedExtensions.Contains(Path.GetExtension(file).ToLower()));

                if (allFilesSupported)
                {
                    dropInfo.Effects = DragDropEffects.Copy;
                    dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                }
                else
                {
                    dropInfo.Effects = DragDropEffects.None;
                }
            }
        }
        else
        {
            dropInfo.Effects = DragDropEffects.None;
        }
    }

    public void Drop(IDropInfo dropInfo)
    {
        if (HasFileSystemData(dropInfo.Data))
        {
            if (dropInfo.Data is IDataObject dataObject)
                if (dataObject.GetData(DataFormats.FileDrop) is string[] files)
                    StatusContext.RunFireAndForgetBlockingTask(async () =>
                        await AddImages(files.ToList(), dropInfo.InsertIndex));
        }
        else if (dropInfo.Data is CombinerListListItem droppedItem)
        {
            var oldIndex = Items.IndexOf(droppedItem);
            var newIndex = dropInfo.InsertIndex;

            if (newIndex > oldIndex) newIndex--; // Compensate for the removal of the item at the old index

            if (oldIndex != newIndex) Items.Move(oldIndex, newIndex);
        }
    }

    public async Task AddImages(List<string> imageFiles, int index = -1)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.Progress($"Adding {imageFiles.Count} Files");

        var imageCounter = 0;

        var useInsert = false;

        if (index >= 0 && index < Items.Count)
        {
            imageFiles.Reverse();
            useInsert = true;
        }

        var listItems = new List<CombinerListListItem>();

        foreach (var file in imageFiles)
        {
            StatusContext.Progress($"Processing {file} - {++imageCounter} of {imageFiles.Count}");
            try
            {
                if (Path.GetExtension(file).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    var fileInfo = new FileInfo(file);
                    var newFile = await Combiner.PdfToJpeg(file, ItemMaxWidthEntryContext.UserValue,
                        FinalImageJpegQuality.UserValue, SelectedBackgroundColor?.Color ?? SKColors.White);
                    listItems.Add(await CombinerListListItem.CreateInstance(newFile, StatusContext));
                }
                else
                {
                    listItems.Add(await CombinerListListItem.CreateInstance(file, StatusContext));
                }
            }
            catch (Exception e)
            {
                StatusContext.ToastError($"Error Adding {file}");
            }
        }

        StatusContext.Progress("Adding Files to List");

        await ThreadSwitcher.ResumeForegroundAsync();

        foreach (var file in listItems)
            if (useInsert)
                Items.Insert(index, file);
            else
                Items.Add(file);
    }

    [BlockingCommand]
    public async Task AddViaFileChooser()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        StatusContext.Progress("Starting photo load.");

        var dialog = new VistaOpenFileDialog { Multiselect = true, Filter = GetVistaOpenFileDialogFilter() };

        var currentSettings = ImageCombinerGuiSettingTools.ReadSettings();

        if (!string.IsNullOrWhiteSpace(currentSettings.LastFileSourceDirectory))
            dialog.FileName = $"{currentSettings.LastFileSourceDirectory}\\";

        if (!(dialog.ShowDialog() ?? false)) return;

        var selectedFiles = (dialog.FileNames?.ToList() ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        if (!selectedFiles.Any()) return;

        var selectedDirectory = Path.GetDirectoryName(selectedFiles.First());

        await ImageCombinerGuiSettingTools.WriteFileSourceDirectory(selectedDirectory ?? string.Empty);

        await AddImages(selectedFiles);
    }

    [NonBlockingCommand]
    public async Task ClearList(CombinerListListItem toShow)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Clear();
    }

    [BlockingCommand]
    public async Task CombineImagesGrid()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var frozenItems = Items.ToList();

        var maxWidth = ItemMaxWidthEntryContext.UserValue;
        var maxHeight = ItemMaxHeightEntryContext.UserValue;

        StatusContext.Progress(
            $"Combining {frozenItems.Count} Images - Max Individual Image Size {maxWidth}x{maxHeight}");


        await ThreadSwitcher.ResumeForegroundAsync();

        var saveDialog = new VistaSaveFileDialog { Filter = "jpg files (*.jpg;*.jpeg)|*.jpg;*.jpeg" };

        var currentSettings = ImageCombinerGuiSettingTools.ReadSettings();
        if (!string.IsNullOrWhiteSpace(currentSettings.SaveToDirectory))
            saveDialog.FileName = $"{currentSettings.SaveToDirectory}\\";
        ;

        if (!saveDialog.ShowDialog() ?? true) return;

        var newFilename = saveDialog.FileName;

        await ThreadSwitcher.ResumeBackgroundAsync();

        var combinedImage = await Combiner.CombineImagesInGrid(frozenItems.Select(x => x.FileFullName).ToList(),
            maxWidth,
            maxHeight, newFilename, FinalImageJpegQuality.UserValue, SelectedBackgroundColor?.Color ?? SKColors.Black,
            StatusContext.ProgressTracker());

        await ThreadSwitcher.ResumeForegroundAsync();

        await ProcessHelpers.OpenExplorerWindowForFile(combinedImage);
    }

    [BlockingCommand]
    public async Task CombineImagesHorizontally()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var frozenItems = Items.ToList();

        var maxWidth = ItemMaxWidthEntryContext.UserValue;
        var maxHeight = ItemMaxHeightEntryContext.UserValue;

        StatusContext.Progress(
            $"Combining {frozenItems.Count} Images - Max Individual Image Size {maxWidth}x{maxHeight}");


        await ThreadSwitcher.ResumeForegroundAsync();

        var saveDialog = new VistaSaveFileDialog { Filter = "jpg files (*.jpg;*.jpeg)|*.jpg;*.jpeg" };

        var currentSettings = ImageCombinerGuiSettingTools.ReadSettings();
        if (!string.IsNullOrWhiteSpace(currentSettings.SaveToDirectory))
            saveDialog.FileName = $"{currentSettings.SaveToDirectory}\\";
        ;

        if (!saveDialog.ShowDialog() ?? true) return;

        var newFilename = saveDialog.FileName;

        await ThreadSwitcher.ResumeBackgroundAsync();

        var combinedImage = await Combiner.CombineImagesHorizontal(frozenItems.Select(x => x.FileFullName).ToList(),
            maxWidth,
            maxHeight, newFilename, FinalImageJpegQuality.UserValue, SelectedBackgroundColor?.Color ?? SKColors.Black,
            StatusContext.ProgressTracker());

        await ThreadSwitcher.ResumeForegroundAsync();

        await ProcessHelpers.OpenExplorerWindowForFile(combinedImage);
    }

    [BlockingCommand]
    public async Task CombineImagesVertically()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var frozenItems = Items.ToList();

        var maxWidth = ItemMaxWidthEntryContext.UserValue;
        var maxHeight = ItemMaxHeightEntryContext.UserValue;

        StatusContext.Progress(
            $"Combining {frozenItems.Count} Images - Max Individual Image Size {maxWidth}x{maxHeight}");


        await ThreadSwitcher.ResumeForegroundAsync();

        var saveDialog = new VistaSaveFileDialog { Filter = "jpg files (*.jpg;*.jpeg)|*.jpg;*.jpeg" };

        var currentSettings = ImageCombinerGuiSettingTools.ReadSettings();
        if (!string.IsNullOrWhiteSpace(currentSettings.SaveToDirectory))
            saveDialog.FileName = $"{currentSettings.SaveToDirectory}\\";
        ;

        if (!saveDialog.ShowDialog() ?? true) return;

        var newFilename = saveDialog.FileName;

        await ThreadSwitcher.ResumeBackgroundAsync();

        var combinedImage = await Combiner.CombineImagesVertical(frozenItems.Select(x => x.FileFullName).ToList(),
            maxWidth,
            maxHeight, newFilename, FinalImageJpegQuality.UserValue, SelectedBackgroundColor?.Color ?? SKColors.Black,
            StatusContext.ProgressTracker());

        await ThreadSwitcher.ResumeForegroundAsync();

        await ProcessHelpers.OpenExplorerWindowForFile(combinedImage);
    }

    public static async Task<CombinerListContext> CreateInstance(StatusControlContext statusContext)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var itemMaxWidthEntryContext =
            await ConversionDataEntryNoChangeIndicatorContext<int>.CreateInstance(
                ConversionDataEntryHelpers.IntGreaterThanZeroConversion);
        itemMaxWidthEntryContext.Title = "Item Max Width";
        itemMaxWidthEntryContext.HelpText =
            "The maximum width of the images. Images will be resized to this width before combining.";
        itemMaxWidthEntryContext.UserText = "4000";

        var itemMaxHeightEntryContext =
            await ConversionDataEntryNoChangeIndicatorContext<int>.CreateInstance(
                ConversionDataEntryHelpers.IntGreaterThanZeroConversion);
        itemMaxHeightEntryContext.Title = "Item Max Height";
        itemMaxHeightEntryContext.HelpText =
            "The maximum height of the images. Images will be resized to this height before combining.";
        itemMaxHeightEntryContext.UserText = "4000";

        var finalImageJpegQuality =
            await ConversionDataEntryNoChangeIndicatorContext<int>.CreateInstance(ConversionDataEntryHelpers
                .IntZeroToOneHundredConversion);
        finalImageJpegQuality.Title = "Final Jpeg Image Quality (0-100)";
        finalImageJpegQuality.HelpText =
            "The quality of the final jpeg image. 0 is the lowest quality, 100 is the highest.";
        finalImageJpegQuality.UserText = "95";

        var newContext = new CombinerListContext
        {
            StatusContext = statusContext,
            Items = [],
            ItemMaxHeightEntryContext = itemMaxHeightEntryContext,
            ItemMaxWidthEntryContext = itemMaxWidthEntryContext,
            FinalImageJpegQuality = finalImageJpegQuality,
            BackgroundColors = GetAllSKColors()
        };

        newContext.BuildCommands();

        return newContext;
    }

    public static List<ColorNameAndSkColor> GetAllSKColors()
    {
        var skColorsType = typeof(SKColors);
        var colorProperties = skColorsType.GetProperties(BindingFlags.Static | BindingFlags.Public);

        var colorList = colorProperties
            .Where(prop => prop.PropertyType == typeof(SKColor))
            .Where(prop => (SKColor)(prop.GetValue(null) ?? SKColors.Transparent) != SKColors.Transparent)
            .Select(prop => new ColorNameAndSkColor { ColorName = prop.Name, Color = (SKColor)prop.GetValue(null) })
            .ToList();

        return colorList.OrderBy(x => x.ColorName).ToList();
    }

    public string GetVistaOpenFileDialogFilter()
    {
        var filter = "Supported Image Files|";
        filter += string.Join(";", Combiner.SupportedExtensions.Select(ext => $"*{ext}"));
        filter += "|All Files|*.*";
        return filter;
    }

    private bool HasFileSystemData(object data)
    {
        if (data is IDataObject dataObject) return dataObject.GetDataPresent(DataFormats.FileDrop);
        return false;
    }

    [NonBlockingCommand]
    public async Task OpenFile(CombinerListListItem toShow)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var ps = new ProcessStartInfo(toShow.FileFullName) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }

    [NonBlockingCommand]
    public async Task RemoveItem(CombinerListListItem toShow)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Remove(toShow);
    }

    [NonBlockingCommand]
    public async Task RemoveSelectedItems(CombinerListListItem toShow)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var frozenSelected = SelectedItems.ToList();

        if (!SelectedItems.Any())
        {
            StatusContext.ToastError("No Items Selected");
            return;
        }

        foreach (var selectedItem in frozenSelected) Items.Remove(selectedItem);
    }

    [BlockingCommand]
    public async Task RotateLeft(CombinerListListItem listItem)
    {
        var rotated = await Combiner.RotateLeft(new FileInfo(listItem.FileFullName).FullName);
        if (rotated == null) return;
        listItem.FileFullName = string.Empty;
        listItem.FileFullName = rotated.FullName;
    }

    [BlockingCommand]
    public async Task RotateRight(CombinerListListItem listItem)
    {
        var rotated = await Combiner.RotateRight(new FileInfo(listItem.FileFullName).FullName);
        if (rotated == null) return;
        listItem.FileFullName = string.Empty;
        listItem.FileFullName = rotated.FullName;
    }

    [NonBlockingCommand]
    public async Task ShowHelpWindow()
    {
        var helpMarkdown =
            $"""
             ## Utilitarian Image Combiner

             This program combines images and pdfs into a single JPEG image. It is designed for utilitarian concerns like record keeping - aesthetic concerns are largely ignored.

             To add images or pdfs use the 'Add Images' button or drag and drop files into the list. You can drag and drop items to reorder the list, rotate and view images, and remove items.

             Images and pdfs can be combined into a Vertical, Horizontal, or Grid orientation. The maximum width and height for each image can be set and along with a JPEG quality setting gives you some control over the size of the final image.

             Supported Input File Extensions: {string.Join(", ", Combiner.SupportedExtensions)}

             ## Background

             For several years I have been generating private sites with the [Pointless Waymarks CMS](https://github.com/cmiles/PointlessWaymarksProject) to track personal items like camera gear, books and home purchases. For these sites it has been very useful to take several photos of an item with my phone (a photo of a lens showing the brand/model, another showing the serial number, maybe one showing the packaging, sometimes another with accessories...), and sometimes a PDF receipt, and combine them into a single image for record keeping. In the past I often did this on my Android phone with [ZomboDroid Image Combiner & Editor](https://play.google.com/store/apps/details?id=com.zombodroid.imagecombinerfree) - but over time as I did this more the photos were not always on my phone and I wanted to be able to also combine images of PDF receipts - so I created this program!

             {HelpMarkdown.CombinedAboutToolsAndPackages}
             """;

        await HelpDisplayWindow.CreateInstanceAndShow([helpMarkdown], "Utilitarian Image Combiner Help and About");
    }

    [NonBlockingCommand]
    public async Task ShowInExplorer(CombinerListListItem toShow)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        await ProcessHelpers.OpenExplorerWindowForFile(toShow.FileFullName);
    }

    public record ColorNameAndSkColor
    {
        public SKColor Color { get; set; }
        public string ColorName { get; set; }
    }
}