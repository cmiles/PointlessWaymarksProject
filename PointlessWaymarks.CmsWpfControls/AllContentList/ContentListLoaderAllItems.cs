using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.ContentList;

namespace PointlessWaymarks.CmsWpfControls.AllContentList
{
    public class ContentListLoaderAllItems : ContentListLoaderBase
    {
        public ContentListLoaderAllItems(int? partialLoadQuantity) : base("Items", partialLoadQuantity)
        {
            ShowType = true;
        }

        public override async Task<bool> CheckAllItemsAreLoaded()
        {
            if (PartialLoadQuantity == null) return true;

            var db = await Db.Context();

            if (await db.FileContents.CountAsync() > PartialLoadQuantity) return false;
            if (await db.GeoJsonContents.CountAsync() > PartialLoadQuantity) return false;
            if (await db.LineContents.CountAsync() > PartialLoadQuantity) return false;
            if (await db.LinkContents.CountAsync() > PartialLoadQuantity) return false;
            if (await db.MapComponents.CountAsync() > PartialLoadQuantity) return false;
            if (await db.NoteContents.CountAsync() > PartialLoadQuantity) return false;
            if (await db.PhotoContents.CountAsync() > PartialLoadQuantity) return false;
            if (await db.PointContents.CountAsync() > PartialLoadQuantity) return false;
            if (await db.PostContents.CountAsync() > PartialLoadQuantity) return false;

            return true;
        }

        public override async Task<List<object>> LoadItems(IProgress<string> progress = null)
        {
            var listItems = new List<object>();

            var db = await Db.Context();

            if (PartialLoadQuantity != null)
            {
                progress?.Report($"Loading File Content from DB - Max {PartialLoadQuantity} Items");
                listItems.AddRange(await db.FileContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .Take(PartialLoadQuantity.Value).ToListAsync());

                progress?.Report($"Loading GeoJson Content from DB - Max {PartialLoadQuantity} Items");
                listItems.AddRange(await db.GeoJsonContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .Take(PartialLoadQuantity.Value).ToListAsync());

                progress?.Report($"Loading Line Content from DB - Max {PartialLoadQuantity} Items");
                listItems.AddRange(await db.LineContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .Take(PartialLoadQuantity.Value).ToListAsync());

                progress?.Report($"Loading Link Content from DB- Max {PartialLoadQuantity} Items");
                listItems.AddRange(await db.LinkContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .Take(PartialLoadQuantity.Value).ToListAsync());

                progress?.Report($"Loading Map Content from DB- Max {PartialLoadQuantity} Items");
                listItems.AddRange(await db.MapComponents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .Take(PartialLoadQuantity.Value).ToListAsync());

                progress?.Report($"Loading Note Content from DB - Max {PartialLoadQuantity} Items");
                listItems.AddRange(await db.NoteContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .Take(PartialLoadQuantity.Value).ToListAsync());

                progress?.Report($"Loading Photo Content from DB - Max {PartialLoadQuantity} Items");
                listItems.AddRange(await db.PhotoContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .Take(PartialLoadQuantity.Value).ToListAsync());

                progress?.Report($"Loading Point Content from DB - Max {PartialLoadQuantity} Items");
                listItems.AddRange(await db.PointContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .Take(PartialLoadQuantity.Value).ToListAsync());

                progress?.Report($"Loading Post Content from DB - Max {PartialLoadQuantity} Items");
                listItems.AddRange(await db.PostContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .Take(PartialLoadQuantity.Value).ToListAsync());

                AllItemsLoaded = await CheckAllItemsAreLoaded();

                return listItems;
            }


            progress?.Report("Loading File Content from DB");
            listItems.AddRange(await db.FileContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .ToListAsync());

            progress?.Report("Loading GeoJson Content from DB");
            listItems.AddRange(await db.GeoJsonContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .ToListAsync());

            progress?.Report("Loading Line Content from DB");
            listItems.AddRange(await db.LineContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .ToListAsync());

            progress?.Report("Loading Link Content from DB");
            listItems.AddRange(await db.LinkContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .ToListAsync());

            progress?.Report("Loading Map Content from DB");
            listItems.AddRange(await db.MapComponents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .ToListAsync());

            progress?.Report("Loading Note Content from DB");
            listItems.AddRange(await db.NoteContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .ToListAsync());

            progress?.Report("Loading Photo Content from DB");
            listItems.AddRange(await db.PhotoContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .ToListAsync());

            progress?.Report("Loading Point Content from DB");
            listItems.AddRange(await db.PointContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .ToListAsync());

            progress?.Report("Loading Post Content from DB");
            listItems.AddRange(await db.PostContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .ToListAsync());

            return listItems;
        }
    }
}