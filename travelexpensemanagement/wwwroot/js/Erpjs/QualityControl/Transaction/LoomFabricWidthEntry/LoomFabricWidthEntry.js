let loomList = [];
let itemList = [];
let typeList = [];
let colorList = [];

let isImporting = false;

let isEditLoading = false;

const allFieldIds = [
    "NumDocNo",
    "DtDocDate",
    "NumTotalRec",
    "ddlShift",
    "ddlPlace",
    "DtTime",
    "ddlInspectBy",
    "TxtRemarks"
];

const itemRecords = [
    "TxtLID",
    "ddlLoom",
    "ddlItemName",
    "ddlType",
    "TxtWidth",
    "TxtGram",
    "ddlColor",
    "TxtDNR",
    "TxtWarpWay",
    "TxtWeftWay",
    "ddlStrength",
    "TxtWarpElong",
    "TxtWeftElong",
    "TxtWarpMesh",
    "TxtWeftMesh",
    "TxtRemarks"
];

let docId = "", readOnly;

function getQueryParam(param) {
    return new URLSearchParams(window.location.search).get(param);
}

$(document).ready(function () {
    initPage();
});

async function initPage() {
    try {
        $("#NumDocNo").focus();
        await handleDocLoad();
        setEnterKeyFocus(allFieldIds);
        wireEvents();
    } catch (err) {
        showToast("Initialization failed: " + err, { type: "error" });
    }
}

function wireEvents() {
    setCurrentTime();
    $('#btn-save').on('click', async (e) => {
        e.preventDefault();

        const isValidDate = await checkValidDate();
        if (!isValidDate) return;

        if (!validateRequiredField('#NumDocNo', 'Doc No') ||
            !validateRequiredField('#DtDocDate', 'Doc Date') ||
            !validateRequiredField('#ddlShift', 'Shift Type') ||
            !validateRequiredField('#ddlInspectBy', 'Inspect By') ||
            !validateRequiredField('#ddlPlace', 'Place Type')) return;

        if (!validateItemRows()) return;

        try {
            const data = await collectFormData();
            docId ? UpdateData(data) : SaveData(data);
        } catch (err) {
            showToast("Error While Saving Data: " + err, { type: "error" });
        }

    });

    $('#ddlDocType').on('change', () => {
        const val = $('#ddlDocType').val();
        if (val) GetDocid(val);
    });

    $(document).on('click', '.btn-delete-action, .btn-Itemdelete-action', function () {
        const tbody = $(this).closest('tbody');
        if (tbody.find('tr').length > 1) {
            $(this).closest('tr').remove();
            updateTotalRecords();
        } else {
            alert('Cannot delete the first row.');

        }
    });

    $(document).on('click', '.btn-add-row, .btn-add-action', function () {
        addItemRecordRow();
    });

    $(document).on('change', 'select[id^="ddlLoom"]', function () {
       
        if (isImporting || isEditLoading) return;
        const selectedLoomId = $(this).val();
        const row = $(this).closest('tr');
        if (selectedLoomId) {
            fetchAndFillRowDataByLoom(selectedLoomId, row);
        }
        updateTotalRecords();
    });

    $(document).on('change', 'select[id^=ddlItemName]', function () {
        //if (isImporting) return;
        if (isImporting || isEditLoading) return;
        const selectedItemId = $(this).val();
        const row = $(this).closest('tr');
        if (selectedItemId) {
            fetchAndFillRowDataByItem(selectedItemId, row);
        }
    });

    $(document).on('change', '#ddlPlace', async function () {
        if (isEditLoading) return;
        await loadMasterData();

        $('#tblLoomFabricWidthEntry tbody tr').each(function (index) {

            const rowNo = index + 1;
            const currentValue = $(`#ddlLoom${rowNo}`).val();

            const loomSelect = $(`#ddlLoom${rowNo}`);
            loomSelect.removeData('loaded');

            bindDropdownData(rowNo);

            if (currentValue) {
                loomSelect.val(currentValue).trigger('change');
            }
        });

    });
    
    $('#btn-import').on('click', function () {
        importObtained();
    });

    $('#btn-print').on('click', function () {
        PrintLoomFabricWidthReport();
    });
}

