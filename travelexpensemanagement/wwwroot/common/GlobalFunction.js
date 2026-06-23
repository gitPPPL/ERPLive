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

////Global Attachment
//const fileInput = document.getElementById('fileInput');
//const browseBtn = document.getElementById('browseBtn');
//const dropZone = document.getElementById('dropZone');
//const fileList = document.getElementById('fileList');
//if (browseBtn && fileInput) {
//    browseBtn.addEventListener('click', () => {
//        fileInput.click();
//    });
//}

//if (fileInput && fileList) {
//    fileInput.addEventListener('change', function () {
//        renderFiles(this.files, fileList);
//    });
//}

//if (dropZone) {
//    dropZone.addEventListener('dragover', e => {
//        e.preventDefault();
//    });
//}

//if (dropZone && fileList) {
//    dropZone.addEventListener('drop', e => {
//        e.preventDefault();
//        renderFiles(e.dataTransfer.files, fileList);
//    });
//}
//function renderFiles(files, fileList) {

//    Array.from(files).forEach(file => {

//        const fileItem = document.createElement('div');
//        fileItem.className = 'erppageattachmentsectionfileitem';

//        fileItem.innerHTML = `
//            <div class="erppageattachmentsectionicon ${getFileColorClass(file.name)}">
//                ${getFileType(file.name)}
//            </div>

//            <div class="erppageattachmentsectioncontent">
//                <div class="erppageattachmentsectionfilename">
//                    ${file.name}
//                </div>

//                <div class="erppageattachmentsectionprogress">
//                    <div class="erppageattachmentsectionprogressbar"></div>
//                </div>
//            </div>

//            <div class="erppageattachmentsectionactions">
//                <button class="erppageattachmentsectionview">View</button>
//                <button class="erppageattachmentsectiondelete">Delete</button>
//            </div>
//        `;

//        fileList.appendChild(fileItem);

//        const progressBar = fileItem.querySelector('.erppageattachmentsectionprogressbar');

//        let progress = 0;

//        const interval = setInterval(() => {
//            progress += 5;
//            progressBar.style.width = progress + '%';

//            if (progress >= 100) {
//                clearInterval(interval);
//            }
//        }, 100);

//        // DELETE
//        fileItem.querySelector('.erppageattachmentsectiondelete')
//            .addEventListener('click', () => {
//                fileItem.remove();
//            });
//    });

//    // reset input here too (extra safety)
//    if (fileInput) {
//        fileInput.value = "";
//    }
//}
//function getFileType(fileName) {
//    return fileName.split('.').pop().toUpperCase();
//}
//function getFileColorClass(fileName) {

//    const ext = fileName.split('.').pop().toLowerCase();

//    switch (ext) {

//        case 'pdf':
//            return 'erppageattachmentsectionpdf';

//        case 'png':
//        case 'jpg':
//        case 'jpeg':
//        case 'gif':
//        case 'svg':
//        case 'webp':
//            return 'erppageattachmentsectionimage';

//        case 'doc':
//        case 'docx':
//            return 'erppageattachmentsectionword';

//        case 'xls':
//        case 'xlsx':
//        case 'csv':
//            return 'erppageattachmentsectionexcel';

//        case 'ppt':
//        case 'pptx':
//            return 'erppageattachmentsectionppt';

//        case 'txt':
//            return 'erppageattachmentsectiontxt';

//        case 'zip':
//        case 'rar':
//        case '7z':
//            return 'erppageattachmentsectionzip';

//        default:
//            return 'erppageattachmentsectiondefault';
//    }
//}

//Smart Global Search Dropdown
const SearchDropdownManager = {};
function InitializeSearchDropdown(dropdownId, getDataFunction) {
    const box = document.getElementById(dropdownId);
    if (!box) {
        console.error("Dropdown not found : " + dropdownId);
        return;
    }
    const input = box.querySelector("input");
    const list = box.querySelector(".dropdown-list");
    let items = [];
    let selectedIndex = -1;
    SearchDropdownManager[dropdownId] = {
        setValue: async function (value) {


            // Load data if not loaded
            if (items.length === 0) {

                items = await getDataFunction();

                SearchDropdownManager[dropdownId].items = items;

            }


            console.log("All dropdown items:", items);

            console.log("Searching value:", value);



            let selected = items.find(x =>
                Number(x.value) === Number(value)
            );



            console.log("Found:", selected);



            if (selected) {


                input.value = selected.text;


                input.setAttribute(
                    "data-value",
                    selected.value
                );


            }


        },
        //setValue: function (value) {
        //    let selected = items.find(x => x.value.toString() === value.toString());
        //    if (selected) {
        //        input.value = selected.text;
        //        input.setAttribute("data-value", selected.value);
        //    }
        //},
        getValue: function () {
            return input.getAttribute("data-value");
        },
        reload: function () {
            items = [];
        }
    };
    async function loadData() {
        if (items.length === 0) {
            let response = await getDataFunction();
            items = response;
            SearchDropdownManager[dropdownId].items = items;
        }
    }

    function showList(searchText = "") {
        let filterData = items.filter(x => x.text.toLowerCase().includes(searchText.toLowerCase()));
        list.innerHTML = filterData.map(x => `

        <div class="dropdown-item"
             data-value="${x.value}">

             ${x.text}

        </div>


        `).join("");
        list.style.display = filterData.length ? "block" : "none";
        [...list.children].forEach(row => {
            row.onclick = function () {
                input.value = this.innerText;
                input.setAttribute("data-value", this.dataset.value);
                list.style.display = "none";
            };
        });
        selectedIndex = -1;
    }
    input.addEventListener("click", async function () {
        await loadData();
        showList(input.value);
    });
    input.addEventListener("input", function () {
        showList(this.value);
    });
    input.addEventListener("keydown", function (e) {
        let rows = list.children;
        if (rows.length === 0) return;
        if (e.key === "ArrowDown") {
            e.preventDefault();
            selectedIndex = (selectedIndex + 1) % rows.length;
        }
        if (e.key === "ArrowUp") {
            e.preventDefault();
            selectedIndex = (selectedIndex - 1 + rows.length) % rows.length;
        }
        [...rows].forEach(x => x.classList.remove("active"));
        if (selectedIndex >= 0) {
            rows[selectedIndex].classList.add("active");
        }
        if (e.key === "Enter" && selectedIndex >= 0) {
            let row = rows[selectedIndex];
            input.value = row.innerText;
            input.setAttribute("data-value", row.dataset.value);
            list.style.display = "none";
        }
    });
    document.addEventListener("click", function (e) {
        if (!box.contains(e.target)) {
            list.style.display = "none";
        }
    });
}