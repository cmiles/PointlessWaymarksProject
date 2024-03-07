let globalLineMaps = [];
let globalElevationCharts = [];
let globalElevationChartLineMarkers = [];

const lazyInit = (elementToObserve, fn) => {
    const observer = new IntersectionObserver((entries) => {
        if (entries.some(({ isIntersecting }) => isIntersecting)) {
            observer.disconnect();
            fn();
        }
    });
    observer.observe(elementToObserve);
};

function openTopoMapLayer() {
    return L.tileLayer("https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png",
        {
            maxNativeZoom: 17,
            maxZoom: 22,
            id: "osmTopo",
            attribution:
                'Map data: &copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors, <a href="http://viewfinderpanoramas.org">SRTM</a> | Map style: &copy; <a href="https://opentopomap.org">OpenTopoMap</a> (<a href="https://creativecommons.org/licenses/by-sa/3.0/">CC-BY-SA</a>)'
        });
}

function nationalBaseMapTopoImageMapLayer() {
    return L.tileLayer("https://basemap.nationalmap.gov/arcgis/rest/services/USGSImageryTopo/MapServer/tile/{z}/{y}/{x}",
        {
            maxNativeZoom: 16,
            maxZoom: 22,
            id: "tnmImageTopo",
            attribution: 'Tiles courtesy of the <a href="https://usgs.gov/">U.S. Geological Survey</a>'
        });
}

function nationalBaseMapTopoMapLayer() {
    return L.tileLayer("https://basemap.nationalmap.gov/arcgis/rest/services/USGSTopo/MapServer/tile/{z}/{y}/{x}",
        {
            maxNativeZoom: 16,
            maxZoom: 22,
            id: "tnmTopo",
            attribution: 'Tiles courtesy of the <a href="https://usgs.gov/">U.S. Geological Survey</a>'
        });
}

function geoJsonLayerStyle(feature) {
    //see https://github.com/mapbox/simplestyle-spec/tree/master/1.1.0
    let newStyle = {};

    if (feature.properties.hasOwnProperty("stroke")) newStyle.color = feature.properties["stroke"];
    if (feature.properties.hasOwnProperty("stroke-width")) newStyle.weight = feature.properties["stroke-width"];
    if (feature.properties.hasOwnProperty("stroke-opacity")) newStyle.opacity = feature.properties["stroke-opacity"];
    if (feature.properties.hasOwnProperty("fill")) newStyle.fillColor = feature.properties["fill"];
    if (feature.properties.hasOwnProperty("fill-opacity")) newStyle.fillOpacity = feature.properties["fill-opacity"];

    return newStyle;
}

function popupHtmlContent(feature) {
    if (feature.properties && (feature.properties.title || feature.properties.description)) {
        const currentUrlWithoutProtocol = window.location.href.replace(/https?:/i, "");

        let popupHtml = "";

        if (feature.properties.title) {
            if (feature.properties["title-link"] && feature.properties["title-link"].length > 0
                && feature.properties["title-link"] !== currentUrlWithoutProtocol) {
                popupHtml += `<a style="text-align: center;" href="${feature.properties["title-link"]}">${feature.properties.title}</a>`;
            } else {
                popupHtml += `<p style="text-align: center;">${feature.properties.title}</p>`;
            }
        }

        if (feature.properties.description) {
            popupHtml += `<p style="text-align: center;">${feature.properties.description}</p>`;
        }

        return popupHtml;
    }

    return "";
}