function fetchAndFillRowDataByLoom(loomCode, row) {
    $.ajax({
        url: `/LoomFabricStrengthEntry/GetProd2List?LoomCode=${loomCode}`,
        type: 'GET',
        dataType: 'json',
        success: function (response) {
           
            if (response.status && response.data.length > 0) {
                const data = response.data[0];
                row.find('input[id^="TxtLID"]').val(loomCode);
                row.find('select[id^="ddlItemName"]').val(data.ITEM_CODE).trigger('change');
                row.find('select[id^="ddlType"]').val(data.PTYPE_CODE).trigger('change');
                row.find('input[id^="TxtWidth"]').val(data.WIDTH);
                row.find('input[id^="TxtGram"]').val(data.GRAM);
                row.find('select[id^="ddlColor"]').val(data.COLOR_CODE).trigger('change');
                row.find('input[id^="TxtDNR"]').val(data.DNR);

            } else {
                showToast("No data found for selected loom.", { type: "warning" });
            }
        },
        error: function () {
            showToast("Failed to load data for selected loom.", { type: "error" });
        }
    });
}

function fetchAndFillRowDataByItem(itemCode, row) {
    $.ajax({
        url: '/LoomFabricWidthEntry/GetItemList',
        type: 'GET',
        data: { itemCode: itemCode },
        dataType: 'json',
        success: function (response) {
            if (response.status && response.data.length > 0) {

                const data = response.data.find(x => x.CODE == itemCode);
                row.find('select[id^="ddlType"]').val(data.PTYPE_CODE).trigger('change');
                row.find('input[id^="TxtWidth"]').val(data.Width);
                row.find('input[id^="TxtGram"]').val(data.Gram);
                row.find('select[id^="ddlColor"]').val(data.COLOR_CODE).trigger('change');
              

            } else {
                showToast("No data found for selected loom.", { type: "warning" });
            }
        },
        error: function () {
            showToast("Failed to load data for selected loom.", { type: "error" });
        }
    });
}

async function handleDocLoad() {
    docId = getQueryParam('id');
    readOnly = getQueryParam('readOnly');
    if (docId) {
         await GetDocData(docId, readOnly);

    } else {
        GetDocid();
        const today = new Date();
        const todayDate = today.getFullYear() + '-' +
        (today.getMonth() + 1).toString().padStart(2, '0') + '-' +
        today.getDate().toString().padStart(2, '0');
        $('#DtDocDate').val(todayDate);
        GetShiftList();
        GetPlaceList();
        await GetEmployeeList();
        await GetLastEntry();
        await loadMasterData();
        addItemRecordRow();
    }
}

function SaveData(saveDt) {
    $.ajax({
        url: '/LoomFabricWidthEntry/SaveOrUpdateLoomFabricWidthEntry',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(saveDt),
        success: function (response) {
            if (response?.status) {
                showToast("Data Saved Successfully", { type: "success" });
                disableAllFields();
                $('#btn-save').hide();
                readOnly = 'true';
            } else {
                showToast("Error while saving", { type: "error" });
            }
        },
        error: function () {
            showToast("Error while saving", { type: "error" });
        }
    });
}

function UpdateData(UpdateDt) {
    $.ajax({
        url: '/LoomFabricWidthEntry/SaveOrUpdateLoomFabricWidthEntry',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(UpdateDt),
        dataType: 'json',
        success: function (response) {
            if (response?.status) {
                showToast("Data Updated Successfully", { type: "success" });
                disableAllFields();
                $('#btn-save').hide();
                readOnly = 'true';
            } else {
                showToast("Update Failed", { type: "error" });
            }
        },
        error: function (xhr, status, error) {
            showToast("Update failed: " + error, { type: "error" });
        }
    });
}

async function fillHeaderData(headdata) {
    if (!Array.isArray(headdata) || headdata.length === 0) {
        
        showToast("No header data found to populate the form.", {type: "error" });

        return;
    }

    const data = headdata[0];
    console.log("Remarks Value:", data.REMARKS);
    $("#TxtDocId").val(data.DOC_ID ?? "");
    $("#NumDocNo").val(data.V_NO ?? "");
    $("#DtDocDate").val(data.V_DATE ? data.V_DATE.substring(0, 10) : "");
    GetShiftList(data.SHIFT ?? 0);
    GetPlaceList(data.PLACE_CODE ?? 0);
    GetEmployeeList(data.EMP_CODE ?? 0);
    $("#TxtRemarks").val(data.REMARKS ?? "");
    $("#TxtTotalRec").val(0);
   // $("#TxtQcTime").val(data.QCTIME ?? "");
    const formattedTime = convertTo24HourFormat(data.QCTIME ?? "");
    $('#DtTime').val(formattedTime);
}

function bindDropdownValue(selector, value, text = null) {
    const $el = $(selector);
    const valStr = String(value ?? '');
    if (!$el.length) {
      
       return;
    }

    const optionExists = $el.find("option").filter(function () {
        return $(this).val() == valStr;
    }).length > 0;

    if (!optionExists) {
        const displayText = text ?? valStr;
       $el.append(new Option(displayText, valStr));
      
    }

    $el.val(valStr).trigger('change');
}

