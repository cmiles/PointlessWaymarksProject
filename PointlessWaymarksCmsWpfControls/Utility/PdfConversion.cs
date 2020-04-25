using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsWpfControls.ImageContentEditor;
using PointlessWaymarksCmsWpfControls.Status;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    public static class PdfConversion
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

            foreach (var loopSelected in toProcess)
            {
                string executionParameters;


                if (pageNumber == 1)
                    executionParameters =
                        $"-jpeg -singlefile \"{loopSelected.targetFile.FullName}\" \"{Path.Combine(loopSelected.destinationFile.Directory.FullName, Path.GetFileNameWithoutExtension(loopSelected.destinationFile.FullName))}\"";
                else
                    executionParameters =
                        $"-jpeg -f {pageNumber} -l {pageNumber} \"{loopSelected.targetFile.FullName}\" \"{Path.Combine(loopSelected.destinationFile.Directory.FullName, Path.GetFileNameWithoutExtension(loopSelected.destinationFile.FullName))}\"";

                var (success, _, errorOutput) = Processes.ExecuteProcess(pdfToCairoExe.FullName, executionParameters,
                    statusContext.ProgressTracker());

                if (!success)
                {
                    if (await statusContext.ShowMessage("PDF Generation Problem",
                        $"Execution Failed for {loopSelected.content.Title} - Continue??{Environment.NewLine}{errorOutput}",
                        new List<string> {"Yes", "No"}) == "No")
                        return;

                    continue;
                }

                FileInfo updatedDestination = null;

                if (pageNumber == 1)
                {
                    //With the singlefile option pdftocairo uses your filename directly
                    loopSelected.destinationFile.Refresh();
                    updatedDestination = loopSelected.destinationFile;
                }
                else
                {
                    var directoryToSearch = loopSelected.destinationFile.Directory;
                    var baseName = loopSelected.targetFile.Name;

                    var possibleFiles = directoryToSearch
                        .EnumerateFiles($"{Path.GetFileNameWithoutExtension(loopSelected.destinationFile.Name)}-*.jpg")
                        .ToList();

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

                if (updatedDestination == null || !updatedDestination.Exists)
                {
                    if (await statusContext.ShowMessage("PDF Generation Problem",
                        $"Execution Failed for {loopSelected.content.Title} - Continue??{Environment.NewLine}{errorOutput}",
                        new List<string> {"Yes", "No"}) == "No")
                        return;

                    continue;
                }

                await ThreadSwitcher.ResumeForegroundAsync();

                var editor = new ImageContentEditorWindow(updatedDestination);
                editor.Show();
                if (pageNumber == 1)
                {
                    editor.ImageEditor.TitleSummarySlugFolder.Title = $"{loopSelected.content.Title} Cover Page";
                    editor.ImageEditor.TitleSummarySlugFolder.Summary =
                        $"Cover Page from {loopSelected.content.Title}.";
                }
                else
                {
                    editor.ImageEditor.TitleSummarySlugFolder.Title = $"{loopSelected.content.Title} Page {pageNumber}";
                    editor.ImageEditor.TitleSummarySlugFolder.Summary =
                        $"Page {pageNumber} from {loopSelected.content.Title}.";
                }

                editor.ImageEditor.TitleSummarySlugFolder.TitleToSlug();

                editor.ImageEditor.TitleSummarySlugFolder.Folder = loopSelected.content.Folder;
                editor.ImageEditor.TagEdit.Tags = loopSelected.content.Tags;
                editor.ImageEditor.ImageSourceNotes = $"Generated by pdftocairo from {updatedDestination.Name}.";
                await ThreadSwitcher.ResumeBackgroundAsync();
            }
        }
    }
}