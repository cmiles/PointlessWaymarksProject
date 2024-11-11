using PDFtoImage;
using PointlessWaymarks.CommonTools;
using SkiaSharp;

namespace PointlessWaymarks.UtilitarianImage;

public static class Combiner
{
    public static List<string> SupportedExtensions =
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
        ".pdf"
    ];

    public static async Task<string> CombineImagesHorizontal(List<string> imagesToCombine, int maxWidth,
        int maxHeight, string outputFileFullName, int quality, SKColor backgroundColor,
        IProgress<string> progress)
    {
        progress.Report($"Combining {imagesToCombine.Count} Images - Max Individual Image Size {maxWidth}x{maxHeight}");

        for (var i = 0; i < imagesToCombine.Count; i++)
            if (Path.GetExtension(imagesToCombine[i]) == ".pdf")
            {
                var jpgFilename = await PdfToJpeg(imagesToCombine[i], maxWidth, quality, backgroundColor);
                imagesToCombine[i] = jpgFilename;
            }

        var images = new List<SKBitmap>();
        var imageResizeCounter = 0;
        foreach (var file in imagesToCombine)
        {
            progress.Report($"Resizing {++imageResizeCounter} of {imagesToCombine.Count}");
            using var image = SKBitmap.Decode(file);
            var resizedImage = DrawImage(image, maxWidth, maxHeight);
            images.Add(resizedImage);
        }

        var combinedImage = DrawImagesToHorizontalStrip(images, backgroundColor, progress);

        using var finalImage = SKImage.FromBitmap(combinedImage);
        using var data = finalImage.Encode(SKEncodedImageFormat.Jpeg, quality);
        var outputFileName = Path.ChangeExtension(outputFileFullName, ".jpg");
        await using var outputStream = File.OpenWrite(outputFileName);
        progress.Report($"Saving {outputFileName}");
        data.SaveTo(outputStream);

        foreach (var img in images) img.Dispose();
        combinedImage.Dispose();

        return outputFileName;
    }

    public static async Task<string> CombineImagesInGrid(List<string> imagesToCombine, int maxWidth, int maxHeight,
        string outputFileFullName, int quality, SKColor backgroundColor, IProgress<string> progress,
        int? rows = null, int? columns = null)
    {
        progress.Report(
            $"Combining {imagesToCombine.Count} Images into a Grid - Max Individual Image Size {maxWidth}x{maxHeight}");

        for (var i = 0; i < imagesToCombine.Count; i++)
            if (Path.GetExtension(imagesToCombine[i]) == ".pdf")
            {
                var jpgFilename = await PdfToJpeg(imagesToCombine[i], maxWidth, quality, backgroundColor);
                imagesToCombine[i] = jpgFilename;
            }

        var images = new List<SKBitmap>();
        var imageResizeCounter = 0;
        foreach (var file in imagesToCombine)
        {
            progress.Report($"Resizing {++imageResizeCounter} of {imagesToCombine.Count}");
            using var image = SKBitmap.Decode(file);
            var resizedImage = DrawImage(image, maxWidth, maxHeight);
            images.Add(resizedImage);
        }

        var combinedImage = DrawImagesToGrid(images, maxWidth, maxHeight, backgroundColor, progress, rows, columns);

        using var finalImage = SKImage.FromBitmap(combinedImage);
        using var data = finalImage.Encode(SKEncodedImageFormat.Jpeg, quality);
        var outputFileName = Path.ChangeExtension(outputFileFullName, ".jpg");
        await using var outputStream = File.OpenWrite(outputFileName);
        progress.Report($"Saving {outputFileName}");
        data.SaveTo(outputStream);

        foreach (var img in images) img.Dispose();
        combinedImage.Dispose();

        return outputFileName;
    }

    public static async Task<string> CombineImagesVertical(List<string> imagesToCombine, int maxWidth,
        int maxHeight, string outputFileFullName, int quality, SKColor backgroundColor,
        IProgress<string> progress)
    {
        progress.Report($"Combining {imagesToCombine.Count} Images - Max Individual Image Size {maxWidth}x{maxHeight}");

        for (var i = 0; i < imagesToCombine.Count; i++)
            if (Path.GetExtension(imagesToCombine[i]) == ".pdf")
            {
                var jpgFilename = await PdfToJpeg(imagesToCombine[i], maxWidth, quality, backgroundColor);
                imagesToCombine[i] = jpgFilename;
            }

        var images = new List<SKBitmap>();
        var imageResizeCounter = 0;
        foreach (var file in imagesToCombine)
        {
            progress.Report($"Resizing {++imageResizeCounter} of {imagesToCombine.Count}");
            using var image = SKBitmap.Decode(file);
            var resizedImage = DrawImage(image, maxWidth, maxHeight);
            images.Add(resizedImage);
        }

        var combinedImage = DrawImagesToVerticalStrip(images, backgroundColor, progress);

        using var finalImage = SKImage.FromBitmap(combinedImage);
        using var data = finalImage.Encode(SKEncodedImageFormat.Jpeg, quality);
        var outputFileName = Path.ChangeExtension(outputFileFullName, ".jpg");
        await using var outputStream = File.OpenWrite(outputFileName);
        progress.Report($"Saving {outputFileName}");
        data.SaveTo(outputStream);

        foreach (var img in images) img.Dispose();
        combinedImage.Dispose();

        return outputFileName;
    }

    private static SKBitmap DrawImage(SKBitmap image, int maxWidth, int maxHeight)
    {
        var width = image.Width;
        var height = image.Height;

        // Check if the image needs to be resized
        if (width > maxWidth || height > maxHeight)
        {
            var ratioX = (float)maxWidth / image.Width;
            var ratioY = (float)maxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            width = (int)(image.Width * ratio);
            height = (int)(image.Height * ratio);
        }

        var sizedImage = new SKBitmap(width, height);
        using var canvas = new SKCanvas(sizedImage);
        canvas.Clear(SKColors.Transparent);

        var paint = new SKPaint
        {
            FilterQuality = SKFilterQuality.High,
            IsAntialias = true
        };

        var destRect = new SKRect(0, 0, width, height);
        canvas.DrawBitmap(image, destRect, paint);

        return sizedImage;
    }

    private static SKBitmap DrawImagesToGrid(List<SKBitmap> images, int maxWidth, int maxHeight,
        SKColor backgroundColor, IProgress<string> progress, int? rows, int? columns)
    {
        var imageCount = images.Count;

        var gridRows = rows ?? 0;
        var gridColumns = columns ?? 0;

        if (rows.HasValue && columns.HasValue)
        {
            if (rows.Value * columns.Value < imageCount)
                throw new ArgumentException("The number of rows and columns is less than the number of images.");
        }
        else if (rows.HasValue)
        {
            gridColumns = (int)Math.Ceiling((double)imageCount / rows.Value);
        }
        else if (columns.HasValue)
        {
            gridRows = (int)Math.Ceiling((double)imageCount / columns.Value);
        }
        else
        {
            gridRows = (int)Math.Ceiling(Math.Sqrt(imageCount));
            gridColumns = (int)Math.Ceiling((double)imageCount / gridRows);
        }

        var cellWidth = Math.Min(images.Max(img => img.Width), maxWidth);
        var cellHeight = Math.Min(images.Max(img => img.Height), maxHeight);

        var gridWidth = cellWidth * gridColumns;
        var gridHeight = cellHeight * gridRows;

        progress.Report(
            $"Combining Images - Grid - to a Final {gridWidth}x{gridHeight} Size with {cellWidth}x{cellHeight} Cells");

        var combinedImage = new SKBitmap(gridWidth, gridHeight);
        using var canvas = new SKCanvas(combinedImage);
        canvas.Clear(backgroundColor);

        var imageCounter = 0;

        for (var r = 0; r < gridRows; r++)
        {
            for (var c = 0; c < gridColumns; c++)
            {
                var xOffset = c * cellWidth + (cellWidth - images[imageCounter].Width) / 2;
                var yOffset = r * cellHeight + (cellHeight - images[imageCounter].Height) / 2;

                progress.Report($"Drawing Image {imageCounter + 1} of {images.Count} - Row {r}, Column {c}");

                canvas.DrawBitmap(images[imageCounter++], xOffset, yOffset);

                if (imageCounter >= imageCount) break;
            }

            if (imageCounter >= imageCount) break;
        }

        return combinedImage;
    }

    private static SKBitmap DrawImagesToHorizontalStrip(List<SKBitmap> images, SKColor backgroundColor,
        IProgress<string> progress)
    {
        var width = images.Sum(img => img.Width);
        var height = images.Max(img => img.Height);

        progress.Report($"Combining Images - Horizontal - to a Final {width}x{height} Size");

        var combinedImage = new SKBitmap(width, height);
        using var canvas = new SKCanvas(combinedImage);
        canvas.Clear(backgroundColor);
        var xOffset = 0;

        var imageCounter = 0;
        foreach (var image in images)
        {
            progress.Report($"Drawing Image {++imageCounter} of {images.Count}");
            var yOffset = (height - image.Height) / 2; // Center the image vertically
            canvas.DrawBitmap(image, xOffset, yOffset);
            xOffset += image.Width;
        }

        return combinedImage;
    }


    private static SKBitmap DrawImagesToVerticalStrip(List<SKBitmap> images, SKColor backgroundColor,
        IProgress<string> progress)
    {
        var width = images.Max(img => img.Width);
        var height = images.Sum(img => img.Height);

        progress.Report($"Combining Images - Vertical - to a Final {width}x{height} Size");

        var combinedImage = new SKBitmap(width, height);
        using var canvas = new SKCanvas(combinedImage);
        canvas.Clear(backgroundColor);
        var yOffset = 0;

        var imageCounter = 0;
        foreach (var image in images)
        {
            progress.Report($"Drawing Image {++imageCounter} of {images.Count}");
            var xOffset = (width - image.Width) / 2; // Center the image horizontally
            canvas.DrawBitmap(image, xOffset, yOffset);
            yOffset += image.Height;
        }

        return combinedImage;
    }

    public static async Task<FileInfo?> Flip(string fullFileName)
    {
        return await Rotate(new FileInfo(fullFileName), ImageRotation.Flip).ConfigureAwait(false);
    }

    public static async Task<string> PdfToJpeg(string pdfFileName, int maxWidth,
        int jpegQuality, SKColor backgroundColor)
    {
        // Convert PDF pages to images asynchronously
        await using var pdfStream = File.OpenRead(pdfFileName);
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
        canvas.Clear(backgroundColor);

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
        var safeName = UniqueFileTools.UniqueFile(new DirectoryInfo(Path.GetDirectoryName(pdfFileName)),
            $"{Path.GetFileNameWithoutExtension(pdfFileName)}.jpg");
        await using var outputStream = File.OpenWrite(safeName.FullName);
        data.SaveTo(outputStream);

        return safeName.FullName;
    }

    public static async Task<FileInfo?> Rotate(FileInfo toRotate, ImageRotation orientation)
    {
        if (!toRotate.Exists) return null;

        // Determine the original file format
        var originalExtension = toRotate.Extension.ToLower();
        var skEncodedImageFormat = originalExtension switch
        {
            ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
            ".png" => SKEncodedImageFormat.Png,
            ".gif" => SKEncodedImageFormat.Gif,
            ".bmp" => SKEncodedImageFormat.Bmp,
            ".webp" => SKEncodedImageFormat.Webp,
            ".ico" => SKEncodedImageFormat.Ico,
            ".wbmp" => SKEncodedImageFormat.Wbmp,
            ".pkm" => SKEncodedImageFormat.Pkm,
            ".ktx" => SKEncodedImageFormat.Ktx,
            ".astc" => SKEncodedImageFormat.Astc,
            ".dng" => SKEncodedImageFormat.Dng,
            ".heif" => SKEncodedImageFormat.Heif,
            _ => throw new NotSupportedException($"File format {originalExtension} is not supported.")
        };

        await using var inputStream = File.OpenRead(toRotate.FullName);
        using var originalBitmap = SKBitmap.Decode(inputStream);

        // Rotate the image
        SKBitmap rotatedBitmap;
        switch (orientation)
        {
            case ImageRotation.Right:
                rotatedBitmap = new SKBitmap(originalBitmap.Height, originalBitmap.Width);
                using (var canvas = new SKCanvas(rotatedBitmap))
                {
                    canvas.Translate(rotatedBitmap.Width, 0);
                    canvas.RotateDegrees(90);
                    canvas.DrawBitmap(originalBitmap, 0, 0);
                }

                break;
            case ImageRotation.Flip:
                rotatedBitmap = new SKBitmap(originalBitmap.Width, originalBitmap.Height);
                using (var canvas = new SKCanvas(rotatedBitmap))
                {
                    canvas.Translate(rotatedBitmap.Width, rotatedBitmap.Height);
                    canvas.RotateDegrees(180);
                    canvas.DrawBitmap(originalBitmap, 0, 0);
                }

                break;
            case ImageRotation.Left:
                rotatedBitmap = new SKBitmap(originalBitmap.Height, originalBitmap.Width);
                using (var canvas = new SKCanvas(rotatedBitmap))
                {
                    canvas.Translate(0, rotatedBitmap.Height);
                    canvas.RotateDegrees(270);
                    canvas.DrawBitmap(originalBitmap, 0, 0);
                }

                break;
            default:
                throw new ArgumentException("Invalid rotation angle.");
        }

        // Save the rotated image in the original format
        using var image = SKImage.FromBitmap(rotatedBitmap);
        var saveAsFileName = toRotate.FullName;

        if (!(skEncodedImageFormat is SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Webp
                or SKEncodedImageFormat.Png))
        {
            skEncodedImageFormat = SKEncodedImageFormat.Jpeg;
            saveAsFileName = Path.ChangeExtension(toRotate.FullName, ".jpg");
        }

        using var data = image.Encode(skEncodedImageFormat, 100);
        await using var outputStream = File.OpenWrite(saveAsFileName);
        data.SaveTo(outputStream);

        return toRotate;
    }

    public static async Task<FileInfo?> RotateLeft(string fullFileName)
    {
        return await Rotate(new FileInfo(fullFileName), ImageRotation.Left).ConfigureAwait(false);
    }

    public static async Task<FileInfo?> RotateRight(string fullFileName)
    {
        return await Rotate(new FileInfo(fullFileName), ImageRotation.Right).ConfigureAwait(false);
    }
}