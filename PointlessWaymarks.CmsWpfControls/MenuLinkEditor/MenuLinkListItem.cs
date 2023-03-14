using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsWpfControls.MenuLinkEditor;

public partial class MenuLinkListItem : ObservableObject
{
    [ObservableProperty] private MenuLink _dbEntry;
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private string _userLink = string.Empty;
    [ObservableProperty] private int _userOrder;

    private MenuLinkListItem(MenuLink dbEntry)
    {
        _dbEntry = dbEntry;

        PropertyChanged += OnPropertyChanged;
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
}