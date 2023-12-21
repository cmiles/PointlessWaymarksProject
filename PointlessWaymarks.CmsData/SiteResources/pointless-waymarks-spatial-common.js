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
    var newStyle = {};

    if (feature.properties.hasOwnProperty("stroke")) newStyle.color = feature.properties["stroke"];
    if (feature.properties.hasOwnProperty("stroke-width")) newStyle.weight = feature.properties["stroke-width"];
    if (feature.properties.hasOwnProperty("stroke-opacity")) newStyle.opacity = feature.properties["stroke-opacity"];
    if (feature.properties.hasOwnProperty("fill")) newStyle.fillColor = feature.properties["fill"];
    if (feature.properties.hasOwnProperty("fill-opacity")) newStyle.fillOpacity = feature.properties["fill-opacity"];

    return newStyle;
}

function onEachMapGeoJsonFeature(feature, layer) {

    //see https://github.com/mapbox/simplestyle-spec/tree/master/1.1.0 - title-link is site specific...

    const currentUrl = window.location.href.replace(/https?:/i, "");

    if (feature.properties && (feature.properties.title || feature.properties.description)) {
        let popupHtml = "";

        if (feature.properties.title) {
            if (feature.properties["title-link"] && feature.properties["title-link"].length > 0
                && feature.properties["title-link"] !== currentUrl) {
                popupHtml += `<a href="${feature.properties["title-link"]}">${feature.properties.title}</a>`;
            } else {
                popupHtml += feature.properties.title;
            }
        }

        if (feature.properties.description) {
            popupHtml += `<p>${feature.properties.description}</p>`;
        }

        if (popupHtml !== "") layer.bindPopup(popupHtml);
    }
}

async function singleGeoJsonMapInit(mapElement, contentId) {

    const geoJsonDataResponse = await fetch(`/GeoJson/Data/GeoJson-${contentId}.json`);
    if (!geoJsonDataResponse.ok)
        throw new Error(geoJsonDataResponse.statusText);

    const geoJsonData = await geoJsonDataResponse.json();

    singleGeoJsonMapInitFromGeoJson(mapElement, geoJsonData);
}