async function collectFormData() {
    const getVal = (id) => {
        const el = document.getElementById(id);
       return el ? el.value.trim() || null : null;
    };

    const parseIntOrNull = (val) => {
        const num = parseInt(val);
        return isNaN(num) ? null : num;
    };

    const parseDateOrNull = (val) => {
        const date = new Date(val);
        return isNaN(date.getTime()) ? null : date.toISOString();
    };

    const V_DATE = parseDateOrNull(getVal("DtDocDate"));
    const tabledt = await collectItemsDetail();
    const DOC_ID = toNullableString(docId);

    const data = {
        V_No: getVal("NumDocNo"),
        V_DATE: V_DATE || null,
        VType:'LINS',
        SHIFT: getVal("ddlShift"),
        PLACE_CODE: parseIntOrNull(getVal("ddlPlace")),
        EMP_CODE: parseIntOrNull(getVal("ddlInspectBy")),
        REMARKS: getVal("TxtRemarks"),
        QCTIME: getTimeAsDateTimeForSql("DtTime"),
        QC_INCHARGE: parseIntOrNull(getVal("ddlQcIncharge")),
        CHEMIST: parseIntOrNull(getVal("ddlChemist")),
        QC_INCHARGENAME: getVal("TxtQcInchargeName"),
        CHEMISTNAME: getVal("TxtChemistName"),
        SRNO: parseIntOrNull(getVal("NumTotalRec")),
        SaveOrUpdate: (!DOC_ID || DOC_ID === "") ? "Save" : "Update",
        Prod2QCData: tabledt || []
    };

    return data;
}

function getTimeAsDateTimeForSql(inputId) {
    const timeString = document.getElementById(inputId)?.value;
    if (!timeString) return null;
    return `${timeString}:00`; 
}

async function collectItemsDetail() {
    const items = [];

    $('#tblLoomFabricWidthEntry tbody tr').each(function () {
        const idx = this.id.replace('row', '');
        const $r = $(this);

        const loom = $r.find(`#ddlLoom${idx}`).val();
        const item = $r.find(`#ddlItemName${idx}`).val();
        const type = $r.find(`#ddlType${idx}`).val();

        if (!loom && !item && !type) {
            return true; // skip blank row
        }

        items.push({
            SNO: parseIntSafe(idx),
            ITEM_CODE: parseIntSafe($r.find(`#ddlItemName${idx}`).val()),
            PTYPE_CODE: parseIntSafe($r.find(`#ddlType${idx}`).val()),
            PTYPE_NAME: $r.find(`#ddlType${idx}`).val() ? $r.find(`#ddlType${idx} option:selected`).text() : null,
            LOOM_CODE: parseIntSafe($r.find(`#ddlLoom${idx}`).val()),
            COLOR_CODE: parseIntSafe($r.find(`#ddlColor${idx}`).val()),
            COLOR_NAME: $r.find(`#ddlColor${idx}`).val() ? $r.find(`#ddlColor${idx} option:selected`).text() : null,
            WIDTH: parseDecimalSafe($r.find(`#TxtWidth${idx}`).val()),
            GRAM: parseDecimalSafe($r.find(`#TxtGram${idx}`).val()),
            DNR: toNullableString($r.find(`#TxtDNR${idx}`).val()),
            RESULT1: parseDecimalSafe($r.find(`#TxtObtWidth${idx}`).val()),
            REMARKS1:$r.find(`#TxtBobMark${idx}`).val(),
            REMARKS: toNullableString($r.find(`#TxtRemarks${idx}`).val())
                
        });
    });

    return items;
}

async function fillItemDetailTable(itemsData) {

    isEditLoading = true;
  
    const $tbody = $('#tblLoomFabricWidthEntry tbody');
    $tbody.empty();

    for (let index = 0; index < itemsData.length; index++) {
        const item = itemsData[index];
       
        const idx = index + 1;

       await addItemRecordRow();
        //await bindDropdownData(idx);

        $(`#TxtLID${idx}`).val(item.LOOM_CODE  || '');
        $(`#TxtWidth${idx}`).val(item.WIDTH ?? '');
        $(`#TxtGram${idx}`).val(item.GRAM ?? '');
        $(`#TxtDNR${idx}`).val(item.DNR ?? '');
        $(`#TxtObtWidth${idx}`).val(item.RESULT1);
        $(`#TxtBobMark${idx}`).val(item.REMARKS1);
        $(`#TxtRemarks${idx}`).val(item.REMARKS ?? '');

        const safeSetDropdown = (selector, value) => {
            const $select = $(`${selector}${idx}`);

            if ($select.find(`option[value="${value}"]`).length > 0) {
                $select.val(value).trigger('change.select2');
            }
        };

        safeSetDropdown('#ddlLoom', item.LOOM_CODE);
        safeSetDropdown('#ddlItemName', item.ITEM_CODE);
        safeSetDropdown('#ddlType', item.PTYPE_CODE);
        safeSetDropdown('#ddlColor', item.COLOR_CODE);           
    }
   
    setTimeout(() => {
        isEditLoading = false;
    }, 500);
    updateTotalRecords();
}

