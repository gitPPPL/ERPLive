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

//Global Focus and Input field is visible

$(document).on(
    'focus',
    '.fixed-grid-table input, .fixed-grid-table textarea, .fixed-grid-table select',
    function () {

        const wrapper = $(this).closest('.fixed-grid-wrapper')[0];
        if (!wrapper) return;

        const td = this.closest('td');
        if (!td) return;

        const tdRect = td.getBoundingClientRect();
        const wrapperRect = wrapper.getBoundingClientRect();

        const safeArea = 140; // action column width

        if (tdRect.right > wrapperRect.right - safeArea) {
            wrapper.scrollLeft +=
                tdRect.right - (wrapperRect.right - safeArea) + 20;
        }
    }
);

//Global Input Value Dropdown Multi Select Filter 
const ERPFilterDropdownManager = {};

function InitializeERPFilterDropdown(config) {

    const container =
        document.getElementById(config.id);

    if (!container) {
        console.error("Dropdown not found : " + config.id);
        return;
    }

    const state = {
        items: config.data || [],
        filteredItems: [],
        selected: []
    };

    state.filteredItems = [...state.items];

    container.innerHTML = `

        <div class="erppage-search-inputdropdown-control">

            <div class="erppage-search-inputdropdown-selected">

                <span class="erppage-search-inputdropdown-placeholder">
                    ${config.placeholder || "Select"}
                </span>

            </div>

            <i class="fa fa-chevron-down erppage-search-inputdropdown-arrow"></i>

        </div>

        <div class="erppage-search-inputdropdown-panel">

            <div class="erppage-search-inputdropdown-search">

                <input type="text"
                       placeholder="Search..." />

            </div>

            <div class="erppage-search-inputdropdown-list"></div>

        </div>
    `;

    const control =
        container.querySelector(".erppage-search-inputdropdown-control");

    const panel =
        container.querySelector(".erppage-search-inputdropdown-panel");

    const searchBox =
        container.querySelector(".erppage-search-inputdropdown-search input");

    const list =
        container.querySelector(".erppage-search-inputdropdown-list");

    const selectedBox =
        container.querySelector(".erppage-search-inputdropdown-selected");


    function RenderItems() {

        list.innerHTML = "";

        const selectedItems =
            state.filteredItems.filter(item =>
                state.selected.some(x => x.id === item.id)
            );

        const unselectedItems =
            state.filteredItems.filter(item =>
                !state.selected.some(x => x.id === item.id)
            );

        const displayItems = [
            ...selectedItems,
            ...unselectedItems
        ];

        displayItems.forEach(item => {

            const checked =
                state.selected.some(x => x.id === item.id);

            const row =
                document.createElement("div");

            row.className =
                "erppage-search-inputdropdown-item";

            if (checked) {
                row.classList.add("selected");
            }

            row.innerHTML = `
            <input type="checkbox"
                   ${checked ? "checked" : ""}>

            <span>${item.text}</span>
        `;

            row.addEventListener("click", function (e) {

                e.stopPropagation();

                ToggleItem(item);

            });

            list.appendChild(row);

        });

    }


    function RenderSelected() {

        selectedBox.innerHTML = "";

        if (state.selected.length === 0) {

            selectedBox.innerHTML = `
                <span class="erppage-search-inputdropdown-placeholder">
                    ${config.placeholder}
                </span>
            `;

            return;
        }

        const maxVisible = 3;

        state.selected
            .slice(0, maxVisible)
            .forEach(item => {

                const chip =
                    document.createElement("span");

                chip.className =
                    "erppage-search-inputdropdown-chip";

                chip.innerHTML = `
                    ${item.text}
                    <i class="fa fa-times"></i>
                `;

                chip.querySelector("i")
                    .addEventListener("click", function (e) {

                        e.stopPropagation();

                        state.selected =
                            state.selected.filter(
                                x => x.id !== item.id
                            );

                        RenderItems();
                        RenderSelected();

                        TriggerChange();
                    });

                selectedBox.appendChild(chip);

            });

        if (state.selected.length > maxVisible) {

            const more =
                document.createElement("span");

            more.className =
                "erppage-search-inputdropdown-chip-more";

            more.innerText =
                `+${state.selected.length - maxVisible} More`;

            selectedBox.appendChild(more);
        }
    }

    function ToggleItem(item) {

        const exists =
            state.selected.some(x => x.id === item.id);

        if (exists) {

            state.selected =
                state.selected.filter(
                    x => x.id !== item.id
                );

        } else {

            state.selected.push(item);
        }

        RenderItems();
        RenderSelected();

        TriggerChange(); 
    }

    function TriggerChange() {

        if (config.onChange) {

            config.onChange(
                state.selected
            );
        }
    }

    searchBox.addEventListener("input", function () {

        const search =
            this.value.toLowerCase();

        state.filteredItems =
            state.items.filter(x =>
                x.text.toLowerCase()
                    .includes(search)
            );

        RenderItems();
    });

    control.addEventListener("click", function () {

        panel.classList.toggle("show");

        searchBox.focus();

    });

    document.addEventListener("click", function (e) {

        if (!container.contains(e.target)) {

            panel.classList.remove("show");
        }

    });

    RenderItems();
    RenderSelected();

    ERPFilterDropdownManager[config.id] = {

        GetValue: function () {

            return state.selected;
        },

        Clear: function () {

            state.selected = [];

            RenderItems();
            RenderSelected();
        },

        SetValue: function (ids) {

            state.selected =
                state.items.filter(x =>
                    ids.includes(x.id)
                );

            RenderItems();
            RenderSelected();
        },

        Reload: function (items) {

            state.items = items;

            state.filteredItems = [...items];

            RenderItems();
        }
    };
}

