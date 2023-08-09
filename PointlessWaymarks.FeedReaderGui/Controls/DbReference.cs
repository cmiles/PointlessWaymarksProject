using PointlessWaymarks.FeedReaderData;

namespace PointlessWaymarks.FeedReaderGui.Controls;

public class DbReference
{
    /// <summary>
    /// Db file when the editor context is created - this allows the editor
    /// to refer to the originating file even if another view has switched
    /// to another db.
    /// </summary>
    public string DbFileFullName { get; init; } = string.Empty;

    /// <summary>
    /// Use this to get the FeedContext Db with the DbFile value set when the
    /// editor was created. This will ensure that even if the main/another view
    /// has switched db files that the editor correctly refers to the originating
    /// file.
    /// </summary>
    /// <returns></returns>
    public async Task<FeedContext> GetInstance()
    {
        return await FeedContext.CreateInstance(DbFileFullName, false);
    }
}