function standardMap(mapElement) {

    var openTopoMap = openTopoMapLayer();
    var tnmTopo = nationalBaseMapTopoMapLayer();
    var tnmImageTopoMap = nationalBaseMapTopoImageMapLayer();

    let map = L.map(mapElement,
        {
            layers: [tnmTopo],
            doubleClickZoom: false,
            gestureHandling: true
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

    return map;
}

async function singleGeoJsonMapInitFromGeoJson(mapElement, geoJsonData) {

    let map = standardMap(mapElement);

    map.fitBounds([
        [geoJsonData.Bounds.MinLatitude, geoJsonData.Bounds.MinLongitude],
        [geoJsonData.Bounds.MaxLatitude, geoJsonData.Bounds.MaxLongitude]
    ]);

    let newMapLayer = new L.geoJSON(geoJsonData.GeoJson, {
        onEachFeature: onEachMapGeoJsonFeature, style: geoJsonLayerStyle
    });

    newMapLayer.addTo(map);
}

async function singleLineMapInit(mapElement, contentId) {

    let lineDataResponse = await fetch(`/Lines/Data/Line-${contentId}.json`);
    if (!lineDataResponse.ok)
        throw new Error(lineDataResponse.statusText);

    let lineData = await lineDataResponse.json();

    singleLineMapInitFromLineData(mapElement, lineData);
}

async function singleLineMapInitFromLineData(mapElement, lineData) {

    let map = standardMap(mapElement);

    map.fitBounds([
        [lineData.Bounds.MinLatitude, lineData.Bounds.MinLongitude],
        [lineData.Bounds.MaxLatitude, lineData.Bounds.MaxLongitude]
    ]);

    let newMapLayer = new L.geoJSON(lineData.GeoJson, {
        onEachFeature: onEachMapGeoJsonFeature, style: geoJsonLayerStyle
    });

    newMapLayer.addTo(map);
}


async function singleLineElevationChartInit(chartCanvas, contentId) {

    let lineDataResponse = await fetch(`/Lines/Data/Line-${contentId}.json`);
    if (!lineDataResponse.ok)
        throw new Error(lineDataResponse.statusText);

    let lineData = await lineDataResponse.json();

    if (lineData.ElevationPlotData.length === 0) return;

    singleLineChartInitFromLineData(chartCanvas, lineData);
}

async function singleLineChartInitFromLineData(chartCanvas, lineData) {

    //This code is to help give the charts a slight bit more cross chart comparability - so the
    //charts will alway end on a multiple of 5 miles and 5,000' of elevation. This is a compromise
    //because the chart won't fill all available space (show max detail) and charts won't always
    //have the same scale, but having worked with this data for years I think this is a very simple
    //compromise that often works out nicely...
    const maxDistanceInMeters = Math.max(...lineData.ElevationPlotData.map(x => x.DistanceFromOrigin));
    const distanceFiveMileUnits = Math.floor((maxDistanceInMeters * 0.0006213711922) / 5);
    const distanceMax = (distanceFiveMileUnits + 1) * 5;

    const maxElevationInMeters = Math.max(...lineData.ElevationPlotData.map(x => x.Elevation));
    const elevationFiveThousandFeetUnits = Math.floor((maxElevationInMeters * 3.280839895) / 5000);
    const elevationMax = (elevationFiveThousandFeetUnits + 1) * 5000;

    //Thank you to https://www.geoapify.com/tutorial/draw-route-elevation-profile-with-chartjs for
    //the starting point on this!

    const chartData = {
        labels: lineData.ElevationPlotData.map(x => x.DistanceFromOrigin * 0.0006213711922),
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
                    callbacks: {
                        title: (tooltipItems) => {
                            return "Distance: " + parseFloat(tooltipItems[0].label).toFixed(2).toLocaleString() + ' miles'
                        },
                        label: (tooltipItem) => {
                            return "Elevation: " + Math.floor(tooltipItem.raw).toLocaleString() + ' feet'
                        },
                    }
                }
            }
        }
    };

    const chart = new Chart(chartCanvas.getContext("2d"), config);
}

async function mapComponentInit(mapElement, contentId) {

    let mapComponentResponse = await window.fetch(`/Maps/Data/Map-${contentId}.json`);
    if (!mapComponentResponse.ok)
        throw new Error(mapComponentResponse.statusText);

    let mapComponent = await mapComponentResponse.json();

    let map = standardMap(mapElement);

    map.fitBounds([
        [mapComponent.MapComponent.InitialViewBoundsMinLatitude, mapComponent.MapComponent.InitialViewBoundsMinLongitude],
        [mapComponent.MapComponent.InitialViewBoundsMaxLatitude, mapComponent.MapComponent.InitialViewBoundsMaxLongitude]
    ]);

    if (mapComponent.PointGuids != null && mapComponent.PointGuids.length > 0) {

        let response = await fetch("/Points/Data/pointdata.json");
        if (!response.ok)
            throw new Error(response.statusText);

        let pointData = await response.json();

        let includedPoints = pointData.filter(x => mapComponent.PointGuids.includes(x.ContentId));

        for (let pagePoint of includedPoints) {
            AddTextOrCircleMarkerToMap(map, pagePoint);
        };
    }

    if (mapComponent.GeoJsonGuids != null && mapComponent.GeoJsonGuids.length > 0) {

        for (let loopGeoJson of mapComponent.GeoJsonGuids) {

            let response = await window.fetch(`/GeoJson/Data/GeoJson-${loopGeoJson}.json`);

            if (!response.ok)
                throw new Error(response.statusText);

            let geoJsonData = await response.json();

            let newMapLayer = new L.geoJSON(geoJsonData.GeoJson, {
                onEachFeature: onEachMapGeoJsonFeature, style: geoJsonLayerStyle
            });

            newMapLayer.addTo(map);
        };
    }

    if (mapComponent.LineGuids != null && mapComponent.LineGuids.length > 0) {

        for (let loopGeoJson of mapComponent.LineGuids) {

            let response = await window.fetch(`/Lines/Data/Line-${loopGeoJson}.json`);

            if (!response.ok)
                throw new Error(response.statusText);

            let geoJsonData = await response.json();

            let newMapLayer = new L.geoJSON(geoJsonData.GeoJson, {
                onEachFeature: onEachMapGeoJsonFeature, style: geoJsonLayerStyle
            });

            newMapLayer.addTo(map);
        };
    }
}

