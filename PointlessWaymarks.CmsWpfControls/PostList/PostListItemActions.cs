using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.PostContentEditor;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.PostList
{
    public class PostListItemActions : INotifyPropertyChanged
    {
        private Command<PostContent> _editContentCommand;
        private StatusControlContext _statusContext;

        public PostListItemActions(StatusControlContext statusContext)
        {
            StatusContext = statusContext;
            EditContentCommand = StatusContext.RunNonBlockingTaskCommand<PostContent>(EditContent);
        }

        public Command<PostContent> EditContentCommand
        {
            get => _editContentCommand;
            set
            {
                if (Equals(value, _editContentCommand)) return;
                _editContentCommand = value;
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

        private async Task EditContent(PostContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null) return;

            var context = await Db.Context();

            var refreshedData = context.PostContents.SingleOrDefault(x => x.ContentId == content.ContentId);

            if (refreshedData == null)
                StatusContext.ToastError($"{content.Title} is no longer active in the database? Can not edit - " +
                                         "look for a historic version...");

            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new PostContentEditorWindow(refreshedData);

            newContentWindow.PositionWindowAndShow();

            await ThreadSwitcher.ResumeBackgroundAsync();
        }

        public static PostListListItem ListItemFromDbItem(PostContent content, PostListItemActions itemActions)
        {
            return new()
            {
                DbEntry = content, SmallImageUrl = ContentListContext.GetSmallImageUrl(content),
                ItemActions = itemActions
            };
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}