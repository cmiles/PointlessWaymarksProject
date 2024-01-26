using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.WpfCommon.ColumnSort;

namespace PointlessWaymarks.CmsWpfControls.LineList;

public class LineListLoader : ContentListLoaderBase, IContentListLoader
{
    public LineListLoader(int? partialLoadQuantity) : base("Lines", partialLoadQuantity)
    {
        DataNotificationTypesToRespondTo = [DataNotificationContentType.Line];
    }

    public override async Task<List<object>> LoadItems(IProgress<string>? progress = null)
    {
        var db = await Db.Context();

        if (PartialLoadQuantity != null)
        {
            progress?.Report($"Loading Line Content from DB - Max {PartialLoadQuantity} Items");
            var returnItems = (await db.LineContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .Take(PartialLoadQuantity.Value).ToListAsync()).Cast<object>().ToList();

            AllItemsLoaded = await db.LineContents.CountAsync() <= returnItems.Count;

            return returnItems;
        }

        progress?.Report("Loading All Line Content from DB");

        AllItemsLoaded = true;

        return (await db.LineContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
            .ToListAsync()).Cast<object>().ToList();
    }

    public ColumnSortControlContext SortContext()
    {
        return SortContextLineDefault();
    }

    public static ColumnSortControlContext SortContextLineDefault()
    {
        return new ColumnSortControlContext
        {
            Items =
            [
                new()
                {
                    DisplayName = "Updated",
                    ColumnName = "DbEntry.LatestUpdate",
                    Order = 1,
                    DefaultSortDirection = ListSortDirection.Descending
                },

                new()
                {
                    DisplayName = "Recorded On",
                    ColumnName = "DbEntry.RecordingStartedOn",
                    DefaultSortDirection = ListSortDirection.Descending
                },

                new()
                {
                    DisplayName = "Title",
                    ColumnName = "DbEntry.Title",
                    DefaultSortDirection = ListSortDirection.Ascending
                },

                new()
                {
                    DisplayName = "Distance",
                    ColumnName = "DbEntry.LineDistance",
                    DefaultSortDirection = ListSortDirection.Ascending
                },

                new()
                {
                    DisplayName = "Climb",
                    ColumnName = "DbEntry.ClimbElevation",
                    DefaultSortDirection = ListSortDirection.Ascending
                },
                    
                new()
                {
                    DisplayName = "Descent",
                    ColumnName = "DbEntry.DescentElevation",
                    DefaultSortDirection = ListSortDirection.Ascending
                },

                new()
                {
                    DisplayName = "Max Elevation",
                    ColumnName = "DbEntry.MaximumElevation",
                    DefaultSortDirection = ListSortDirection.Ascending
                },
                
                new()
                {
                    DisplayName = "Min Elevation",
                    ColumnName = "DbEntry.MinimumElevation",
                    DefaultSortDirection = ListSortDirection.Ascending
                },
                
                new()
                {
                    DisplayName = "Time",
                    ColumnName = "RecordedOnLengthInMinutes",
                    DefaultSortDirection = ListSortDirection.Ascending
                }
            ]
        };
    }
}