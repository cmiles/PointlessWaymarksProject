using System.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.MenuLinkEditor;

[NotifyPropertyChanged]
public partial class MenuLinkListItem
{
    private MenuLinkListItem(MenuLink dbEntry)
    {
        DbEntry = dbEntry;

        PropertyChanged += OnPropertyChanged;
    }

    public MenuLink DbEntry { get; set; }
    public bool HasChanges { get; set; }
    public MenuLinkEditorContentTypeSearchListChoice? SelectedRssPage { get; set; }
    public MenuLinkEditorContentTypeSearchListChoice? SelectedSearchPage { get; set; }
    public string UserLink { get; set; } = string.Empty;
    public int UserOrder { get; set; }

    public void CheckForChanges()
    {
        if (DbEntry.Id < 1)
        {
            HasChanges = true;
            return;
        }

        HasChanges = CleanedUserLink() != DbEntry.LinkTag || UserOrder != DbEntry.MenuOrder;
    }

    private string CleanedUserLink()
    {
        var toReturn = UserLink;

        return toReturn.Trim();
    }

    public static Task<MenuLinkListItem> CreateInstance(MenuLink dbEntry)
    {
        return Task.FromResult(new MenuLinkListItem(dbEntry));
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName == nameof(DbEntry))
        {
            UserLink = (DbEntry.LinkTag ?? string.Empty).Trim();
            UserOrder = DbEntry.MenuOrder;
        }

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChanges();
    }
}