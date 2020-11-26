const lazyInit = (elementToObserve, fn) => {
    const observer = new IntersectionObserver((entries) => {
        if (entries.some(({ isIntersecting }) => isIntersecting)) {
            observer.disconnect();
            fn();
        }
    });
    observer.observe(elementToObserve);
};

async function mapComponentInit(mapElement, contentId) {
    var openTopoMap = L.tileLayer('https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png',
        {
            maxZoom: 17,
            id: 'osmTopo',
            attribution:
                'Map data: &copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors, <a href="http://viewfinderpanoramas.org">SRTM</a> | Map style: &copy; <a href="https://opentopomap.org">OpenTopoMap</a> (<a href="https://creativecommons.org/licenses/by-sa/3.0/">CC-BY-SA</a>)'
        });

    const mapComponentResponse = await fetch(`/Maps/Data/Map-${contentId}.json`);
    if (!mapComponentResponse.ok)
        throw new Error(mapComponentResponse.statusText);

    const mapComponent = await mapComponentResponse.json();

    var map = L.map(mapElement,
        {
            layers: [openTopoMap],
            doubleClickZoom: false,
            gestureHandling: true
        });

    map.fitBounds([
        [mapComponent.MapComponent.InitialViewBoundsMinY, mapComponent.MapComponent.InitialViewBoundsMinX],
        [mapComponent.MapComponent.InitialViewBoundsMaxY, mapComponent.MapComponent.InitialViewBoundsMaxX]
    ]);

    if (mapComponent.PointGuids?.length) {
        const response = await fetch('/Points/Data/pointdata.json');
        if (!response.ok)
            throw new Error(response.statusText);

        const pointData = await response.json();

        let includedPoints = pointData.filter(x => mapComponent.PointGuids.includes(x.ContentId));

        for (let pagePoint of includedPoints) {
            let pointContentMarker = new L.marker([pagePoint.Latitude, pagePoint.Longitude],
                {
                    draggable: false,
                    autoPan: true
                }).addTo(map);

            pointContentMarker
                .bindPopup(
                    `<p style="margin-top: .5rem; margin-bottom: 0;">${pagePoint.Title}</p><p style="margin-left: .5rem; margin-top: .1rem;">${pagePoint.DetailTypeString}</p>`);
        };
    }
}

async function singlePointMapInit(mapElement, displayedPointSlug) {
    var openTopoMap = L.tileLayer('https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png',
        {
            maxZoom: 17,
            id: 'osmTopo',
            attribution:
                'Map data: &copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors, <a href="http://viewfinderpanoramas.org">SRTM</a> | Map style: &copy; <a href="https://opentopomap.org">OpenTopoMap</a> (<a href="https://creativecommons.org/licenses/by-sa/3.0/">CC-BY-SA</a>)'
        });

    const response = await fetch('/Points/Data/pointdata.json');
    if (!response.ok)
        throw new Error(response.statusText);

    const pointData = await response.json();

    let pagePoint = pointData.filter(x => x.Slug === displayedPointSlug)[0];

    var map = L.map(mapElement,
        {
            center: { lat: pagePoint.Latitude, lng: pagePoint.Longitude },
            zoom: 13,
            layers: [openTopoMap],
            doubleClickZoom: false,
            gestureHandling: true
        });

    let pointContentMarker = new L.marker([pagePoint.Latitude, pagePoint.Longitude],
        {
            draggable: false,
            autoPan: true
        }).addTo(map);

    pointContentMarker
        .bindPopup(
            `<p style="margin-top: .5rem; margin-bottom: 0;">${pagePoint.Title}</p><p style="margin-left: .5rem; margin-top: .1rem;">${pagePoint.DetailTypeString}</p>`).openPopup();

    for (let circlePoint of pointData) {
        if (circlePoint.Slug == displayedPointSlug) continue;
        let toAdd = L.circle([circlePoint.Latitude, circlePoint.Longitude],
            60,
            { color: 'gray', fillColor: 'gray', fillOpacity: .5 });
        toAdd.bindTooltip(
            `<p style="margin-top: .5rem; margin-bottom: 0;">${circlePoint.Title}</p><p style="margin-left: .5rem; margin-top: .1rem;"${circlePoint.DetailTypeString}</p>`);
        toAdd.addTo(map).on("click", (e) => window.location.href = circlePoint.PointPageUrl);
    };
}