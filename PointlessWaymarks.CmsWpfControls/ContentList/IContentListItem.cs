using System.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Utility;

namespace PointlessWaymarks.CmsWpfControls.ContentList;

public interface IContentListItem : INotifyPropertyChanged, ISelectedTextTracker
{
    IContentCommon Content();
    Guid? ContentId();
    string DefaultBracketCode();
    Task DefaultBracketCodeToClipboard();
    Task Delete();
    Task Edit();
    Task ExtractNewLinks();
    Task GenerateHtml();
    Task OpenUrl();
    Task ViewHistory();
}