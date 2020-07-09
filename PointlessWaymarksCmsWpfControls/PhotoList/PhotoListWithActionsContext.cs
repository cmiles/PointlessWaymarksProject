using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using AngleSharp.Text;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using MvvmHelpers.Commands;
using Omu.ValueInjecter;
using Ookii.Dialogs.Wpf;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html.CommonHtml;
using PointlessWaymarksCmsWpfControls.ContentHistoryView;
using PointlessWaymarksCmsWpfControls.PhotoContentEditor;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;
using SinglePhotoPage = PointlessWaymarksCmsData.Html.PhotoHtml.SinglePhotoPage;

namespace PointlessWaymarksCmsWpfControls.PhotoList
{
    public class PhotoListWithActionsContext : INotifyPropertyChanged
    {
        private Command _deleteSelectedCommand;
        private Command _editSelectedContentCommand;
        private Command _forcedResizeCommand;
        private Command _generatePhotoListCommand;
        private Command _generateSelectedHtmlCommand;
        private PhotoListContext _listContext;
        private Command _newContentCommand;
        private Command _openUrlForPhotoListCommand;
        private Command _openUrlForSelectedCommand;
        private Command _photoCodesToClipboardForSelectedCommand;
        private Command _photoLinkCodesToClipboardForSelectedCommand;
        private Command _refreshDataCommand;
        private Command _reportAllPhotosCommand;
        private Command _reportBlankLicenseCommand;
        private Command _reportNoTagsCommand;
        private Command _reportTakenAndLicenseYearDoNotMatchCommand;
        private Command _reportTitleAndTakenDoNotMatchCommand;
        private Command _selectedToExcelCommand;
        private StatusControlContext _statusContext;

        public PhotoListWithActionsContext(StatusControlContext statusContext)
        {
            SetupContextAndCommands(statusContext);

            ListContext = new PhotoListContext(StatusContext, PhotoListContext.PhotoListLoadMode.Recent);

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(ListContext.LoadData);
        }

