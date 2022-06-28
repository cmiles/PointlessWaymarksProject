using System.IO;
using Windows.Graphics.Imaging;
using Windows.Media.Editing;
using Windows.Storage;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ImageContentEditor;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.Utility;

public static class ImageExtractionHelpers
{
    public static async Task PdfPageToImageWithPdfToCairo(StatusControlContext statusContext,
        List<FileContent> selected, int pageNumber)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();


        var pdfToCairoDirectoryString = UserSettingsSingleton.CurrentSettings().PdfToCairoExeDirectory;
        if (string.IsNullOrWhiteSpace(pdfToCairoDirectoryString))
        {
            statusContext.ToastError(
                "Sorry - this function requires that pdftocairo.exe be on the system - please set the directory... ");
            return;
        }

        var pdfToCairoDirectory = new DirectoryInfo(pdfToCairoDirectoryString);
        if (!pdfToCairoDirectory.Exists)
        {
            statusContext.ToastError(
                $"{pdfToCairoDirectory.FullName} doesn't exist? Check your pdftocairo bin directory setting.");
            return;
        }

        var pdfToCairoExe = new FileInfo(Path.Combine(pdfToCairoDirectory.FullName, "pdftocairo.exe"));
        if (!pdfToCairoExe.Exists)
        {
            statusContext.ToastError(
                $"{pdfToCairoExe.FullName} doesn't exist? Check your pdftocairo bin directory setting.");
            return;
        }

        var toProcess = new List<(FileInfo targetFile, FileInfo destinationFile, FileContent content)>();