function onEachMapGeoJsonFeatureWrapper(map) {
    return function onEachMapGeoJsonFeature(feature, layer) {
        //see https://github.com/mapbox/simplestyle-spec/tree/master/1.1.0 - title-link is site specific...

        let popupHtml = popupHtmlContent(feature, window.location.href);

        if (popupHtml !== "") {
            layer.bindPopup(popupHtml);
        }

        if (feature.geometry.type === "LineString") {
            let possibleMaps = globalLineMaps.filter(x => x.contentId === feature.properties["content-id"]);
            if (possibleMaps.length === 0) {
                globalLineMaps.push({ "contentId": feature.properties["content-id"], "lineMap": map });
            }

            layer.on('mouseover', function (e) {
                console.log(e);

                let chartLookup = globalElevationCharts.filter(x => x.contentId === e.target.feature.properties["content-id"]);

                if (!chartLookup?.length) {
                    return;
                }

                let chart = chartLookup[0].elevationChart;

                let coordinates = e.target.feature.geometry.coordinates;
                let distanceArray = [];
                for (let i = 0; i < coordinates.length; i++) {
                    distanceArray.push(e.latlng.distanceTo([coordinates[i][1], coordinates[i][0]]));
                }
                let closestPointIndex = distanceArray.indexOf(Math.min.apply(null, distanceArray));

                const tooltip = chart.tooltip;

                const chartArea = chart.chartArea;
                tooltip.setActiveElements([
                    { datasetIndex: 0, index: closestPointIndex, }],
                    { x: (chartArea.left + chartArea.right) / 2, y: (chartArea.top + chartArea.bottom) / 2, });

                chart.update();
            });
        }
    }
}

async function singleGeoJsonMapInit(mapElement, contentId) {

    const geoJsonDataResponse = await fetch(`/ContentData/${contentId}.json`);
    if (!geoJsonDataResponse.ok)
        throw new Error(geoJsonDataResponse.statusText);

    const geoJsonData = await geoJsonDataResponse.json();

    await singleGeoJsonMapInitFromGeoJson(mapElement, geoJsonData);
}

function standardMap(mapElement) {

    let openTopoMap = openTopoMapLayer();
    let tnmTopo = nationalBaseMapTopoMapLayer();
    let tnmImageTopoMap = nationalBaseMapTopoImageMapLayer();

    let map = L.map(mapElement,
        {
            layers: [tnmTopo],
            doubleClickZoom: false,
            gestureHandling: true
        });

    let baseLayers = {
        "TNM Topo": tnmTopo,
        "TNM Topo Image": tnmImageTopoMap,
        "OpenTopo": openTopoMap
    };

    L.control.layers(baseLayers).addTo(map);

    map.addControl(L.control.locate({
        locateOptions: {
            enableHighAccuracy: true
        }
    }));

    return map;
}

async function singleGeoJsonMapInitFromGeoJson(mapElement, geoJsonData) {

    let map = standardMap(mapElement);

    map.fitBounds([
        [geoJsonData.Content.InitialViewBoundsMinLatitude, geoJsonData.Content.InitialViewBoundsMinLongitude],
        [geoJsonData.Content.InitialViewBoundsMaxLatitude, geoJsonData.Content.InitialViewBoundsMaxLongitude]
    ]);

    let newMapLayer = new L.geoJSON(JSON.parse(geoJsonData.Content.GeoJson), {
        onEachFeature: onEachMapGeoJsonFeatureWrapper(map), style: geoJsonLayerStyle
    });

    newMapLayer.addTo(map);
}

async function singleLineMapInit(mapElement, contentId) {

    let lineDataResponse = await fetch(`/ContentData/${contentId}.json`);
    if (!lineDataResponse.ok)
        throw new Error(lineDataResponse.statusText);

    let lineData = await lineDataResponse.json();

    await singleLineMapInitFromLineData(contentId, mapElement, lineData);
}

async function singleLineMapInitFromLineData(contentId, mapElement, lineData) {

    let map = standardMap(mapElement);

    map.fitBounds([
        [lineData.Content.InitialViewBoundsMinLatitude, lineData.Content.InitialViewBoundsMinLongitude],
        [lineData.Content.InitialViewBoundsMaxLatitude, lineData.Content.InitialViewBoundsMaxLongitude]
    ]);

    let newMapLayer = new L.geoJSON(JSON.parse(lineData.Content.Line), {
        onEachFeature: onEachMapGeoJsonFeatureWrapper(map), style: geoJsonLayerStyle
    });

    newMapLayer.addTo(map);

    if (lineData.Content.ShowContentReferencesOnMap) {
        showMapElementsList(map, lineData.MapElements);
    }
}


