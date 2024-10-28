using System.Text;
using DocumentFormat.OpenXml.Vml.Office;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.BracketCodes;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.LineHtml;
using PointlessWaymarks.CmsData.ContentHtml.MapComponentData;
using PointlessWaymarks.CmsData.ContentHtml.PointHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;
using SimMetricsCore;

namespace PointlessWaymarks.CmsData.ContentHtml.TrailHtml;

public static class TrailParts
{
    public static async Task<string> TrailGeneratedInfo(TrailContent dbContent)
    {
        var generatedBlock = new StringBuilder();

        if (dbContent.MapComponentId is not null)
            generatedBlock.AppendLine(MapParts.MapDivAndScript(dbContent.MapComponentId.Value));

        if (dbContent.MapComponentId is null && dbContent.LineContentId is not null)
            generatedBlock.AppendLine(LineParts.LineDivAndScript(dbContent.LineContentId.Value));
        else if (dbContent.LineContentId is not null)
            generatedBlock.AppendLine(LineParts.LineElevationChartDivAndScript(dbContent.LineContentId.Value));

        var db = await Db.Context();

        if (dbContent.LineContentId is not null)
        {
            var line = await db.LineContents.SingleOrDefaultAsync(x => x.ContentId == dbContent.LineContentId);

            if (line is not null)
                generatedBlock.AppendLine(LineParts.LineStatisticsGeneralDisplayDiv(line).ToString());
        }

        var detailsBlock = new StringBuilder();
        detailsBlock.AppendLine();

        var summaryIsInTitle = dbContent.Title.ContainsFuzzy(dbContent.Summary, 0.8, SimMetricType.JaroWinkler);
        var titleIsInSummary = dbContent.Summary.ContainsFuzzy(dbContent.Title, 0.8, SimMetricType.JaroWinkler);

        if (!summaryIsInTitle && !titleIsInSummary)
        {
            detailsBlock.AppendLine($"*{dbContent.Summary}*");
            detailsBlock.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(dbContent.LocationArea))
            detailsBlock.AppendLine($"  - **Location**: {dbContent.LocationArea}");
        detailsBlock.AppendLine(
            $"  - **Fees**: {dbContent.Fee}{(string.IsNullOrWhiteSpace(dbContent.FeeNote) ? string.Empty : $". {dbContent.FeeNote}")}");
        detailsBlock.AppendLine(
            $"  - **Dogs**: {dbContent.Dogs}{(string.IsNullOrWhiteSpace(dbContent.Dogs) ? string.Empty : $". {dbContent.DogsNote}")}");
        detailsBlock.AppendLine(
            $"  - **Bikes**: {dbContent.Bikes}{(string.IsNullOrWhiteSpace(dbContent.Bikes) ? string.Empty : $". {dbContent.BikesNote}")}");
        if (!string.IsNullOrWhiteSpace(dbContent.TrailShape))
            detailsBlock.AppendLine($"  - **Trail Type**: {dbContent.TrailShape}");

        if (!string.IsNullOrWhiteSpace(dbContent.OtherDetails))
        {
            var lines = dbContent.OtherDetails
                .Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            foreach (var loopLines in lines) detailsBlock.AppendLine($"  - {loopLines}");
        }

        generatedBlock.AppendLine($"{Environment.NewLine}{Environment.NewLine}{detailsBlock.ToString()}");

        if (dbContent?.StartingPointContentId != null || dbContent?.EndingPointContentId != null)
        {
            if (dbContent.StartingPointContentId == dbContent.EndingPointContentId)
            {
                var startEndPoint = await Db.PointContentDto(dbContent.StartingPointContentId.Value);

                generatedBlock.AppendLine((await PointParts.StandAlonePointDetailsDiv(startEndPoint, "Start/End:")).ToString());
                //generatedBlock.AppendLine($"  -Start/End{Environment.NewLine}");
                //generatedBlock.AppendLine($"{await BracketCodePointDetails.Create(dbContent.StartingPointContentId.Value)}{Environment.NewLine}{dbContent.BodyContent}");
            }
            else
            {
                if (dbContent.StartingPointContentId is not null)
                {
                    var startPoint = await Db.PointContentDto(dbContent.StartingPointContentId.Value);

                    generatedBlock.AppendLine((await PointParts.StandAlonePointDetailsDiv(startPoint, "Start:")).ToString());

                    //generatedBlock.AppendLine($"  -Start{Environment.NewLine}");
                    //generatedBlock.AppendLine($"{await BracketCodePointDetails.Create(dbContent.StartingPointContentId.Value)}{Environment.NewLine}{dbContent.BodyContent}");
                }

                if (dbContent.EndingPointContentId is not null)
                {
                    var endPoint = await Db.PointContentDto(dbContent.StartingPointContentId.Value);

                    generatedBlock.AppendLine((await PointParts.StandAlonePointDetailsDiv(endPoint, "End:")).ToString());

                    //generatedBlock.AppendLine($"  -End{Environment.NewLine}");
                    //generatedBlock.AppendLine($"{await BracketCodePointDetails.Create(dbContent.EndingPointContentId.Value)}{Environment.NewLine}{dbContent.BodyContent}");
                }

            }
        }

        var finalBlock = generatedBlock.ToString();
        var bracketProcessedBlock =
            await BracketCodeCommon.ProcessCodesForSite(finalBlock).ConfigureAwait(false);
        var processedBlock = ContentProcessing.ProcessContent(bracketProcessedBlock,
            ContentFormatEnum.MarkdigMarkdown01).RemoveOuterPTags();
        
        return processedBlock;
    }
}