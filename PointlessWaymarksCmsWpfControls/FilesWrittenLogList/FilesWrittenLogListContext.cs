#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.FilesWrittenLogList
{
    public class FilesWrittenLogListContext : INotifyPropertyChanged
    {
        private bool _changeSlashes = true;
        private Command? _filesToExcelCommand;
        private bool _filterForFilesInCurrentGenerationDirectory = true;
        private Command? _generateItemsCommand;
        private ObservableCollection<FileWrittenLogListDateTimeFilterChoice>? _generationChoices;
        private ObservableCollection<FilesWrittenLogListListItem>? _items;
        private Command? _scriptStringsToClipboardCommand;
        private Command? _scriptStringsToPowerShellScriptCommand;
        private Command? _selectedFilesToExcelCommand;
        private FileWrittenLogListDateTimeFilterChoice? _selectedGenerationChoice;
        private List<FilesWrittenLogListListItem> _selectedItems = new List<FilesWrittenLogListListItem>();
        private Command? _selectedScriptStringsToClipboardCommand;
        private Command? _selectedScriptStringsToPowerShellScriptCommand;
        private Command? _selectedWrittenFilesToClipboardCommand;
        private StatusControlContext? _statusContext;
        private string _userFilePrefix = string.Empty;
        private string _userScriptPrefix = "aws s3 cp";
        private Command? _writtenFilesToClipboardCommand;

        public FilesWrittenLogListContext(StatusControlContext? statusContext, bool loadInBackground)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            GenerateItemsCommand = StatusContext.RunBlockingTaskCommand(async () => await GenerateItems());
            ScriptStringsToClipboardCommand =
                StatusContext.RunBlockingTaskCommand(async () => await ScriptStringsToClipboard());
            SelectedScriptStringsToClipboardCommand =
                StatusContext.RunBlockingTaskCommand(async () => await SelectedScriptStringsToClipboard());
            WrittenFilesToClipboardCommand =
                StatusContext.RunBlockingTaskCommand(async () => await WrittenFilesToClipboard());
            SelectedWrittenFilesToClipboardCommand =
                StatusContext.RunBlockingTaskCommand(async () => await SelectedWrittenFilesToClipboard());
            SelectedScriptStringsToPowerShellScriptCommand =
                StatusContext.RunBlockingTaskCommand(async () => await SelectedScriptStringsToPowerShellScript());
            ScriptStringsToPowerShellScriptCommand =
                StatusContext.RunBlockingTaskCommand(async () => await ScriptStringsToPowerShellScript());
            SelectedFilesToExcelCommand =
                StatusContext.RunBlockingTaskCommand(async () => await SelectedFilesToExcel());
            FilesToExcelCommand = StatusContext.RunBlockingTaskCommand(async () => await FilesToExcel());

            if (loadInBackground) StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
        }

        public bool ChangeSlashes
        {
            get => _changeSlashes;
            set
            {
                if (value == _changeSlashes) return;
                _changeSlashes = value;
                OnPropertyChanged();
            }
        }

        public Command? FilesToExcelCommand
        {
            get => _filesToExcelCommand;
            set
            {
                if (Equals(value, _filesToExcelCommand)) return;
                _filesToExcelCommand = value;
                OnPropertyChanged();
            }
        }

        public bool FilterForFilesInCurrentGenerationDirectory
        {
            get => _filterForFilesInCurrentGenerationDirectory;
            set
            {
                if (value == _filterForFilesInCurrentGenerationDirectory) return;
                _filterForFilesInCurrentGenerationDirectory = value;
                OnPropertyChanged();
            }
        }

        public Command? GenerateItemsCommand
        {
            get => _generateItemsCommand;
            set
            {
                if (Equals(value, _generateItemsCommand)) return;
                _generateItemsCommand = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<FileWrittenLogListDateTimeFilterChoice>? GenerationChoices
        {
            get => _generationChoices;
            set
            {
                if (Equals(value, _generationChoices)) return;
                _generationChoices = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<FilesWrittenLogListListItem>? Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged();
            }
        }

        public Command? ScriptStringsToClipboardCommand
        {
            get => _scriptStringsToClipboardCommand;
            set
            {
                if (Equals(value, _scriptStringsToClipboardCommand)) return;
                _scriptStringsToClipboardCommand = value;
                OnPropertyChanged();
            }
        }

        public Command? ScriptStringsToPowerShellScriptCommand
        {
            get => _scriptStringsToPowerShellScriptCommand;
            set
            {
                if (Equals(value, _scriptStringsToPowerShellScriptCommand)) return;
                _scriptStringsToPowerShellScriptCommand = value;
                OnPropertyChanged();
            }
        }

        public Command? SelectedFilesToExcelCommand
        {
            get => _selectedFilesToExcelCommand;
            set
            {
                if (Equals(value, _selectedFilesToExcelCommand)) return;
                _selectedFilesToExcelCommand = value;
                OnPropertyChanged();
            }
        }

        public FileWrittenLogListDateTimeFilterChoice? SelectedGenerationChoice
        {
            get => _selectedGenerationChoice;
            set
            {
                if (value == _selectedGenerationChoice) return;
                _selectedGenerationChoice = value;
                OnPropertyChanged();
            }
        }

        public List<FilesWrittenLogListListItem> SelectedItems
        {
            get => _selectedItems;
            set
            {
                if (Equals(value, _selectedItems)) return;
                _selectedItems = value;
                OnPropertyChanged();
            }
        }

        public Command? SelectedScriptStringsToClipboardCommand
        {
            get => _selectedScriptStringsToClipboardCommand;
            set
            {
                if (Equals(value, _selectedScriptStringsToClipboardCommand)) return;
                _selectedScriptStringsToClipboardCommand = value;
                OnPropertyChanged();
            }
        }

        public Command? SelectedScriptStringsToPowerShellScriptCommand
        {
            get => _selectedScriptStringsToPowerShellScriptCommand;
            set
            {
                if (Equals(value, _selectedScriptStringsToPowerShellScriptCommand)) return;
                _selectedScriptStringsToPowerShellScriptCommand = value;
                OnPropertyChanged();
            }
        }

        public Command? SelectedWrittenFilesToClipboardCommand
        {
            get => _selectedWrittenFilesToClipboardCommand;
            set
            {
                if (Equals(value, _selectedWrittenFilesToClipboardCommand)) return;
                _selectedWrittenFilesToClipboardCommand = value;
                OnPropertyChanged();
            }
        }

        public StatusControlContext? StatusContext
        {
            get => _statusContext;
            set
            {
                if (Equals(value, _statusContext)) return;
                _statusContext = value;
                OnPropertyChanged();
            }
        }

        public string UserFilePrefix
        {
            get => _userFilePrefix;
            set
            {
                if (value == _userFilePrefix) return;
                _userFilePrefix = value;
                OnPropertyChanged();
            }
        }

        public string UserScriptPrefix
        {
            get => _userScriptPrefix;
            set
            {
                if (value == _userScriptPrefix) return;
                _userScriptPrefix = value;
                OnPropertyChanged();
            }
        }

        public Command? WrittenFilesToClipboardCommand
        {
            get => _writtenFilesToClipboardCommand;
            set
            {
                if (Equals(value, _writtenFilesToClipboardCommand)) return;
                _writtenFilesToClipboardCommand = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public static async Task<FilesWrittenLogListContext> CreateInstance(StatusControlContext? statusContext)
        {
            var newContext = new FilesWrittenLogListContext(statusContext, false);
            await newContext.LoadData();
            return newContext;
        }

        public async Task FileItemsToScriptFile(List<FilesWrittenLogListListItem> toWrite)
        {
            var scriptString = FileItemsToScriptString(toWrite);

            var file = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalSiteScriptsDirectory().FullName,
                $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---File-Upload-Script.ps1"));

            await File.WriteAllTextAsync(file.FullName, scriptString);

            var db = await Db.Context();

            await db.GenerationFileScriptLogs.AddAsync(new GenerationFileScriptLog
            {
                FileName = file.Name, WrittenOnVersion = DateTime.Now.TrimDateTimeToSeconds().ToUniversalTime()
            });

            await db.SaveChangesAsync();

            await LoadDateTimeFilterChoices();

            await ThreadSwitcher.ResumeForegroundAsync();

            var ps = new ProcessStartInfo("explorer.exe", $"/select, \"{file.FullName}\"")
            {
                UseShellExecute = true, Verb = "open"
            };
            Process.Start(ps);
        }

        public string FileItemsToScriptString(List<FilesWrittenLogListListItem> toConvert)
        {
            var sortedItems = toConvert.OrderByDescending(x => x.WrittenFile.Count(y => y == '\\'))
                .ThenBy(x => x.WrittenFile).ToList();

            sortedItems = sortedItems.Where(x => File.Exists(x.WrittenFile)).ToList();

            return string.Join(Environment.NewLine,
                sortedItems.Where(x => x.IsInGenerationDirectory).Distinct().ToList().Select(x =>
                    $"{UserScriptPrefix}{(string.IsNullOrWhiteSpace(UserScriptPrefix) ? "" : " ")}'{x.WrittenFile}' {x.TransformedFile};"));
        }

        private async Task FilesToClipboard(List<FilesWrittenLogListListItem> items)
        {
            var scriptString = string.Join(Environment.NewLine, items.Select(x => x.WrittenFile).Distinct().ToList());

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(scriptString);
        }

        private async Task FilesToExcel()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (Items == null || !Items.Any())
            {
                StatusContext?.ToastError("No Items?");
                return;
            }

            FilesToExcel(Items.ToList());
        }

        private void FilesToExcel(List<FilesWrittenLogListListItem> items)
        {
            ExcelHelpers.ContentToExcelFileAsTable(items.Cast<object>().ToList(), "WrittenFiles", limitRowHeight: false,
                progress: StatusContext?.ProgressTracker());
        }

        public async Task GenerateItems()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedGenerationChoice == null)
            {
                StatusContext?.ToastError("Please make a Generation Date Choice");
                return;
            }

            StatusContext?.Progress("Setting up db");

            var db = await Db.Context();

            var generationDirectory = new DirectoryInfo(UserSettingsSingleton.CurrentSettings().LocalSiteRootDirectory)
                .FullName;

            StatusContext?.Progress(
                $"Filtering for Generation Directory: {FilterForFilesInCurrentGenerationDirectory}");

            IQueryable<GenerationFileWriteLog> searchQuery = FilterForFilesInCurrentGenerationDirectory
                ? db.GenerationFileWriteLogs.Where(x => x.FileName.StartsWith(generationDirectory))
                : db.GenerationFileWriteLogs;

            if (SelectedGenerationChoice.FilterDateTimeUtc != null)
            {
                StatusContext?.Progress($"Filtering by Date and Time {SelectedGenerationChoice.FilterDateTimeUtc:F}");
                searchQuery = searchQuery.Where(x => x.WrittenOnVersion >= SelectedGenerationChoice.FilterDateTimeUtc);
            }

            var dbItems = await searchQuery.OrderByDescending(x => x.WrittenOnVersion).ToListAsync();

            var transformedItems = new List<FilesWrittenLogListListItem>();

            StatusContext?.Progress($"Processing {dbItems.Count} items for display");
            foreach (var loopDbItems in dbItems)
            {
                var directory = new DirectoryInfo(UserSettingsSingleton.CurrentSettings().LocalSiteRootDirectory);
                var fileBase = loopDbItems.FileName.Replace(directory.FullName, string.Empty);
                var isInGenerationDirectory = loopDbItems.FileName.StartsWith(generationDirectory);
                var transformedFileName = ToTransformedFileString(fileBase);

                transformedItems.Add(new FilesWrittenLogListListItem
                {
                    FileBase = fileBase,
                    TransformedFile = transformedFileName,
                    WrittenOn = loopDbItems.WrittenOnVersion.ToLocalTime(),
                    WrittenFile = loopDbItems.FileName,
                    IsInGenerationDirectory = isInGenerationDirectory
                });
            }

            StatusContext?.Progress("Transferring items to the UI");

            await ThreadSwitcher.ResumeForegroundAsync();

            if (Items == null)
            {
                Items = new ObservableCollection<FilesWrittenLogListListItem>(transformedItems);
            }
            else
            {
                Items.Clear();
                transformedItems.ForEach(x => Items.Add(x));
            }
        }

        private async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            UserFilePrefix = UserSettingsSingleton.CurrentSettings().RemoteFileRoot;

            await LoadDateTimeFilterChoices();
        }

        private async Task LoadDateTimeFilterChoices()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            StatusContext?.Progress("Setting up db");

            var db = await Db.Context();

            var logChoiceList = new List<FileWrittenLogListDateTimeFilterChoice>();

            StatusContext?.Progress("Adding Generation Dates");

            logChoiceList.AddRange(
                (await db.GenerationLogs.OrderByDescending(x => x.GenerationVersion).Take(15).ToListAsync()).Select(x =>
                    new FileWrittenLogListDateTimeFilterChoice
                    {
                        DisplayText = $"{x.GenerationVersion.ToLocalTime():F} - Html Generated",
                        FilterDateTimeUtc = x.GenerationVersion
                    }));

            StatusContext?.Progress($"Using {logChoiceList.Count} Generation Dates - Adding Script Dates");

            logChoiceList.AddRange(
                (await db.GenerationFileScriptLogs.OrderByDescending(x => x.WrittenOnVersion).Take(30).ToListAsync())
                .Select(x => new FileWrittenLogListDateTimeFilterChoice
                {
                    DisplayText = $"{x.WrittenOnVersion.ToLocalTime():F} - Script Generated",
                    FilterDateTimeUtc = x.WrittenOnVersion
                }));

            StatusContext?.Progress("Finished adding Filter Date Times - setting up choices");


            logChoiceList = logChoiceList.OrderByDescending(x => x.FilterDateTimeUtc).ToList();

            logChoiceList.Add(new FileWrittenLogListDateTimeFilterChoice {DisplayText = "All"});


            FileWrittenLogListDateTimeFilterChoice toSelect;

            if (SelectedGenerationChoice == null)
            {
                toSelect = logChoiceList[0];
            }
            else
            {
                var possibleCurrentObject = logChoiceList.FirstOrDefault(x =>
                    x.FilterDateTimeUtc == SelectedGenerationChoice.FilterDateTimeUtc &&
                    x.DisplayText == SelectedGenerationChoice.DisplayText);

                toSelect = possibleCurrentObject ?? logChoiceList[0];
            }

            StatusContext?.Progress("Transferring choices to the UI");

            await ThreadSwitcher.ResumeForegroundAsync();

            GenerationChoices ??= new ObservableCollection<FileWrittenLogListDateTimeFilterChoice>();
            GenerationChoices.Clear();
            logChoiceList.ForEach(x => GenerationChoices.Add(x));

            SelectedGenerationChoice = toSelect;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (propertyName == nameof(UserFilePrefix) || propertyName == nameof(UserScriptPrefix) ||
                propertyName == nameof(ChangeSlashes))
                StatusContext?.RunBlockingAction(() =>
                {
                    var currentItems = Items?.ToList();
                    currentItems?.Where(x => x.IsInGenerationDirectory).ToList().ForEach(x =>
                        x.TransformedFile = ToTransformedFileString(x.FileBase));
                });
        }

        public async Task ScriptStringsToClipboard()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (Items == null || !Items.Any())
            {
                StatusContext?.ToastError("No Items?");
                return;
            }

            var scriptString = FileItemsToScriptString(Items.ToList());

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(scriptString);
        }

        public async Task ScriptStringsToPowerShellScript()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (Items == null || !Items.Any())
            {
                StatusContext?.ToastError("No Items Selected?");
                return;
            }

            await FileItemsToScriptFile(Items.ToList()).ConfigureAwait(true);
        }

        private async Task SelectedFilesToExcel()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (Items == null || !Items.Any())
            {
                StatusContext?.ToastError("No Items?");
                return;
            }

            FilesToExcel(SelectedItems);
        }

        public async Task SelectedScriptStringsToClipboard()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (!SelectedItems.Any())
            {
                StatusContext?.ToastError("No Items Selected?");
                return;
            }

            var scriptString = FileItemsToScriptString(SelectedItems);

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(scriptString);
        }

        public async Task SelectedScriptStringsToPowerShellScript()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (!SelectedItems.Any())
            {
                StatusContext?.ToastError("No Items Selected?");
                return;
            }

            await FileItemsToScriptFile(SelectedItems).ConfigureAwait(true);
        }

        public async Task SelectedWrittenFilesToClipboard()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (Items == null || !Items.Any())
            {
                StatusContext?.ToastError("No Items?");
                return;
            }

            await FilesToClipboard(SelectedItems).ConfigureAwait(true);
        }

        public string ToTransformedFileString(string fileBase)
        {
            var allPieces = $"{UserFilePrefix.TrimNullToEmpty()}{fileBase}";
            if (ChangeSlashes) allPieces = allPieces.Replace("\\", "/");

            return allPieces;
        }

        public async Task WrittenFilesToClipboard()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (Items == null || !Items.Any())
            {
                StatusContext?.ToastError("No Items?");
                return;
            }

            await FilesToClipboard(Items.ToList()).ConfigureAwait(true);
        }
    }
}