function GetDocTypeAsync(selectedValue) {
    return new Promise((resolve, reject) => {
        $.ajax({
            url: '/LoomFabricStrengthEntry/GetDocType',
            type: 'GET',
            dataType: 'json',
            success: function (response) {
                if (response.status) {
                    const $dropdown = $('#ddlDocType');
                    $dropdown.empty();
                    $.each(response.data, function (index, item) {
                        $dropdown.append(`<option value="${item.CODE}">${item.NAME}</option>`);
                    });

                    if (selectedValue && $dropdown.find(`option[value="${selectedValue}"]`).length > 0) {
                        $dropdown.val(selectedValue).trigger('change');
                    } else {
                        $dropdown.prop('selectedIndex', 0);
                    }

                    resolve();
                } else {
                    reject("Invalid response status.");
                }
            },
            error: function (xhr, status, error) {
                showToast("Document Type Load failed: " + error, { type: "error" });
                reject(error);
            }
        });
    });
}

function GetDocid() {
    $.ajax({
        url: '/LoomFabricWidthEntry/GetMaxVNo',
        type: 'GET',
        success: function (response) {
            if (response.status === true && response.data) {
                $('#NumDocNo').val(response.data.vNo || '');
                $('#TxtDocId').val(response.data.docId || '');
            } else {
                $('#txtDocNo').val('');
                $('#TxtDocId').val('');
            }
        },
        error: function (xhr, status, error) {
            showToast("Error fetching Doc ID:" + err, { type: "error" });
        }
    });
}

function GetPlaceList(selectedValue=null) {
    $.ajax({
        url: '/LoomFabricStrengthEntry/GetPlaceMast',
        type: 'GET',
        dataType: 'json',
        success: function (response) {
            if (response.status) {
                const $DropdownId = $('#ddlPlace');
                $DropdownId.empty();
                $DropdownId.append('<option value="">- Select Place Name -</option>');
                $.each(response.data, function (index, item) {
                    $DropdownId.append(`<option value="${item.CODE}">${item.NAME}</option>`);
                });

                $DropdownId.select2({
                    placeholder: "- Select -",
                    allowClear: true
                });

                $DropdownId.off('select2:open').on('select2:open', function () {

                    setTimeout(function () {

                        let searchBox = document.querySelector(
                            '.select2-container--open .select2-search__field'
                        );

                        if (searchBox) {
                            searchBox.focus();
                        }

                    }, 0);

                });

                if (selectedValue && $DropdownId.find(`option[value="${selectedValue}"]`).length > 0) {
                    $DropdownId.val(selectedValue).trigger('change');
                }
                else {
                    $DropdownId.val('').trigger('change');
                }

            } else {
                showToast("Place Dropdown load failed", { type: "error" });
            }
        },
        error: function (xhr, status, error) {
            showToast("Error While Loafing Place : " + error, { type: "error" });

        }
    });
}

async function GetEmployeeList(selectedValue=null) {
    return $.ajax({
        url:'/LoomFabricStrengthEntry/GetUserMast',
        type: 'GET',
        dataType: 'json',
        success: function (response) {
                if (response.status) {
                const $DropdownId = $('#ddlInspectBy');
                $DropdownId.empty();
                $DropdownId.append('<option value="">- Select Employee Name -</option>');
                    $.each(response.data, function (index, item) {
                        //$DropdownId.append(`<option value="${item.CODE}">${item.NAME}</option>`);
                        $DropdownId.append(
                            `<option value="${item.CODE}" data-name="${item.NAME}">
                                            ${item.NAME} | ${item.CODE}
                            </option>`
                    );

                });
                $DropdownId.select2({
                    placeholder: "- Select -",
                    allowClear: true
                });

                $DropdownId.off('select2:open').on('select2:open', function () {

                    setTimeout(function () {

                        let searchBox = document.querySelector(
                            '.select2-container--open .select2-search__field'
                        );

                        if (searchBox) {
                            searchBox.focus();
                        }

                    }, 0);

                });

                if (selectedValue && $DropdownId.find(`option[value="${selectedValue}"]`).length > 0) {
                    $DropdownId.val(selectedValue).trigger('change');
                }
            else {
                $DropdownId.val('').trigger('change');
            }

            } else {
                showToast("Employee Name Load Failed", { type: "warning" });   
            }
        },
        error: function (xhr, status, error) {
            showToast("Error While Loading Employee : " + error, { type: "error" });
       
        }
    });
}

