using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.ContentList;

namespace PointlessWaymarks.CmsWpfControls.AllContentList;

public class AllContentListLoader : ContentListLoaderBase
{
    private int _partialLoadCount;

    public AllContentListLoader(int? partialLoadQuantity) : base("Items", partialLoadQuantity)
    {
        ShowType = true;
    }

    public override async Task<List<object>> LoadItems(IProgress<string>? progress = null)
    {
        _partialLoadCount = 0;

        if (PartialLoadQuantity != null)
        {
            var itemBag = new ConcurrentBag<List<object>>();

            var taskSet = new List<Func<Task>>
            {
                async () =>
                {
                    var funcDb = await Db.Context();
                    progress?.Report($"Loading File Content from DB - Max {PartialLoadQuantity} Items");
                    var dbItems = await funcDb.FileContents
                        .OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(PartialLoadQuantity.Value).Cast<object>().ToListAsync();
                    itemBag.Add(dbItems);
                    if (await funcDb.FileContents.CountAsync() > dbItems.Count)
                        Interlocked.Increment(ref _partialLoadCount);
                },
                async () =>
                {
                    var funcDb = await Db.Context();
                    progress?.Report($"Loading GeoJson Content from DB - Max {PartialLoadQuantity} Items");
                    var dbItems = await funcDb.GeoJsonContents
                        .OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(PartialLoadQuantity.Value).Cast<object>().ToListAsync();
                    itemBag.Add(dbItems);
                    if (await funcDb.GeoJsonContents.CountAsync() > dbItems.Count)
                        Interlocked.Increment(ref _partialLoadCount);
                },
                async () =>
                {
                    var funcDb = await Db.Context();
                    progress?.Report($"Loading Image Content from DB - Max {PartialLoadQuantity} Items");
                    var dbItems = await funcDb.ImageContents
                        .OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(PartialLoadQuantity.Value).Cast<object>().ToListAsync();
                    itemBag.Add(dbItems);
                    if (await funcDb.ImageContents.CountAsync() > dbItems.Count)
                        Interlocked.Increment(ref _partialLoadCount);
                },
                async () =>
                {
                    var funcDb = await Db.Context();
                    progress?.Report($"Loading Line Content from DB - Max {PartialLoadQuantity} Items");
                    var dbItems = await funcDb.LineContents
                        .OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(PartialLoadQuantity.Value).Cast<object>().ToListAsync();
                    itemBag.Add(dbItems);
                    if (await funcDb.LineContents.CountAsync() > dbItems.Count)
                        Interlocked.Increment(ref _partialLoadCount);
                },
                async () =>
                {
                    var funcDb = await Db.Context();
                    progress?.Report($"Loading Link Content from DB- Max {PartialLoadQuantity} Items");
                    var dbItems = await funcDb.LinkContents
                        .OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(PartialLoadQuantity.Value).Cast<object>().ToListAsync();
                    itemBag.Add(dbItems);
                    if (await funcDb.LinkContents.CountAsync() > dbItems.Count)
                        Interlocked.Increment(ref _partialLoadCount);
                },
                async () =>
                {
                    var funcDb = await Db.Context();
                    progress?.Report($"Loading Map Content from DB- Max {PartialLoadQuantity} Items");
                    var dbItems = await funcDb.MapComponents
                        .OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(PartialLoadQuantity.Value).Cast<object>().ToListAsync();
                    itemBag.Add(dbItems);
                    if (await funcDb.MapComponents.CountAsync() > dbItems.Count)
                        Interlocked.Increment(ref _partialLoadCount);
                },
                async () =>
                {
                    var funcDb = await Db.Context();
                    progress?.Report($"Loading Note Content from DB - Max {PartialLoadQuantity} Items");
                    var dbItems = await funcDb.NoteContents
                        .OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(PartialLoadQuantity.Value).Cast<object>().ToListAsync();
                    itemBag.Add(dbItems);
                    if (await funcDb.NoteContents.CountAsync() > dbItems.Count)
                        Interlocked.Increment(ref _partialLoadCount);
                },
                async () =>
                {
                    var funcDb = await Db.Context();
                    progress?.Report($"Loading Photo Content from DB - Max {PartialLoadQuantity} Items");
                    var dbItems = await funcDb.PhotoContents
                        .OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(PartialLoadQuantity.Value).Cast<object>().ToListAsync();
                    itemBag.Add(dbItems);
                    if (await funcDb.PhotoContents.CountAsync() > dbItems.Count)
                        Interlocked.Increment(ref _partialLoadCount);
                },
                async () =>
                {
                    var funcDb = await Db.Context();
                    progress?.Report($"Loading Point Content from DB - Max {PartialLoadQuantity} Items");
                    var dbItems = await funcDb.PointContents
                        .OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(PartialLoadQuantity.Value).Cast<object>().ToListAsync();
                    itemBag.Add(dbItems);
                    if (await funcDb.PointContents.CountAsync() > dbItems.Count)
                        Interlocked.Increment(ref _partialLoadCount);
                },
                async () =>
                {
                    var funcDb = await Db.Context();
                    progress?.Report($"Loading Post Content from DB - Max {PartialLoadQuantity} Items");
                    var dbItems = await funcDb.PostContents
                        .OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(PartialLoadQuantity.Value).Cast<object>().ToListAsync();
                    itemBag.Add(dbItems);
                    if (await funcDb.PostContents.CountAsync() > dbItems.Count)
                        Interlocked.Increment(ref _partialLoadCount);
                },
                async () =>
                {
                    var funcDb = await Db.Context();
                    progress?.Report($"Loading Video Content from DB - Max {PartialLoadQuantity} Items");
                    var dbItems = await funcDb.VideoContents
                        .OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(PartialLoadQuantity.Value).Cast<object>().ToListAsync();
                    itemBag.Add(dbItems);
                    if (await funcDb.VideoContents.CountAsync() > dbItems.Count)
                        Interlocked.Increment(ref _partialLoadCount);
                }
            };

            await Parallel.ForEachAsync(taskSet, async (x, _) => await x()).ConfigureAwait(false);

            AllItemsLoaded = _partialLoadCount == 0;

            return itemBag.SelectMany(x => x.Select(y => y)).ToList();
        }

        var listItems = new List<object>();
        var db = await Db.Context();

        progress?.Report("Loading File Content from DB");
        listItems.AddRange(await db.FileContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
            .ToListAsync());

        progress?.Report("Loading GeoJson Content from DB");
        listItems.AddRange(await db.GeoJsonContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
            .ToListAsync());

        progress?.Report("Loading Image Content from DB");
        listItems.AddRange(await db.ImageContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
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

        AllItemsLoaded = true;

        return listItems;
    }
}