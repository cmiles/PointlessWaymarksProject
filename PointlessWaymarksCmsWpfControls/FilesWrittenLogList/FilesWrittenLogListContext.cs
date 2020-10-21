#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        private bool _filterForFilesInCurrentGenerationDirectory = true;
        private Command? _generateItemsCommand;
        private ObservableCollection<string>? _generationChoices;
        private ObservableCollection<FilesWrittenLogListListItem>? _items;
        private Command? _scriptStringsToClipboardCommand;
        private string? _selectedGenerationChoice;
        private List<FilesWrittenLogListListItem> _selectedItems = new List<FilesWrittenLogListListItem>();
        private Command? _selectedScriptStringsToClipboardCommand;
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
                StatusContext.RunBlockingTaskCommand(async () => await SelectedStringsToClipboard());
            WrittenFilesToClipboardCommand =
                StatusContext.RunBlockingTaskCommand(async () => await WrittenFilesToClipboard());

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

        public ObservableCollection<string>? GenerationChoices
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

        public string? SelectedGenerationChoice
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

        public async Task GenerateItems()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (string.IsNullOrWhiteSpace(SelectedGenerationChoice))
            {
                StatusContext?.ToastError("Please make a Generation Date Choice");
                return;
            }

            var db = await Db.Context();

            var generationDirectory = new DirectoryInfo(UserSettingsSingleton.CurrentSettings().LocalSiteRootDirectory)
                .FullName;

            IQueryable<GenerationFileWriteLog> searchQuery = FilterForFilesInCurrentGenerationDirectory
                ? db.GenerationFileWriteLogs.Where(x => x.FileName.StartsWith(generationDirectory))
                : db.GenerationFileWriteLogs;

            if (SelectedGenerationChoice != "All")
            {
                var parsedGenerationChoice = DateTime.Parse(SelectedGenerationChoice);
                searchQuery = searchQuery.Where(x => x.WrittenOnVersion >= parsedGenerationChoice);
            }

            var dbItems = await searchQuery.OrderBy(x => x.WrittenOnVersion).ToListAsync();

            var transformedItems = new List<FilesWrittenLogListListItem>();

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
                    WrittenOn = loopDbItems.WrittenOnVersion,
                    WrittenFile = loopDbItems.FileName,
                    IsInGenerationDirectory = isInGenerationDirectory
                });
            }

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

            var db = await Db.Context();

            var logChoiceList = new List<string> {"All"};

            logChoiceList.AddRange((await db.GenerationLogs.Select(x => x.GenerationVersion).OrderByDescending(x => x)
                .ToListAsync()).Select(x => x.ToString("F")));

            await ThreadSwitcher.ResumeForegroundAsync();

            GenerationChoices ??= new ObservableCollection<string>();
            GenerationChoices.Clear();
            logChoiceList.ForEach(x => GenerationChoices.Add(x));
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

            var sortedItems = Items.OrderByDescending(x => x.WrittenFile.Count(y => y == '\\'))
                .ThenBy(x => x.WrittenFile);

            var scriptString = string.Join(Environment.NewLine,
                sortedItems.Where(x => x.IsInGenerationDirectory).Distinct().ToList().Select(x =>
                    $"{UserScriptPrefix}{(string.IsNullOrWhiteSpace(UserScriptPrefix) ? "" : " ")}'{x.WrittenFile}' {x.TransformedFile};"));

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(scriptString);
        }

        public async Task SelectedStringsToClipboard()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (!SelectedItems.Any())
            {
                StatusContext?.ToastError("No Items Selected?");
                return;
            }

            var sortedItems = SelectedItems.OrderByDescending(x => x.WrittenFile.Count(y => y == '\\'))
                .ThenBy(x => x.WrittenFile);

            var scriptString = string.Join(Environment.NewLine,
                sortedItems.Where(x => x.IsInGenerationDirectory).Distinct().ToList().Select(x =>
                    $"{UserScriptPrefix}{(string.IsNullOrWhiteSpace(UserScriptPrefix) ? "" : " ")}'{x.WrittenFile}' {x.TransformedFile};"));

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(scriptString);
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

            var scriptString = string.Join(Environment.NewLine, Items.Select(x => x.WrittenFile).Distinct().ToList());

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(scriptString);
        }
    }
}