const allFieldIds = [
    "NumDocNo",
    "DtDocDate",
    "NumTotalRec",
    "ddlShift",
    "ddlPlace",
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
        if (readOnly === 'true') {
            readOnlyMode();
        }
    } catch (err) {
        toastr.error('Initialization failed: ' + err);
    }
}

function wireEvents() {

    $('#btn-save').on('click', async (e) => {
        e.preventDefault();

        const isValidDate = await checkValidDate();
        if (!isValidDate) return;

        if (!validateRequiredField('#NumDocNo', 'Doc No') ||
            !validateRequiredField('#DtDocDate', 'Doc Date') ||
            !validateRequiredField('#ddlShift', 'Shift') ||
            !validateRequiredField('#ddlPlace', 'Place') ||
            !validateRequiredField('#ddlInspectBy', 'Inspect By')) return;

        if (!validateItemRows) return;
        try {
            const data = await collectFormData();
            if (data.Prod2QCData && data.Prod2QCData.length) {
                docId ? UpdateData(data) : SaveData(data);
            } else {
                showToast("Please fill First Row atleast", { type: "warning" });
                return;
            }
        } catch (err) {
            showToast("Error while saving", { type: "error" });
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
        } else {
            showToast("Cannot delete the first row.", { type: "error" });
        }
    });

    $(document).on('click', '.btn-add-row, .btn-add-action', function () {
        addItemRecordRow();
    });

    $(document).on('change', 'select[id^="ddlLoom"]', function () {
        const selectedLoomId = $(this).val();
        const row = $(this).closest('tr');
        if (selectedLoomId) {
            fetchAndFillRowDataByLoom(selectedLoomId, row);
        }
    });

    $(document).on('change', 'select[id^=ddlItemName]', function () {
        const selectedItemId = $(this).val();
        const row = $(this).closest('tr');
        if (selectedItemId) {
            fetchAndFillRowDataByItem(selectedItemId, row);
        }
    });

    $(document).on('change', '#ddlPlace', function () {
        let tbody = $('#tblFabricStrengthEntry tbody');
        tbody.find('tr').each(function (index, row) {
            let rowIndex = index + 1;
            bindDropdownData(rowIndex);
        });
    });

    $(document).on('input', 'input[id^=TxtWeftWay]', function () {
        let currentRow = $(this).closest('tr');
        let rowIndex = currentRow.index() + 1;
        var WeftWay = $(this).val();
        let warpWayValue = currentRow.find('input[id^=TxtWarpWay]').val();
        bindStrengthDropdown(rowIndex, WeftWay, warpWayValue);

    });

    $(document).on('input', 'input[id^="TxtWarpWay"], input[id^="TxtWeftWay"]', function () {

        const $row = $(this).closest('tr');
        const rowIndex = $row.index() + 1;

        const warpWay = $row.find(`#TxtWarpWay${rowIndex}`).val().trim();
        const weftWay = $row.find(`#TxtWeftWay${rowIndex}`).val().trim();

        if (warpWay !== '' && weftWay !== '') {

            const combinedText = `${warpWay} - ${weftWay}`;
            const $strength = $row.find(`#ddlStrength${rowIndex}`);

            bindStrengthDropdown(rowIndex, weftWay, warpWay).then(() => {

                let matchedValue = null;

                $strength.find('option').each(function () {
                    if ($(this).text().trim() === combinedText) {
                        matchedValue = $(this).val();
                        return false;
                    }
                });

                if (matchedValue) {
                    $strength.val(matchedValue).trigger('change');

                } else {
                    $strength.append(
                        `<option value="${combinedText}">${combinedText}</option>`
                    );

                    $strength.val(combinedText).trigger('change');
                }
            });
        }
    });

    $(document).on('focus', '[id^=ddlStrength]', function () {
        const $ddl = $(this);
        const id = $ddl.attr('id');
        const rowNum = id.replace('ddlStrength', '');
        const warpWayValue = $(`#TxtWarpWay${rowNum}`).val() || 0;
        const weftWayValue = $(`#TxtWeftWay${rowNum}`).val() || 0;

        bindStrengthDropdown(rowNum, weftWayValue, warpWayValue).then(() => {
            const combinedValue = `${warpWayValue} - ${weftWayValue}`;
            const options = $ddl.find('option');
            let matchedOption = null;

            options.each(function () {
                if ($(this).text() === combinedValue) {
                    matchedOption = $(this).val();
                    return false;
                }
            });

            if (matchedOption) {
                $ddl.val(matchedOption).trigger('change');
            } else {
                if (combinedValue.trim()) {
                    $ddl.append(`<option value="${combinedValue}">${combinedValue}</option>`);
                    $ddl.val(combinedValue).trigger('change');
                }
            }
        });
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
                // Controls
                const $lid = row.find('input[id^="TxtLID"]');
                const $item = row.find('select[id^="ddlItemName"]');
                const $type = row.find('select[id^="ddlType"]');
                const $width = row.find('input[id^="TxtWidth"]');
                const $gram = row.find('input[id^="TxtGram"]');
                const $color = row.find('select[id^="ddlColor"]');
                const $dnr = row.find('input[id^="TxtDNR"]');
                $lid.val(loomCode);

                // Wait a bit to ensure dropdowns are fully initialized
                //setTimeout(function () {

                //    // Item Name
                //    if ($item.find(`option[value="${data.ITEM_CODE}"]`).length > 0) {
                //        $item.val(data.ITEM_CODE).trigger('change.select2');
                //    }

                //    // Type
                //    if ($type.find(`option[value="${data.PTYPE_CODE}"]`).length > 0) {
                //        $type.val(data.PTYPE_CODE).trigger('change.select2');
                //    }

                //    // Width
                //    $width.val(data.WIDTH);

                //    // Gram
                //    $gram.val(data.GRAM);

                //    // Color
                //    if ($color.find(`option[value="${data.COLOR_CODE}"]`).length > 0) {
                //        $color.val(data.COLOR_CODE).trigger('change.select2');
                //    }

                //    // DNR
                //    $dnr.val(data.DNR);
                //    row.find('input[id^="TxtWarpWay"]').focus().select();
                //}, 200);

                if ($item.find(`option[value="${data.ITEM_CODE}"]`).length > 0) {
                    $item.val(data.ITEM_CODE).trigger('change');
                }

                if ($type.find(`option[value="${data.PTYPE_CODE}"]`).length > 0) {
                    $type.val(data.PTYPE_CODE).trigger('change');
                }

                // Width
                $width.val(data.WIDTH);
                $gram.val(data.GRAM);

                if ($color.find(`option[value="${data.COLOR_CODE}"]`).length > 0) {
                    $color.val(data.COLOR_CODE).trigger('change');
                }

                $dnr.val(data.DNR);
                row.find('input[id^="TxtWarpWay"]').focus().select();
            }
            else {
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
        url: `/LoomFabricStrengthEntry/GetItemList?itemCode=${itemCode}`,
        type: 'GET',
        dataType: 'json',
        success: function (response) {
            if (response.status && response.data.length > 0) {
                const data = response.data[0];
                // Type Dropdown
                row.find('select[id^="ddlType"]').val(data.PTYPE_CODE).trigger('change');

                // Width
                row.find('input[id^="TxtWidth"]').val(data.WIDTH);

                // Gram
                // If TxtGram is textbox, use GRAM_NAME or GRAM_CODE as needed
                row.find('input[id^="TxtGram"]').val(data.GRAM_NAME);   // or data.GRAM_CODE

                // Color Dropdown
                row.find('select[id^="ddlColor"]').val(data.COLOR_CODE).trigger('change');

                // Optional hidden fields
                row.find('input[id^="hdnItemCode"]').val(data.CODE);
                row.find('input[id^="hdnPTypeCode"]').val(data.PTYPE_CODE);
                row.find('input[id^="hdnColorCode"]').val(data.COLOR_CODE);
            }
            else {
                showToast("No Data Found For Selected  Item", { type: "warning" });
            }
        },
        error: function () {
            showToast("Failed To Load Data For Selected Item", { type: "error" });
        }
    });
}