async function singleLineElevationChartInit(chartCanvas, contentId) {

    let lineDataResponse = await fetch(`/ContentData/${contentId}.json`);
    if (!lineDataResponse.ok)
        throw new Error(lineDataResponse.statusText);

    let lineData = await lineDataResponse.json();

    if (lineData.ElevationPlotData.length === 0) return;

    await singleLineChartInitFromLineData(contentId, chartCanvas, lineData);
}

async function singleLineChartInitFromLineData(contentId, chartCanvas, lineData) {

    //This code is to help give the charts a slight bit more cross chart comparability - so the
    //charts will always end on a multiple of 5 miles and 5,000' of elevation. This is a compromise
    //because the chart won't fill all available space (show max detail) and charts won't always
    //have the same scale, but having worked with this data for years I think this is a very simple
    //compromise that often works out nicely...
    const sourceData = lineData;
    const maxDistanceInMeters = Math.max(...lineData.ElevationPlotData.map(x => x.AccumulatedDistance));
    const distanceFiveMileUnits = Math.floor((maxDistanceInMeters * 0.0006213711922) / 5);
    const distanceMax = (distanceFiveMileUnits + 1) * 5;

    const maxElevationInMeters = Math.max(...lineData.ElevationPlotData.map(x => x.Elevation));
    const elevationFiveThousandFeetUnits = Math.floor((maxElevationInMeters * 3.280839895) / 5000);
    const elevationMax = (elevationFiveThousandFeetUnits + 1) * 5000;

    //Thank you to https://www.geoapify.com/tutorial/draw-route-elevation-profile-with-chartjs for
    //the starting point on this!

    const chartData = {
        labels: lineData.ElevationPlotData.map(x => x.AccumulatedDistance * 0.0006213711922),
        datasets: [{
            data: lineData.ElevationPlotData.map(x => x.Elevation * 3.280839895),
            fill: true,
            borderColor: '#66ccff',
            backgroundColor: '#66ccff66',
            tension: 0.1,
            pointRadius: 0,
            spanGaps: true
        }]
    };

    const config = {
        type: 'line',
        data: chartData,
        plugins: [{
            beforeInit: (chart, args, options) => {
                chart.options.scales.x.min = 0;
                chart.options.scales.x.max = distanceMax;
                chart.options.scales.y.min = 0;
                chart.options.scales.y.max = elevationMax;
            }
        }],
        options: {
            animation: false,
            maintainAspectRatio: false,
            interaction: { intersect: false, mode: 'index' },
            tooltip: { position: 'nearest' },
            scales: {
                x: { type: 'linear' },
                y: { type: 'linear' },
            },
            plugins: {
                title: { align: "center", display: true, text: "Distance: Miles, Elevation: Feet" },
                legend: { display: false },
                tooltip: {
                    displayColors: false,
                    backgroundColor: 'rgba(0, 0, 0, 0.5)',
                    callbacks: {
                        title: (tooltipItems) => {
                            return "Distance: " + parseFloat(tooltipItems[0].label).toFixed(2).toLocaleString() + " miles";
                        },
                        label: (tooltipItem) => {

                            let possibleMaps = globalLineMaps.filter(x => x.contentId === lineData.Content.ContentId);

                            if (possibleMaps?.length) {

                                let connectedMap = possibleMaps[0].lineMap;
                                var location = [lineData.ElevationPlotData[tooltipItem.dataIndex].Latitude, lineData.ElevationPlotData[tooltipItem.dataIndex].Longitude];
                                var feature = JSON.parse(lineData.Content.Line).features[0];

                                setElevationChartLineMarker(connectedMap, feature, location);
                            }

                            return ["Elevation: " + Math.floor(tooltipItem.raw).toLocaleString() + " feet",
                            "Accumulated Climb: " + Math.floor(lineData.ElevationPlotData[tooltipItem.dataIndex].AccumulatedClimb).toLocaleString(),
                            "Accumulated Descent: " + Math.floor(lineData.ElevationPlotData[tooltipItem.dataIndex].AccumulatedDescent).toLocaleString()
                            ];
                        },
                    }
                }
            }
        }
    };

    const chart = new Chart(chartCanvas.getContext("2d"), config);
    globalElevationCharts.push({ "contentId": contentId, "elevationChart": chart });

    chart.canvas.onclick = (e) => {
        const points = chart.getElementsAtEventForMode(e, 'nearest', { intersect: true }, true);
        if (!points?.length) return;

        let possibleMaps = globalLineMaps.filter(x => x.contentId === contentId);
        if (!possibleMaps?.length) {
            return;
        }

        let connectedMap = possibleMaps[0].lineMap;

        let location = [lineData.ElevationPlotData[points[0].index].Latitude, lineData.ElevationPlotData[points[0].index].Longitude];
        connectedMap.flyTo(location);

        let feature = lineData.GeoJson.features[0];

        setElevationChartLineMarker(connectedMap, feature, location);
    };

}

