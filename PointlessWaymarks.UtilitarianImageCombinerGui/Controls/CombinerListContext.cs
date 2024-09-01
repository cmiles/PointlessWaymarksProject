using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using GongSolutions.Wpf.DragDrop;
using Metalama.Patterns.Observability;
using Ookii.Dialogs.Wpf;
using PDFtoImage;
using PhotoSauce.MagicScaler;
using PhotoSauce.MagicScaler.Transforms;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
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
    public List<string> SupportedExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".bmp",
        ".webp",
        ".ico",
        ".wbmp",
        ".pkm",
        ".ktx",
        ".astc",
        ".dng",
        ".heif",
        ".heic",
        ".pdf"
    ];

    public required ConversionDataEntryNoChangeIndicatorContext<int> FinalImageJpegQuality { get; set; }
    public required ConversionDataEntryNoChangeIndicatorContext<int> ItemMaxHeightEntryContext { get; set; }
    public required ConversionDataEntryNoChangeIndicatorContext<int> ItemMaxWidthEntryContext { get; set; }
    public required ObservableCollection<CombinerListListItem> Items { get; set; }
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
                    files.All(file => SupportedExtensions.Contains(Path.GetExtension(file).ToLower()));

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

    public string GetVistaOpenFileDialogFilter()
    {
        var filter = "Supported Image Files|";
        filter += string.Join(";", SupportedExtensions.Select(ext => $"*{ext}"));
        filter += "|All Files|*.*";
        return filter;
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
                    var newFileName = UniqueFileTools.UniqueFile(fileInfo.Directory,
                        $"{Path.GetFileNameWithoutExtension(fileInfo.Name)}.jpg");
                    await CombinePdfPagesToJpeg(file, newFileName.FullName, ItemMaxWidthEntryContext.UserValue,
                        FinalImageJpegQuality.UserValue);
                    listItems.Add(await CombinerListListItem.CreateInstance(newFileName.FullName, StatusContext));
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

    public async Task CombinePdfPagesToJpeg(string pdfFilePath, string outputFilePath, int maxWidth, int jpegQuality)
    {
        // Convert PDF pages to images asynchronously
        await using var pdfStream = File.OpenRead(pdfFilePath);
        var skBitmaps = await Conversion.ToImagesAsync(pdfStream).ToListAsync();

        // Calculate the total height and maximum width
        var totalHeight = 0;
        var combinedWidth = 0;
        foreach (var bitmap in skBitmaps)
        {
            totalHeight += bitmap.Height;
            combinedWidth = Math.Max(combinedWidth, bitmap.Width);
        }

        // Scale the combined width to the max width if necessary
        if (combinedWidth > maxWidth)
        {
            var scale = (float)maxWidth / combinedWidth;
            combinedWidth = maxWidth;
            totalHeight = (int)(totalHeight * scale);
        }

        // Create a new bitmap to hold the combined image
        using var combinedBitmap = new SKBitmap(combinedWidth, totalHeight);
        using var canvas = new SKCanvas(combinedBitmap);
        canvas.Clear(SKColors.White);

        // Draw each image onto the combined bitmap
        var yOffset = 0;
        foreach (var bitmap in skBitmaps)
        {
            var scaledHeight = (int)(bitmap.Height * ((float)combinedWidth / bitmap.Width));
            var destRect = new SKRect(0, yOffset, combinedWidth, yOffset + scaledHeight);
            canvas.DrawBitmap(bitmap, destRect);
            yOffset += scaledHeight;
        }

        // Save the combined image as a JPEG
        using var image = SKImage.FromBitmap(combinedBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, jpegQuality);
        await using var outputStream = File.OpenWrite(outputFilePath);
        data.SaveTo(outputStream);
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
            FinalImageJpegQuality = finalImageJpegQuality
        };

        newContext.BuildCommands();

        return newContext;
    }

    [BlockingCommand]
    public async Task RotateRight(CombinerListListItem listItem)
    {
        var rotated = await Rotate(new FileInfo(listItem.FileFullName), Orientation.Rotate90).ConfigureAwait(false);
        if (rotated == null) return;
        listItem.FileFullName = string.Empty;
        listItem.FileFullName = rotated.FullName;
    }

    [BlockingCommand]
    public async Task RotateLeft(CombinerListListItem listItem)
    {
        var rotated = await Rotate(new FileInfo(listItem.FileFullName), Orientation.Rotate270).ConfigureAwait(false);
        if (rotated == null) return;
        listItem.FileFullName = string.Empty;
        listItem.FileFullName = rotated.FullName;
    }

    public static async Task<FileInfo?> Rotate(FileInfo toRotate, Orientation orientation)
    {
        if (!toRotate.Exists) return null;

        var newFile = Path.Combine(toRotate.Directory?.FullName ?? string.Empty, $"{Guid.NewGuid()}.jpg");

        var newFileInfo = new FileInfo(newFile);
        if (newFileInfo.Exists) newFileInfo.Delete();

        using var pl =
            MagicImageProcessor.BuildPipeline(toRotate.FullNameWithLongFilePrefix(), new ProcessImageSettings());
        pl.AddTransform(new OrientationTransform(orientation));
        await using var outStream = new FileStream(newFileInfo.FullName, FileMode.Create);

        pl.WriteOutput(outStream);

        pl.Dispose();
        await outStream.DisposeAsync().ConfigureAwait(false);

        var finalFileName = toRotate.FullName;
        toRotate.Delete();

        File.Move(newFileInfo.FullName, finalFileName);

        newFileInfo = new FileInfo(finalFileName);

        return newFileInfo;
    }

    [NonBlockingCommand]
    public async Task ShowInExplorer(CombinerListListItem toShow)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        await ProcessHelpers.OpenExplorerWindowForFile(toShow.FileFullName);
    }

    [NonBlockingCommand]
    public async Task ClearList(CombinerListListItem toShow)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Clear();
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

    [BlockingCommand]
    public async Task CombineImagesVertically()
    {
        await CombineImages(CombinerOrientation.Vertical);
    }

    [BlockingCommand]
    public async Task CombineImagesHorizontally()
    {
        await CombineImages(CombinerOrientation.Horizontal);
    }

    [BlockingCommand]
    public async Task CombineImagesGrid()
    {
        await CombineImages(CombinerOrientation.Grid);
    }

    public async Task CombineImages(CombinerOrientation orientation)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var frozenItems = Items.ToList();

        var maxWidth = ItemMaxWidthEntryContext.UserValue;
        var maxHeight = ItemMaxHeightEntryContext.UserValue;

        StatusContext.Progress(
            $"Combining {frozenItems.Count} Images - Max Individual Image Size {maxWidth}x{maxHeight}");

        var images = new List<Bitmap>();
        var imageResizeCounter = 0;
        foreach (var file in frozenItems)
        {
            StatusContext.Progress($"Resizing {++imageResizeCounter} of {frozenItems.Count}");
            var image = new Bitmap(file.FileFullName);
            var resizedImage = ResizeImage(image, maxWidth, maxHeight);
            images.Add(resizedImage);
        }

        var combinedImage = orientation switch
        {
            CombinerOrientation.Vertical => CombineImagesVertically(images),
            CombinerOrientation.Horizontal => CombineImagesHorizontally(images),
            CombinerOrientation.Grid => CombineImagesInGrid(images),
            _ => throw new ArgumentOutOfRangeException(nameof(orientation), orientation, null)
        };

        await ThreadSwitcher.ResumeForegroundAsync();

        var saveDialog = new VistaSaveFileDialog { Filter = "jpg files (*.jpg;*.jpeg)|*.jpg;*.jpeg" };

        var currentSettings = ImageCombinerGuiSettingTools.ReadSettings();
        if (!string.IsNullOrWhiteSpace(currentSettings.SaveToDirectory))
            saveDialog.FileName = $"{currentSettings.SaveToDirectory}\\"; ;

        if (saveDialog.ShowDialog() ?? false)
        {
            await ImageCombinerGuiSettingTools.WriteSaveToDirectory(Path.GetDirectoryName(saveDialog.FileName) ??
                                                                    string.Empty);

            var jpegEncoder = ImageCodecInfo.GetImageDecoders()
                .FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
            if (jpegEncoder != null)
            {
                var encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, FinalImageJpegQuality.UserValue);
                combinedImage.Save($"{saveDialog.FileName}.jpg", jpegEncoder, encoderParameters);
            }
        }

        foreach (var image in images) image.Dispose();
        combinedImage.Dispose();
    }

    public async Task CombineImagesInGrid()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var frozenItems = Items.ToList();

        var maxWidth = ItemMaxWidthEntryContext.UserValue;
        var maxHeight = ItemMaxHeightEntryContext.UserValue;

        var images = new List<Bitmap>();
        foreach (var file in frozenItems)
        {
            var image = new Bitmap(file.FileFullName);
            var resizedImage = ResizeImage(image, maxWidth, maxHeight);
            images.Add(resizedImage);
        }

        var combinedImage = CombineImagesInGrid(images);

        await ThreadSwitcher.ResumeForegroundAsync();

        var saveDialog = new VistaSaveFileDialog { Filter = "jpg files (*.jpg;*.jpeg)|*.jpg;*.jpeg" };

        if (saveDialog.ShowDialog() ?? false)
        {
            var jpegEncoder = ImageCodecInfo.GetImageDecoders()
                .FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
            if (jpegEncoder != null)
            {
                var encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, FinalImageJpegQuality.UserValue);
                StatusContext.Progress($"Saving {saveDialog.FileName}");
                combinedImage.Save($"{saveDialog.FileName}.jpg", jpegEncoder, encoderParameters);
            }
        }

        foreach (var image in images) image.Dispose();
        combinedImage.Dispose();
    }

    private Bitmap CombineImagesInGrid(List<Bitmap> images)
    {
        var imageCount = images.Count;
        var gridSize = (int)Math.Ceiling(Math.Sqrt(imageCount));

        var cellWidth = Math.Min(images.Max(img => img.Width), ItemMaxWidthEntryContext.UserValue);
        var cellHeight = Math.Min(images.Max(img => img.Height), ItemMaxHeightEntryContext.UserValue);

        var gridWidth = cellWidth * gridSize;
        var gridHeight = cellHeight * gridSize;

        StatusContext.Progress(
            $"Combining Images - Grid - to a Final {gridWidth}x{gridHeight} Size with {cellWidth}x{cellHeight} Cells");

        var combinedImage = new Bitmap(gridWidth, gridHeight);
        using var graphics = Graphics.FromImage(combinedImage);
        graphics.Clear(Color.Black);

        var imageCounter = 0;
        for (var i = 0; i < imageCount; i++)
        {
            var row = i / gridSize;
            var col = i % gridSize;

            var xOffset = col * cellWidth + (cellWidth - images[i].Width) / 2;
            var yOffset = row * cellHeight + (cellHeight - images[i].Height) / 2;

            StatusContext.Progress($"Drawing Image {++imageCounter} of {images.Count} - Row {row}, Column {col}");

            graphics.DrawImage(images[i], xOffset, yOffset);
        }

        return combinedImage;
    }


    private Bitmap ResizeImage(Bitmap image, int maxWidth, int maxHeight)
    {
        // Check if the image needs to be resized
        if (image.Width <= maxWidth && image.Height <= maxHeight) return new Bitmap(image);

        var ratioX = (double)maxWidth / image.Width;
        var ratioY = (double)maxHeight / image.Height;
        var ratio = Math.Min(ratioX, ratioY);

        var newWidth = (int)(image.Width * ratio);
        var newHeight = (int)(image.Height * ratio);

        var newImage = new Bitmap(newWidth, newHeight);
        using var graphics = Graphics.FromImage(newImage);
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.DrawImage(image, 0, 0, newWidth, newHeight);

        return newImage;
    }


    private Bitmap CombineImagesVertically(List<Bitmap> images)
    {
        var width = images.Max(img => img.Width);
        var height = images.Sum(img => img.Height);

        StatusContext.Progress($"Combining Images - Vertical - to a Final {width}x{height} Size");

        var combinedImage = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(combinedImage);
        graphics.Clear(Color.Black);
        var yOffset = 0;

        var imageCounter = 0;
        foreach (var image in images)
        {
            StatusContext.Progress($"Drawing Image {++imageCounter} of {images.Count}");
            var xOffset = (width - image.Width) / 2; // Center the image horizontally
            graphics.DrawImage(image, xOffset, yOffset);
            yOffset += image.Height;
        }

        return combinedImage;
    }

    private Bitmap CombineImagesHorizontally(List<Bitmap> images)
    {
        var width = images.Sum(img => img.Width);
        var height = images.Max(img => img.Height);

        StatusContext.Progress($"Combining Images - Horizontal - to a Final {width}x{height} Size");

        var combinedImage = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(combinedImage);
        graphics.Clear(Color.Black);
        var xOffset = 0;

        var imageCounter = 0;
        foreach (var image in images)
        {
            StatusContext.Progress($"Drawing Image {++imageCounter} of {images.Count}");
            var yOffset = (height - image.Height) / 2; // Center the image vertically
            graphics.DrawImage(image, xOffset, yOffset);
            xOffset += image.Width;
        }

        return combinedImage;
    }

    private bool HasFileSystemData(object data)
    {
        if (data is IDataObject dataObject) return dataObject.GetDataPresent(DataFormats.FileDrop);
        return false;
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
            
            Supported File Extensions: {string.Join(", ", SupportedExtensions)}
            
            ## Background
            
            For several years I have been generating private sites with the [Pointless Waymarks CMS](https://github.com/cmiles/PointlessWaymarksProject) to track personal items like camera gear, books and home purchases. For these sites it has been very useful to take several photos of an item with my phone (a photo of a lens showing the brand/model, another showing the serial number, maybe one showing the packaging, sometimes another with accessories...), and sometimes a PDF receipt, and combine them into a single image for record keeping. In the past I often did this on my Android phone with [ZomboDroid Image Combiner & Editor](https://play.google.com/store/apps/details?id=com.zombodroid.imagecombinerfree) - but over time as I did this more the photos were not always on my phone and I wanted to be able to also combine images of PDF receipts - so I created this program!
            
            {HelpMarkdown.CombinedAboutToolsAndPackages}
            """;

        await HelpDisplayWindow.CreateInstanceAndShow([helpMarkdown], "Utilitarian Image Combiner Help and About");
    }
}