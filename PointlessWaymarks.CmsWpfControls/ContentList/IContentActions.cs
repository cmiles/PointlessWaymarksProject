using System.ComponentModel;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.ContentList
{
    public interface IContentActions<T> : INotifyPropertyChanged
    {
        Command<T> DeleteCommand { get; set; }
        Command<T> EditCommand { get; set; }
        Command<T> ExtractNewLinksCommand { get; set; }
        Command<T> GenerateHtmlCommand { get; set; }
        Command<T> LinkCodeToClipboardCommand { get; set; }
        Command<T> OpenUrlCommand { get; set; }
        StatusControlContext StatusContext { get; set; }
        Command<T> ViewHistoryCommand { get; set; }
        string DefaultBracketCode(T content);
        Task DefaultBracketCodeToClipboard(T content);
        Task Delete(T content);
        Task Edit(T content);
        Task ExtractNewLinks(T content);
        Task GenerateHtml(T content);
        Task OpenUrl(T content);
        Task ViewHistory(T content);
    }
}