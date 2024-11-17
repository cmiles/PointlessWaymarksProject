using System.Collections.ObjectModel;
using System.ComponentModel;
using KellermanSoftware.CompareNetObjects;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.ImageHelpers;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.SitePictureSizesEditor;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class SitePictureSizesEditorContext
{
    public bool HasValidationIssues { get; set; }
    public HelpDisplayContext? HelpContext { get; set; }

    public string HelpMarkdown =>
        """
        ### Site Picture Sizes

        When the Pointless Waymarks CMS generates Photo and Image Content it pre-generates multiple sizes of the images to help with performance and display on different devices.

        This strategy allows for efficient image sizes on a variety of different devices without needing an image server - but it does require more storage space.

        Different sites will have different goals and requirements for image sizes - for a site that you will access only with the Pointless Waymarks Site Viewer it might make sense to have save space and have only a few image sizes, for a site that you expect to get a wide variety of traffic from different devices you might want to have more image sizes...

        This editor will set the default set of image sizes that the CMS will generate for your site. Changes to this editor will not automatically be applied to content that has already been generated.
        """;

    public required ObservableCollection<SitePictureSizesEditorItem> Items { get; set; }
    public SitePictureSizesEditorItem? SelectedItem { get; set; }
    public List<SitePictureSizesEditorItem>? SelectedItems { get; set; } = [];
    public required StatusControlContext StatusContext { get; set; }
    public string ValidationIssuesMessage { get; set; } = string.Empty;

    /// <summary>
    ///     Use this method to add items rather than adding directly to the Items collection - this method will properly handle
    ///     the property changed events
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    public async Task AddItems(List<SitePictureSizesEditorItem> items)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        foreach (var loopItem in items) loopItem.PropertyChanged += OnPropertyChanged;

        await ThreadSwitcher.ResumeForegroundAsync();
        items.ForEach(x => Items.Add(x));
    }

    [BlockingCommand]
    public async Task AddNew()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var toAdd = new SitePictureSizesEditorItem();
        Items.Add(toAdd);
        ScrollIntoViewRequest?.Invoke(this, new ScrollSitePictureSizeIntoViewEventArgs { ToScrollTo = toAdd });
        SelectedItem = toAdd;
    }

    public (bool hasChanges, string changeNotes) CheckForChanges()
    {
        var reference = UserSettingsSingleton.CurrentSettings().SitePictureSizes;
        var current = Items.Select(x => new SitePictureSize { MaxDimension = x.MaxDimension, Quality = x.Quality })
            .OrderByDescending(x => x.MaxDimension).ToList();
        var result = new CompareLogic().Compare(reference, current);

        return (!result.AreEqual, result.DifferencesString);
    }

    public void CheckForValidationIssues()
    {
        if (!Items.Any())
        {
            HasValidationIssues = true;
            ValidationIssuesMessage = "At least one Picture Size is required";
            return;
        }

        if (Items.Any(x => x.MaxDimension < 1))
        {
            HasValidationIssues = true;
            ValidationIssuesMessage = "Max Dimension must be greater than 0";
            return;
        }

        if (Items.Any(x => x.Quality is < 1 or > 100))
        {
            HasValidationIssues = true;
            ValidationIssuesMessage = "Quality must be between 1 and 100";
            return;
        }

        if (Items.Select(x => x.MaxDimension).Distinct().Count() != Items.Count)
        {
            HasValidationIssues = true;
            ValidationIssuesMessage = "Max Dimension values must be unique";
            return;
        }

        HasValidationIssues = false;
        ValidationIssuesMessage = string.Empty;
    }

    public event EventHandler? CloseWindowRequest;

    public static async Task<SitePictureSizesEditorContext> CreateInstance(StatusControlContext? statusContext = null)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var settings = UserSettingsSingleton.CurrentSettings().SitePictureSizes;

        var factoryItems = settings.Select(x => new SitePictureSizesEditorItem()
            { MaxDimension = x.MaxDimension, Quality = x.Quality }).ToList();

        var factoryContext = await StatusControlContext.CreateInstance(statusContext);

        await ThreadSwitcher.ResumeForegroundAsync();


        var sitePictureSizesEditorContext = new SitePictureSizesEditorContext()
        {
            Items = [],
            StatusContext = factoryContext
        };

        sitePictureSizesEditorContext.BuildCommands();

        sitePictureSizesEditorContext.PropertyChanged += sitePictureSizesEditorContext.OnPropertyChanged;

        await sitePictureSizesEditorContext.AddItems(factoryItems);

        sitePictureSizesEditorContext.HelpContext =
            new HelpDisplayContext(sitePictureSizesEditorContext.HelpMarkdown.AsList());

        sitePictureSizesEditorContext.CheckForValidationIssues();

        return sitePictureSizesEditorContext;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        CheckForValidationIssues();
    }

    /// Use this method to remove items rather than adding directly to the Items collection - this method will properly handle the property changed events
    public async Task RemoveItems(List<SitePictureSizesEditorItem> items)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        foreach (var loopItem in items)
        {
            loopItem.PropertyChanged -= OnPropertyChanged;
            Items.Remove(loopItem);
        }
    }

    [BlockingCommand]
    public async Task RemoveSelected()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        if (SelectedItems is null || !SelectedItems.Any())
        {
            await StatusContext.ToastError("No Items Selected to Remove");
            return;
        }

        await RemoveItems(SelectedItems.ToList());
    }

    [BlockingCommand]
    public async Task ReplaceListWithDefaults()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        await RemoveItems(Items.ToList());

        var newItems = PictureResizing.SrcSetSizeAndQualityDefaultList().Select(x =>
            new SitePictureSizesEditorItem() { MaxDimension = x.size, Quality = x.quality }).ToList();

        await AddItems(newItems);
    }

    [BlockingCommand]
    public async Task Save()
    {
        await SaveList(false);
    }

    [BlockingCommand]
    public async Task SaveAndClose()
    {
        await SaveList(true);
    }

    public async Task SaveList(bool closeWindow)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (HasValidationIssues)
        {
            await StatusContext.ToastError("Validation Issues - Cannot Save");
            return;
        }

        if (!Items.Any())
        {
            await StatusContext.ToastError("No Picture Sizes to Save? You must have at least one entry...");
            return;
        }

        var settings = Items.Select(x => new SitePictureSize { MaxDimension = x.MaxDimension, Quality = x.Quality })
            .OrderByDescending(x => x.MaxDimension).ToList();

        UserSettingsSingleton.CurrentSettings().SitePictureSizes = settings;
        await UserSettingsSingleton.CurrentSettings().WriteSettings();

        await StatusContext.ToastSuccess("Site Picture Sizes Saved");

        if (closeWindow) CloseWindowRequest?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler<ScrollSitePictureSizeIntoViewEventArgs>? ScrollIntoViewRequest;

    [BlockingCommand]
    public async Task SortBySize()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        Items.SortByDescending(x => x.MaxDimension);
    }
}

public class ScrollSitePictureSizeIntoViewEventArgs : EventArgs
{
    public required SitePictureSizesEditorItem ToScrollTo { get; set; }
}