function setElevationChartLineMarker(map, feature, location) {

    let elevationChartLineMarkers = globalElevationChartLineMarkers.filter(x => x.map == map);
    let elevationChartLineMarker;
    if (elevationChartLineMarkers?.length) elevationChartLineMarker = elevationChartLineMarkers[0].marker;

    let featurePopUpContent = popupHtmlContent(feature);
    if (location) featurePopUpContent += `<p>${location}</p>`;

    if (!elevationChartLineMarker) {
        elevationChartLineMarker = L.circle(location, {
            color: '#f03',
            fillColor: '#f03',
            fillOpacity: 0.5,
            radius: 30
        });

        const circlePopup = L.popup({ autoClose: false, autoPan: false }).setContent(featurePopUpContent);
        elevationChartLineMarker.bindPopup(circlePopup);
        elevationChartLineMarker.addTo(map);
        globalElevationChartLineMarkers.push({ "map": map, "marker": elevationChartLineMarker });
    } else {
        elevationChartLineMarker.setLatLng(location);
        elevationChartLineMarker.getPopup().setContent(featurePopUpContent);
    }
}

async function mapComponentInit(mapElement, contentId) {

    let mapComponentResponse = await window.fetch(`/ContentData/${contentId}.json`);
    if (!mapComponentResponse.ok)
        throw new Error(mapComponentResponse.statusText);

    let mapComponent = await mapComponentResponse.json();

    let map = standardMap(mapElement);

    map.fitBounds([
        [mapComponent.Content.InitialViewBoundsMinLatitude, mapComponent.Content.InitialViewBoundsMinLongitude],
        [mapComponent.Content.InitialViewBoundsMaxLatitude, mapComponent.Content.InitialViewBoundsMaxLongitude]
    ]);

    showMapElementsList(map, mapComponent.MapElements);
}

