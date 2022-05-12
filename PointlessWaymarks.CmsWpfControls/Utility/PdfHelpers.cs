using System.IO;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ImageContentEditor;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.Utility;

public static class PdfHelpers
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
                        new List<string> {"Yes", "No"}) == "No")
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

            if (updatedDestination is not {Exists: true})
            {
                if (await statusContext.ShowMessage("PDF Generation Problem",
                        $"Execution Failed for {content.Title} - Continue??{Environment.NewLine}{errorOutput}",
                        new List<string> {"Yes", "No"}) == "No")
                    return;

                continue;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            var newImage = new ImageContent {ContentId = Guid.NewGuid()};

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
}