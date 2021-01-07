using System;
using System.Linq;
using System.Text.Json;
using HtmlTags;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Database.PointDetailDataModels;
using PointlessWaymarks.CmsData.Html.CommonHtml;
using PointlessWaymarks.CmsData.Spatial;

namespace PointlessWaymarks.CmsData.Html.PointHtml
{
    public static class PointParts
    {
        public static HtmlTag GoogleMapsLatLongLink(PointContentDto point)
        {
            return new LinkTag("Google Maps", GoogleMapsLatLongUrl(point), "point-map-external-link");
        }

        public static string GoogleMapsLatLongUrl(PointContentDto point)
        {
            return $"https://www.google.com/maps/search/?api=1&query={point.Latitude},{point.Longitude}";
        }

        public static HtmlTag OsmCycleMapLatLongLink(PointContentDto point)
        {
            return new LinkTag("OSM Cycle Maps", OsmCycleMapsLatLongUrl(point), "point-map-external-link");
        }

        public static string OsmCycleMapsLatLongUrl(PointContentDto point)
        {
            return $"http://www.openstreetmap.org/?lat={point.Latitude}&lon={point.Longitude}&zoom=13&layers=C";
        }

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

        public static string PointDivAndScript(string pointSlug)
        {
            var divScriptGuidConnector = Guid.NewGuid();

            var tag =
                $"<div id=\"Point-{divScriptGuidConnector}\" class=\"leaflet-container leaflet-retina leaflet-fade-anim leaflet-grab leaflet-touch-drag point-content-map\"></div>";

            var script =
                $"<script>lazyInit(document.querySelector(\"#Point-{divScriptGuidConnector}\"), () => singlePointMapInit(document.querySelector(\"#Point-{divScriptGuidConnector}\"), \"{pointSlug}\"))</script>";

            return tag + script;
        }

        public static HtmlTag PointTextInfoDiv(PointContentDto point)
        {
            var container = new DivTag().AddClass("point-text-info-container");
            var pTag = new HtmlTag("p")
                .Text(
                    $"Lat: {point.Latitude}, Long: {point.Longitude}{(point.Elevation == null ? string.Empty : $", Elevation: {point.Elevation.MetersToFeet()}")}, {OsmCycleMapLatLongLink(point)}, {GoogleMapsLatLongLink(point)}")
                .AddClass("point-location-text").Encoded(false);

            container.Children.Add(pTag);

            return container;
        }
    }
}