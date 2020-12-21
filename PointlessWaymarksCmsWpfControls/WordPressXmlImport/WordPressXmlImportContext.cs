#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using Ookii.Dialogs.Wpf;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility.ThreadSwitcher;
using PressSharper;

namespace PointlessWaymarksCmsWpfControls.WordPressXmlImport
{
    public class WordPressXmlImportContext : INotifyPropertyChanged
    {
        private ObservableCollection<WordPressXmlImportListItem>? _items;
        private Command _loadWordPressXmlFileCommand;
        private StatusControlContext _statusContext;
        private Blog? _wordPressData;

        public WordPressXmlImportContext(StatusControlContext? statusContext)
        {
            _statusContext = statusContext ?? new StatusControlContext();

            _loadWordPressXmlFileCommand = StatusContext.RunBlockingTaskCommand(LoadWordPressXmlFile);
        }

        public ObservableCollection<WordPressXmlImportListItem>? Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged();
            }
        }

        public Command LoadWordPressXmlFileCommand
        {
            get => _loadWordPressXmlFileCommand;
            set
            {
                if (Equals(value, _loadWordPressXmlFileCommand)) return;
                _loadWordPressXmlFileCommand = value;
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

        public async Task LoadWordPressXmlFile()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            StatusContext.Progress("Starting photo load.");

            var dialog = new VistaOpenFileDialog
            {
                Multiselect = false,
                Filter = "xml files (*.xml)|*.xml|All files (*.*)|*.*",
                InitialDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteScriptsDirectory().FullName
            };

            if (!(dialog.ShowDialog() ?? false)) return;

            var selectedFileName = dialog.FileNames?.FirstOrDefault() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(selectedFileName)) return;

            var file = new FileInfo(selectedFileName);

            try
            {
                _wordPressData = new Blog(await File.ReadAllTextAsync(file.FullName));
            }
            catch (Exception e)
            {
                await StatusContext.ShowMessageWithOkButton("WordPress Import Error", e.ToString());
                return;
            }

            var posts = _wordPressData.GetPosts().ToList();

            var processedContent = new List<WordPressXmlImportListItem>();

            foreach (var loopPosts in posts)
                processedContent.Add(new WordPressXmlImportListItem
                {
                    CreatedBy = loopPosts.Author.DisplayName,
                    CreatedOn = loopPosts.PublishDate,
                    Folder = loopPosts.Categories.First().Name,
                    Tags = string.Join(",", loopPosts.Tags.Select(x => x.Name)),
                    Title = loopPosts.Title,
                    Slug = loopPosts.Slug,
                    Content = loopPosts.Body,
                    WordPressType = "Post"
                });

            var pages = _wordPressData.GetPages().ToList();

            foreach (var loopPages
                in pages)
                processedContent.Add(new WordPressXmlImportListItem
                {
                    CreatedBy = loopPages
                        .Author.DisplayName,
                    CreatedOn = loopPages
                        .PublishDate,
                    Folder = string.Empty,
                    Tags = string.Empty,
                    Title = loopPages
                        .Title,
                    Slug = loopPages
                        .Slug,
                    Content = loopPages
                        .Body,
                    WordPressType = "Page"
                });

            await ThreadSwitcher.ResumeForegroundAsync();

            Items ??= new ObservableCollection<WordPressXmlImportListItem>();

            Items.Clear();

            processedContent.ForEach(x => Items.Add(x));
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}