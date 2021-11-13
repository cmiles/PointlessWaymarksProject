using System.Globalization;
using System.Windows.Data;
using PointlessWaymarks.CmsWpfControls.FileList;
using PointlessWaymarks.CmsWpfControls.GeoJsonList;
using PointlessWaymarks.CmsWpfControls.ImageList;
using PointlessWaymarks.CmsWpfControls.LineList;
using PointlessWaymarks.CmsWpfControls.LinkList;
using PointlessWaymarks.CmsWpfControls.MapComponentList;
using PointlessWaymarks.CmsWpfControls.NoteList;
using PointlessWaymarks.CmsWpfControls.PhotoList;
using PointlessWaymarks.CmsWpfControls.PostList;

namespace PointlessWaymarks.CmsWpfControls.Utility
{
    public class ListItemTypeToReadableNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            return value switch
            {
                FileListListItem => "File",
                GeoJsonListListItem => "GeoJson",
                ImageListListItem => "Image",
                LineListListItem => "Line",
                LinkListListItem => "Link",
                MapComponentListListItem => "Map",
                NoteListListItem => "Note",
                PhotoListListItem => "Photo",
                PostListListItem => "Post",
                _ => string.Empty
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}