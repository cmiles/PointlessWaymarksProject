#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.FileContentEditor;
using PointlessWaymarks.CmsWpfControls.PostContentEditor;
using PointlessWaymarks.PressSharper;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.WordPressXmlImport
{
    public class WordPressXmlImportContext : INotifyPropertyChanged
    {
        private bool _filterOutExistingPostUrls = true;
        private bool _folderFromCategory;
        private bool _folderFromYear = true;
        private bool _importPages = true;
        private bool _importPosts = true;
        private ObservableCollection<WordPressXmlImportListItem>? _items;
        private Command _loadWordPressXmlFileCommand;
        private List<WordPressXmlImportListItem> _selectedItems = new();
        private Command _selectedToFileContentEditorCommand;
        private Command _selectedToPostContentEditorCommand;
        private StatusControlContext _statusContext;
        private string _userFilterText = string.Empty;
        private Blog? _wordPressData;

        public WordPressXmlImportContext(StatusControlContext? statusContext)
        {
            _statusContext = statusContext ?? new StatusControlContext();

            _loadWordPressXmlFileCommand = StatusContext.RunBlockingTaskCommand(LoadWordPressXmlFile);
            _selectedToPostContentEditorCommand = StatusContext.RunBlockingTaskCommand(SelectedToPostContentEditor);
            _selectedToFileContentEditorCommand = StatusContext.RunBlockingTaskCommand(SelectedToFileContentEditor);
        }

        public bool FilterOutExistingPostUrls
        {
            get => _filterOutExistingPostUrls;
            set
            {
                if (value == _filterOutExistingPostUrls) return;
                _filterOutExistingPostUrls = value;
                OnPropertyChanged();
            }
        }

        public bool FolderFromCategory
        {
            get => _folderFromCategory;
            set
            {
                if (value == _folderFromCategory) return;
                _folderFromCategory = value;
                OnPropertyChanged();
            }
        }

        public bool FolderFromYear
        {
            get => _folderFromYear;
            set
            {
                if (value == _folderFromYear) return;
                _folderFromYear = value;
                OnPropertyChanged();
            }
        }

        public bool ImportPages
        {
            get => _importPages;
            set
            {
                if (value == _importPages) return;
                _importPages = value;
                OnPropertyChanged();
            }
        }

        public bool ImportPosts
        {
            get => _importPosts;
            set
            {
                if (value == _importPosts) return;
                _importPosts = value;
                OnPropertyChanged();
            }
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

        public List<WordPressXmlImportListItem> SelectedItems
        {
            get => _selectedItems;
            set
            {
                if (Equals(value, _selectedItems)) return;
                _selectedItems = value;
                OnPropertyChanged();
            }
        }

        public Command SelectedToFileContentEditorCommand
        {
            get => _selectedToFileContentEditorCommand;
            set
            {
                if (Equals(value, _selectedToFileContentEditorCommand)) return;
                _selectedToFileContentEditorCommand = value;
                OnPropertyChanged();
            }
        }

        public Command SelectedToPostContentEditorCommand
        {
            get => _selectedToPostContentEditorCommand;
            set
            {
                if (Equals(value, _selectedToPostContentEditorCommand)) return;
                _selectedToPostContentEditorCommand = value;
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

        public string UserFilterText
        {
            get => _userFilterText;
            set
            {
                if (value == _userFilterText) return;
                _userFilterText = value;
                OnPropertyChanged();

                StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(FilterList);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private async Task FilterList()
        {
            if (Items == null || !Items.Any()) return;

            await ThreadSwitcher.ResumeForegroundAsync();

            ((CollectionView) CollectionViewSource.GetDefaultView(Items)).Filter = o =>
            {
                if (string.IsNullOrWhiteSpace(UserFilterText)) return true;

                var loweredString = UserFilterText.ToLower();

#pragma warning disable IDE0083 // Use pattern matching
                if (!(o is WordPressXmlImportListItem pi)) return false;
#pragma warning restore IDE0083 // Use pattern matching
                if (pi.Category.ToLower().Contains(loweredString)) return true;
                if (pi.Slug.ToLower().Contains(loweredString)) return true;
                if (pi.Title.ToLower().Contains(loweredString)) return true;
                if (pi.Tags.ToLower().Contains(loweredString)) return true;
                return false;
            };
        }

        public async Task LoadWordPressXmlFile()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            if (!ImportPages && !ImportPosts)
            {
                StatusContext.ToastError("Please choose one or both of Import Pages/Posts - nothing to import...");
                return;
            }

            StatusContext.Progress("Starting file load.");

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

            StatusContext.Progress($"Starting import of {file.FullName}");

            try
            {
                _wordPressData = new Blog(await File.ReadAllTextAsync(file.FullName));
            }
            catch (Exception e)
            {
                await StatusContext.ShowMessageWithOkButton("WordPress Import Error", e.ToString());
                return;
            }

            var processedContent = new List<WordPressXmlImportListItem>();

            var existingPostUrls = await (await Db.Context()).PostContents.Select(x => x.Slug).ToListAsync();
            var existingFileUrls = await (await Db.Context()).FileContents.Select(x => x.Slug).ToListAsync();

            var existingUrls = existingFileUrls.Concat(existingPostUrls).ToList();

            if (ImportPosts)
            {
                StatusContext.Progress("Starting Post Import");

                var posts = _wordPressData.GetPosts().ToList();

                StatusContext.Progress($"Found {posts.Count} Posts - processing...");

                foreach (var loopPosts in posts.OrderBy(x => x.PublishDate))
                {
                    if (existingUrls.Contains(loopPosts.Slug.ToLower())) continue;

                    processedContent.Add(new WordPressXmlImportListItem
                    {
                        CreatedBy = loopPosts.Author.DisplayName,
                        CreatedOn = loopPosts.PublishDate,
                        Category = loopPosts.Categories.First().Name,
                        Tags = string.Join(",", loopPosts.Tags.Select(x => x.Name)),
                        Title = loopPosts.Title,
                        Slug = loopPosts.Slug,
                        Summary = loopPosts.Excerpt,
                        Content = loopPosts.Body,
                        WordPressType = "Post"
                    });
                }

                StatusContext.Progress($"Added {processedContent.Count} Items");
            }

            if (ImportPages)
            {
                StatusContext.Progress("Starting Page Import");

                var pages = _wordPressData.GetPages().ToList();

                StatusContext.Progress($"Found {pages.Count} Posts - processing...");

                var processedCount = processedContent.Count;

                foreach (var loopPages in pages)
                {
                    if (existingUrls.Contains(loopPages.Slug.ToLower())) continue;

                    processedContent.Add(new WordPressXmlImportListItem
                    {
                        CreatedBy = loopPages.Author.DisplayName,
                        CreatedOn = loopPages.PublishDate,
                        Category = string.Empty,
                        Tags = string.Empty,
                        Title = loopPages.Title,
                        Slug = loopPages.Slug,
                        Content = loopPages.Body,
                        WordPressType = "Page"
                    });
                }

                StatusContext.Progress($"Added {processedContent.Count - processedCount} Pages");
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            StatusContext.Progress("Setting up UI");

            Items ??= new ObservableCollection<WordPressXmlImportListItem>();
            Items.Clear();
            processedContent.ForEach(x => Items.Add(x));

            await FilterList();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task SelectedToFileContentEditor()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (!SelectedItems.Any())
            {
                StatusContext.ToastWarning("No Items Selected?");
                return;
            }

            foreach (var loopItems in SelectedItems)
            {
                var newContent = new FileContent
                {
                    BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
                    UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
                    ShowInMainSiteFeed = true,
                    BodyContent = loopItems.Content,
                    CreatedBy = loopItems.CreatedBy,
                    CreatedOn = loopItems.CreatedOn,
                    Folder = FolderFromYear
                        ? loopItems.CreatedOn.Year.ToString()
                        : loopItems.Category.Replace(" ", "-"),
                    Slug = loopItems.Slug,
                    Tags = loopItems.Tags,
                    Title = loopItems.Title
                };


                await ThreadSwitcher.ResumeForegroundAsync();
                new FileContentEditorWindow(newContent).PositionWindowAndShow();
                await ThreadSwitcher.ResumeBackgroundAsync();
            }
        }

        public async Task SelectedToPostContentEditor()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (!SelectedItems.Any())
            {
                StatusContext.ToastWarning("No Items Selected?");
                return;
            }

            foreach (var loopItems in SelectedItems)
            {
                var newPost = new PostContent
                {
                    BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
                    UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
                    ShowInMainSiteFeed = true,
                    BodyContent = loopItems.Content,
                    CreatedBy = loopItems.CreatedBy,
                    CreatedOn = loopItems.CreatedOn,
                    Folder = FolderFromYear
                        ? loopItems.CreatedOn.Year.ToString()
                        : loopItems.Category.Replace(" ", "-"),
                    Slug = loopItems.Slug,
                    Tags = loopItems.Tags,
                    Title = loopItems.Title
                };


                await ThreadSwitcher.ResumeForegroundAsync();
                new PostContentEditorWindow(newPost).PositionWindowAndShow();
                await ThreadSwitcher.ResumeBackgroundAsync();
            }
        }
    }
}