#nullable enable
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ColumnSort;
using PointlessWaymarks.CmsWpfControls.FileContentEditor;
using PointlessWaymarks.CmsWpfControls.LinkContentEditor;
using PointlessWaymarks.CmsWpfControls.PostContentEditor;
using PointlessWaymarks.PressSharper;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.WordPressXmlImport;

public partial class WordPressXmlImportContext : ObservableObject
{
    [ObservableProperty] private bool _filterOutExistingPostUrls = true;
    [ObservableProperty] private bool _folderFromCategory;
    [ObservableProperty] private bool _folderFromYear = true;
    [ObservableProperty] private bool _importPages = true;
    [ObservableProperty] private bool _importPosts = true;
    [ObservableProperty] private ObservableCollection<WordPressXmlImportListItem>? _items;
    [ObservableProperty] private ContentListSelected<WordPressXmlImportListItem>? _listSelection;
    [ObservableProperty] private ColumnSortControlContext _listSort;
    [ObservableProperty] private RelayCommand _loadWordPressXmlFileCommand;
    [ObservableProperty] private List<WordPressXmlImportListItem> _selectedItems = new();
    [ObservableProperty] private RelayCommand _selectedToFileContentEditorCommand;
    [ObservableProperty] private RelayCommand _selectedToLinkContentEditorCommand;
    [ObservableProperty] private RelayCommand _selectedToPostContentEditorAutoSaveCommand;
    [ObservableProperty] private RelayCommand _selectedToPostContentEditorCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private string _userFilterText = string.Empty;
    [ObservableProperty] private Blog? _wordPressData;


    public WordPressXmlImportContext(StatusControlContext? statusContext)
    {
        _statusContext = statusContext ?? new StatusControlContext();

        _loadWordPressXmlFileCommand = StatusContext.RunBlockingTaskCommand(LoadWordPressXmlFile);
        _selectedToPostContentEditorCommand =
            StatusContext.RunBlockingTaskCommand(async () => await SelectedToPostContentEditor(false));
        _selectedToPostContentEditorAutoSaveCommand =
            StatusContext.RunBlockingTaskCommand(async () => await SelectedToPostContentEditor(true));
        _selectedToFileContentEditorCommand = StatusContext.RunBlockingTaskCommand(SelectedToFileContentEditor);
        _selectedToLinkContentEditorCommand = StatusContext.RunBlockingTaskCommand(SelectedToLinkContentEditor);

        _listSort = new ColumnSortControlContext
        {
            Items = new List<ColumnSortControlSortItem>
            {
                new()
                {
                    DisplayName = "Created",
                    ColumnName = "CreatedOn",
                    DefaultSortDirection = ListSortDirection.Descending,
                    Order = 1
                },
                new()
                {
                    DisplayName = "Title",
                    ColumnName = "Title",
                    DefaultSortDirection = ListSortDirection.Ascending
                },
                new()
                {
                    DisplayName = "Category",
                    ColumnName = "Category",
                    DefaultSortDirection = ListSortDirection.Descending
                }
            }
        };

        PropertyChanged += OnPropertyChanged;

        ListSort.SortUpdated += (_, list) =>
            Dispatcher.CurrentDispatcher.Invoke(() => { ListContextSortHelpers.SortList(list, Items); });
    }

    private async Task FilterList()
    {
        if (Items == null || !Items.Any()) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        ((CollectionView)CollectionViewSource.GetDefaultView(Items)).Filter = o =>
        {
            if (string.IsNullOrWhiteSpace(UserFilterText)) return true;

            var loweredString = UserFilterText.ToLower();

#pragma warning disable IDE0083 // Use pattern matching
            if (o is not WordPressXmlImportListItem pi) return false;
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

        ListSelection = await ContentListSelected<WordPressXmlImportListItem>.CreateInstance(StatusContext);

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
            InitialDirectory = UserSettingsSingleton.CurrentSettings().LocalScriptsDirectory().FullName
        };

        if (!(dialog.ShowDialog() ?? false)) return;

        var selectedFileName = dialog.FileNames?.FirstOrDefault() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(selectedFileName)) return;

        var file = new FileInfo(selectedFileName);

        StatusContext.Progress($"Starting import of {file.FullName}");

