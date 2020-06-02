using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using MvvmHelpers.Commands;
using Omu.ValueInjecter;
using Ookii.Dialogs.Wpf;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.PhotoHtml;
using PointlessWaymarksCmsWpfControls.ContentHistoryView;
using PointlessWaymarksCmsWpfControls.PhotoContentEditor;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.PhotoList
{
    public class PhotoListWithActionsContext : INotifyPropertyChanged
    {
        private Command _deleteSelectedCommand;
        private Command _editSelectedContentCommand;
        private Command _generatePhotoListCommand;
        private Command _generateSelectedHtmlCommand;
        private PhotoListContext _listContext;
        private Command _newContentCommand;
        private Command _openUrlForPhotoListCommand;
        private Command _openUrlForSelectedCommand;
        private Command _photoCodesToClipboardForSelectedCommand;
        private Command _refreshDataCommand;
        private StatusControlContext _statusContext;

        public PhotoListWithActionsContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            GenerateSelectedHtmlCommand = new Command(() => StatusContext.RunBlockingTask(GenerateSelectedHtml));
            EditSelectedContentCommand = new Command(() => StatusContext.RunBlockingTask(EditSelectedContent));
            PhotoCodesToClipboardForSelectedCommand =
                new Command(() => StatusContext.RunBlockingTask(PhotoCodesToClipboardForSelected));
            OpenUrlForSelectedCommand = new Command(() => StatusContext.RunNonBlockingTask(OpenUrlForSelected));
            OpenUrlForPhotoListCommand = new Command(() => StatusContext.RunNonBlockingTask(OpenUrlForPhotoList));
            NewContentCommand = new Command(() => StatusContext.RunNonBlockingTask(NewContent));
            NewContentFromFilesCommand = new Command(() =>
                StatusContext.RunBlockingTask(async () => await NewContentFromFiles(false)));
            NewContentFromFilesWithAutosaveCommand = new Command(() =>
                StatusContext.RunBlockingTask(async () => await NewContentFromFiles(true)));
            ViewHistoryCommand = new Command(() => StatusContext.RunNonBlockingTask(ViewHistory));
            RefreshDataCommand = new Command(() => StatusContext.RunBlockingTask(ListContext.LoadData));

            DeleteSelectedCommand = new Command(() => StatusContext.RunBlockingTask(Delete));
            ExtractNewLinksInSelectedCommand =
                new Command(() => StatusContext.RunBlockingTask(ExtractNewLinksInSelected));

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

        public Command GeneratePhotoListCommand
        {
            get => _generatePhotoListCommand;
            set
            {
                if (Equals(value, _generatePhotoListCommand)) return;
                _generatePhotoListCommand = value;
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

        public PhotoListContext ListContext
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

        public Command NewContentFromFilesCommand { get; set; }

        public Command NewContentFromFilesWithAutosaveCommand { get; set; }

        public Command OpenUrlForPhotoListCommand
        {
            get => _openUrlForPhotoListCommand;
            set
            {
                if (Equals(value, _openUrlForPhotoListCommand)) return;
                _openUrlForPhotoListCommand = value;
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

        public Command PhotoCodesToClipboardForSelectedCommand
        {
            get => _photoCodesToClipboardForSelectedCommand;
            set
            {
                if (Equals(value, _photoCodesToClipboardForSelectedCommand)) return;
                _photoCodesToClipboardForSelectedCommand = value;
                OnPropertyChanged();
            }
        }

        public Command RefreshDataCommand
        {
            get => _refreshDataCommand;
            set
            {
                if (Equals(value, _refreshDataCommand)) return;
                _refreshDataCommand = value;
                OnPropertyChanged();
            }
        }

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

        public Command ViewHistoryCommand { get; set; }

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
                if (await StatusContext.ShowMessage("Delete Multiple Items",
                    $"You are about to delete {selected.Count} items - do you really want to delete all of these photos?",
                    new List<string> {"Yes", "No"}) == "No")
                    return;

            var selectedItems = selected.ToList();
            var settings = UserSettingsSingleton.CurrentSettings();

            foreach (var loopSelected in selectedItems)
            {
                if (loopSelected.DbEntry == null || loopSelected.DbEntry.Id < 1)
                {
                    StatusContext.ToastError("Entry is not saved - Skipping?");
                    return;
                }


                var possibleContentDirectory = settings.LocalSitePhotoContentDirectory(loopSelected.DbEntry, false);
                if (possibleContentDirectory.Exists)
                {
                    StatusContext.Progress($"Deleting Generated Folder {possibleContentDirectory.FullName}");
                    possibleContentDirectory.Delete(true);
                }

                var context = await Db.Context();

                var toHistoric = await context.PhotoContents.Where(x => x.ContentId == loopSelected.DbEntry.ContentId)
                    .ToListAsync();

                StatusContext.Progress($"Writing {loopSelected.DbEntry.Title} Last Historic Entry");

                foreach (var loopToHistoric in toHistoric)
                {
                    var newHistoric = new HistoricPhotoContent();
                    newHistoric.InjectFrom(loopToHistoric);
                    newHistoric.Id = 0;
                    await context.HistoricPhotoContents.AddAsync(newHistoric);
                    context.PhotoContents.Remove(loopToHistoric);
                }

                StatusContext.Progress($"Submitting Db Delete for {loopSelected.DbEntry.Title}");

                await context.SaveChangesAsync(true);
            }

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
                    context.PhotoContents.SingleOrDefault(x => x.ContentId == loopSelected.DbEntry.ContentId);

                if (refreshedData == null)
                {
                    StatusContext.ToastError(
                        $"{loopSelected.DbEntry.Title} is no longer active in the database? Can not edit - " +
                        "look for a historic version...");
                    continue;
                }

                await ThreadSwitcher.ResumeForegroundAsync();

                var newContentWindow = new PhotoContentEditorWindow(refreshedData);

                newContentWindow.Show();

                await ThreadSwitcher.ResumeBackgroundAsync();
            }
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
                    context.PhotoContents.SingleOrDefault(x => x.ContentId == loopSelected.DbEntry.ContentId);

                if (refreshedData == null) continue;

                await LinkExtraction.ExtractNewAndShowLinkStreamEditors($"{refreshedData.UpdateNotes}",
                    StatusContext.ProgressTracker());
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

                var htmlContext = new SinglePhotoPage(loopSelected.DbEntry);

                htmlContext.WriteLocalHtml();

                StatusContext.ToastSuccess($"Generated {htmlContext.PageUrl}");

                loopCount++;
            }
        }

        private async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            ListContext = new PhotoListContext(StatusContext);
        }

        private async Task NewContent()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new PhotoContentEditorWindow();

            newContentWindow.Show();
        }

        private async Task NewContentFromFiles(bool autoSaveAndClose)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            StatusContext.Progress("Starting photo load.");

            var dialog = new VistaOpenFileDialog {Multiselect = true};

            if (!(dialog.ShowDialog() ?? false)) return;

            var selectedFiles = dialog.FileNames?.ToList() ?? new List<string>();

            if (!selectedFiles.Any()) return;

            await ThreadSwitcher.ResumeBackgroundAsync();

            const int maxLimit = 100;

            if (selectedFiles.Count > maxLimit)
            {
                StatusContext.ToastError(
                    $"Sorry - max limit is {maxLimit} files at once, {selectedFiles.Count} selected...");
                return;
            }

            var selectedFileInfos = selectedFiles.Select(x => new FileInfo(x)).ToList();

            if (!selectedFileInfos.Any(x => x.Exists))
            {
                StatusContext.ToastError("Files don't exist?");
                return;
            }

            selectedFileInfos = selectedFileInfos.Where(x => x.Exists).ToList();

            if (!selectedFileInfos.Any(FileTypeHelpers.PhotoFileTypeIsSupported))
            {
                StatusContext.ToastError("None of the files appear to be supported file types...");
                return;
            }

            if (selectedFileInfos.Any(x => !FileTypeHelpers.PhotoFileTypeIsSupported(x)))
                StatusContext.ToastWarning(
                    $"Skipping - not supported - {string.Join(", ", selectedFileInfos.Where(x => !FileTypeHelpers.PhotoFileTypeIsSupported(x)))}");

            var validFiles = selectedFileInfos.Where(FileTypeHelpers.PhotoFileTypeIsSupported).ToList();

            foreach (var loopFile in validFiles)
            {
                await ThreadSwitcher.ResumeForegroundAsync();

                if (autoSaveAndClose)
                {
                    var editor = new PhotoContentEditorWindow(true);

                    if (validFiles.Count > 5)
                    {
                        await TryAutomateEditorSaveGenerateAndClose(editor, loopFile);
                    }
                    else
                        StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () =>
                            await TryAutomateEditorSaveGenerateAndClose(editor, loopFile));
                }
                else
                {
                    var editor = new PhotoContentEditorWindow(loopFile);
                    editor.Show();
                }

                StatusContext.Progress($"New Photo Editor - {loopFile.FullName} ");

                await ThreadSwitcher.ResumeBackgroundAsync();
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task OpenUrlForPhotoList()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var url = $@"http://{UserSettingsSingleton.CurrentSettings().PhotoListUrl()}";

            var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
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
                var url = $@"https://{settings.PhotoPageUrl(loopSelected.DbEntry)}";

                var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
                Process.Start(ps);
            }
        }

        private async Task PhotoCodesToClipboardForSelected()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListContext.SelectedItems == null || !ListContext.SelectedItems.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var finalString = string.Empty;

            foreach (var loopSelected in ListContext.SelectedItems)
                finalString += BracketCodePhotos.PhotoBracketCode(loopSelected.DbEntry) + Environment.NewLine;

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);

            StatusContext.ToastSuccess($"To Clipboard {finalString}");
        }

        private async Task TryAutomateEditorSaveGenerateAndClose(PhotoContentEditorWindow editor, FileInfo loopFile)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            editor.StatusContext.BlockUi = true;

            await ThreadSwitcher.ResumeForegroundAsync();

            editor.Show();

            await ThreadSwitcher.ResumeBackgroundAsync();

            await editor.PhotoEditor.LoadData(null);
            editor.PhotoEditor.SelectedFile = loopFile;
            await editor.PhotoEditor.ProcessSelectedFile();

            var validation = await editor.PhotoEditor.ValidateAll();

            if (validation.Any(x => !x.Item1))
            {
                //Todo: Get validation errors to user
                editor.StatusContext.BlockUi = false;
                return;
            }

            try
            {
                await editor.PhotoEditor.SaveAndGenerateHtml();
            }
            catch (Exception e)
            {
                await editor.StatusContext.ShowMessageWithOkButton("Trouble with Autosave", e.Message);
                return;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            editor.Close();
        }

        private async Task ViewHistory()
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
                StatusContext.ToastError("Please Select a Single Item");
                return;
            }

            var singleSelected = selected.Single();

            if (singleSelected.DbEntry == null || singleSelected.DbEntry.ContentId == Guid.Empty)
            {
                StatusContext.ToastWarning("No History - New/Unsaved Entry?");
                return;
            }

            var db = await Db.Context();

            StatusContext.Progress($"Looking up Historic Entries for {singleSelected.DbEntry.Title}");

            var historicItems = await db.HistoricPhotoContents
                .Where(x => x.ContentId == singleSelected.DbEntry.ContentId).ToListAsync();

            StatusContext.Progress($"Found {historicItems.Count} Historic Entries");

            if (historicItems.Count < 1)
            {
                StatusContext.ToastWarning("No History to Show...");
                return;
            }

            var historicView = new ContentViewHistoryPage($"Historic Entries - {singleSelected.DbEntry.Title}",
                UserSettingsSingleton.CurrentSettings().SiteName, $"Historic Entries - {singleSelected.DbEntry.Title}",
                historicItems.OrderByDescending(x => x.LastUpdatedOn.HasValue).ThenByDescending(x => x.LastUpdatedOn)
                    .Select(ObjectDumper.Dump).ToList());

            historicView.WriteHtmlToTempFolderAndShow(StatusContext.ProgressTracker());
        }
    }
}