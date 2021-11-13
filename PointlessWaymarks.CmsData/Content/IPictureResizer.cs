namespace PointlessWaymarks.CmsData.Content
{
    public interface IPictureResizer
    {
        Task<FileInfo?> ResizeTo(FileInfo toResize, int width, int quality, string imageTypeString, bool addSizeString,
            IProgress<string>? progress = null);
    }
}