        try
        {
            WordPressData = new Blog(await File.ReadAllTextAsync(file.FullName));
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

            var posts = WordPressData.GetPosts().ToList();

            StatusContext.Progress($"Found {posts.Count} Posts - processing...");

            foreach (var loopPosts in posts.OrderBy(x => x.PublishDate))
            {
                if (existingUrls.Contains(loopPosts.Slug.ToLower())) continue;

                processedContent.Add(new WordPressXmlImportListItem
                {
                    CreatedBy = loopPosts.Author?.DisplayName ?? string.Empty,
                    CreatedOn = loopPosts.PublishDate ?? DateTime.Now,
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

            var pages = WordPressData.GetPages().ToList();

            StatusContext.Progress($"Found {pages.Count} Posts - processing...");

            var processedCount = processedContent.Count;

            foreach (var loopPages in pages)
            {
                if (existingUrls.Contains(loopPages.Slug.ToLower())) continue;

                processedContent.Add(new WordPressXmlImportListItem
                {
                    CreatedBy = loopPages.Author?.DisplayName ?? string.Empty,
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

        ListContextSortHelpers.SortList(ListSort.SortDescriptions(), Items);
        await FilterList();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName == nameof(UserFilterText)) StatusContext.RunFireAndForgetNonBlockingTask(FilterList);
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
                ContentId = Guid.NewGuid(),
                BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
                UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
                ShowInMainSiteFeed = true,
                BodyContent = loopItems.Content,
                CreatedBy = loopItems.CreatedBy,
                CreatedOn = loopItems.CreatedOn,
                FeedOn = loopItems.CreatedOn,
                ContentVersion = Db.ContentVersionDateTime(),
                Folder =
                    FolderFromYear ? loopItems.CreatedOn.Year.ToString() : loopItems.Category.Replace(" ", "-"),
                Slug = loopItems.Slug,
                Tags = loopItems.Tags,
                Title = loopItems.Title
            };

            await ThreadSwitcher.ResumeForegroundAsync();
            (await FileContentEditorWindow.CreateInstance(newContent)).PositionWindowAndShow();
            await ThreadSwitcher.ResumeBackgroundAsync();
        }
    }

    public async Task SelectedToLinkContentEditor()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!SelectedItems.Any())
        {
            StatusContext.ToastWarning("No Items Selected?");
            return;
        }

        foreach (var loopItems in SelectedItems)
        {
            var possibleLinkMatch = Regex.Match(loopItems.Content,
                @"\b(https?)://[-A-Z0-9+&@#/%?=~_|$!:,.;]*[A-Z0-9+&@#/%=~_|$]", RegexOptions.IgnoreCase);

            var possibleLink = possibleLinkMatch.Success ? possibleLinkMatch.Value : string.Empty;


            var newPost = LinkContent.CreateInstance();

            newPost.Url = possibleLink;
            newPost.Comments = loopItems.Content;
            newPost.CreatedBy = loopItems.CreatedBy;
            newPost.CreatedOn = loopItems.CreatedOn;
            newPost.Tags = loopItems.Tags;
            newPost.Title = loopItems.Title;

            await (await LinkContentEditorWindow.CreateInstance(newPost)).PositionWindowAndShowOnUiThread();
        }
    }

    public async Task SelectedToPostContentEditor(bool autoSave)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!SelectedItems.Any())
        {
            StatusContext.ToastWarning("No Items Selected?");
            return;
        }

        var frozenNow = DateTime.Now;

        foreach (var loopItems in SelectedItems)
        {
            var newPost = new PostContent
            {
                ContentId = Guid.NewGuid(),
                BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
                UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
                ShowInMainSiteFeed = true,
                BodyContent = loopItems.Content,
                CreatedBy = loopItems.CreatedBy,
                CreatedOn = loopItems.CreatedOn,
                FeedOn = loopItems.CreatedOn,
                ContentVersion = Db.ContentVersionDateTime(),
                Summary = string.IsNullOrWhiteSpace(loopItems.Summary) ? loopItems.Title : loopItems.Summary,
                Folder =
                    FolderFromYear ? loopItems.CreatedOn.Year.ToString() : loopItems.Category.Replace(" ", "-"),
                Slug = loopItems.Slug,
                Tags = loopItems.Tags,
                Title = loopItems.Title
            };

            if (autoSave)
            {
                var validationReturn = await PostGenerator.Validate(newPost);

                if (!validationReturn.HasError)
                {
                    var saveResult =
                        await PostGenerator.SaveAndGenerateHtml(newPost, frozenNow, StatusContext.ProgressTracker());

                    if (!saveResult.generationReturn.HasError) continue;
                }
            }

            await (await PostContentEditorWindow.CreateInstance(newPost)).PositionWindowAndShowOnUiThread();
        }
    }
}