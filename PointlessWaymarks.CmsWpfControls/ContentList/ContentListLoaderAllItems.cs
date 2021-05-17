using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Database;

namespace PointlessWaymarks.CmsWpfControls.ContentList
{
    public static class ContentListLoaderAllItems
    {
        public static async Task<bool> AllLoaded(int? threshold)
        {
            if (threshold == null) return true;

            var db = await Db.Context();

            if (await db.FileContents.CountAsync() > threshold) return false;
            if (await db.GeoJsonContents.CountAsync() > threshold) return false;
            if (await db.LineContents.CountAsync() > threshold) return false;
            if (await db.LinkContents.CountAsync() > threshold) return false;
            if (await db.MapComponents.CountAsync() > threshold) return false;
            if (await db.NoteContents.CountAsync() > threshold) return false;
            if (await db.PhotoContents.CountAsync() > threshold) return false;
            if (await db.PointContents.CountAsync() > threshold) return false;
            if (await db.PostContents.CountAsync() > threshold) return false;

            return true;
        }

        public static async Task<List<object>> LoadAll(int? partialLoadItems, IProgress<string> progress = null)
        {
            var listItems = new List<object>();

            var db = await Db.Context();

            if (partialLoadItems != null)
            {
                progress?.Report($"Loading File Content from DB - Max {partialLoadItems} Items");
                listItems.AddRange(
                    await db.FileContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(partialLoadItems.Value).ToListAsync());

                progress?.Report($"Loading GeoJson Content from DB - Max {partialLoadItems} Items");
                listItems.AddRange(
                    await db.GeoJsonContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(partialLoadItems.Value).ToListAsync());

                progress?.Report($"Loading Line Content from DB - Max {partialLoadItems} Items");
                listItems.AddRange(
                    await db.LineContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(partialLoadItems.Value).ToListAsync());

                progress?.Report($"Loading Link Content from DB- Max {partialLoadItems} Items");
                listItems.AddRange(
                    await db.LinkContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(partialLoadItems.Value).ToListAsync());

                progress?.Report($"Loading Map Content from DB- Max {partialLoadItems} Items");
                listItems.AddRange(
                    await db.MapComponents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(partialLoadItems.Value).ToListAsync());

                progress?.Report($"Loading Note Content from DB - Max {partialLoadItems} Items");
                listItems.AddRange(
                    await db.NoteContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(partialLoadItems.Value).ToListAsync());

                progress?.Report($"Loading Photo Content from DB - Max {partialLoadItems} Items");
                listItems.AddRange(
                    await db.PhotoContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(partialLoadItems.Value).ToListAsync());

                progress?.Report($"Loading Point Content from DB - Max {partialLoadItems} Items");
                listItems.AddRange(
                    await db.PointContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(partialLoadItems.Value).ToListAsync());

                progress?.Report($"Loading Post Content from DB - Max {partialLoadItems} Items");
                listItems.AddRange(
                    await db.PostContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(partialLoadItems.Value).ToListAsync());

                return listItems;
            }


            progress?.Report("Loading File Content from DB");
            listItems.AddRange(
                await db.FileContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .ToListAsync());

            progress?.Report("Loading GeoJson Content from DB");
            listItems.AddRange(
                await db.GeoJsonContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .ToListAsync());

            progress?.Report("Loading Line Content from DB");
            listItems.AddRange(
                await db.LineContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .ToListAsync());

            progress?.Report("Loading Link Content from DB");
            listItems.AddRange(
                await db.LinkContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .ToListAsync());

            progress?.Report("Loading Map Content from DB");
            listItems.AddRange(
                await db.MapComponents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .ToListAsync());

            progress?.Report("Loading Note Content from DB");
            listItems.AddRange(
                await db.NoteContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .ToListAsync());

            progress?.Report("Loading Photo Content from DB");
            listItems.AddRange(
                await db.PhotoContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .ToListAsync());

            progress?.Report("Loading Point Content from DB");
            listItems.AddRange(
                await db.PointContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .ToListAsync());

            progress?.Report("Loading Post Content from DB");
            listItems.AddRange(
                await db.PostContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .ToListAsync());

            return listItems;
        }
    }
}