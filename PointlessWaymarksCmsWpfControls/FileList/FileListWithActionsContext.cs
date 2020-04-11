using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using MvvmHelpers.Commands;
using Omu.ValueInjecter;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.FileHtml;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsWpfControls.FileContentEditor;
using PointlessWaymarksCmsWpfControls.ImageContentEditor;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.FileList
{
    public class FileListWithActionsContext : INotifyPropertyChanged
    {
        private Command _deleteSelectedCommand;
        private Command _editSelectedContentCommand;
        private Command _fileDownloadLinkCodesToClipboardForSelectedCommand;
        private Command _firstPagePreviewFromPdfToCairoCommand;
        private Command _generateSelectedHtmlCommand;
        private FileListContext _listContext;
        private Command _newContentCommand;
        private Command _openUrlForSelectedCommand;
        private List<(object, string)> _pdfPreviewGenerationErrorOutput = new List<(object, string)>();

        private List<(object, string)> _pdfPreviewGenerationProgress = new List<(object, string)>();
        private Command _photoPageLinkCodesToClipboardForSelectedCommand;
        private StatusControlContext _statusContext;

        public FileListWithActionsContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
        }

        public Command DeleteSelectedCommand
        {
            get => _deleteSelectedCommand;
            set
            {
                if (Equals(value, _deleteSelectedCommand)) return;
                _deleteSelectedCommand = value;
                OnPropertyChanged();
            }
        }

        public Command EditSelectedContentCommand
        {
            get => _editSelectedContentCommand;
            set
            {
                if (Equals(value, _editSelectedContentCommand)) return;
                _editSelectedContentCommand = value;
                OnPropertyChanged();
            }
        }


        public Command ExtractNewLinksInSelectedCommand { get; set; }

        public Command FileDownloadLinkCodesToClipboardForSelectedCommand
        {
            get => _fileDownloadLinkCodesToClipboardForSelectedCommand;
            set
            {
                if (Equals(value, _fileDownloadLinkCodesToClipboardForSelectedCommand)) return;
                _fileDownloadLinkCodesToClipboardForSelectedCommand = value;
                OnPropertyChanged();
            }
        }

        public Command FilePageLinkCodesToClipboardForSelectedCommand
        {
            get => _photoPageLinkCodesToClipboardForSelectedCommand;
            set
            {
                if (Equals(value, _photoPageLinkCodesToClipboardForSelectedCommand)) return;
                _photoPageLinkCodesToClipboardForSelectedCommand = value;
                OnPropertyChanged();
            }
        }

        public Command FirstPagePreviewFromPdfToCairoCommand
        {
            get => _firstPagePreviewFromPdfToCairoCommand;
            set
            {
                if (Equals(value, _firstPagePreviewFromPdfToCairoCommand)) return;
                _firstPagePreviewFromPdfToCairoCommand = value;
                OnPropertyChanged();
            }
        }


        public Command GenerateSelectedHtmlCommand
        {
            get => _generateSelectedHtmlCommand;
            set
            {
                if (Equals(value, _generateSelectedHtmlCommand)) return;
                _generateSelectedHtmlCommand = value;
                OnPropertyChanged();
            }
        }

        public FileListContext ListContext
        {
            get => _listContext;
            set
            {
                if (Equals(value, _listContext)) return;
                _listContext = value;
                OnPropertyChanged();
            }
        }

        public Command NewContentCommand
        {
            get => _newContentCommand;
            set
            {
                if (Equals(value, _newContentCommand)) return;
                _newContentCommand = value;
                OnPropertyChanged();
            }
        }

        public Command OpenUrlForSelectedCommand
        {
            get => _openUrlForSelectedCommand;
            set
            {
                if (Equals(value, _openUrlForSelectedCommand)) return;
                _openUrlForSelectedCommand = value;
                OnPropertyChanged();
            }
        }

        public Command RefreshDataCommand { get; set; }

        public StatusControlContext StatusContext
        {
            get => _statusContext;
            set
            {
                if (Equals(value, _statusContext)) return;
                _statusContext = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private async Task Delete()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var selected = ListContext.SelectedItems;

            if (selected == null || !selected.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            if (selected.Count > 1)
            {
                StatusContext.ToastError("Sorry - please delete one at a time");
                return;
            }

            var selectedItem = selected.Single();

            if (selectedItem.DbEntry == null || selectedItem.DbEntry.Id < 1)
            {
                StatusContext.ToastError("Entry is not saved?");
                return;
            }

            var settings = UserSettingsSingleton.CurrentSettings();

            var possibleContentDirectory = settings.LocalSiteFileContentDirectory(selectedItem.DbEntry, false);
            if (possibleContentDirectory.Exists) possibleContentDirectory.Delete(true);

            var context = await Db.Context();

            var toHistoric = await context.FileContents.Where(x => x.ContentId == selectedItem.DbEntry.ContentId)
                .ToListAsync();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricFileContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                await context.HistoricFileContents.AddAsync(newHistoric);
                context.FileContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync(true);

            await LoadData();
        }

        private async Task EditSelectedContent()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListContext.SelectedItems == null || !ListContext.SelectedItems.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var context = await Db.Context();
            var frozenList = ListContext.SelectedItems;

            foreach (var loopSelected in frozenList)
            {
                var refreshedData =
                    context.FileContents.SingleOrDefault(x => x.ContentId == loopSelected.DbEntry.ContentId);

                if (refreshedData == null)
                {
                    StatusContext.ToastError(
                        $"{loopSelected.DbEntry.Title} is no longer active in the database? Can not edit - " +
                        "look for a historic version...");
                    continue;
                }

                await ThreadSwitcher.ResumeForegroundAsync();

                var newContentWindow = new FileContentEditorWindow(refreshedData);

                newContentWindow.Show();

                await ThreadSwitcher.ResumeBackgroundAsync();
            }
        }

        private void ErrorOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            //* Do your stuff with the output (write to console/log/StringBuilder)
            _pdfPreviewGenerationErrorOutput.Add((sendingProcess, outLine.Data));
        }

        public (bool success, string standardOutput, string errorOutput) ExecuteProcess(string programToExecute,
            string executionParameters, IProgress<string> progress)
        {
            if (string.IsNullOrWhiteSpace(programToExecute)) return (false, string.Empty, "Blank program to Execute?");

            var programToExecuteFile = new FileInfo(programToExecute);

            if (!programToExecuteFile.Exists)
                return (false, string.Empty, $"Program to Execute {programToExecuteFile} does not exist.");

            var standardOutput = new StringBuilder();
            var errorOutput = new StringBuilder();

            progress?.Report($"Setting up execution of {programToExecute} {executionParameters}");

            using var process = new Process
            {
                StartInfo =
                {
                    FileName = programToExecute,
                    Arguments = executionParameters,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true
                }
            };

            void OnStandardOutput(object o, DataReceivedEventArgs e)
            {
                standardOutput.AppendLine(e.Data);
                progress?.Report(e.Data);
            }

            void OnErrorOutput(object o, DataReceivedEventArgs e)
            {
                errorOutput.AppendLine(e.Data);
                progress?.Report(e.Data);
            }

            process.OutputDataReceived += OnStandardOutput;
            process.ErrorDataReceived += OnErrorOutput;

            bool result;

            try
            {
                progress?.Report("Starting Process");
                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                result = process.WaitForExit(180000);
            }
            finally
            {
                process.OutputDataReceived -= OnStandardOutput;
                process.ErrorDataReceived -= OnErrorOutput;
            }

            return (result, standardOutput.ToString(), errorOutput.ToString());
        }

        private async Task ExtractNewLinksInSelected()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListContext.SelectedItems == null || !ListContext.SelectedItems.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var context = await Db.Context();
            var frozenList = ListContext.SelectedItems;

            foreach (var loopSelected in frozenList)
            {
                var refreshedData =
                    context.FileContents.SingleOrDefault(x => x.ContentId == loopSelected.DbEntry.ContentId);

                if (refreshedData == null) continue;

                await LinkExtraction.ExtractNewAndShowLinkStreamEditors(
                    $"{refreshedData.BodyContent} {refreshedData.UpdateNotes}", StatusContext.ProgressTracker());
            }
        }

        private async Task FileDownloadLinkCodesToClipboardForSelected()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListContext.SelectedItems == null || !ListContext.SelectedItems.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var finalString = string.Empty;

            foreach (var loopSelected in ListContext.SelectedItems)
                finalString +=
                    @$"{{{{filedownloadlink {loopSelected.DbEntry.ContentId}; {loopSelected.DbEntry.Title}}}}}{Environment.NewLine}";

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);

            StatusContext.ToastSuccess($"To Clipboard {finalString}");
        }

        private async Task FilePageLinkCodesToClipboardForSelected()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListContext.SelectedItems == null || !ListContext.SelectedItems.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var finalString = string.Empty;

            foreach (var loopSelected in ListContext.SelectedItems)
                finalString +=
                    @$"{{{{filelink {loopSelected.DbEntry.ContentId}; {loopSelected.DbEntry.Title}}}}}{Environment.NewLine}";

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);

            StatusContext.ToastSuccess($"To Clipboard {finalString}");
        }

        private async Task FirstPagePreviewFromPdfToCairo()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var selected = ListContext.SelectedItems;

            if (selected == null || !selected.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var pdfToCairoDirectoryString = UserSettingsSingleton.CurrentSettings().PdfToCairoExeDirectory;
            if (string.IsNullOrWhiteSpace(pdfToCairoDirectoryString))
            {
                StatusContext.ToastError(
                    "Sorry - this function requires that pdftocairo.exe be on the system - please set the directory... ");
                return;
            }

            var pdfToCairoDirectory = new DirectoryInfo(pdfToCairoDirectoryString);
            if (!pdfToCairoDirectory.Exists)
            {
                StatusContext.ToastError(
                    $"{pdfToCairoDirectory.FullName} doesn't exist? Check your pdftocairo bin directory setting.");
                return;
            }

            var pdfToCairoExe = new FileInfo(Path.Combine(pdfToCairoDirectory.FullName, "pdftocairo.exe"));
            if (!pdfToCairoExe.Exists)
            {
                StatusContext.ToastError(
                    $"{pdfToCairoExe.FullName} doesn't exist? Check your pdftocairo bin directory setting.");
                return;
            }

            var toProcess = new List<(FileInfo targetFile, FileInfo destinationFile, FileContent content)>();
            foreach (var loopSelected in selected)
            {
                var targetFile = new FileInfo(Path.Combine(
                    UserSettingsSingleton.CurrentSettings().LocalSiteFileContentDirectory(loopSelected.DbEntry)
                        .FullName, loopSelected.DbEntry.OriginalFileName));

                if (!targetFile.Extension.ToLower().Contains("pdf"))
                    continue;

                var destinationFile = new FileInfo(Path.Combine(UserSettingsUtilities.TempStorageDirectory().FullName,
                    $"{Path.GetFileNameWithoutExtension(targetFile.Name)}-FirstPage.jpg"));

                if (destinationFile.Exists)
                {
                    destinationFile.Delete();
                    destinationFile.Refresh();
                }

                toProcess.Add((targetFile, destinationFile, loopSelected.DbEntry));
            }

            if (!toProcess.Any())
            {
                StatusContext.ToastError("No PDFs found? This process can only generate PDF previews...");
                return;
            }

            foreach (var loopSelected in toProcess)
            {
                var executionParameters =
                    $"-jpeg -singlefile \"{loopSelected.targetFile.FullName}\" \"{Path.Combine(loopSelected.destinationFile.Directory.FullName, Path.GetFileNameWithoutExtension(loopSelected.destinationFile.FullName))}\"";

                var executionResult = ExecuteProcess(pdfToCairoExe.FullName, executionParameters,
                    StatusContext.ProgressTracker());

                if (!executionResult.success)
                {
                    if (loopSelected == toProcess.Last())
                    {
                        await StatusContext.ShowMessage("PDF Generation Problem",
                            $"Execution Failed for {loopSelected.content.Title} - Continue??{Environment.NewLine}{executionResult.errorOutput}",
                            new List<string> {"Yes", "No"});
                    }
                    else
                    {
                        if ((await StatusContext.ShowMessage("PDF Generation Problem",
                            $"Execution Failed for {loopSelected.content.Title} - Continue??{Environment.NewLine}{executionResult.errorOutput}",
                            new List<string> {"Yes", "No"}) == "No")) break;
                    }
                }

                loopSelected.destinationFile.Refresh();

                if (loopSelected.destinationFile.Exists)
                {
                    await ThreadSwitcher.ResumeForegroundAsync();

                    var editor = new ImageContentEditorWindow(loopSelected.destinationFile);
                    editor.Show();
                    editor.ImageEditor.TitleSummarySlugFolder.Title = $"{loopSelected.content.Title} Cover Page";
                    editor.ImageEditor.TitleSummarySlugFolder.TitleToSlug();
                    editor.ImageEditor.TitleSummarySlugFolder.Summary =
                        $"Cover Page from {loopSelected.content.Title}.";
                    editor.ImageEditor.TitleSummarySlugFolder.Folder = loopSelected.content.Folder;
                    editor.ImageEditor.TagEdit.Tags = loopSelected.content.Tags;
                    editor.ImageEditor.ImageSourceNotes =
                        $"Generated by pdftocairo from {loopSelected.destinationFile.Name}.";
                    await ThreadSwitcher.ResumeBackgroundAsync();
                }
            }
        }

        private async Task GenerateSelectedHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListContext.SelectedItems == null || !ListContext.SelectedItems.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var loopCount = 1;
            var totalCount = ListContext.SelectedItems.Count;

            foreach (var loopSelected in ListContext.SelectedItems)
            {
                StatusContext.Progress(
                    $"Generating Html for {loopSelected.DbEntry.Title}, {loopCount} of {totalCount}");

                var htmlContext = new SingleFilePage(loopSelected.DbEntry);

                htmlContext.WriteLocalHtml();

                StatusContext.ToastSuccess($"Generated {htmlContext.PageUrl}");

                loopCount++;
            }
        }

        private async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            ListContext = new FileListContext(StatusContext);

            GenerateSelectedHtmlCommand = new Command(() => StatusContext.RunBlockingTask(GenerateSelectedHtml));
            EditSelectedContentCommand = new Command(() => StatusContext.RunBlockingTask(EditSelectedContent));
            FilePageLinkCodesToClipboardForSelectedCommand = new Command(() =>
                StatusContext.RunBlockingTask(FilePageLinkCodesToClipboardForSelected));
            FileDownloadLinkCodesToClipboardForSelectedCommand = new Command(() =>
                StatusContext.RunBlockingTask(FileDownloadLinkCodesToClipboardForSelected));
            OpenUrlForSelectedCommand = new Command(() => StatusContext.RunNonBlockingTask(OpenUrlForSelected));
            NewContentCommand = new Command(() => StatusContext.RunNonBlockingTask(NewContent));
            RefreshDataCommand = new Command(() => StatusContext.RunBlockingTask(ListContext.LoadData));
            DeleteSelectedCommand = new Command(() => StatusContext.RunBlockingTask(Delete));
            FirstPagePreviewFromPdfToCairoCommand =
                new Command(() => StatusContext.RunBlockingTask(FirstPagePreviewFromPdfToCairo));
            ExtractNewLinksInSelectedCommand =
                new Command(() => StatusContext.RunBlockingTask(ExtractNewLinksInSelected));
        }

        private async Task NewContent()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new FileContentEditorWindow(null);

            newContentWindow.Show();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task OpenUrlForSelected()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListContext.SelectedItems == null || !ListContext.SelectedItems.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var settings = UserSettingsSingleton.CurrentSettings();

            foreach (var loopSelected in ListContext.SelectedItems)
            {
                var url = $@"http://{settings.FilePageUrl(loopSelected.DbEntry)}";

                var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
                Process.Start(ps);
            }
        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            //* Do your stuff with the output (write to console/log/StringBuilder)
            _pdfPreviewGenerationProgress.Add((sendingProcess, outLine.Data));
        }
    }
}