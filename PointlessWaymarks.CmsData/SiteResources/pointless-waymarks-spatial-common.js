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
    return L.tileLayer('https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png',
        {
            maxZoom: 17,
            id: 'osmTopo',
            attribution:
                'Map data: &copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors, <a href="http://viewfinderpanoramas.org">SRTM</a> | Map style: &copy; <a href="https://opentopomap.org">OpenTopoMap</a> (<a href="https://creativecommons.org/licenses/by-sa/3.0/">CC-BY-SA</a>)'
        });
}

function geoJsonLayerStyle(feature) {

    var newStyle = {};

    if (feature.properties.hasOwnProperty('stroke')) newStyle.color = feature.properties['stroke'];
    if (feature.properties.hasOwnProperty('stroke-width')) newStyle.weight = feature.properties['stroke-width'];
    if (feature.properties.hasOwnProperty('stroke-opacity')) newStyle.opacity = feature.properties['stroke-opacity'];
    if (feature.properties.hasOwnProperty('fill')) newStyle.fillColor = feature.properties['fill'];
    if (feature.properties.hasOwnProperty('fill-opacity')) newStyle.fillOpacity = feature.properties['fill-opacity'];

    return newStyle;
}

function onEachMapGeoJsonFeature(feature, layer) {

    var currentUrl = window.location.href;

    if (feature.properties && feature.properties.title) {
        if (feature.properties.link && feature.properties.link.length > 0
            && feature.properties.link !== currentUrl) {
            layer.bindPopup(`<a href="${feature.properties.link}">${feature.properties.title}</a>`);
        } else {
            layer.bindPopup(feature.properties.title);
        }
    }
}

async function singleGeoJsonMapInit(mapElement, contentId) {

    let geoJsonDataResponse = await fetch(`/GeoJson/Data/GeoJson-${contentId}.json`);
    if (!geoJsonDataResponse.ok)
        throw new Error(geoJsonDataResponse.statusText);

    let geoJsonData = await geoJsonDataResponse.json();

    let map = L.map(mapElement,
        {
            layers: [openTopoMapLayer()],
            doubleClickZoom: false,
            gestureHandling: true
        });

    map.fitBounds([
        [geoJsonData.Bounds.InitialViewBoundsMinLatitude, geoJsonData.Bounds.InitialViewBoundsMinLongitude],
        [geoJsonData.Bounds.InitialViewBoundsMaxLatitude, geoJsonData.Bounds.InitialViewBoundsMaxLongitude]
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

    let map = L.map(mapElement,
        {
            layers: [openTopoMapLayer()],
            doubleClickZoom: false,
            gestureHandling: true
        });

    map.fitBounds([
        [lineData.Bounds.InitialViewBoundsMinLatitude, lineData.Bounds.InitialViewBoundsMinLongitude],
        [lineData.Bounds.InitialViewBoundsMaxLatitude, lineData.Bounds.InitialViewBoundsMaxLongitude]
    ]);

    let newMapLayer = new L.geoJSON(lineData.GeoJson, {
        onEachFeature: onEachMapGeoJsonFeature, style: geoJsonLayerStyle
    });

    newMapLayer.addTo(map);
}

async function mapComponentInit(mapElement, contentId) {

    let mapComponentResponse = await fetch(`/Maps/Data/Map-${contentId}.json`);
    if (!mapComponentResponse.ok)
        throw new Error(mapComponentResponse.statusText);

    let mapComponent = await mapComponentResponse.json();

    let map = L.map(mapElement,
        {
            layers: [openTopoMapLayer()],
            doubleClickZoom: false,
            gestureHandling: true,
            closePopupOnClick: false
        });

    map.fitBounds([
        [mapComponent.MapComponent.InitialViewBoundsMinLatitude, mapComponent.MapComponent.InitialViewBoundsMinLongitude],
        [mapComponent.MapComponent.InitialViewBoundsMaxLatitude, mapComponent.MapComponent.InitialViewBoundsMaxLongitude]
    ]);

    if (mapComponent.PointGuids?.length) {

        let response = await fetch('/Points/Data/pointdata.json');
        if (!response.ok)
            throw new Error(response.statusText);

        let pointData = await response.json();

        let includedPoints = pointData.filter(x => mapComponent.PointGuids.includes(x.ContentId));

        for (let pagePoint of includedPoints) {
            const pointContentMarker = new L.marker([pagePoint.Latitude, pagePoint.Longitude],
                {
                    draggable: false,
                    autoPan: true
                }).addTo(map);

            const pointPopup = L.popup({ autoClose: false })
                .setContent(`<a href="https:${pagePoint.PointPageUrl}">${pagePoint.Title}</a>`);
            let boundPopup = pointContentMarker.bindPopup(pointPopup);

            if (mapComponent.ShowDetailsGuid.includes(pagePoint.ContentId)) {
                boundPopup.openPopup();
            }
        };
    }

    if (mapComponent.GeoJsonGuids?.length) {

        for (let loopGeoJson of mapComponent.GeoJsonGuids) {

            let response = await fetch(`/GeoJson/Data/GeoJson-${loopGeoJson}.json`);

            if (!response.ok)
                throw new Error(response.statusText);

            let geoJsonData = await response.json();

            let newMapLayer = new L.geoJSON(geoJsonData.GeoJson, {
                onEachFeature: onEachMapGeoJsonFeature, style: geoJsonLayerStyle
            });

            newMapLayer.addTo(map);
        };
    }

    if (mapComponent.LineGuids?.length) {

        for (let loopGeoJson of mapComponent.LineGuids) {

            let response = await fetch(`/Lines/Data/Line-${loopGeoJson}.json`);

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

    let response = await fetch('/Points/Data/pointdata.json');
    if (!response.ok)
        throw new Error(response.statusText);

    let pointData = await response.json();

    let pagePoint = pointData.filter(x => x.Slug === displayedPointSlug)[0];

    let map = L.map(mapElement,
        {
            center: { lat: pagePoint.Latitude, lng: pagePoint.Longitude },
            zoom: 13,
            layers: [openTopoMapLayer()],
            doubleClickZoom: false,
            gestureHandling: true,
            closePopupOnClick: false
        });

    let pointContentMarker = new L.marker([pagePoint.Latitude, pagePoint.Longitude],
        {
            draggable: false,
            autoPan: true
        }).addTo(map);

    const pointPopup = L.popup({ autoClose: false })
        .setContent(`<a href="https:${pagePoint.PointPageUrl}">${pagePoint.Title}</a>`);
    const boundPopup = pointContentMarker.bindPopup(pointPopup);

    boundPopup.openPopup();

    for (let circlePoint of pointData) {
        if (circlePoint.Slug == displayedPointSlug) continue;
        let toAdd = L.circle([circlePoint.Latitude, circlePoint.Longitude],
            80,
            { color: 'blue', fillColor: 'blue', fillOpacity: .5 });

        const circlePopup = L.popup({ autoClose: false, autoPan: false })
            .setContent(`<a href="https:${circlePoint.PointPageUrl}">${circlePoint.Title}</a>`);
        const boundCirclePopup = toAdd.bindPopup(circlePopup);

        toAdd.addTo(map);

        //boundCirclePopup.openPopup();
    };
}