        foreach (var loopSelected in selected)
        {
            var targetFile = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalSiteFileContentDirectory(loopSelected).FullName,
                loopSelected.OriginalFileName));

            if (!targetFile.Exists) continue;

            if (!targetFile.Extension.ToLower().Contains("pdf"))
                continue;

            FileInfo destinationFile;
            if (pageNumber == 1)
            {
                destinationFile = new FileInfo(Path.Combine(UserSettingsUtilities.TempStorageDirectory().FullName,
                    $"{Path.GetFileNameWithoutExtension(targetFile.Name)}-CoverPage.jpg"));

                if (destinationFile.Exists)
                {
                    destinationFile.Delete();
                    destinationFile.Refresh();
                }
            }
            else
            {
                destinationFile = new FileInfo(Path.Combine(UserSettingsUtilities.TempStorageDirectory().FullName,
                    $"{Path.GetFileNameWithoutExtension(targetFile.Name)}-Page.jpg"));
            }

            toProcess.Add((targetFile, destinationFile, loopSelected));
        }

        if (!toProcess.Any())
        {
            statusContext.ToastError("No PDFs found? This process can only generate PDF previews...");
            return;
        }

        foreach (var (targetFile, destinationFile, content) in toProcess)
        {
            if (destinationFile.Directory == null)
            {
                statusContext.ToastError($"Problem with {destinationFile.FullName} - Directory is Null?");
                continue;
            }

            var executionParameters = pageNumber == 1
                ? $"-jpeg -singlefile \"{targetFile.FullName}\" \"{Path.Combine(destinationFile.Directory.FullName, Path.GetFileNameWithoutExtension(destinationFile.FullName))}\""
                : $"-jpeg -f {pageNumber} -l {pageNumber} \"{targetFile.FullName}\" \"{Path.Combine(destinationFile.Directory.FullName, Path.GetFileNameWithoutExtension(destinationFile.FullName))}\"";

            var (success, _, errorOutput) = ProcessHelpers.ExecuteProcess(pdfToCairoExe.FullName,
                executionParameters, statusContext.ProgressTracker());

            if (!success)
            {
                if (await statusContext.ShowMessage("PDF Generation Problem",
                        $"Execution Failed for {content.Title} - Continue??{Environment.NewLine}{errorOutput}",
                        new List<string> { "Yes", "No" }) == "No")
                    return;

                continue;
            }

            FileInfo updatedDestination = null;

            if (pageNumber == 1)
            {
                //With the singlefile option pdftocairo uses your filename directly
                destinationFile.Refresh();
                updatedDestination = destinationFile;
            }
            else
            {
                var directoryToSearch = destinationFile.Directory;

                var possibleFiles = directoryToSearch
                    .EnumerateFiles($"{Path.GetFileNameWithoutExtension(destinationFile.Name)}-*.jpg").ToList();

                foreach (var loopFiles in possibleFiles)
                {
                    var fileNamePageNumber = loopFiles.Name.Split("-Page-").ToList().Last().Replace(".jpg", "");

                    if (int.TryParse(fileNamePageNumber, out var possiblePageNumber) &&
                        possiblePageNumber == pageNumber)
                    {
                        updatedDestination = loopFiles;
                        break;
                    }
                }
            }

            if (updatedDestination is not { Exists: true })
            {
                if (await statusContext.ShowMessage("PDF Generation Problem",
                        $"Execution Failed for {content.Title} - Continue??{Environment.NewLine}{errorOutput}",
                        new List<string> { "Yes", "No" }) == "No")
                    return;

                continue;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            var newImage = new ImageContent { ContentId = Guid.NewGuid() };

            if (pageNumber == 1)
            {
                newImage.Title = $"{content.Title} Cover Page";
                newImage.Summary = $"Cover Page from {content.Title}.";
            }
            else
            {
                newImage.Title = $"{content.Title} - Page {pageNumber}";
                newImage.Summary = $"Page {pageNumber} from {content.Title}.";
            }

            newImage.FeedOn = DateTime.Now;
            newImage.ShowInSearch = false;
            newImage.Folder = content.Folder;
            newImage.Tags = content.Tags;
            newImage.Slug = SlugUtility.Create(true, newImage.Title);
            newImage.BodyContentFormat = ContentFormatDefaults.Content.ToString();
            newImage.BodyContent = $"Generated by pdftocairo from {BracketCodeFiles.Create(content)}.";
            newImage.UpdateNotesFormat = ContentFormatDefaults.Content.ToString();

            var editor = await ImageContentEditorWindow.CreateInstance(newImage, updatedDestination);
            editor.PositionWindowAndShow();

            await ThreadSwitcher.ResumeBackgroundAsync();
        }
    }

    public static async Task VideoFrameToImage(StatusControlContext statusContext,
        List<FileContent> selected)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var toProcess = new List<(FileInfo targetFile, FileInfo destinationFile, FileContent content)>();

        foreach (var loopSelected in selected)
        {
            var targetFile = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalSiteFileContentDirectory(loopSelected).FullName,
                loopSelected.OriginalFileName));

            if (!targetFile.Exists) continue;

            if (!targetFile.Extension.ToLower().Contains("mp4"))
                continue;

            var file = await StorageFile.GetFileFromPathAsync(targetFile.FullName);
            var mediaClip = await MediaClip.CreateFromFileAsync(file);
            var mediaComposition = new MediaComposition();
            mediaComposition.Clips.Add(mediaClip);
            var imageStream = await mediaComposition.GetThumbnailAsync(
                TimeSpan.FromMilliseconds(100), 0, 0, VideoFramePrecision.NearestFrame);
            var decoder = await BitmapDecoder.CreateAsync(imageStream);
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            var destinationFile = new FileInfo(Path.Combine(UserSettingsUtilities.TempStorageDirectory().FullName,
                $"{Path.GetFileNameWithoutExtension(targetFile.Name)}-Cover.jpg"));

            var destinationStorageDirectory =
                await StorageFolder.GetFolderFromPathAsync(UserSettingsUtilities.TempStorageDirectory().FullName);

            var thumbnailFile = await destinationStorageDirectory.CreateFileAsync(
                $"{Path.GetFileNameWithoutExtension(targetFile.Name)}-Cover.jpg",
                CreationCollisionOption.ReplaceExisting);
            using var stream = await thumbnailFile.OpenAsync(FileAccessMode.ReadWrite);
            var encoder =
                await BitmapEncoder.CreateAsync(
                    BitmapEncoder.JpegEncoderId, stream);
            encoder.SetSoftwareBitmap(softwareBitmap);
            await encoder.FlushAsync();

            toProcess.Add((targetFile, destinationFile, loopSelected));
        }

        if (!toProcess.Any())
        {
            statusContext.ToastError("No MP4s found? This process can only generate MP4 previews...");
            return;
        }

        foreach (var (targetFile, destinationFile, content) in toProcess)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var newImage = new ImageContent
            {
                ContentId = Guid.NewGuid(),
                Title = $"{content.Title} Cover",
                Summary = $"Cover from {content.Title}.",
                FeedOn = DateTime.Now,
                ShowInSearch = false,
                Folder = content.Folder,
                Tags = content.Tags
            };

            newImage.Slug = SlugUtility.Create(true, newImage.Title);
            newImage.BodyContentFormat = ContentFormatDefaults.Content.ToString();
            newImage.BodyContent = $"Image from {BracketCodeFiles.Create(content)}.";
            newImage.UpdateNotesFormat = ContentFormatDefaults.Content.ToString();

            var editor = await ImageContentEditorWindow.CreateInstance(newImage, destinationFile);
            editor.PositionWindowAndShow();

            await ThreadSwitcher.ResumeBackgroundAsync();
        }
    }

    public static async Task<Guid?> VideoFrameToImageAutoSave(StatusControlContext statusContext,
        FileContent selected)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var targetFile = new FileInfo(Path.Combine(
            UserSettingsSingleton.CurrentSettings().LocalSiteFileContentDirectory(selected).FullName,
            selected.OriginalFileName));

        if (!targetFile.Exists) return null;

        if (!targetFile.Extension.ToLower().Contains("mp4"))
            return null;

        var file = await StorageFile.GetFileFromPathAsync(targetFile.FullName);
        var mediaClip = await MediaClip.CreateFromFileAsync(file);
        var mediaComposition = new MediaComposition();
        mediaComposition.Clips.Add(mediaClip);
        var imageStream = await mediaComposition.GetThumbnailAsync(
            TimeSpan.FromMilliseconds(100), 0, 0, VideoFramePrecision.NearestFrame);
        var decoder = await BitmapDecoder.CreateAsync(imageStream);
        var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

        var destinationFile = new FileInfo(Path.Combine(UserSettingsUtilities.TempStorageDirectory().FullName,
            $"{Path.GetFileNameWithoutExtension(targetFile.Name)}-Video-Cover-Image.jpg"));

        var destinationStorageDirectory =
            await StorageFolder.GetFolderFromPathAsync(UserSettingsUtilities.TempStorageDirectory().FullName);

        var thumbnailFile = await destinationStorageDirectory.CreateFileAsync(
            $"{Path.GetFileNameWithoutExtension(targetFile.Name)}-Video-Cover-Image.jpg",
            CreationCollisionOption.ReplaceExisting);
        using (var stream = await thumbnailFile.OpenAsync(FileAccessMode.ReadWrite))
        {
            var encoder =
                await BitmapEncoder.CreateAsync(
                    BitmapEncoder.JpegEncoderId, stream);
            encoder.SetSoftwareBitmap(softwareBitmap);
            await encoder.FlushAsync();
        }

        var newImage = new ImageContent
        {
            ContentId = Guid.NewGuid(),
            CreatedBy = selected.CreatedBy,
            CreatedOn = DateTime.Now,
            Title = $"{selected.Title} Video Cover Image",
            Summary = $"Video Cover Image from {selected.Title}.",
            FeedOn = DateTime.Now,
            ShowInSearch = false,
            Folder = selected.Folder,
            Tags = selected.Tags,
            Slug = SlugUtility.Create(true, $"{selected.Title} Video Cover Image"),
            BodyContentFormat = ContentFormatDefaults.Content.ToString(),
            BodyContent = $"Frame from {BracketCodeFiles.Create(selected)}.",
            UpdateNotesFormat = ContentFormatDefaults.Content.ToString()
        };

        var autoSaveReturn =
            await ImageGenerator.SaveAndGenerateHtml(newImage, destinationFile, true, null,
                statusContext.ProgressTracker());


        if (autoSaveReturn.generationReturn.HasError)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var editor = await ImageContentEditorWindow.CreateInstance(newImage, destinationFile);
            editor.PositionWindowAndShow();

            return null;
        }

        return autoSaveReturn.imageContent.ContentId;
    }
}