async function showMapElementsList(map, mapElementList) {

    if (mapElementList.PointContentIds != null && mapElementList.PointContentIds.length > 0) {

        for (let loopPoint of mapElementList.PointContentIds) {
            let response = await fetch(`/ContentData/${loopPoint}.json`);
            if (!response.ok)
                throw new Error(response.statusText);

            let pointData = await response.json();

            await AddTextOrCircleMarkerToMap(map, pointData);
        }
    }

    if (mapElementList.PhotoContentIds != null && mapElementList.PhotoContentIds.length > 0) {

        for (let loopPhoto of mapElementList.PhotoContentIds) {

            let response = await fetch(`/ContentData/${loopPhoto}.json`);
            if (!response.ok)
                throw new Error(response.statusText);

            let pointData = await response.json();

            AddPhotoMarkerToMap(map, pointData);
        }
    }

    if (mapElementList.GeoJsonContentIds != null && mapElementList.GeoJsonContentIds.length > 0) {

        for (let loopGeoJson of mapElementList.GeoJsonContentIds) {

            let response = await window.fetch(`/ContentData/${loopGeoJson}.json`);

            if (!response.ok)
                throw new Error(response.statusText);

            let geoJsonData = await response.json();

            let newMapLayer = new L.geoJSON(geoJsonData.Content.GeoJson, {
                onEachFeature: onEachMapGeoJsonFeatureWrapper(map), style: geoJsonLayerStyle
            });

            newMapLayer.addTo(map);
        }
    }

    if (mapElementList.LineContentIds != null && mapElementList.LineContentIds.length > 0) {

        for (let loopGeoJson of mapElementList.LineContentIds) {

            let response = await window.fetch(`/ContentData/${loopGeoJson}.json`);

            if (!response.ok)
                throw new Error(response.statusText);

            let geoJsonData = await response.json();

            let newMapLayer = new L.geoJSON(geoJsonData.Content.GeoJson, {
                onEachFeature: onEachMapGeoJsonFeatureWrapper(map), style: geoJsonLayerStyle
            });

            newMapLayer.addTo(map);
        }
    }
}

async function singlePointMapInit(mapElement, pointContentId) {

    let response = await fetch(`/ContentData/${pointContentId}.json`);
    if (!response.ok)
        throw new Error(response.statusText);

    let pointData = await response.json();

    singlePointMapInitFromPointData(mapElement, pointData);
}

async function singlePointMapInitFromPointData(mapElement, pointData) {

    let pagePoint = pointData.Content;

    var openTopoMap = openTopoMapLayer();
    var tnmTopo = nationalBaseMapTopoMapLayer();
    var tnmImageTopoMap = nationalBaseMapTopoImageMapLayer();

    let map = L.map(mapElement,
        {
            center: { lat: pagePoint.Latitude, lng: pagePoint.Longitude },
            zoom: 13,
            layers: [tnmTopo],
            doubleClickZoom: false,
            gestureHandling: true,
            closePopupOnClick: false
        });

    var baseLayers = {
        "TNM Topo": tnmTopo,
        "TNM Topo Image": tnmImageTopoMap,
        "OpenTopo": openTopoMap
    };

    L.control.layers(baseLayers).addTo(map);

    map.addControl(L.control.locate({
        locateOptions: {
            enableHighAccuracy: true
        }
    }));

    AddMarkerToMap(map, pagePoint);
}

async function AddTextOrCircleMarkerToMap(map, pointToAdd) {

    let popupContent = `<a href="${urlFromContent(pointToAdd)}">${pointToAdd.Content.Title}</a>`;
    if (pointToAdd.Content.MainPicture) {
        let response = await fetch(`/ContentData/${pointToAdd.Content.MainPicture}.json`);
        if (response.ok) {
            let pictureData = await response.json();
            if (pictureData.SmallPictureUrl) popupContent += `<p style="text-align: center;"><img src="${pictureData.SmallPictureUrl}"></img></p>`;
        }
    }
    if (pointToAdd.Content.MainPicture.Summary) popupContent += `<p>${pointToAdd.Content.MainPicture.Summary}</p>`;

    if (pointToAdd.Content.MapLabel) {
        let toAdd = L.marker([pointToAdd.Content.Latitude, pointToAdd.Content.Longitude],
            {
                icon: L.divIcon({
                    className: 'point-map-label',
                    html: pointToAdd.Content.MapLabel,
                    iconAnchor: [0, 0]
                })
            });
        const textMarkerPopup = L.popup({ autoClose: false, autoPan: false })
            .setContent(popupContent);
        const boundTextMarkerPopup = toAdd.bindPopup(textMarkerPopup);
        toAdd.addTo(map);

        let labelMarker = L.circleMarker([pointToAdd.Content.Latitude, pointToAdd.Content.Longitude],
            { radius: 1, color: "blue", fillColor: "blue", fillOpacity: .5 });

        labelMarker.addTo(map);
    } else {
        let toAdd = L.circleMarker([pointToAdd.Content.Latitude, pointToAdd.Content.Longitude],
            { radius: 10, color: "blue", fillColor: "blue", fillOpacity: .5 });

        const circlePopup = L.popup({ autoClose: false, autoPan: false })
            .setContent(popupContent);
        const boundCirclePopup = toAdd.bindPopup(circlePopup);
        toAdd.addTo(map);
    }
}

