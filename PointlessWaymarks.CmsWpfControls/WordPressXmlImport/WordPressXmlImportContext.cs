using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.ContentGeneration;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.FileContentEditor;
using PointlessWaymarks.CmsWpfControls.LinkContentEditor;
using PointlessWaymarks.CmsWpfControls.PostContentEditor;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PressSharper;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ColumnSort;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using ColumnSortControlContext = PointlessWaymarks.WpfCommon.ColumnSort.ColumnSortControlContext;
using ColumnSortControlSortItem = PointlessWaymarks.WpfCommon.ColumnSort.ColumnSortControlSortItem;

namespace PointlessWaymarks.CmsWpfControls.WordPressXmlImport;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class WordPressXmlImportContext
{
    public WordPressXmlImportContext(StatusControlContext? statusContext)
    {
        StatusContext = statusContext ?? new StatusControlContext();

        BuildCommands();

        ListSort = new ColumnSortControlContext
        {
            Items =
            [
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
            ]
        };

        PropertyChanged += OnPropertyChanged;

        ListSort.SortUpdated += (_, list) =>
            StatusContext.RunFireAndForgetNonBlockingTask(() => ListContextSortHelpers.SortList(list, Items));
    }

    public bool FilterOutExistingPostUrls { get; set; } = true;
    public bool FolderFromCategory { get; set; }
    public bool FolderFromYear { get; set; } = true;
    public bool ImportPages { get; set; } = true;
    public bool ImportPosts { get; set; } = true;
    public ObservableCollection<WordPressXmlImportListItem>? Items { get; set; }
    public ContentListSelected<WordPressXmlImportListItem>? ListSelection { get; set; }
    public ColumnSortControlContext ListSort { get; set; }
    public List<WordPressXmlImportListItem> SelectedItems { get; set; } = [];
    public StatusControlContext StatusContext { get; set; }
    public string UserFilterText { get; set; } = string.Empty;
    public Blog? WordPressData { get; set; }

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

    [BlockingCommand]
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

        Items ??= [];
        Items.Clear();
        processedContent.ForEach(x => Items.Add(x));

        await ListContextSortHelpers.SortList(ListSort.SortDescriptions(), Items);
        await FilterList();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName == nameof(UserFilterText)) StatusContext.RunFireAndForgetNonBlockingTask(FilterList);
    }

    [BlockingCommand]
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

    [BlockingCommand]
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

    [BlockingCommand]
    public async Task SelectedToPostContentEditorWithAutosave()
    {
        await SelectedToPostContentEditor(true);
    }

    [BlockingCommand]
    public async Task SelectedToPostContentEditorWithoutAutosave()
    {
        await SelectedToPostContentEditor(false);
    }
}