        public PhotoListWithActionsContext(StatusControlContext statusContext,
            Func<Task<List<PhotoContent>>> reportFilter)
        {
            SetupContextAndCommands(statusContext);

            ListContext =
                new PhotoListContext(StatusContext, PhotoListContext.PhotoListLoadMode.ReportQuery)
                {
                    ReportGenerator = reportFilter
                };

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(ListContext.LoadData);
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

        public Command ForcedResizeCommand
        {
            get => _forcedResizeCommand;
            set
            {
                if (Equals(value, _forcedResizeCommand)) return;
                _forcedResizeCommand = value;
                OnPropertyChanged();
            }
        }

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

        public Command PhotoLinkCodesToClipboardForSelectedCommand
        {
            get => _photoLinkCodesToClipboardForSelectedCommand;
            set
            {
                if (Equals(value, _photoLinkCodesToClipboardForSelectedCommand)) return;
                _photoLinkCodesToClipboardForSelectedCommand = value;
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

        public Command ReportAllPhotosCommand
        {
            get => _reportAllPhotosCommand;
            set
            {
                if (Equals(value, _reportAllPhotosCommand)) return;
                _reportAllPhotosCommand = value;
                OnPropertyChanged();
            }
        }

        public Command ReportBlankLicenseCommand
        {
            get => _reportBlankLicenseCommand;
            set
            {
                if (Equals(value, _reportBlankLicenseCommand)) return;
                _reportBlankLicenseCommand = value;
                OnPropertyChanged();
            }
        }

        public Command ReportNoTagsCommand
        {
            get => _reportNoTagsCommand;
            set
            {
                if (Equals(value, _reportNoTagsCommand)) return;
                _reportNoTagsCommand = value;
                OnPropertyChanged();
            }
        }

        public Command ReportTakenAndLicenseYearDoNotMatchCommand
        {
            get => _reportTakenAndLicenseYearDoNotMatchCommand;
            set
            {
                if (Equals(value, _reportTakenAndLicenseYearDoNotMatchCommand)) return;
                _reportTakenAndLicenseYearDoNotMatchCommand = value;
                OnPropertyChanged();
            }
        }

        public Command ReportTitleAndTakenDoNotMatchCommand
        {
            get => _reportTitleAndTakenDoNotMatchCommand;
            set
            {
                if (Equals(value, _reportTitleAndTakenDoNotMatchCommand)) return;
                _reportTitleAndTakenDoNotMatchCommand = value;
                OnPropertyChanged();
            }
        }

        public Command SelectedToExcelCommand
        {
            get => _selectedToExcelCommand;
            set
            {
                if (Equals(value, _selectedToExcelCommand)) return;
                _selectedToExcelCommand = value;
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
                    newHistoric.LastUpdatedOn = DateTime.Now;
                    await context.HistoricPhotoContents.AddAsync(newHistoric);
                    context.PhotoContents.Remove(loopToHistoric);
                }

                StatusContext.Progress($"Submitting Db Delete for {loopSelected.DbEntry.Title}");

                await context.SaveChangesAsync(true);

                await ThreadSwitcher.ResumeForegroundAsync();

                //Try to update the UI - use try catch to cover all the 'no longer present' scenarios.
                try
                {
                    ListContext.Items.Remove(loopSelected);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                await ThreadSwitcher.ResumeBackgroundAsync();
            }
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

        private async Task ForcedResize()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListContext.SelectedItems == null || !ListContext.SelectedItems.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var totalCount = ListContext.SelectedItems.Count;
            var currentLoop = 1;

            foreach (var loopSelected in ListContext.SelectedItems)
            {
                if (currentLoop % 10 == 0)
                    StatusContext.Progress($"Cleaning Generated Images And Resizing {currentLoop} of {totalCount} - " +
                                           $"{loopSelected.DbEntry.Title}");
                PictureResizing.CopyCleanResizePhoto(loopSelected.DbEntry, StatusContext.ProgressTracker());
                currentLoop++;
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
                await ThreadSwitcher.ResumeBackgroundAsync();

                if (autoSaveAndClose)
                {
                    var (metaGenerationReturn, metaContent) = await PhotoGenerator.PhotoMetadataToNewPhotoContent(
                        loopFile, UserSettingsSingleton.CurrentSettings().DefaultCreatedBy,
                        StatusContext.ProgressTracker());

                    if (metaGenerationReturn.HasError)
                    {
                        await ThreadSwitcher.ResumeForegroundAsync();

                        var editor = new PhotoContentEditorWindow(loopFile);
                        editor.Show();
#pragma warning disable 4014
                        //Allow execution to continue so Automation can continue
                        editor.StatusContext.ShowMessageWithOkButton("Problem Extracting Metadata",
                            metaGenerationReturn.GenerationNote);
#pragma warning restore 4014
                        continue;
                    }

                    var (saveGenerationReturn, saveContent) =
                        await PhotoGenerator.SaveAndGenerateHtml(metaContent, loopFile, true,
                            StatusContext.ProgressTracker());

                    if (saveGenerationReturn.HasError)
                    {
                        await ThreadSwitcher.ResumeForegroundAsync();

                        var editor = new PhotoContentEditorWindow(loopFile);
                        editor.Show();
#pragma warning disable 4014
                        //Allow execution to continue so Automation can continue
                        editor.StatusContext.ShowMessageWithOkButton("Problem Saving",
                            saveGenerationReturn.GenerationNote);
#pragma warning restore 4014
                        continue;
                    }
                }
                else
                {
                    await ThreadSwitcher.ResumeForegroundAsync();

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

            var finalString = ListContext.SelectedItems.Aggregate(string.Empty,
                (current, loopSelected) =>
                    current + (BracketCodePhotos.PhotoBracketCode(loopSelected.DbEntry) + Environment.NewLine));

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);

            StatusContext.ToastSuccess($"To Clipboard {finalString}");
        }

        private async Task PhotoLinkCodesToClipboardForSelected()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListContext.SelectedItems == null || !ListContext.SelectedItems.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var finalString = ListContext.SelectedItems.Aggregate(string.Empty,
                (current, loopSelected) =>
                    current + (BracketCodePhotos.PhotoBracketCode(loopSelected.DbEntry) + Environment.NewLine));

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);

            StatusContext.ToastSuccess($"To Clipboard {finalString}");
        }

        private async Task<List<PhotoContent>> ReportAllPhotosGenerator()
        {
            var db = await Db.Context();

            return await db.PhotoContents.OrderByDescending(x => x.PhotoCreatedOn).ToListAsync();
        }

        private async Task<List<PhotoContent>> ReportBlackLicenseGenerator()
        {
            var db = await Db.Context();

            var allContents = await db.PhotoContents.OrderByDescending(x => x.PhotoCreatedOn).ToListAsync();

            var returnList = new List<PhotoContent>();

            foreach (var loopContents in allContents)
                if (string.IsNullOrWhiteSpace(loopContents.License))
                    returnList.Add(loopContents);

            return returnList;
        }

        private async Task<List<PhotoContent>> ReportNoTagsGenerator()
        {
            var db = await Db.Context();

            return await db.PhotoContents.Where(x => x.Tags == "").ToListAsync();
        }

        private async Task<List<PhotoContent>> ReportTakenAndLicenseYearDoNotMatchGenerator()
        {
            var db = await Db.Context();

            var allContents = await db.PhotoContents.OrderByDescending(x => x.PhotoCreatedOn).ToListAsync();

            var returnList = new List<PhotoContent>();

            foreach (var loopContents in allContents)
            {
                if (string.IsNullOrWhiteSpace(loopContents.License))
                {
                    returnList.Add(loopContents);
                    continue;
                }

                var possibleYear = Regex.Match(loopContents.License, @"(?<PossibleYear>[12]\d\d\d)",
                    RegexOptions.IgnoreCase).Value;

                if (string.IsNullOrWhiteSpace(possibleYear)) continue;

                if (!int.TryParse(possibleYear, out var licenseYear)) continue;

                var createdOn = loopContents.PhotoCreatedOn.Year;

                if (createdOn == licenseYear) continue;

                returnList.Add(loopContents);
            }

            return returnList;
        }

        private async Task<List<PhotoContent>> ReportTitleAndTakenDoNotMatchGenerator()
        {
            var db = await Db.Context();

            var allContents = await db.PhotoContents.OrderByDescending(x => x.PhotoCreatedOn).ToListAsync();

            var returnList = new List<PhotoContent>();

            foreach (var loopContents in allContents)
            {
                if (string.IsNullOrWhiteSpace(loopContents.Title)) continue;

                var splitName = loopContents.Title.Split(" ");

                if (splitName.Length < 2) continue;

                if (!splitName[0].All(x => x.IsDigit())) continue;

                if (!int.TryParse(splitName[0], out var titleYear)) continue;

                var dateInfo = new DateTimeFormatInfo();

                if (!dateInfo.MonthNames.Contains(splitName[1])) continue;

                var titleMonth = dateInfo.MonthNames.ToList().IndexOf(splitName[1]) + 1;

                if (titleYear == loopContents.PhotoCreatedOn.Year &&
                    titleMonth == loopContents.PhotoCreatedOn.Month) continue;

                returnList.Add(loopContents);
            }

            return returnList;
        }

        private async Task RunReport(Func<Task<List<PhotoContent>>> toRun, string title)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var context = new PhotoListWithActionsContext(null, toRun);

            await ThreadSwitcher.ResumeForegroundAsync();

            var newWindow = new PhotoListWindow {PhotoListContext = context, WindowTitle = title};

            newWindow.Show();
        }

        public async Task SelectedToExcel()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListContext.SelectedItems == null || !ListContext.SelectedItems.Any())
            {
                StatusContext.ToastError("Nothing to send to Excel?");
                return;
            }

