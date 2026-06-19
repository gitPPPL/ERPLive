   $(document).ready(function () {
    $('.erppage-tab').on('click', function () {
        var tabId = $(this).data('tab');
        $('.erppage-tab').removeClass('active');
        $(this).addClass('active');
        $('.erppage-tab-content').removeClass('active');
        $('#' + tabId).addClass('active');
    });
    
    $('.erppage-accordion-header').on('click', function () {
        var parentTab = $(this).closest('.erppage-tab-content');
        if (parentTab.hasClass('active')) {
            parentTab.removeClass('active');
        } else {
            $('.erppage-tab-content').removeClass('active');
            parentTab.addClass('active');
        }
    });
    if ($('.erppage-tab.active').length === 0) {
        $('.erppage-tab:first').addClass('active');
    }

    if ($('.erppage-tab-content.active').length === 0) {
        $('.erppage-tab-content:first').addClass('active');
    }
    $('.erppage-tab').on('click', function () {
        var tabId = $(this).data('tab');

        $('.erppage-tab-content').removeClass('active');
        $('#' + tabId).addClass('active');
    });
    
});

// GLOBAL TABLE RESIZER
function makeColumnsResizable(selector = ".resizable-table") {

    document.querySelectorAll(selector).forEach(table => {

        const cols = table.querySelectorAll("colgroup col");
        const headers = table.querySelectorAll("thead th");

        headers.forEach((th, index) => {

            if (th.style.display === "none") return;

            // ❗ SKIP ACTION COLUMN (IMPORTANT)
            if (th.classList.contains("action-col")) return;

            // prevent duplicate resizer
            if (th.querySelector(".resizer")) return;

            const resizer = document.createElement("div");
            resizer.classList.add("resizer");

            //th.style.position = "relative"; // IMPORTANT for drag handle
            th.appendChild(resizer);

            let startX = 0;
            let startWidth = 0;

            resizer.addEventListener("mousedown", function (e) {

                startX = e.pageX;
                startWidth = cols[index].offsetWidth;

                document.body.style.cursor = "col-resize";

                function mouseMove(e) {
                    const newWidth = startWidth + (e.pageX - startX);

                    if (newWidth > 50) {
                        cols[index].style.width = newWidth + "px";
                    }
                }

                function mouseUp() {
                    document.removeEventListener("mousemove", mouseMove);
                    document.removeEventListener("mouseup", mouseUp);
                    document.body.style.cursor = "";
                }

                document.addEventListener("mousemove", mouseMove);
                document.addEventListener("mouseup", mouseUp);
            });
        });
    });
}

document.addEventListener("DOMContentLoaded", function () {
    makeColumnsResizable(); 
});
//Global Sorting of Table columns
const sortDirections = {};

document.addEventListener("click", function (e) {

    const th = e.target.closest("th");

    if (!th) return;

    const table = th.closest(".sortable-table");

    if (!table) return;

    const columnIndex = th.getAttribute("data-column");

    if (columnIndex === null) return;

    sortTable(table, columnIndex, th);
});
function sortTable(table, columnIndex, header) {

    const tbody = table.querySelector("tbody");

    let rows = Array.from(tbody.querySelectorAll("tr"));
    const sortKey = table.dataset.sortId + "_" + columnIndex;

    sortDirections[sortKey] = !sortDirections[sortKey];

    const isAsc = sortDirections[sortKey];
    table.querySelectorAll(".sort-icon").forEach(icon => {
        icon.innerHTML = "";
    });
    const currentIcon = header.querySelector(".sort-icon");

    if (currentIcon) {
        currentIcon.innerHTML = isAsc ? "▲" : "▼";
    }

    rows.sort((a, b) => {

        let aText = a.cells[columnIndex].innerText.trim();
        let bText = b.cells[columnIndex].innerText.trim();

        if (!isNaN(Date.parse(aText)) && !isNaN(Date.parse(bText))) {

            return isAsc
                ? new Date(aText) - new Date(bText)
                : new Date(bText) - new Date(aText);
        }

        if (!isNaN(aText) && !isNaN(bText)) {

            return isAsc
                ? aText - bText
                : bText - aText;
        }

        return isAsc
            ? aText.localeCompare(bText)
            : bText.localeCompare(aText);
    });

    tbody.innerHTML = "";

    rows.forEach(row => tbody.appendChild(row));
}