function GetShiftList(selectedValue=null) {
    $.ajax({
        url: '/LoomFabricStrengthEntry/GetShiftList',
        type: 'GET',
        dataType: 'json',
        success: function (response) {
            if (response.status) {
                const $DropdownId = $('#ddlShift');
                $DropdownId.empty();
                $DropdownId.append('<option value="">- Select Shift Name -</option>');
                $.each(response.data, function (index, item) {
                    $DropdownId.append(`<option value="${item.CODE}">${item.NAME}</option>`);
                });
                $DropdownId.select2({
                    placeholder: "- Select -",
                    allowClear: true
                });

                $DropdownId.off('select2:open').on('select2:open', function () {

                    setTimeout(function () {

                        let searchBox = document.querySelector(
                            '.select2-container--open .select2-search__field'
                        );

                        if (searchBox) {
                            searchBox.focus();
                        }

                    }, 0);

                });

                if (selectedValue && $DropdownId.find(`option[value="${selectedValue}"]`).length > 0) {
                    $DropdownId.val(selectedValue).trigger('change');
                }
                else {
                    $DropdownId.val('').trigger('change');
                }

            } else {
                showToast("Shift Load Failed", { type: "warning" });

            }
        },
        error: function (xhr, status, error) {
            showToast("Error While Loading Shift : " + error, { type: "error" });

        }
    });
}

async function GetDocData(MasterTblId, readOnly) {
    isEditLoading = true;
    try {
        const response = await $.ajax({
            url: '/LoomFabricStrengthEntry/GetLoomFabricSById',
            type: 'GET',
            data: {id: MasterTblId }
        });

        if (!response.status) {
            showToast("No data returned", { type: "error" });
            return;
        }

        await loadMasterData();
        await GetShiftList();
        await GetPlaceList();
        await GetEmployeeList();

        await fillHeaderData(response.header);
        await fillItemDetailTable(response.detail);

        // readonly logic
        if (readOnly === 'true') {
            $('#btn-save, #cancelBtn').hide();
            disableAllFields();
        } else {
            $('#btn-save, #cancelBtn').show();
            enableAllFields();
        }

    } catch (error) {
        console.error("GetDocData Error:", error);
        console.error(error.stack);
        showToast("Failed To load Data", { type: "error" });
       
    }
}

async function addItemRecordRow() {
    let tbody = $('#tblLoomFabricWidthEntry tbody');
    let rowCount = tbody.find('tr').length + 1;

    let newRow = `
    <tr class="no-border-input" id="row${rowCount}">
        <td class="hidden-col"><input type="text" style="width:100px;" class="form-control" id="TxtLID${rowCount}" readonly/></td>
        <td>
            <select class="form-control" id="ddlLoom${rowCount}">
                    <option value=""></option>
            </select>
        </td>

        <td>
            <select class="form-control" id="ddlItemName${rowCount}">
                <option value="">Select Item Name</option>
            </select>
        </td>

        <td class="hidden-col">
            <select class="form-control"  id="ddlType${rowCount}">
                <option value="">Select Type</option>
            </select>
        </td>

        <td class="hidden-col"><input type="number" class="form-control"  id="TxtWidth${rowCount}" /></td>
        <td class="hidden-col"><input type="number" style="width:100px;" class="form-control" id="TxtGram${rowCount}" /></td>

        <td class="hidden-col">
            <select class="form-control" style="width:100px;" id="ddlColor${rowCount}">
                <option value="">- Select Color -</option>
            </select>
        </td>
        <td><input type="text" class="form-control" id="TxtDNR${rowCount}" /></td>
        <td><input type="number" class="form-control" id="TxtObtWidth${rowCount}" /></td>
        <td><input type="text" class="form-control" id="TxtBobMark${rowCount}" /></td>
        <td><input type="text" class="form-control" id="TxtRemarks${rowCount}" /></td>
        <td class="action-col">
         <button class="act-btn add btn-add-action btn-Itemadd-action" title="Add Row" style="cursor:pointer;"><i class="fa fa-plus"></i></button>
                <button class="act-btn delete btn-delete-action btn-Itemdelete-action" title="Delete Row" style="cursor:pointer;"><i class="fa fa-trash"></i></button>
        </td>
    </tr>
    `;
     
    tbody.append(newRow);
    await bindDropdownData(rowCount);
    setEnterKeyFocusOnTable?.(itemRecords, rowCount);
}