async function handleDocLoad() {
    docId = getQueryParam('id');
    readOnly = getQueryParam('readOnly');
    if (docId) {
        $('#ddlDocType').prop('disabled', true);
        await GetDocData(docId, readOnly);

    } else {
        GetDocid();
        const today = new Date();
        const todayDate = today.getFullYear() + '-' +
            (today.getMonth() + 1).toString().padStart(2, '0') + '-' +
            today.getDate().toString().padStart(2, '0');
        $('#DtDocDate').val(todayDate);
        
        await Promise.all([
            GetShiftList(),
            GetPlaceList(),
            GetEmployeeList()
        ]);
        await loadLastQCEntry();
        addItemRecordRow();
    }
}

function SaveData(saveDt) {
    $.ajax({
        url: '/LoomFabricStrengthEntry/SaveOrUpdateLoomFabricEntry',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(saveDt),
        success: function (response) {
            if (response?.status) {
                showToast("Data Saved Successfully", { type: "success" });
                $('#btn-save').hide();
                //setTimeout(() => {
                //    window.location.href = '/LoomFabricStrengthEntryList/Index';
                //}, 1500);
            } else {
                showToast(response?.message || "Saved Failed. Please Try Again", { type: "error" });
            }
        },
        error: function () {
            showToast("Error Occurred While Saving.", { type: "error" });
        }
    });
}

