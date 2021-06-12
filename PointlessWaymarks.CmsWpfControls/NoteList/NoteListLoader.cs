using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.ContentList;

namespace PointlessWaymarks.CmsWpfControls.NoteList
{
    public class NoteListLoader : ContentListLoaderBase
    {
        public NoteListLoader(int? partialLoadQuantity) : base("Notes", partialLoadQuantity)
        {
            DataNotificationTypesToRespondTo = new List<DataNotificationContentType> {DataNotificationContentType.Note};
        }

        public override async Task<List<object>> LoadItems(IProgress<string> progress = null)
        {
            var db = await Db.Context();

            if (PartialLoadQuantity != null)
            {
                progress?.Report($"Loading Note Content from DB - Max {PartialLoadQuantity} Items");
                var returnItems = (await db.NoteContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .Take(PartialLoadQuantity.Value).ToListAsync()).Cast<object>().ToList();

                AllItemsLoaded = await db.NoteContents.CountAsync() <= returnItems.Count;

                return returnItems;
            }

            progress?.Report("Loading All Note Content from DB");

            AllItemsLoaded = true;

            return (await db.NoteContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .ToListAsync()).Cast<object>().ToList();
        }
    }
}