//=====New Code==========
async function loadMasterData() {

    const placeCode = $('#ddlPlace').val();

    const [loomRes, itemRes, typeRes, colorRes] = await Promise.all([
        $.get(`/LoomFabricStrengthEntry/GetLoomList?PlaceCode=${placeCode}`),
        $.get('/LoomFabricWidthEntry/GetItemList'),
        $.get('/LoomFabricStrengthEntry/GetItemType'),
        $.get('/LoomFabricStrengthEntry/GetColor')
    ]);

    loomList = loomRes.data || [];
    itemList = itemRes.data || [];
    typeList = typeRes.data || [];
    colorList = colorRes.data || [];
}

function bindDropdownData(rowCount) {
    const loomSelect = $(`#ddlLoom${rowCount}`);
    const itemSelect = $(`#ddlItemName${rowCount}`);
    const typeSelect = $(`#ddlType${rowCount}`);
    const colorSelect = $(`#ddlColor${rowCount}`);

    const setOptionsOnce = (select, list) => {
      if (select.data('loaded')) return;

        let html = '<option value=""></option>';
        for (let i = 0; i < list.length; i++) {
            html += `<option value="${list[i].CODE}">${list[i].NAME}</option>`;
        }
        select.html(html);
        select.data('loaded', true);
    };

    setOptionsOnce(loomSelect, loomList);
    setOptionsOnce(itemSelect, itemList);
    setOptionsOnce(typeSelect, typeList);
    setOptionsOnce(colorSelect, colorList);

    const init = (select, placeholder) => {
        if (!select.hasClass('select2-hidden-accessible')) {
        select.select2({
            width: '100%',
            allowClear: true,
            placeholder: placeholder
        });
        }
    };

    init(loomSelect, 'Select Loom');
    init(itemSelect, 'Select Item');
    init(typeSelect, 'Select Type');
    init(colorSelect, 'Select Color');
}

function formatDate(dateStr) {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    return d.toISOString().split('T')[0];
}

function parseIntSafe(value) {
    const parsed = parseInt(value, 10);
    return isNaN(parsed) ? null : parsed;
}

function parseFloatSafe(value) {
    const parsed = parseFloat(value);
    return isNaN(parsed) ? null : parsed;
}

function parseDate(dateStr) {
    if (!dateStr) return null;
    const parts = dateStr.split(/[-\/]/);
    if (parts.length === 3) {
        let[day, month, year] = parts.map(p => parseInt(p, 10));
    if (year < 1000) year += 2000;
    return new Date(year, month - 1, day);
    }
    return null;
}

function toNullableInt(val) {
    const parsed = parseInt(val);
    return isNaN(parsed) ? null : parsed;
}

function toNullableDate(val) {
    const date = new Date(val);
    return isNaN(date.getTime()) ? null : val;
}

function toNullableString(val) {
    return val?.trim() || null;
}

function allowOnlyDecimal(input) {
    input.value = input.value
        .replace(/[^0-9.]/g, '')
        .replace(/(\..*)\./g, '$1');
}

function allowOnlyNumbers(input) {
    input.value = input.value.replace(/[^0-9]/g, '');
}

function setFieldsEnabled(enabled) {
    allFieldIds.forEach(id => {
        const el = document.getElementById(id);
        if (el) {
            el.disabled = !enabled;
        }
    });
}

function parseDecimalSafe(val) {
    const num = parseFloat(val);
    return isNaN(num) ? null : num;
}

function setEnterKeyFocus(sequence) {
    sequence.forEach((id, index) => {
        $(`#${id}`).on('keypress', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                if (index + 1 < sequence.length) {
                    $(`#${sequence[index + 1]}`).focus();
                }
            }
        });
    });
}

function enableAllFields() {

    $('#LoomFabricWidthEntryForm')
        .find('input, select, textarea, button')
        .prop('disabled', false);

    $('input[id^="TxtLID"]').prop('readonly', true);

    $('.btn-add-action, .btn-delete-action').show();

    $('select').trigger('change.select2');
}

function disableAllFields() {

    $('#LoomFabricWidthEntryForm')
        .find('input, select, textarea, button')
        .not('#btn-print')
        .prop('disabled', true);

    $('.btn-add-action, .btn-delete-action').prop('disabled', true);

    $('select').trigger('change.select2');
}

