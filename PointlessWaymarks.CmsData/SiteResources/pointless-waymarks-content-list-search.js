document.addEventListener('DOMContentLoaded', processEnableAfterLoadingElements);

function processEnableAfterLoadingElements() {
    Array.from(document.querySelectorAll('.enable-after-loading'))
        .filter(x => x.disabled)
        .forEach(x => {
            x.disabled = false;
            if (x.classList.contains('wait-cursor')) x.classList.remove('wait-cursor');
        });
}

function searchContent() {

    var filterText = document.querySelector('#userSearchText').value.toUpperCase();
    var contentTypes = Array.from(document.querySelectorAll('.content-list-filter-checkbox'))
        .filter(x => x.checked).map(x => x.value);

    var contentDivs = Array.from(document.querySelectorAll('.content-list-item-container'));

    // Loop through all list items, and hide those who don't match the search query
    for (var i = 0; i < contentDivs.length; i++) {
        var loopDiv = contentDivs[i];
        var divDataContentType = loopDiv.getAttribute('data-contenttype');

        if (contentTypes.length && !contentTypes.includes(divDataContentType)) {
            loopDiv.style.display = "none";
            continue;
        }

        var divDataText = loopDiv.getAttribute('data-title').concat(
            loopDiv.getAttribute('data-summary'),
            loopDiv.getAttribute('data-tags')).toUpperCase();

        if (divDataText.indexOf(filterText) > -1) {
            loopDiv.style.display = '';
        } else {
            loopDiv.style.display = 'none';
        }
    }
}