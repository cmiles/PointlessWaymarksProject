using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.LinkContentEditor;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.LinkList
{
    public class LinkListItemActions : INotifyPropertyChanged
    {
        private Command<string> _copyUrlCommand;
        private Command<LinkContent> _editContentCommand;
        private Command<string> _openUrlCommand;


        private StatusControlContext _statusContext;

        public LinkListItemActions(StatusControlContext statusContext)
        {
            StatusContext = statusContext;
            EditContentCommand = StatusContext.RunNonBlockingTaskCommand<LinkContent>(EditContent);
            OpenUrlCommand = StatusContext.RunNonBlockingTaskCommand<string>(OpenUrl);
            CopyUrlCommand = StatusContext.RunNonBlockingTaskCommand<string>(async x =>
            {
                await ThreadSwitcher.ResumeForegroundAsync();

                Clipboard.SetText(x);

                StatusContext.ToastSuccess($"To Clipboard {x}");
            });
        }

        public Command<string> CopyUrlCommand
        {
            get => _copyUrlCommand;
            set
            {
                if (Equals(value, _copyUrlCommand)) return;
                _copyUrlCommand = value;
                OnPropertyChanged();
            }
        }

        public Command<LinkContent> EditContentCommand
        {
            get => _editContentCommand;
            set
            {
                if (Equals(value, _editContentCommand)) return;
                _editContentCommand = value;
                OnPropertyChanged();
            }
        }

        public Command<string> OpenUrlCommand
        {
            get => _openUrlCommand;
            set
            {
                if (Equals(value, _openUrlCommand)) return;
                _openUrlCommand = value;
                OnPropertyChanged();
            }
        }

        public StatusControlContext StatusContext
        {
            get => _statusContext;
            set
            {
                if (Equals(value, _statusContext)) return;
                _statusContext = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private async Task EditContent(LinkContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null) return;

            var context = await Db.Context();

            var refreshedData = context.LinkContents.SingleOrDefault(x => x.ContentId == content.ContentId);

            if (refreshedData == null)
                StatusContext.ToastError($"{content.Title} is no longer active in the database? Can not edit - " +
                                         "look for a historic version...");

            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new LinkContentEditorWindow(refreshedData);

            newContentWindow.PositionWindowAndShow();

            await ThreadSwitcher.ResumeBackgroundAsync();
        }


        public static LinkListListItem ListItemFromDbItem(LinkContent content, LinkListItemActions itemActions)
        {
            var newItem = new LinkListListItem {DbEntry = content, ItemActions = itemActions};

            return newItem;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task OpenUrl(string url)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (string.IsNullOrWhiteSpace(url))
            {
                StatusContext.ToastError("Link is blank?");
                return;
            }

            var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }
    }
}