function setEnterKeyFocusOnTable(sequence, rowCount) {
    sequence.forEach((id, index) => {
        let elementId = `#${id}${rowCount}`;
        $(document).on('keydown', elementId, function (e) {
            if (e.key === 'Enter' || e.key === 'Tab' || e.keyCode === 13 || e.keyCode === 9) {
                e.preventDefault();

                let nextIndex = index + 1;
                if (nextIndex < sequence.length) {
                    let nextElementId = `#${sequence[nextIndex]}${rowCount}`;
                    if ($(nextElementId).length) {
                        $(nextElementId).focus();
                    }
                } else {

                    addItemRecordRow();
                    setEnterKeyFocus(sequence, rowCount + 1);
                    $(`#${sequence[0]}${rowCount + 1}`).focus();
                }
            }
        });
    });
}

function convertTo24HourFormat(timeStr) {
    if (!timeStr) return '';

    if (/^\d{2}:\d{2}(:\d{2})?$/.test(timeStr)) {
        return timeStr.substring(0, 5);
    }

    const [time, modifier] = timeStr.split(' ');
    if (!time || !modifier) return '';

    let [hours, minutes] = time.split(':');

    if (modifier.toUpperCase() === 'PM' && hours !== '12') {
        hours = String(Number(hours) + 12);
    } else if (modifier.toUpperCase() === 'AM' && hours === '12') {
        hours = '00';
    }

    return `${hours.padStart(2, '0')}:${minutes.padStart(2, '0')}`;
}

function updateTotalRecords() {

    console.log("updateTotalRecords called");

    let count = 0;

    $('#tblLoomFabricWidthEntry tbody tr').each(function () {

        const rowId = $(this).attr('id').replace('row', '');

        const loom = $(`#ddlLoom${rowId}`).val();
        const item = $(`#ddlItemName${rowId}`).val();
        const type = $(`#ddlType${rowId}`).val();

        if (loom || item || type) {
            count++;
        }
    });

    console.log("Total Count =", count);

    $('#TxtTotalRec').val(count);
}

function setCurrentTime() {
    const now = new Date();
    const hours = String(now.getHours()).padStart(2, '0');
    const minutes = String(now.getMinutes()).padStart(2, '0');

    document.getElementById("DtTime").value = `${hours}:${minutes}`;
}

async function importObtained() {
    try {

        const $tbody = $("#tblLoomFabricWidthEntry tbody");
        const shift = $("#ddlShift").val();
        const placeCode = $("#ddlPlace").val();
        const vDateRaw = $("#DtDocDate").val();
        const vDate = vDateRaw;
        const vTime = $("#DtTime").val();
        const response = await $.get('/LoomFabricWidthEntry/ImportWidth', {
            shift,
            placeCode,
            vDate,
            vTime
        });

        if (!response.status) {
            showToast("Error while Importing Data " + response.message, { type: "error" });
            return;
        }

        const list = response.data || [];

        if (list.length === 0) {
            showToast("No data found For Selected Place and Shift", { type: "warning" });
            return;
        }

        $tbody.empty();
        isImporting = true;

        for (const [index, item] of list.entries()) {

            addItemRecordRow();

            let rowNo = index + 1;

            $(`#TxtLID${rowNo}`).val(item.LOOM_CODE);

            $(`#TxtWidth${rowNo}`).val(item.WidthMM);
            $(`#TxtGram${rowNo}`).val(item.GRAM);
            $(`#TxtDNR${rowNo}`).val(item.DNR);

            $(`#TxtObtWidth${rowNo}`).val(item.OB_WIDTH);
            $(`#TxtRemarks${rowNo}`).val(item.Remarks);

            $(`#ddlLoom${rowNo}`).val(item.LOOM_CODE).trigger('change');
            $(`#ddlItemName${rowNo}`).val(item.ITEM_CODE).trigger('change');
            $(`#ddlType${rowNo}`).val(item.PTYPE_CODE).trigger('change');
            $(`#ddlColor${rowNo}`).val(item.COLOR_CODE).trigger('change');
        }

        isImporting = false;

    } catch (error) {
        console.log(error);
        showToast("Import failed.", { type: "error" });
    }
}

async function GetLastEntry() {
    try {
        const response = await $.get('/LoomFabricWidthEntry/GetLastEntry');

        if (!response.status) {
            toastr.warning(response.message);
            return;
        }

        const data = response.data;
        
        if (data.shift) {
            $('#ddlShift').val(data.shift).trigger('change');
        }

        if (data.placE_CODE) {
            $('#ddlPlace').val(data.placE_CODE).trigger('change');
        }

        if (data.emP_CODE) {
            $('#ddlInspectBy').val(data.emP_CODE).trigger('change');
        }
    }
    catch (error) {
        
        showToast("Failed to load last entry data.", {type: "error" });
    }
}