//Global Input Value Dropdown Single Select Filter 

const ERPSingleDropdownManager = {};

function InitializeERPSingleDropdown(config) {

    const container = document.getElementById(config.id);

    if (!container) {
        console.error("Dropdown not found : " + config.id);
        return;
    }

    const state = {
        items: config.data || [],
        filteredItems: [],
        selected: null
    };

    state.filteredItems = [...state.items];

    container.innerHTML = `

    <div class="erppage-search-inputdropdown-control">

        <div class="erppage-search-inputdropdown-selected">

            <span class="erppage-search-inputdropdown-placeholder">
                ${config.placeholder || "Select"}
            </span>

        </div>

        <i class="fa fa-chevron-down erppage-search-inputdropdown-arrow"></i>

    </div>

    <div class="erppage-search-inputdropdown-panel">

        <div class="erppage-search-inputdropdown-search">
            <input type="text" placeholder="Search..." />
        </div>

        <div class="erppage-search-inputdropdown-list"></div>

    </div>

    `;

    const control = container.querySelector(".erppage-search-inputdropdown-control");
    const panel = container.querySelector(".erppage-search-inputdropdown-panel");
    const searchBox = container.querySelector(".erppage-search-inputdropdown-search input");
    const list = container.querySelector(".erppage-search-inputdropdown-list");
    const selectedBox = container.querySelector(".erppage-search-inputdropdown-selected");

    function RenderItems() {

        list.innerHTML = "";

        state.filteredItems.forEach(item => {

            const row = document.createElement("div");

            row.className = "erppage-search-inputdropdown-item";

            if (state.selected && state.selected.id == item.id) {
                row.classList.add("selected");
            }

            row.innerHTML = `<span>${item.text}</span>`;

            row.onclick = function () {

                state.selected = item;

                RenderItems();
                RenderSelected();

                panel.classList.remove("show");

                if (config.onChange)
                    config.onChange(item);
            };

            list.appendChild(row);

        });

    }

    function RenderSelected() {

        if (!state.selected) {

            selectedBox.innerHTML = `

            <span class="erppage-search-inputdropdown-placeholder">

                ${config.placeholder}

            </span>`;

            return;
        }

        selectedBox.innerHTML = `

            <span class="erppage-search-inputdropdown-chip-single">

                ${state.selected.text}

            </span>
        `;
    }

    searchBox.addEventListener("input", function () {

        const search = this.value.toLowerCase();

        state.filteredItems = state.items.filter(x =>
            x.text.toLowerCase().includes(search));

        RenderItems();

    });

    control.onclick = function () {

        panel.classList.toggle("show");

        searchBox.focus();

    };

    document.addEventListener("click", function (e) {

        if (!container.contains(e.target)) {
            panel.classList.remove("show");
        }

    });

    RenderItems();
    RenderSelected();

    ERPSingleDropdownManager[config.id] = {

        GetValue() {
            return state.selected;
        },

        Clear() {

            state.selected = null;

            RenderItems();

            RenderSelected();

        },

        SetValue(id) {

            state.selected =
                state.items.find(x => x.id == id);

            RenderItems();

            RenderSelected();

        },

        Reload(items) {

            state.items = items;

            state.filteredItems = [...items];

            RenderItems();

        }

    };

}


//Global Attachment

//const fileInput = document.getElementById('fileInput');
//const browseBtn = document.getElementById('browseBtn');
//const dropZone = document.getElementById('dropZone');
//const fileList = document.getElementById('fileList');
//const uploadedFiles = new Set();
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

//        const fileKey = `${file.name.trim().toLowerCase()}`; // name + extension

//        // CHECK DUPLICATE
//        if (uploadedFiles.has(fileKey)) {
//            showAttachmentError("You have already attached this file: " + file.name);
//            return;
//        }

//        uploadedFiles.add(fileKey);

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

//        // DELETE (remove from Set also)
//        fileItem.querySelector('.erppageattachmentsectiondelete')
//            .addEventListener('click', () => {

//                fileItem.remove();
//                uploadedFiles.delete(fileKey);
//            });
//    });

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

//function showAttachmentError(message) {
//    const toast = document.createElement("div");
//    toast.className = "erppage-toast-error";
//    toast.innerText = message;

//    document.body.appendChild(toast);

//    setTimeout(() => {
//        toast.remove();
//    }, 3000);
//}

//Global Dragable Modal Popup

(function () {
    let activeModal = null;
    let offsetX = 0;
    let offsetY = 0;

    function initDraggable(modal) {
        const header = modal.querySelector(".erppagesmodal-header");
        if (!header) return;

        header.style.cursor = "move";

        header.addEventListener("mousedown", function (e) {
            activeModal = modal;

            const rect = modal.getBoundingClientRect();

            offsetX = e.clientX - rect.left;
            offsetY = e.clientY - rect.top;

            modal.style.margin = "0";
            modal.style.left = rect.left + "px";
            modal.style.top = rect.top + "px";
        });

        document.addEventListener("mousemove", function (e) {
            if (!activeModal) return;

            activeModal.style.left = (e.clientX - offsetX) + "px";
            activeModal.style.top = (e.clientY - offsetY) + "px";
        });

        document.addEventListener("mouseup", function () {
            activeModal = null;
        });
    }

    function scanModals() {
        document.querySelectorAll(".modal .modal-dialog.erppage-modal-drag")
            .forEach(initDraggable);
    }

    // Observe DOM for dynamically opened modals
    const observer = new MutationObserver(scanModals);

    observer.observe(document.body, {
        childList: true,
        subtree: true
    });

    // Initial scan
    document.addEventListener("DOMContentLoaded", scanModals);
})();

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