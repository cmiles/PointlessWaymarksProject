document.addEventListener('DOMContentLoaded', processEnableAfterLoadingElements);

function processEnableAfterLoadingElements() {
    Array.from(document.querySelectorAll('.enable-after-loading'))
        .forEach(x => {
            x.classList.remove('wait-cursor');
        });

    gsap.to(".enable-after-loading", {
        duration: .5, opacity: 1, onComplete: function () {
            this.targets().forEach(x => x.style.pointerEvents = 'auto');
        } });
}

function debounce(func, timeout = 500) {
    let timer;
    return (...args) => {
        clearTimeout(timer);
        timer = setTimeout(() => { func.apply(this, args); }, timeout);
    };
}

function sortCreatedAscending() {
    var list = document.querySelector('.content-list-container');

    Array.from(document.querySelectorAll('.content-list-item-container'))
        .sort((a, b) => a.getAttribute('data-created') > b.getAttribute('data-created') ? 1 : -1)
        .forEach(node => list.appendChild(node));
}

function sortCreatedDescending() {
    var list = document.querySelector('.content-list-container');

    Array.from(document.querySelectorAll('.content-list-item-container'))
        .sort((a, b) => a.getAttribute('data-created') < b.getAttribute('data-created') ? 1 : -1)
        .forEach(node => list.appendChild(node));
}

function sortUpdatedAscending() {
    var list = document.querySelector('.content-list-container');

    Array.from(document.querySelectorAll('.content-list-item-container'))
        .sort((a, b) => a.getAttribute('data-updated') > b.getAttribute('data-updated') ? 1 : -1)
        .forEach(node => list.appendChild(node));
}

function sortUpdatedDescending() {
    var list = document.querySelector('.content-list-container');

    Array.from(document.querySelectorAll('.content-list-item-container'))
        .sort((a, b) => a.getAttribute('data-updated') < b.getAttribute('data-updated') ? 1 : -1)
        .forEach(node => list.appendChild(node));
}

function sortTitleAscending() {
    var list = document.querySelector('.content-list-container');

    Array.from(document.querySelectorAll('.content-list-item-container'))
        .sort((a, b) => a.getAttribute('data-title') > b.getAttribute('data-title') ? 1 : -1)
        .forEach(node => list.appendChild(node));
}

function sortTitleDescending() {
    var list = document.querySelector('.content-list-container');

    Array.from(document.querySelectorAll('.content-list-item-container'))
        .sort((a, b) => a.getAttribute('data-title') < b.getAttribute('data-title') ? 1 : -1)
        .forEach(node => list.appendChild(node));
}

function searchContent() {

    gsap.to(".content-list-container", { duration: .5, opacity: 0, display: "none" });

    var filterText = document.querySelector('#userSearchText').value.toUpperCase();

    var contentTypes = Array.from(document.querySelectorAll('.content-list-filter-checkbox'))
        .filter(x => x.checked).map(x => x.value);

    var mainSiteFeedFilter = Array.from(document.querySelectorAll('.site-main-feed-filter-checkbox')).some(x => x.checked);

    var contentDivs = Array.from(document.querySelectorAll('.content-list-item-container'));

    // Loop through all list items, and hide those who don't match the search query
    for (var i = 0; i < contentDivs.length; i++) {
        var loopDiv = contentDivs[i];

        var divMainSiteFeed = loopDiv.getAttribute('data-site-main-feed');

        if (mainSiteFeedFilter && divMainSiteFeed === 'false') {
            loopDiv.classList.remove("shown-list-item");
            loopDiv.classList.add("hidden-list-item");
            continue;
        }

        var divDataContentType = loopDiv.getAttribute('data-content-type');

        if (contentTypes.length && !contentTypes.includes(divDataContentType)) {
            loopDiv.classList.remove("shown-list-item");
            loopDiv.classList.add("hidden-list-item");
            continue;
        }

        var divDataText = loopDiv.getAttribute('data-title').concat(
            loopDiv.getAttribute('data-summary'),
            loopDiv.getAttribute('data-tags')).toUpperCase();

        if (filterText == null || filterText.trim() === '') {
            loopDiv.classList.remove("hidden-list-item");
            loopDiv.classList.add("shown-list-item");
        }
        else if (divDataText.indexOf(filterText) > -1) {
            loopDiv.classList.remove("hidden-list-item");
            loopDiv.classList.add("shown-list-item");
        } else {
            loopDiv.classList.remove("shown-list-item");
            loopDiv.classList.add("hidden-list-item");
        }
    }

    document.querySelectorAll(".shown-list-item").forEach(x => x.style.display = '');
    document.querySelectorAll(".hidden-list-item").forEach(x => x.style.display = 'none');

    gsap.to(".content-list-container", { duration: .5, opacity: 1, display: "" });
}

const processSearchContent = debounce(() => searchContent());