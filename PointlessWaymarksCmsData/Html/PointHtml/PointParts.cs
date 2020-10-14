using System.Linq;
using System.Text.Json;
using HtmlTags;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Database.PointDetailDataModels;
using PointlessWaymarksCmsData.Html.CommonHtml;

namespace PointlessWaymarksCmsData.Html.PointHtml
{
    public static class PointParts
    {
        public static HtmlTag PointDetailsDiv(PointContentDto dbEntry)
        {
            if (dbEntry?.PointDetails == null && !dbEntry.PointDetails.Any()) return HtmlTag.Empty();

            var containerDiv = new DivTag().AddClass("point-detail-list-container");

            foreach (var loopDetail in dbEntry.PointDetails)
            {
                var outerDiv = new DivTag().AddClass("point-detail-container");
                var typeLine = new HtmlTag("p").Text(loopDetail.DataType).AddClass("point-detail-type");
                outerDiv.Children.Add(typeLine);

                switch (loopDetail.DataType)
                {
                    case "Campground":
                    {
                        var pointDetails = JsonSerializer.Deserialize<Campground>(loopDetail.StructuredDataAsJson);

                        if (pointDetails == null) return outerDiv;

                        var infoList = new HtmlTag("ul").AddClass("point-detail-info-list");

                        if (pointDetails.Fee != null)
                            infoList.Children.Add(new HtmlTag("li").Text($"Fee: {pointDetails.Fee}"));

                        if (!string.IsNullOrEmpty(pointDetails.Notes))
                        {
                            var noteText = ContentProcessing.ProcessContent(
                                BracketCodeCommon.ProcessCodesForSite(pointDetails.Notes, null),
                                pointDetails.NotesContentFormat);

                            infoList.Children.Add(new HtmlTag("li").Encoded(false).Text(noteText));
                        }

                        outerDiv.Children.Add(infoList);

                        break;
                    }
                    case "Parking":
                    {
                        var pointDetails = JsonSerializer.Deserialize<Parking>(loopDetail.StructuredDataAsJson);

                        if (pointDetails == null) return outerDiv;

                        var infoList = new HtmlTag("ul").AddClass("point-detail-info-list");

                        if (pointDetails.Fee != null)
                            infoList.Children.Add(new HtmlTag("li").Text($"Fee: {pointDetails.Fee}"));

                        if (!string.IsNullOrEmpty(pointDetails.Notes))
                        {
                            var noteText = ContentProcessing.ProcessContent(
                                BracketCodeCommon.ProcessCodesForSite(pointDetails.Notes, null),
                                pointDetails.NotesContentFormat);

                            infoList.Children.Add(new HtmlTag("li").Encoded(false).Text(noteText));
                        }

                        outerDiv.Children.Add(infoList);

                        break;
                    }
                    case "Feature":
                    {
                        var pointDetails = JsonSerializer.Deserialize<Feature>(loopDetail.StructuredDataAsJson);

                        if (pointDetails == null) return outerDiv;

                        typeLine.Text($"Point Detail: {pointDetails.Type}");

                        var infoList = new HtmlTag("ul").AddClass("point-detail-info-list");

                        if (!string.IsNullOrEmpty(pointDetails.Notes))
                        {
                            var noteText = ContentProcessing.ProcessContent(
                                BracketCodeCommon.ProcessCodesForSite(pointDetails.Notes, null),
                                pointDetails.NotesContentFormat);

                            infoList.Children.Add(new HtmlTag("li").Encoded(false).Text(noteText));
                        }

                        outerDiv.Children.Add(infoList);

                        break;
                    }
                    case "Peak":
                    {
                        var pointDetails = JsonSerializer.Deserialize<Peak>(loopDetail.StructuredDataAsJson);

                        if (pointDetails == null) return outerDiv;

                        var infoList = new HtmlTag("ul").AddClass("point-detail-info-list");

                        if (!string.IsNullOrEmpty(pointDetails.Notes))
                        {
                            var noteText = ContentProcessing.ProcessContent(
                                BracketCodeCommon.ProcessCodesForSite(pointDetails.Notes, null),
                                pointDetails.NotesContentFormat);

                            infoList.Children.Add(new HtmlTag("li").Encoded(false).Text(noteText));
                        }

                        outerDiv.Children.Add(infoList);

                        break;
                    }
                    case "Restroom":
                    {
                        var pointDetails = JsonSerializer.Deserialize<Restroom>(loopDetail.StructuredDataAsJson);

                        if (pointDetails == null) return outerDiv;

                        var infoList = new HtmlTag("ul").AddClass("point-detail-info-list");

                        if (!string.IsNullOrEmpty(pointDetails.Notes))
                        {
                            var noteText = ContentProcessing.ProcessContent(
                                BracketCodeCommon.ProcessCodesForSite(pointDetails.Notes, null),
                                pointDetails.NotesContentFormat);

                            infoList.Children.Add(new HtmlTag("li").Encoded(false).Text(noteText));
                        }

                        outerDiv.Children.Add(infoList);

                        break;
                    }
                    case "TrailJunction":
                    {
                        var pointDetails = JsonSerializer.Deserialize<TrailJunction>(loopDetail.StructuredDataAsJson);

                        if (pointDetails == null) return outerDiv;

                        var infoList = new HtmlTag("ul").AddClass("point-detail-info-list");

                        if (pointDetails.Sign != null)
                            infoList.Children.Add(
                                new HtmlTag("li").Text(pointDetails.Sign.Value ? "Signed" : "No Sign"));

                        if (!string.IsNullOrEmpty(pointDetails.Notes))
                        {
                            var noteText = ContentProcessing.ProcessContent(
                                BracketCodeCommon.ProcessCodesForSite(pointDetails.Notes, null),
                                pointDetails.NotesContentFormat);

                            infoList.Children.Add(new HtmlTag("li").Encoded(false).Text(noteText));
                        }

                        outerDiv.Children.Add(infoList);

                        break;
                    }
                }

                containerDiv.Children.Add(outerDiv);
            }

            return containerDiv;
        }
    }
}