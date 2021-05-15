using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ImageContentEditor;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.ImageList
{
    public class ImageListItemActions : INotifyPropertyChanged
    {
        private StatusControlContext _statusContext;
        private Command<ImageContent> _editContentCommand;
        private Command<ImageContent> _viewFileCommand;

        public ImageListItemActions(StatusControlContext statusContext)
        {
            StatusContext = statusContext;
            
            EditContentCommand = StatusContext.RunNonBlockingTaskCommand<ImageContent>(EditContent);
                        ViewFileCommand = StatusContext.RunNonBlockingTaskCommand<ImageContent>(ViewImage);

        }

        private async Task ViewImage(ImageContent listItem)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (listItem == null)
            {
                StatusContext.ToastError("Nothing Items to Open?");
                return;
            }

            if (string.IsNullOrWhiteSpace(listItem.OriginalFileName))
            {
                StatusContext.ToastError("No File?");
                return;
            }

            var toOpen = UserSettingsSingleton.CurrentSettings().LocalSiteImageContentFile(listItem);

            if (toOpen is not {Exists: true})
            {
                StatusContext.ToastError("File doesn't exist?");
                return;
            }

            var url = toOpen.FullName;

            var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }
        
        public Command<ImageContent> ViewFileCommand
        {
            get => _viewFileCommand;
            set
            {
                if (Equals(value, _viewFileCommand)) return;
                _viewFileCommand = value;
                OnPropertyChanged();
            }
        }

        public Command<ImageContent> EditContentCommand
        {
            get => _editContentCommand;
            set
            {
                if (Equals(value, _editContentCommand)) return;
                _editContentCommand = value;
                OnPropertyChanged();
            }
        }

        public static string GetSmallImageUrl(ImageContent content)
        {
            if (content == null) return null;

            string smallImageUrl;

            try
            {
                smallImageUrl = PictureAssetProcessing.ProcessImageDirectory(content).SmallPicture?.File.FullName;
            }
            catch
            {
                smallImageUrl = null;
            }

            return smallImageUrl;
        }
        
        private async Task EditContent(ImageContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null) return;

            var context = await Db.Context();

            var refreshedData = context.ImageContents.SingleOrDefault(x => x.ContentId == content.ContentId);

            if (refreshedData == null)
                StatusContext.ToastError($"{content.Title} is no longer active in the database? Can not edit - " +
                                         "look for a historic version...");

            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new ImageContentEditorWindow(refreshedData);

            newContentWindow.PositionWindowAndShow();

            await ThreadSwitcher.ResumeBackgroundAsync();
        }



        public static ImageListListItem ListItemFromDbItem(ImageContent content, ImageListItemActions itemActions)
        {
            return new() {DbEntry = content, SmallImageUrl = GetSmallImageUrl(content), ItemActions = itemActions};
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

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}