function UpdateData(UpdateDt) {
    $.ajax({
        url: '/LoomFabricStrengthEntry/SaveOrUpdateLoomFabricEntry',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(UpdateDt),
        dataType: 'json',
        success: function (response) {
            if (response?.status) {
                showToast("Data Updated Successfully", { type: "success" });
                $('#btn-save').hide(); 
                //setTimeout(() => {
                //    window.location.href = '/LoomFabricStrengthEntryList/Index';
                //}, 1500);
            } else {
                showToast(response?.message || "Update Failed. Please Try Again", { type: "error" });
            }
        },
        error: function (xhr, status, error) {
            showToast("Error Occurred While Updating.", { type: "error" });
        }
    });
}

async function fillHeaderData(headdata) {
    if (!Array.isArray(headdata) || headdata.length === 0) {
        showToast("No Header Data Found TO Populate The Form", { type: "error" });
        return;
    }
    const data = headdata[0];
    $("#TxtDocId").val(data.DOC_ID ?? "");
    $("#NumDocNo").val(data.V_NO ?? "");
    $("#DtDocDate").val(data.V_DATE ? data.V_DATE.substring(0, 10) : "");
    GetShiftList(data.SHIFT ?? 0);
    GetPlaceList(data.PLACE_CODE ?? 0);
    GetEmployeeList(data.EMP_CODE ?? 0);
    $("#TxtRemarks").val(data.REMARKS ?? "");
    $("#NumTotalRec").val(0);

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
        VType: 'LMQC',
        SHIFT: getVal("ddlShift"),
        PLACE_CODE: parseIntOrNull(getVal("ddlPlace")),
        EMP_CODE: parseIntOrNull(getVal("ddlInspectBy")),
        REMARKS: getVal("TxtRemarks"),
        QCTIME: getVal("TxtQcTime"),
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

async function collectItemsDetail() {
    const items = [];
    $('#tblFabricStrengthEntry tbody tr').each(function () {
        const idx = this.id.replace('row', '');
        const $r = $(this);
        var loomId = parseIntSafe($r.find(`#ddlLoom${idx}`).val());

        if (loomId && loomId > 0) {
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

                WARP_ELONG: parseDecimalSafe($r.find(`#TxtWarpElong${idx}`).val()),
                WEFT_ELONG: parseDecimalSafe($r.find(`#TxtWeftElong${idx}`).val()),
                WARP_MESH: parseDecimalSafe($r.find(`#TxtWarpMesh${idx}`).val()),
                WEFT_MESH: parseDecimalSafe($r.find(`#TxtWeftMesh${idx}`).val()),
                MESH_CODE: parseIntSafe($r.find(`#ddlStrength${idx}`).val()),
                REMARKS: toNullableString($r.find(`#TxtRemarks${idx}`).val()),
                PLACE_CODE: null,
                EMP_CODE: 0,
                MESH: $r.find(`#ddlStrength${idx}`).val() ? $r.find(`#ddlStrength${idx} option:selected`).text() : null,
                RUNNO: null,
                LOOM_TYPE: null,
                MAKE_T: null,
                RESULT1: parseDecimalSafe($r.find(`#TxtWarpWay${idx}`).val()),
                REMARKS1: null,
                RESULT2: parseDecimalSafe($r.find(`#TxtWeftWay${idx}`).val()),
                REMARKS2: null,
                PRKG: null,
                WASTE: null,
                PSIZE: null,
                CPRDN: null,
                PAISA_TYPE: null,
                PAISA_SIZE: null,
                PAISA_MTR: null,
                PAISA_TYPE1: null,
                PORD_TYPE: null,
                PORD_NO: null,
                COND1: null,
                COND2: null,
                SHIFT_SCH: null,
                REPORT_FILTER: null,
                TIME1_WIDTH: null,
                TIME2_WIDTH: null,
                TIME3_WIDTH: null,
                TIME4_WIDTH: null,
                TIME5_WIDTH: null,
                PC_LOWMELT: null,
                GLUE_CONTENT: null,
                OTHERS: null,
                YELLOWP: null,
                BLUEP: null,
                OTHERP: null,
                GRADE: null,
                YELLOW160C: null,
                MOISTURE: null,
                BULKDENSITY: null,
                PH_FLAKES: null,
                OVERSIZED: null,
                SRNO: null,
                SUPPLY_TYPE: null,
                COLOR_TYPE: null
            });
        }
    });

    return items;
}

