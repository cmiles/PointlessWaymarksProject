#nullable enable
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.FileContentEditor;
using PointlessWaymarks.CmsWpfControls.LinkContentEditor;
using PointlessWaymarks.CmsWpfControls.PostContentEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.PressSharper;

using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.WordPressXmlImport;

[ObservableObject]
public partial class WordPressXmlImportContext
{
    [ObservableProperty] private bool _filterOutExistingPostUrls = true;
    [ObservableProperty] private bool _folderFromCategory;
    [ObservableProperty] private bool _folderFromYear = true;
    [ObservableProperty] private bool _importPages = true;
    [ObservableProperty] private bool _importPosts = true;
    [ObservableProperty] private ObservableCollection<WordPressXmlImportListItem>? _items;
    [ObservableProperty] private ContentListSelected<WordPressXmlImportListItem>? _listSelection;
    [ObservableProperty] private RelayCommand _loadWordPressXmlFileCommand;
    [ObservableProperty] private List<WordPressXmlImportListItem> _selectedItems = new();
    [ObservableProperty] private RelayCommand _selectedToFileContentEditorCommand;
    [ObservableProperty] private RelayCommand _selectedToLinkContentEditorCommand;
    [ObservableProperty] private RelayCommand _selectedToPostContentEditorCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private string _userFilterText = string.Empty;
    [ObservableProperty] private Blog? _wordPressData;

    public WordPressXmlImportContext(StatusControlContext? statusContext)
    {
        _statusContext = statusContext ?? new StatusControlContext();

        PropertyChanged += OnPropertyChanged;

        _loadWordPressXmlFileCommand = StatusContext.RunBlockingTaskCommand(LoadWordPressXmlFile);
        _selectedToPostContentEditorCommand = StatusContext.RunBlockingTaskCommand(SelectedToPostContentEditor);
        _selectedToFileContentEditorCommand = StatusContext.RunBlockingTaskCommand(SelectedToFileContentEditor);
        _selectedToLinkContentEditorCommand = StatusContext.RunBlockingTaskCommand(SelectedToLinkContentEditor);
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

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
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
                BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
                UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
                ShowInMainSiteFeed = true,
                BodyContent = loopItems.Content,
                CreatedBy = loopItems.CreatedBy,
                CreatedOn = loopItems.CreatedOn,
                Folder =
                    FolderFromYear ? loopItems.CreatedOn.Year.ToString() : loopItems.Category.Replace(" ", "-"),
                Slug = loopItems.Slug,
                Tags = loopItems.Tags,
                Title = loopItems.Title
            };


            await ThreadSwitcher.ResumeForegroundAsync();
            new FileContentEditorWindow(newContent).PositionWindowAndShow();
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

            var newPost = new LinkContent
            {
                Url = possibleLink,
                Comments = loopItems.Content,
                CreatedBy = loopItems.CreatedBy,
                CreatedOn = loopItems.CreatedOn,
                Tags = loopItems.Tags,
                Title = loopItems.Title
            };

            await ThreadSwitcher.ResumeForegroundAsync();
            new LinkContentEditorWindow(newPost).PositionWindowAndShow();
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
                Folder =
                    FolderFromYear ? loopItems.CreatedOn.Year.ToString() : loopItems.Category.Replace(" ", "-"),
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