document.querySelectorAll(".sortable-table").forEach((table, index) => {
    table.dataset.sortId = index;
});

const fileInput = document.getElementById('fileInput');
const browseBtn = document.getElementById('browseBtn');
const dropZone = document.getElementById('dropZone');
const fileList = document.getElementById('fileList');

//browseBtn.addEventListener('click', () => {
//    fileInput.click();
//});


// Browse button
if (browseBtn && fileInput) {
    browseBtn.addEventListener('click', () => {
        fileInput.click();
    });
}

// File input change
if (fileInput && fileList) {
    fileInput.addEventListener('change', function () {
        renderFiles(this.files, fileList);
    });
}

// Drag over
if (dropZone) {
    dropZone.addEventListener('dragover', e => {
        e.preventDefault();
    });
}

// Drop
if (dropZone && fileList) {
    dropZone.addEventListener('drop', e => {
        e.preventDefault();
        renderFiles(e.dataTransfer.files, fileList);
    });
}

//fileInput.addEventListener('change', function () {
//    renderFiles(this.files);
//});

//dropZone.addEventListener('dragover', e => {
//    e.preventDefault();
//});

//dropZone.addEventListener('drop', e => {
//    e.preventDefault();
//    renderFiles(e.dataTransfer.files);
//});

function renderFiles(files) {

    Array.from(files).forEach(file => {

        const fileItem = document.createElement('div');
        fileItem.className =
            'erppageattachmentsectionfileitem';

        fileItem.innerHTML = `
                <div class="erppageattachmentsectionicon ${getFileColorClass(file.name)}">
                    ${getFileType(file.name)}
                </div>

                <div class="erppageattachmentsectioncontent">

                    <div class="erppageattachmentsectionfilename">
                        ${file.name}
                    </div>

                    <div class="erppageattachmentsectionprogress">
                        <div class="erppageattachmentsectionprogressbar"></div>
                    </div>

                </div>

                <div class="erppageattachmentsectionactions">

                    <button class="erppageattachmentsectionview">
                        View
                    </button>

                    <button class="erppageattachmentsectiondelete">
                        Delete
                    </button>

                </div>
            `;

        fileList.appendChild(fileItem);

        const progressBar =
            fileItem.querySelector(
                '.erppageattachmentsectionprogressbar'
            );

        let progress = 0;

        const interval = setInterval(() => {

            progress += 5;

            progressBar.style.width =
                progress + '%';

            if (progress >= 100) {
                clearInterval(interval);
            }

        }, 100);

        fileItem
            .querySelector(
                '.erppageattachmentsectiondelete'
            )
            .addEventListener('click', () => {
                fileItem.remove();
            });

    });
}

function getFileType(fileName) {
    return fileName.split('.').pop().toUpperCase();
}

function getFileColorClass(fileName) {

    const ext = fileName.split('.').pop().toLowerCase();

    switch (ext) {

        case 'pdf':
            return 'erppageattachmentsectionpdf';

        case 'png':
        case 'jpg':
        case 'jpeg':
        case 'gif':
        case 'svg':
        case 'webp':
            return 'erppageattachmentsectionimage';

        case 'doc':
        case 'docx':
            return 'erppageattachmentsectionword';

        case 'xls':
        case 'xlsx':
        case 'csv':
            return 'erppageattachmentsectionexcel';

        case 'ppt':
        case 'pptx':
            return 'erppageattachmentsectionppt';

        case 'txt':
            return 'erppageattachmentsectiontxt';

        case 'zip':
        case 'rar':
        case '7z':
            return 'erppageattachmentsectionzip';

        default:
            return 'erppageattachmentsectiondefault';
    }
}