async function fillItemDetailTable(itemsData) {
    console.log("Filling item details:", itemsData);
    const $tbody = $('#tblFabricStrengthEntry tbody');
    $tbody.empty();

    for (let index = 0; index < itemsData.length; index++) {
        const item = itemsData[index];
        const idx = index + 1;

        addItemRecordRow();
        await bindDropdownData(idx);

        $(`#TxtLID${idx}`).val(item.LOOM_CODE || '');
        $(`#TxtWidth${idx}`).val(item.WIDTH ?? '');
        $(`#TxtGram${idx}`).val(item.GRAM ?? '');
        $(`#TxtDNR${idx}`).val(item.DNR ?? '');
        $(`#TxtWarpWay${idx}`).val(item.RESULT1 ?? '');
        $(`#TxtWeftWay${idx}`).val(item.RESULT2 ?? '');
        $(`#TxtWarpElong${idx}`).val(item.WARP_ELONG ?? '');
        $(`#TxtWeftElong${idx}`).val(item.WEFT_ELONG ?? '');
        $(`#TxtWarpMesh${idx}`).val(item.WARP_MESH ?? '');
        $(`#TxtWeftMesh${idx}`).val(item.WEFT_MESH ?? '');
        $(`#TxtRemarks${idx}`).val(item.REMARKS ?? '');

        const safeSetDropdown = (selector, value) => {
            const $select = $(`${selector}${idx}`);
            if ($select.find(`option[value="${value}"]`).length > 0) {
                $select.val(value).trigger('change');
            }
        };

        safeSetDropdown('#ddlLoom', item.LOOM_CODE);
        safeSetDropdown('#ddlItemName', item.ITEM_CODE);
        safeSetDropdown('#ddlType', item.PTYPE_CODE);
        safeSetDropdown('#ddlColor', item.COLOR_CODE);
        /* safeSetDropdown('#ddlStrength', item.MESH_CODE);*/
        console.log("RESULT1 =", item.RESULT1);
        console.log("RESULT2 =", item.RESULT2);
        await bindStrengthDropdown(
            idx,
            item.RESULT1,
            item.RESULT2
        );
        $(`#ddlStrength${idx}`).val(item.MESH_CODE).trigger('change');
        
    }
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
                showToast("Document Type Load Failed :" +error , { type: "error" });
                reject(error);
            }
        });
    });
}

