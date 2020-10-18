using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using MvvmHelpers.Commands;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.FilesWrittenLogList
{
    public class FilesWrittenLogListContext : INotifyPropertyChanged
    {
        private Command _generateItemsCommand;
        private ObservableCollection<string> _generationChoices;
        private List<FileWrittenEntry> _items;
        private string _selectedGenerationChoice;
        private StatusControlContext _statusContext;

        private FilesWrittenLogListContext()
        {
        }

        public FilesWrittenLogListContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            GenerateItemsCommand = StatusContext.RunBlockingTaskCommand(async () => await GenerateItems());

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
        }

        public Command GenerateItemsCommand
        {
            get => _generateItemsCommand;
            set
            {
                if (Equals(value, _generateItemsCommand)) return;
                _generateItemsCommand = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> GenerationChoices
        {
            get => _generationChoices;
            set
            {
                if (Equals(value, _generationChoices)) return;
                _generationChoices = value;
                OnPropertyChanged();
            }
        }

        public List<FileWrittenEntry> Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged();
            }
        }

        public string SelectedGenerationChoice
        {
            get => _selectedGenerationChoice;
            set
            {
                if (value == _selectedGenerationChoice) return;
                _selectedGenerationChoice = value;
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

        public event PropertyChangedEventHandler? PropertyChanged;

        public static async Task<FilesWrittenLogListContext> CreateInstance(StatusControlContext statusContext)
        {
            var newContext =
                new FilesWrittenLogListContext {StatusContext = statusContext ?? new StatusControlContext()};
            await newContext.LoadData();
            return newContext;
        }

        public async Task GenerateItems()
        {
            if (string.IsNullOrWhiteSpace(SelectedGenerationChoice))
            {
                StatusContext.ToastError("Please make a Generation Date Choice");
                return;
            }

            var db = await Db.Context();

            if (SelectedGenerationChoice == "All")
            {
                Items = (await db.GenerationFileWriteLogs.OrderBy(x => x.WrittenOnVersion).ToListAsync())
                    .Select(x => new FileWrittenEntry(x.WrittenOnVersion, x.FileName)).ToList();
                return;
            }

            var parsedGenerationChoice = DateTime.Parse(SelectedGenerationChoice);

            Items = (await db.GenerationFileWriteLogs.Where(x => x.WrittenOnVersion >= parsedGenerationChoice)
                    .OrderBy(x => x.WrittenOnVersion).ToListAsync())
                .Select(x => new FileWrittenEntry(x.WrittenOnVersion, x.FileName)).ToList();
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
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public record FileWrittenEntry(DateTime WrittenOn, string FileName);
    }
}