            ExcelHelpers.ContentToExcelFileAsTable(
                ListContext.SelectedItems.Select(x => x.DbEntry).Cast<object>().ToList(), "SelectedPhotos");
        }

        private void SetupContextAndCommands(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            GenerateSelectedHtmlCommand = new Command(() => StatusContext.RunBlockingTask(GenerateSelectedHtml));
            EditSelectedContentCommand = new Command(() => StatusContext.RunBlockingTask(EditSelectedContent));
            PhotoCodesToClipboardForSelectedCommand =
                new Command(() => StatusContext.RunBlockingTask(PhotoCodesToClipboardForSelected));
            PhotoLinkCodesToClipboardForSelectedCommand = new Command(() =>
                StatusContext.RunBlockingTask(PhotoLinkCodesToClipboardForSelected));
            OpenUrlForSelectedCommand = new Command(() => StatusContext.RunNonBlockingTask(OpenUrlForSelected));
            OpenUrlForPhotoListCommand = new Command(() => StatusContext.RunNonBlockingTask(OpenUrlForPhotoList));
            NewContentCommand = new Command(() => StatusContext.RunNonBlockingTask(NewContent));
            NewContentFromFilesCommand = new Command(() =>
                StatusContext.RunBlockingTask(async () => await NewContentFromFiles(false)));
            NewContentFromFilesWithAutosaveCommand = new Command(() =>
                StatusContext.RunBlockingTask(async () => await NewContentFromFiles(true)));
            ViewHistoryCommand = new Command(() => StatusContext.RunNonBlockingTask(ViewHistory));
            RefreshDataCommand = new Command(() => StatusContext.RunBlockingTask(ListContext.LoadData));
            ForcedResizeCommand = new Command(() => StatusContext.RunBlockingTask(ForcedResize));
            SelectedToExcelCommand = new Command(() => StatusContext.RunNonBlockingTask(SelectedToExcel));

            DeleteSelectedCommand = new Command(() => StatusContext.RunBlockingTask(Delete));
            ExtractNewLinksInSelectedCommand =
                new Command(() => StatusContext.RunBlockingTask(ExtractNewLinksInSelected));

            ReportNoTagsCommand = new Command(() =>
                StatusContext.RunNonBlockingTask(async () =>
                    await RunReport(ReportNoTagsGenerator, "No Tags Photo List")));
            ReportTitleAndTakenDoNotMatchCommand = new Command(() => StatusContext.RunNonBlockingTask(async () =>
                await RunReport(ReportTitleAndTakenDoNotMatchGenerator, "Title and Created Mismatch Photo List")));
            ReportTakenAndLicenseYearDoNotMatchCommand = new Command(() => StatusContext.RunNonBlockingTask(async () =>
                await RunReport(ReportTakenAndLicenseYearDoNotMatchGenerator,
                    "Title and Created Mismatch Photo List")));
            ReportAllPhotosCommand = new Command(() => StatusContext.RunNonBlockingTask(async () =>
                await RunReport(ReportAllPhotosGenerator, "Title and Created Mismatch Photo List")));
            ReportBlankLicenseCommand = new Command(() => StatusContext.RunNonBlockingTask(async () =>
                await RunReport(ReportBlackLicenseGenerator, "Title and Created Mismatch Photo List")));
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