async function singlePointMapInit(mapElement, displayedPointSlug) {

    let response = await fetch("/Points/Data/pointdata.json");
    if (!response.ok)
        throw new Error(response.statusText);

    let pointData = await response.json();

    singlePointMapInitFromPointData(mapElement, displayedPointSlug, pointData);
};

async function singlePointMapInitFromPointData(mapElement, displayedPointSlug, pointData) {

    let pagePoint = pointData.filter(x => x.Slug === displayedPointSlug)[0];

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

    for (let circlePoint of pointData) {
        if (circlePoint.Slug == displayedPointSlug) continue;

        AddTextOrCircleMarkerToMap(map, circlePoint);
    };
}

function AddTextOrCircleMarkerToMap(map, pointToAdd) {

    if (pointToAdd.MapLabel) {
        let toAdd = L.marker([pointToAdd.Latitude, pointToAdd.Longitude],
            {
                icon: L.divIcon({
                    className: 'point-map-label',
                    html: pointToAdd.MapLabel,
                    iconAnchor: [-6, 12]
                })
            });
        const textMarkerPopup = L.popup({ autoClose: false, autoPan: false })
            .setContent(`<a href="${pointToAdd.PointPageUrl}">${pointToAdd.Title}</a><p>${pointToAdd.Summary}</p>`);
        const boundTextMarkerPopup = toAdd.bindPopup(textMarkerPopup);
        toAdd.addTo(map);

        let labelMarker = L.circleMarker([pointToAdd.Latitude, pointToAdd.Longitude],
            { radius: 1, color: "blue", fillColor: "blue", fillOpacity: .5 });

        labelMarker.addTo(map);
    }
    else {
        let toAdd = L.circleMarker([pointToAdd.Latitude, pointToAdd.Longitude],
            { radius: 10, color: "blue", fillColor: "blue", fillOpacity: .5 });

        const circlePopup = L.popup({ autoClose: false, autoPan: false })
            .setContent(`<a href="${pointToAdd.PointPageUrl}">${pointToAdd.Title}</a><p>${pointToAdd.Summary}</p>`);
        const boundCirclePopup = toAdd.bindPopup(circlePopup);
        toAdd.addTo(map);
    }
}

function AddMarkerToMap(map, pointToAdd) {

    if (pointToAdd.MapLabel) {
        let toAdd = L.marker([pointToAdd.Latitude, pointToAdd.Longitude],
            {
                icon: L.divIcon({
                    className: 'point-map-label',
                    html: pointToAdd.MapLabel,
                    iconAnchor: [0, 0]
                })
            });
        const textMarkerPopup = L.popup({ autoClose: false, autoPan: false })
            .setContent(`<a href="${pointToAdd.PointPageUrl}">${pointToAdd.Title}</a><p>${pointToAdd.Summary}</p>`);
        const boundTextMarkerPopup = toAdd.bindPopup(textMarkerPopup);
        toAdd.addTo(map);

        let labelMarker = L.marker([pointToAdd.Latitude, pointToAdd.Longitude],
            { draggable: false, autoPan: true, iconAnchor: [0, 0] });

        labelMarker.addTo(map);
    }
    else {
        let toAdd = L.marker([pointToAdd.Latitude, pointToAdd.Longitude],
            { draggable: false, autoPan: true, iconAnchor: [0, 0] });

        const circlePopup = L.popup({ autoClose: false, autoPan: false })
            .setContent(`<a href="${pointToAdd.PointPageUrl}">${pointToAdd.Title}</a><p>${pointToAdd.Summary}</p>`);
        const boundCirclePopup = toAdd.bindPopup(circlePopup);
        toAdd.addTo(map);
    }
}