function GetDocid() {
    $.ajax({
        url: '/LoomFabricStrengthEntry/GetMaxVNo',
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
            showToast("Error fetching Doc ID:" + error, { type: "error" });
        }
    });
}

function GetPlaceList(selectedValue = null) {
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

                $DropdownId.on('select2:open', function () {
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
                showToast("Place Name Load Failed", { type: "error" });
            }
        },
        error: function (xhr, status, error) {
            showToast("Place Name Load Failed ", error, { type: "error" });
        }
    });
}

async function GetEmployeeList(selectedValue = null) {

    return new Promise((resolve, reject) => {

        $.ajax({
            url: '/LoomFabricStrengthEntry/GetUserMast',
            type: 'GET',
            dataType: 'json',

            success: function (response) {

                if (response.status) {

                    const $DropdownId = $('#ddlInspectBy');

                    $DropdownId.empty();
                    $DropdownId.append(
                        '<option value="">- Select Employee -</option>'
                    );

                    $.each(response.data, function (index, item) {

                        $DropdownId.append(
                            `<option value="${item.CODE}" data-name="${item.NAME}">
                                ${item.NAME} | ${item.CODE}
                            </option>`
                        );

                    });

                    $DropdownId.select2({
                        placeholder: "- Select Employee -",
                        allowClear: true,
                        width: '100%',
                        templateSelection: function (data) {

                            if (!data.id) return data.text;

                            const selectedOption = $(data.element);
                            return selectedOption.data('name') || data.text;
                        }
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

                    if (
                        selectedValue &&
                        $DropdownId.find(`option[value="${selectedValue}"]`).length > 0
                    ) {

                        $DropdownId.val(String(selectedValue)).trigger('change');

                    } else {

                        $DropdownId.val('').trigger('change');

                    }

                    resolve(response); // IMPORTANT
                }
                else {

                    showToast("Employee Name Load Failed", { type: "error" });
                    reject("Invalid response");
                }
            },

            error: function (xhr, status, error) {

                showToast("Employee Name Load Failed", { type: "error" });
                reject(error);
            }
        });

    });
}

function GetShiftList(selectedValue = null) {
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

                $DropdownId.on('select2:open', function () {
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
                showToast("Shift Load Failed", { type: "error" });
            }
        },
        error: function (xhr, status, error) {
            showToast("Shift Load Failed", error, { type: "error" });
        }
    });
}

async function GetDocData(MasterTblId, readOnly) {
    try {
        const response = await $.ajax({
            url: '/LoomFabricStrengthEntry/GetLoomFabricSById',
            type: 'GET',
            data: { id: MasterTblId }
        });
        if (response.status) {
            await fillHeaderData(response.header);
            await fillItemDetailTable(response.detail);
            if (readOnly === 'true') {
                $('#btn-save, #cancelBtn').hide();
                disableAllFields();
            } else {
                $('#btn-save, #cancelBtn').show();
                enableAllFields();
            }
        } else {
            showToast("No Data Found", { type: "error" });
        }
    } catch (error) {
        showToast("Failed To Load Data", { type: "error" });
    }
}