function validateItemRows() {

    let validRowCount = 0;
    let isValid = true;

    $('#tblLoomFabricWidthEntry  tbody tr').each(function () {

        const rowId = $(this).attr('id').replace('row', '');

        const loom = $(`#ddlLoom${rowId}`).val();
        const item = $(`#ddlItemName${rowId}`).val();
        const type = $(`#ddlType${rowId}`).val();
        const color = $(`#ddlColor${rowId}`).val();
        // Empty row skip
        if (!loom && !item && !type) {
            return true;
        }

        validRowCount++;

        if (!loom || Number(loom) === 0) {
            setInvalid($(`#ddlLoom${rowId}`), 'Invalid Loom.');
            isValid = false;
            return false;
        }

        if (!item || Number(item) === 0) {
            setInvalid($(`#ddlItemName${rowId}`), 'Item Name is required.');
            isValid = false;
            return false;
        }

        if (!type || Number(type) === 0) {
            setInvalid($(`#ddlType${rowId}`), 'Invalid PType.');
            isValid = false;
            return false;
        }

        if (!color || Number(color) === 0) {
            setInvalid($(`#ddlColor${rowId}`), 'Invalid Color.');
            isValid = false;
            return false;
        }

    });

    if (validRowCount === 0) {
        showToast("No Record in grid to save.", { type: "warning" });
        return false;
    }

    return isValid;
}

async function checkValidDate() {

    const data = {
        vdate: $("#DtDocDate").val(),
        vtype: "LINS",
        vno: $("#NumDocNo").val()
    };

    try {

        const response = await fetch('/LoomFabricWidthEntry/CheckValidDate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });
        const result = await response.json();

        if (result.status === false) {
            showToast(result.message, { type: "warning" });
            return false;
        }
        return true;

    } catch (error) {
        showToast("Date validation failed", { type: "error" });
        return false;
    }
}

function formatDateDDMMYYYY(dateStr) {
    if (!dateStr) return "";

    let d = new Date(dateStr);

    let day = String(d.getDate()).padStart(2, '0');
    let month = String(d.getMonth() + 1).padStart(2, '0');
    let year = d.getFullYear();

    return `${day} ${month} ${year}`;
}

let isPrinting = false;

async function PrintLoomFabricWidthReport() {
    if (isPrinting) return;
    isPrinting = true;
    try {
        const model = {
            V_DATE: $("#DtDocDate").val(),
            SHIFT: $("#ddlShift").val(),
            PLACE_CODE: $("#ddlPlace").val()
        };

        const buildRes = await $.post(
            '/LoomFabricWidthEntry/PrintLoom',
            model
        );

        if (!buildRes.status) {
            showToast("Data build failed", { type: "error" });
            return;
        }

        // 2. Ab report API call
        var reportName = "LOOM_INSPNewN";

        var SelForMul =
            " {tempQCFabricWidth.COMP_CODE}=" + window.globalVariables.compCode +
            " AND {tempQCFabricWidth.BRANCH_CODE}=" + window.globalVariables.branchCode +
            " AND {tempQCFabricWidth.YEAR_CODE}=" + window.globalVariables.yearCode;

        var formulaFields = {
            Reportname: reportName,
            selectionFormula: SelForMul,
            Database: window.database.db,
            Parameters: {
                RPTNAME: "LOOM INSPECTION REPORT",
                F1: "Date : " + formatDateDDMMYYYY($("#DtDocDate").val()),
                F2: "Shift : " + $("#ddlShift option:selected").text(),
                F3: "Place : " + $("#ddlPlace option:selected").text(),
                comp_name: window.globalVariables.companyName,
                comp_add1: window.globalVariables.add1,
                comp_add2: window.globalVariables.add2
            }
        };

        $.ajax({
            url: 'http://localhost:34089/Report/PendingQCReport',
            type: 'POST',
            data: JSON.stringify(formulaFields),
            contentType: "application/json",
            xhrFields: { responseType: 'blob' },
            success: function (response) {
                var file = new Blob([response], { type: 'application/pdf' });
                var fileName = `${reportName}.pdf`;

                var link = document.createElement('a');
                link.href = URL.createObjectURL(file);
                link.download = fileName;
                link.click();
            }
        });
    } finally {
        isPrinting = false;
    }
    
}
