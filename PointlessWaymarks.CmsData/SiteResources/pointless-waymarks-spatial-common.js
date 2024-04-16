let globalLineMaps = [];
let globalElevationCharts = [];
let globalElevationChartLineMarkers = [];
let mapIcons;

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

/*Source folder-file-outline - Colton Wiscombe - https://pictogrammers.com/library/mdi/icon/folder-file-outline/ */
const pointlessWaymarksFileIcon = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path d="M4 18H11V20H4C2.9 20 2 19.11 2 18V6C2 4.89 2.89 4 4 4H10L12 6H20C21.1 6 22 6.89 22 8V10.17L20.41 8.59L20 8.17V8H4V18M23 14V21C23 22.11 22.11 23 21 23H15C13.9 23 13 22.11 13 21V12C13 10.9 13.9 10 15 10H19L23 14M21 15H18V12H15V21H21V15Z" /></svg>';

/*Source image-outline - GreenTurtwig - https://pictogrammers.com/library/mdi/icon/image-outline/ */
const pointlessWaymarksImageIcon = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 30 30"><path d="M19,19H5V5H19M19,3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5A2,2 0 0,0 19,3M13.96,12.29L11.21,15.83L9.25,13.47L6.5,17H17.5L13.96,12.29Z" /></svg>';

/*Source: https://raw.githubusercontent.com/nationalparkservice/symbol-library/gh-pages/src/standalone/photography-black-30.svg8*/
const pointlessWaymarksPhotoIcon = `<svg version="1.1" id="Layer_1" xmlns="http://www.w3.org/2000/svg" 
    xmlns:xlink="http://www.w3.org/1999/xlink" x="0px" y="0px"
	 viewBox="0 0 30 30" enable-background="new 0 0 30 30" xml:space="preserve">
        <rect x="4" y="4" width="6" height="2"/>
        <circle cx="10.5" cy="16.5" r="3.5"/>
        <path d="M27,7H3c-1.7,0-3,1.3-3,3v13c0,1.6,1.3,3,3,3h24c1.7,0,3-1.4,3-3V10C30,8.3,28.7,7,27,7z M10.5,23.5c-3.9,0-7-3.1-7-7
	        c0-3.9,3.1-7,7-7s7,3.1,7,7C17.5,20.4,14.4,23.5,10.5,23.5z M26,12h-3c-0.5,0-1-0.5-1-1s0.5-1,1-1h3c0.5,0,1,0.5,1,1S26.5,12,26,12z"/>
</svg>`;

/*Source file-code-outline - Terren - https://pictogrammers.com/library/mdi/icon/file-code-outline/ */
const pointlessWaymarksPostIcon = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 30 30"><path d="M14 2H6C4.89 2 4 2.9 4 4V20C4 21.11 4.89 22 6 22H18C19.11 22 20 21.11 20 20V8L14 2M18 20H6V4H13V9H18V20M9.54 15.65L11.63 17.74L10.35 19L7 15.65L10.35 12.3L11.63 13.56L9.54 15.65M17 15.65L13.65 19L12.38 17.74L14.47 15.65L12.38 13.56L13.65 12.3L17 15.65Z" /></svg>';

/*Source video-outline - Google - https://pictogrammers.com/library/mdi/icon/video-outline/ */
const pointlessWaymarksVideoIcon = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path d="M15,8V16H5V8H15M16,6H4A1,1 0 0,0 3,7V17A1,1 0 0,0 4,18H16A1,1 0 0,0 17,17V13.5L21,17.5V6.5L17,10.5V7A1,1 0 0,0 16,6Z" /></svg>';

/*Source: https://raw.githubusercontent.com/nationalparkservice/symbol-library/gh-pages/src/standalone/dot-black-30.svg*/
const pointlessWaymarksDotIcon = `<svg version="1.1" id="Layer_1" xmlns:sketch="http://www.bohemiancoding.com/sketch/ns"
	 xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" x="0px" y="0px" 
	 viewBox="0 0 30 30" style="enable-background:new 0 0 30 30;" xml:space="preserve">
    <circle  id="Oval-3-Copy-2" sketch:type="MSShapeGroup" cx="15" cy="15" r="8">
    </circle>
</svg>`;

let mapMarkerColorHexLookup = {
    "red": "#d63e2a",
    "darkred": "#a13336",
    "lightred": "#ff8e7f",
    "orange": "#f69730",
    "beige": "#ffcb92",
    "green": "#72b026",
    "darkgreen": "#728224",
    "lightgreen": "#bbf970",
    "blue": "#38aadd",
    "darkblue": "#00649f",
    "lightblue": "#8adaff",
    "purple": "#d152b8",
    "darkpurple": "#5b396b",
    "pink": "#ff91ea",
    "cadetblue": "#436978",
    "white": "#fbfbfb",
    "gray": "#575757",
    "lightgray": "#a3a3a3",
    "black": "#303030",
    "default": "#000"
};

