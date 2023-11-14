document.addEventListener('DOMContentLoaded', processEnableAfterLoadingElements);

function processEnableAfterLoadingElements() {
    Array.from(document.querySelectorAll('.enable-after-loading'))
        .forEach(x => {
            x.classList.remove('wait-cursor');
        });

    gsap.to(".enable-after-loading", {
        duration: .5, opacity: 1, onComplete: function () {
            this.targets().forEach(x => x.style.pointerEvents = 'auto');
        }
    });
}

function debounce(func, timeout = 500) {
    let timer;
    return (...args) => {
        clearTimeout(timer);
        timer = setTimeout(() => { func.apply(this, args); }, timeout);
    };
}

function searchContent() {

    var filterText = document.querySelector('#userSearchText').value.toUpperCase();

    var contentDivs = Array.from(document.querySelectorAll('.camera-roll-photo-item-container'));

    var nonPhotoListDivs = Array.from(document.querySelectorAll('.camera-roll-year-list-container, .camera-roll-month-list-container, .camera-roll-info-item-container'));

    if (filterText == null || filterText.trim() === '') {
        nonPhotoListDivs.forEach(x => x.classList.remove("hidden-list-item"));
        nonPhotoListDivs.forEach(x => x.classList.add("shown-list-item"));
    } else {
        nonPhotoListDivs.forEach(x => x.classList.add("hidden-list-item"));
        nonPhotoListDivs.forEach(x => x.classList.remove("shown-list-item"));
    }

    // Loop through all list items, and hide those who don't match the search query
    for (var i = 0; i < contentDivs.length; i++) {
        var loopDiv = contentDivs[i];

        var divDataText = loopDiv.getAttribute('data-title').concat(
            loopDiv.getAttribute('data-summary'),
            loopDiv.getAttribute('data-tags').replace(/-/g, ' ')).toUpperCase();

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

    gsap.to(".hidden-list-item", { duration: .5, opacity: 0, display: "none" });
    gsap.to(".shown-list-item", { duration: .5, opacity: 1, display: "" });
}

const processSearchContent = debounce(() => searchContent());