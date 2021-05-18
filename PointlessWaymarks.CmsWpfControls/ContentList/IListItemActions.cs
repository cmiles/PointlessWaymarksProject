using System.ComponentModel;
using System.Threading.Tasks;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.ContentList
{
    public interface IListItemActions<T> : INotifyPropertyChanged
    {
        Command<T> DeleteCommand { get; set; }
        Command<T> EditCommand { get; set; }
        Command<T> ExtractNewLinksCommand { get; set; }
        Command<T> GenerateHtmlCommand { get; set; }
        Command<T> LinkCodeToClipboardCommand { get; set; }
        Command NewContentCommand { get; set; }
        Command<T> OpenUrlCommand { get; set; }
        StatusControlContext StatusContext { get; set; }
        Command<T> ViewFileCommand { get; set; }
        Command<T> ViewHistoryCommand { get; set; }
        Task Delete(T content);
        Task Edit(T content);
        Task ExtractNewLinks(T content);
        Task GenerateHtml(T content);
        Task LinkCodeToClipboard(T content);
        Task NewContent();
        Task OpenUrl(T content);
        Task ViewFile(T listItem);
        Task ViewHistory(T content);
    }
}