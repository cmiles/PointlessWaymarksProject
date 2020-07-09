using System.Threading.Tasks;
using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsData.Html.HtmlGeneration
{
    public static class Generate
    {
        public static async Task WriteRelatedContentInformationToDb(IContentCommon content)
        {
        }

        //public static async Task<List<Guid>> ContentChanged(DateTime contentAfter, IProgress<string> progress)
        //{
        //    var db = await Db.Context();

        //    var files = await db.FileContents.Where(x => x.ContentVersion > contentAfter).Select(x => x.ContentId).ToListAsync();
        //    var images = await db.ImageContents.Where(x => x.ContentVersion > contentAfter).Select(x => x.ContentId).ToListAsync();
        //    var links = await db.LinkStreams.Where(x => x.ContentVersion > contentAfter).Select(x => x.ContentId).ToListAsync();
        //    var notes = await db.NoteContents.Where(x => x.ContentVersion > contentAfter).Select(x => x.ContentId).ToListAsync();
        //    var photos = await db.PhotoContents.Where(x => x.ContentVersion > contentAfter).Select(x => x.ContentId).ToListAsync();
        //    var posts = await db.PostContents.Where(x => x.ContentVersion > contentAfter).Select(x => x.ContentId).ToListAsync();

        //    var originalContentChanges = files.Concat(images).Concat(links).Concat(notes).Concat(photos).Concat(posts).ToList();

        //    var originalContentSets = originalContentChanges.Partition(500);

        //    foreach (var loopSets in originalContentSets)
        //    {

        //    }
        //}
    }
}