const mapIconColors = ['red', 'darkred', 'lightred', 'orange', 'beige', 'green', 'darkgreen', 'lightgreen', 'blue', 'darkblue', 'lightblue', 'purple', 'darkpurple', 'pink', 'cadetblue', 'white', 'gray', 'lightgray', 'black'];

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

async function standardMap(mapElement) {

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

    let iconsResponse = await fetch("/Points/Data/pwmapicons.json");
    if (iconsResponse.ok) mapIcons = await iconsResponse.json();
    else mapIcons = [];

    return map;
}

async function singleGeoJsonMapInitFromGeoJson(mapElement, geoJsonData) {

    let map = await standardMap(mapElement);

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

    let map = await standardMap(mapElement);

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

    let map = await standardMap(mapElement);

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

            await AddMarkerToMap(map, pointData);
        }
    }

    if (mapElementList.FileContentIds != null && mapElementList.FileContentIds.length > 0) {

        for (let loopPhoto of mapElementList.FileContentIds) {

            let response = await fetch(`/ContentData/${loopPhoto}.json`);
            if (!response.ok)
                throw new Error(response.statusText);

            let pointData = await response.json();

            AddOptionalLocationContentMarkerToMap(map, pointData, 'file');
        }
    }

    if (mapElementList.ImageContentIds != null && mapElementList.ImageContentIds.length > 0) {

        for (let loopImage of mapElementList.ImageContentIds) {

            let response = await fetch(`/ContentData/${loopImage}.json`);
            if (!response.ok)
                throw new Error(response.statusText);

            let pointData = await response.json();

            AddOptionalLocationContentMarkerToMap(map, pointData, 'image');
        }
    }

    if (mapElementList.PhotoContentIds != null && mapElementList.PhotoContentIds.length > 0) {

        for (let loopPhoto of mapElementList.PhotoContentIds) {

            let response = await fetch(`/ContentData/${loopPhoto}.json`);
            if (!response.ok)
                throw new Error(response.statusText);

            let pointData = await response.json();

            AddOptionalLocationContentMarkerToMap(map, pointData, 'photo');
        }
    }

    if (mapElementList.PostContentIds != null && mapElementList.PostContentIds.length > 0) {

        for (let loopPost of mapElementList.PostContentIds) {

            let response = await fetch(`/ContentData/${loopPost}.json`);
            if (!response.ok)
                throw new Error(response.statusText);

            let pointData = await response.json();

            AddOptionalLocationContentMarkerToMap(map, pointData, 'post');
        }
    }

    if (mapElementList.VideoContentIds != null && mapElementList.VideoContentIds.length > 0) {

        for (let loopVideo of mapElementList.VideoContentIds) {

            let response = await fetch(`/ContentData/${loopVideo}.json`);
            if (!response.ok)
                throw new Error(response.statusText);

            let pointData = await response.json();

            AddOptionalLocationContentMarkerToMap(map, pointData, 'video');
        }
    }

    if (mapElementList.GeoJsonContentIds != null && mapElementList.GeoJsonContentIds.length > 0) {

        for (let loopGeoJson of mapElementList.GeoJsonContentIds) {

            let response = await window.fetch(`/ContentData/${loopGeoJson}.json`);

            if (!response.ok)
                throw new Error(response.statusText);

            let geoJsonData = await response.json();

            let newMapLayer = new L.geoJSON(JSON.parse(geoJsonData.Content.GeoJson), {
                onEachFeature: onEachMapGeoJsonFeatureWrapper(map), style: geoJsonLayerStyle
            });

            newMapLayer.addTo(map);
        }
    }

    if (mapElementList.LineContentIds != null && mapElementList.LineContentIds.length > 0) {

        for (let loopLines of mapElementList.LineContentIds) {

            let response = await window.fetch(`/ContentData/${loopLines}.json`);

            if (!response.ok)
                throw new Error(response.statusText);

            let lineData = await response.json();

            let newMapLayer = new L.geoJSON(JSON.parse(lineData.Content.Line), {
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

    let map = await standardMap(mapElement);
    map.setView([pointData.Content.Latitude, pointData.Content.Longitude], 13);

    AddMarkerToMap(map, pointData);
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
    if (pointToAdd.Content.Summary) popupContent += `<p>${pointToAdd.Content.Summary}</p>`;

    const markerPopup = L.popup({ autoClose: false, autoPan: false })
        .setContent(popupContent);

    if (pointToAdd.Content.MapLabel) {

        if (!pointToAdd.Content.MapIconName) {
            let labelMarker = L.circleMarker([pointToAdd.Content.Latitude, pointToAdd.Content.Longitude],
                { radius: 2, color: getMapMarkerHexColor(pointToAdd.Content.MapMarkerColor), fillColor: getMapMarkerHexColor(pointToAdd.Content.MapMarkerColor), fillOpacity: .5 });

            labelMarker.addTo(map);
        }

        let labelText = L.marker([pointToAdd.Content.Latitude, pointToAdd.Content.Longitude],
            {
                icon: L.divIcon({
                    className: 'point-map-label',
                    html: `<p style="font-size: 20px;font-weight: bold; height: auto !important;width: max-content !important;">${pointToAdd.Content.MapLabel}</p>`,
                    iconAnchor: [-4, 26]
                })
            });

        const boundLabelMarkerPopup = labelText.bindPopup(markerPopup);
        labelText.addTo(map);
    }

    if (pointToAdd.Content.MapIconName) {
        let standardMarkerSvg = `data:image/svg+xml;utf8,${getMapIconSvg(pointToAdd.Content.MapIconName)}`;
        let standardMarkerColor = getMapMarkerColor(pointToAdd.Content.MapMarkerColor);

        let standardMarkerToAdd = L.marker([pointToAdd.Content.Latitude, pointToAdd.Content.Longitude],
            {
                icon: L.AwesomeSVGMarkers.icon({
                    svgIcon: standardMarkerSvg,
                    markerColor: standardMarkerColor,
                    iconColor: '#000000'
                })
            });

        const standardLabelMarkerPopup = standardMarkerToAdd.bindPopup(markerPopup);
        standardMarkerToAdd.addTo(map);
    }
}

function urlFromContent(content) {
    const currentOrigin = window.location.origin;

    if (!content.ContentType) return currentOrigin;
    if (content.ContentType == 'Photo') return `${currentOrigin}/Photos/${content.Content.Folder}/${content.Content.Slug}/${content.Content.Slug}.html`;
    if (content.ContentType == 'Point') return `${currentOrigin}/Points/${content.Content.Folder}/${content.Content.Slug}/${content.Content.Slug}.html`;

    return currentOrigin;
}

function AddOptionalLocationContentMarkerToMap(map, optionalLocationContent, contentType) {

    let popupContent = `<a href="${urlFromContent(optionalLocationContent)}">${optionalLocationContent.Content.Title}</a>`;
    if (optionalLocationContent.SmallPictureUrl) popupContent += `<p style="text-align: center;"><img src="${optionalLocationContent.SmallPictureUrl}"></img></p>`;
    if (optionalLocationContent.Content.Summary) popupContent += `<p>${optionalLocationContent.Content.Summary}</p>`;

    let icon = pointlessWaymarksDotIcon;
    if (contentType === 'file') icon = pointlessWaymarksFileIcon;
    else if (contentType == 'image') icon = pointlessWaymarksImageIcon;
    else if (contentType == 'photo') icon = pointlessWaymarksPhotoIcon;
    else if (contentType == 'post') icon = pointlessWaymarksPostIcon;
    else if (contentType == 'video') icon = pointlessWaymarksVideoIcon;

    let toAdd = L.marker([optionalLocationContent.Content.Latitude, optionalLocationContent.Content.Longitude],
        {
            icon: L.AwesomeSVGMarkers.icon({
                svgIcon: `data:image/svg+xml;utf8,${icon}`,
                markerColor: 'blue', iconColor: '#000000'
            })
        });

    const markerPopup = L.popup({ autoClose: false, autoPan: false })
        .setContent(popupContent);
    toAdd.bindPopup(markerPopup);
    toAdd.addTo(map);
}

function getMapMarkerColor(iconName) {
    if (!iconName) return 'blue';
    if (mapIconColors.includes(iconName)) return iconName;
    return 'blue';
}

function getMapIconSvg(iconName) {
    if (!iconName) return pointlessWaymarksDotIcon;
    var possibleMapJsonIcons = mapIcons.filter(x => x.IconName === iconName);
    if (possibleMapJsonIcons.length === 0) return pointlessWaymarksDotIcon;
    return possibleMapJsonIcons[0].IconSvg;
}

function getMapMarkerHexColor(colorName) {
    if (!colorName) return mapMarkerColorHexLookup["default"];
    var possibleColor = mapMarkerColorHexLookup[colorName];
    if (!possibleColor) return mapMarkerColorHexLookup["default"];
    return possibleColor;
}