function addItemRecordRow() {
    let tbody = $('#tblFabricStrengthEntry tbody');
    let rowCount = tbody.find('tr').length + 1;

    let newRow = `
        <tr class="no-border-input" id="row${rowCount}">
            <td><input type="text" style="width:100px;" class="form-control" id="TxtLID${rowCount}" readonly/></td>

            <td>
                <select class="form-control" style="width:400px;" id="ddlLoom${rowCount}">
                    <option value="">- Select Loom -</option>
                </select>
            </td>

            <td>
                <select class="form-control" style="width:400px;" id="ddlItemName${rowCount}">
                    <option value="">- Select Item Name -</option>
                </select>
            </td>

            <td style="display:none;">
                <select class="form-control" style="width:200px;" id="ddlType${rowCount}">
                    <option value="">- Select Type -</option>
                </select>
            </td>

            <td style="display:none;"><input type="text" style="width:100px;" maxlength="10" oninput="allowOnlyDecimal(this)" class="form-control" id="TxtWidth${rowCount}" /></td>
            <td style="display:none;"><input type="text" style="width:100px;" maxlength="10" oninput="allowOnlyDecimal(this)" class="form-control" id="TxtGram${rowCount}" /></td>

            <td style="display:none;">
                <select class="form-control" style="width:100px;" id="ddlColor${rowCount}">
                    <option value="">- Select Color -</option>
                </select>
            </td>

            <td><input type="text" style="width:100px; class="form-control" maxlength="10" oninput="allowOnlyDecimal(this)" id="TxtDNR${rowCount}" /></td>
            <td><input type="text" style="width:100px; class="form-control" maxlength="10" oninput="allowOnlyDecimal(this)" id="TxtWarpWay${rowCount}" /></td>
            <td><input type="text" style="width:100px; class="form-control" maxlength="10" oninput="allowOnlyDecimal(this)" id="TxtWeftWay${rowCount}" /></td>

            <td>
                <select class="form-control" id="ddlStrength${rowCount}">
                    <option value="">- Select Strength -</option>
                </select>
            </td>

            <td><input type="text" style="width:100px; class="form-control" maxlength="10" oninput="allowOnlyDecimal(this)" id="TxtWarpElong${rowCount}" /></td>
            <td><input type="text" style="width:100px; class="form-control" maxlength="10" oninput="allowOnlyDecimal(this)" id="TxtWeftElong${rowCount}" /></td>
            <td><input type="text" style="width:100px; class="form-control" maxlength="10" oninput="allowOnlyDecimal(this)" id="TxtWarpMesh${rowCount}" /></td>
            <td><input type="text" style="width:100px; class="form-control" maxlength="10" oninput="allowOnlyDecimal(this)" id="TxtWeftMesh${rowCount}" /></td>
            <td><input type="text" style="width:100px; class="form-control" maxlength="300" id="TxtRemarks${rowCount}" /></td>

            <td class="action-col">
                <button class="act-btn add btn-add-action btn-Itemadd-action" title="Add Row" style="cursor:pointer;"><i class="fa fa-plus"></i></button>
                <button class="act-btn delete btn-delete-action btn-Itemdelete-action" title="Delete Row" style="cursor:pointer;"><i class="fa fa-trash"></i></button>
            </td>
        </tr>
        `;
     
    tbody.append(newRow);
    bindDropdownData(rowCount);
    setEnterKeyFocusOnTable?.(itemRecords, rowCount);
}

