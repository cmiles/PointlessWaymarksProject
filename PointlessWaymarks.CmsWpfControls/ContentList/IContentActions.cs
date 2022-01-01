using System.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.ContentList;

public interface IContentActions<T> : INotifyPropertyChanged
{
    RelayCommand<T> DeleteCommand { get; set; }
    RelayCommand<T> EditCommand { get; set; }
    RelayCommand<T> ExtractNewLinksCommand { get; set; }
    RelayCommand<T> GenerateHtmlCommand { get; set; }
    RelayCommand<T> LinkCodeToClipboardCommand { get; set; }
    RelayCommand<T> ViewOnSiteCommand { get; set; }
    StatusControlContext StatusContext { get; set; }
    RelayCommand<T> ViewHistoryCommand { get; set; }
    string DefaultBracketCode(T content);
    Task DefaultBracketCodeToClipboard(T content);
    Task Delete(T content);
    Task Edit(T content);
    Task ExtractNewLinks(T content);
    Task GenerateHtml(T content);
    Task ViewOnSite(T content);
    Task ViewHistory(T content);
}