async function AddMarkerToMap(map, pointToAdd) {

    let popupContent = `<a href="${urlFromContent(pointToAdd)}">${pointToAdd.Content.Title}</a>`;
    if (pointToAdd.Content.MainPicture) {
        let response = await fetch(`/ContentData/${pointToAdd.Content.MainPicture}.json`);
        if (response.ok) {
            let pictureData = await response.json();
            if (pictureData.SmallPictureUrl) popupContent += `<p style="text-align: center;"><img src="${pictureData.SmallPictureUrl}"></img></p>`;
        }
    }
    if (pointToAdd.Content.MainPicture.Summary) popupContent += `<p>${pointToAdd.Content.MainPicture.Summary}</p>`;

    if (pointToAdd.Content.MapLabel) {
        let toAdd = L.marker([pointToAdd.Content.Latitude, pointToAdd.Content.Longitude],
            {
                icon: L.divIcon({
                    className: 'point-map-label',
                    html: pointToAdd.Content.MapLabel,
                    iconAnchor: [0, 0]
                })
            });
        const textMarkerPopup = L.popup({ autoClose: false, autoPan: false })
            .setContent(popupContent);
        const boundTextMarkerPopup = toAdd.bindPopup(textMarkerPopup);
        toAdd.addTo(map);

        let labelMarker = L.marker([pointToAdd.Content.Latitude, pointToAdd.Content.Longitude],
            { draggable: false, autoPan: true, iconAnchor: [0, 0] });

        labelMarker.addTo(map);
    } else {
        let toAdd = L.marker([pointToAdd.Content.Latitude, pointToAdd.Content.Longitude],
            { draggable: false, autoPan: true, iconAnchor: [0, 0] });

        const circlePopup = L.popup({ autoClose: false, autoPan: false })
            .setContent(popupContent);
        toAdd.bindPopup(circlePopup);
        toAdd.addTo(map);
    }
}

function urlFromContent(content) {
    const currentOrigin = window.location.origin;

    if (!content.ContentType) return currentOrigin;
    if (content.ContentType == 'Photo') return `${currentOrigin}/Photos/${content.Content.Slug}/${content.Content.Slug}.html}`;
    if (content.ContentType == 'Point') return `${currentOrigin}/Points/${content.Content.Slug}/${content.Content.Slug}.html}`;

    return currentOrigin;
}

function AddPhotoMarkerToMap(map, photoToAdd) {

    let popupContent = `<a href="${urlFromContent(photoToAdd)}">${photoToAdd.Content.Title}</a>`;
    if (photoToAdd.SmallPictureUrl) popupContent += `<p style="text-align: center;"><img src="${photoToAdd.SmallPictureUrl}"></img></p>`;
    if (photoToAdd.Content.Summary) popupContent += `<p>${photoToAdd.Content.Summary}</p>`;

    let toAdd = L.marker([photoToAdd.Content.Latitude, photoToAdd.Content.Longitude],
        { draggable: false, autoPan: true, iconAnchor: [0, 0] });

    const circlePopup = L.popup({ autoClose: false, autoPan: false })
        .setContent(popupContent);
    toAdd.bindPopup(circlePopup);
    toAdd.addTo(map);
}