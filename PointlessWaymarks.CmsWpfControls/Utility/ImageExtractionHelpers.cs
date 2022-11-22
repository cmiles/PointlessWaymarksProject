using System.Drawing;
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
using Windows.Data.Pdf;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsWpfControls.Utility;

public static class ImageExtractionHelpers
{
    public static async Task PdfPageToImage(StatusControlContext statusContext,
        List<FileContent> selected, int pageNumber)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

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
            }
            else
            {
                destinationFile = new FileInfo(Path.Combine(UserSettingsUtilities.TempStorageDirectory().FullName,
                    $"{Path.GetFileNameWithoutExtension(targetFile.Name)}-Page.jpg"));
            }

            if (destinationFile.Exists)
            {
                destinationFile.Delete();
                destinationFile.Refresh();
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

            var file = await StorageFile.GetFileFromPathAsync(targetFile.FullName);
            var pdfDocument = await PdfDocument.LoadFromFileAsync(file);
            var pageIndex = (uint)pageNumber - 1;

            var destinationStorageDirectory =
                await StorageFolder.GetFolderFromPathAsync(destinationFile.Directory.FullName);
            var destinationStorageFile = await destinationStorageDirectory.CreateFileAsync(destinationFile.Name);

            using (var pdfPage = pdfDocument.GetPage(pageIndex))
            using (var transaction = await destinationStorageFile.OpenTransactedWriteAsync())
            {
                var pdfPageRenderOptions = new PdfPageRenderOptions
                {
                    DestinationHeight = (uint)pdfPage.Size.Height * 2,
                    DestinationWidth = (uint)pdfPage.Size.Width * 2,
                    BitmapEncoderId = BitmapEncoder.JpegEncoderId
                };

                await pdfPage.RenderToStreamAsync(transaction.Stream, pdfPageRenderOptions);
            }

            destinationFile.Refresh();

            await ThreadSwitcher.ResumeForegroundAsync();

            var newImage = new ImageContent { ContentId = Guid.NewGuid() };

            if (pageNumber == 1)
            {
                newImage.Title = $"{content.Title} Cover Page";
                newImage.Summary = $"Cover Page from {content.Title}.";
            }
            else
            {
                newImage.Title = $"{content.Title} - Page {pageNumber}";;
                newImage.Summary = $"Page {pageNumber} from {content.Title}.";
            }

            newImage.FeedOn = DateTime.Now;
            newImage.ShowInSearch = false;
            newImage.Folder = content.Folder;
            newImage.Tags = content.Tags;
            newImage.Slug = SlugTools.CreateSlug(true, newImage.Title);
            newImage.BodyContentFormat = ContentFormatDefaults.Content.ToString();
            newImage.BodyContent = $"Generated from {BracketCodeFiles.Create(content)}.";
            newImage.UpdateNotesFormat = ContentFormatDefaults.Content.ToString();

            var editor = await ImageContentEditorWindow.CreateInstance(newImage, destinationFile);
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

            newImage.Slug = SlugTools.CreateSlug(true, newImage.Title);
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
            Slug = SlugTools.CreateSlug(true, $"{selected.Title} Video Cover Image"),
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