function bindDropdownData(rowCount, minStd = 0, maxStd = 0) {
    const loomSelect = $(`#ddlLoom${rowCount}`);
    const itemSelect = $(`#ddlItemName${rowCount}`);
    const typeSelect = $(`#ddlType${rowCount}`);
    const colorSelect = $(`#ddlColor${rowCount}`);
    const strengthSelect = $(`#ddlStrength${rowCount}`);
    const placeCode = $('#ddlPlace').val() || 0;
    console.log(placeCode, minStd, maxStd);
    const loadDropdown = (url, selectElem, defaultText, formatter) => {
        return new Promise((resolve, reject) => {
            $.ajax({
                url: url,
                type: 'GET',
                dataType: 'json',
                success: function (response) {
                    if (response.status) {
                        selectElem.empty().append(
                            $('<option>', {
                                value: '',
                                text: defaultText,
                                disabled: true,
                                selected: true
                            })
                        );

                        $.each(response.data, function (i, item) {
                            selectElem.append(formatter(item));
                        });

                        selectElem.select2({
                            width: '100%',
                            placeholder: defaultText,
                            allowClear: true,
                            minimumResultsForSearch: 0
                        });

                        selectElem.on('select2:open', function () {
                            setTimeout(function () {
                                let searchBox = document.querySelector(
                                    '.select2-container--open .select2-search__field'
                                );

                                if (searchBox) {
                                    searchBox.focus();
                                }
                            }, 0);
                        });

                        resolve();
                    } else {
                        showToast(`${defaultText} load failed`, { type: "error" });
                        resolve();
                    }
                },
                error: function (xhr, status, error) {
                    showToast(`Error loading ${defaultText}: ${error}`, { type: "error" });
                    reject(error);
                }
            });
        });
    };

    return Promise.all([
        loadDropdown(
            `/LoomFabricStrengthEntry/GetLoomList?PlaceCode=${placeCode}`,
            loomSelect,
            "- Select Loom -",
            item => `<option value="${item.CODE}">${item.NAME}</option>`
        ),
        loadDropdown(
            '/LoomFabricStrengthEntry/GetItemList',
            itemSelect,
            "- Select Item -",
            item => `<option value="${item.CODE}">${item.NAME}</option>`
        ),
        loadDropdown(
            '/LoomFabricStrengthEntry/GetItemType',
            typeSelect,
            "- Select Type -",
            item => `<option value="${item.CODE}">${item.NAME}</option>`
        ),
        loadDropdown(
            '/LoomFabricStrengthEntry/GetColor',
            colorSelect,
            "- Select Color -",
            item => `<option value="${item.CODE}">${item.NAME}</option>`
        ),
        loadDropdown(
            `/LoomFabricStrengthEntry/GetStrengthList?minStd=${minStd}&maxStd=${maxStd}`,
            strengthSelect,
            "- Select Strength -",
            item => `<option value="${item.CODE}">${item.NAME}</option>`
        )
    ]);
}

function bindStrengthDropdown(rowCount, minStd = 0, maxStd = 0) {
    const strengthSelect = $(`#ddlStrength${rowCount}`);
    return $.ajax({
        url: `/LoomFabricStrengthEntry/GetStrengthList?minStd=${minStd}&maxStd=${maxStd}`,
        type: 'GET',
        dataType: 'json',
        success: function (response) {
            console.log("matchingCode =", response.matchingCode);
            if (response.status) {
                strengthSelect.empty().append(
                    $('<option>', {
                        value: '',
                        text: "- Select Strength -",
                        disabled: true,
                        selected: true
                    })
                );

                $.each(response.data, function (i, item) {
                    strengthSelect.append(`<option value="${item.CODE}">${item.NAME}</option>`);
                });

                strengthSelect.select2({
                    width: '100%',
                    placeholder: "- Select Strength -",
                    allowClear: true,
                    minimumResultsForSearch: 0
                });

                strengthSelect.on('select2:open', function () {
                    setTimeout(function () {
                        let searchBox = document.querySelector(
                            '.select2-container--open .select2-search__field'
                        );

                        if (searchBox) {
                            searchBox.focus();
                        }
                    }, 0);
                });

                if (response.isExist && response.matchingCode) {
                    strengthSelect.val(response.matchingCode).trigger('change');
                }

            } else {
                showToast("Failed To Load Strength List", { type: "error" });
            }
        },
        error: function (xhr, status, error) {
            showToast(`Error loading strength list: ${error}`, { type: "error" });
        }
    });
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
        let [day, month, year] = parts.map(p => parseInt(p, 10));
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
    allFieldIds.forEach(id => {
        const el = document.getElementById(id);
        if (el) el.disabled = false;
    });
}

