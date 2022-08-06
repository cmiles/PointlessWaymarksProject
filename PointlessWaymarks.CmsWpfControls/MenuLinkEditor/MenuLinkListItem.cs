using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsWpfControls.MenuLinkEditor;

[ObservableObject]
public partial class MenuLinkListItem
{
    [ObservableProperty] private MenuLink _dbEntry;
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private string _userLink;
    [ObservableProperty] private int _userOrder;

    public MenuLinkListItem()
    {
        PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName == nameof(DbEntry))
        {
            if (DbEntry == null)
            {
                UserLink = string.Empty;
                UserOrder = 0;
            }
            else
            {
                UserLink = (DbEntry.LinkTag ?? string.Empty).Trim();
                UserOrder = DbEntry.MenuOrder;
            }
        }

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChanges();
    }

    public void CheckForChanges()
    {
        if (DbEntry == null || DbEntry.Id < 1)
        {
            HasChanges = true;
            return;
        }

        HasChanges = CleanedUserLink() != DbEntry.LinkTag || UserOrder != DbEntry.MenuOrder;
    }

    private string CleanedUserLink()
    {
        var toReturn = UserLink ?? string.Empty;

        return toReturn.Trim();
    }

}