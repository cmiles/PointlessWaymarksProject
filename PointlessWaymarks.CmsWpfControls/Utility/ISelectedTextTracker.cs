using System.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsWpfControls.Utility
{
    public interface IContentListItem : INotifyPropertyChanged, ISelectedTextTracker, IContentCommonGuiListItem
    {
        IContentCommon Content();
    }

    public interface IContentListSmallImage
    {
        string SmallImageUrl { get; set; }
    }

    public interface ISelectedTextTracker
    {
        CurrentSelectedTextTracker SelectedTextTracker { get; set; }
    }
}