function disableAllFields() {
    allFieldIds.forEach(id => {
        const el = document.getElementById(id);
        if (el) el.disabled = true;
    });
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

async function checkValidDate() {

    const data = {
        vdate: $("#DtDocDate").val(),
        vtype: "LMQC",
        vno: $("#NumDocNo").val()
    };

    try {

        const response = await fetch('/LoomFabricStrengthEntry/CheckValidDate', {
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
        console.error(error);
        showToast("Date validation failed", { type: "error" });
        return false;
    }
}

function validateItemRows() {

    let validRowCount = 0;
    let isValid = true;

    $('#tblFabricStrengthEntry tbody tr').each(function () {

        const rowId = $(this).attr('id').replace('row', '');

        const loom = $(`#ddlLoom${rowId}`).val();
        const item = $(`#ddlItemName${rowId}`).val();
        const type = $(`#ddlType${rowId}`).val();
        const color = $(`#ddlColor${rowId}`).val();
        const strength = $(`#ddlStrength${rowId}`).val();

        const result1 = parseFloat($(`#TxtWarpWay${rowId}`).val()) || 0;
        const result2 = parseFloat($(`#TxtWeftWay${rowId}`).val()) || 0;

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

        if (!strength || Number(strength) === 0) {
            setInvalid($(`#ddlStrength${rowId}`), 'Invalid Strength.');
            isValid = false;
            return false;
        }

        if (!color || Number(color) === 0) {
            setInvalid($(`#ddlColor${rowId}`), 'Invalid Color.');
            isValid = false;
            return false;
        }

        if (result1 === 0) {
            setInvalid($(`#TxtWarpWay${rowId}`), 'Result1 is required.');
            isValid = false;
            return false;
        }

        if (result2 === 0) {
            setInvalid($(`#TxtWeftWay${rowId}`), 'Result2 is required.');
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

async function loadLastQCEntry() {
    try {

        const response = await $.ajax({
            url: '/LoomFabricStrengthEntry/GetLastQCEntry',
            type: 'GET'
        });

        if (response.status && response.data) {
            console.log("Load Prev Data", response);
            
            if (response.data.shift) {
                console.log("Shift options =", $('#ddlShift option').length);
                $('#ddlShift')
                    .val(response.data.shift)
                    .trigger('change');
            }

            if (response.data.placE_CODE) {
                console.log("Place options =", $('#ddlPlace option').length);
                $('#ddlPlace').val(response.data.placE_CODE).trigger('change');
            }

            if (response.data.emP_CODE) {
                console.log("Employee options =", $('#ddlInspectBy option').length);
                $('#ddlInspectBy').val(response.data.emP_CODE).trigger('change');
            }
        }

    } catch (err) {
        console.error("Failed to load last QC entry", err);
    }
}

function readOnlyMode() {

    console.log("Function Calleed");
   
    $("#TxtQcInchargeName, #TxtChemistName, #TxtQcTime").prop("readonly", true).prop("disabled", true);

    $("#ddlQcIncharge, #ddlChemist, #ddlPlace, #ddlShift, #ddlInspectBy").prop("disabled", true).trigger("change.select2");

    $('#tblFabricStrengthEntry tbody tr').each(function () {

        $(this).find('input, textarea').prop('disabled', true).prop('readonly', true);
        $(this).find('select').prop('disabled', true).trigger("change.select2");
        $(this).find('button').css({
            'pointer-events': 'none',
            'opacity': '0.4'
        });
    });

    $('#tblFabricStrengthEntry').css({
        'pointer-events': 'none',
        'opacity': '0.85'
    });

    $("#formContainer").find("input, select, textarea, button").prop("disabled", true).prop("readonly", true).attr("tabindex", "-1");
    $("#btn-save, #cancelBtn, .btn-add-action, .btn-delete-action").prop("disabled", true);
}

