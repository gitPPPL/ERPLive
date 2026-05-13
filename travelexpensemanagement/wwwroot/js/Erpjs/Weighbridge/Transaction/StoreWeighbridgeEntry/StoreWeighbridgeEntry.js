var allFieldIds = [
    "DtDocDate",
    "ddlStatus",
    "ddlGateNo",
    "DdlPartyName",
    "TxtPartyWeight",
    "ChkCrystalReport",
    "Txtlabel12"
];
const itemRecords = [
    "TxtWeight",
    "TxtTWgt",
    "TxtNWgt",
    "TxtDateTime",
    "TxtFrom",
    "TxtTo",
    "TxtItem",
    "TxtRemarks"
];

let GateList = [];
let docId = "";
let readOnly;
let docTypeInOut = "";
let isFillingData = false;

function getQueryParam(param) {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get(param);
}
/* ================= INIT ================= */
$(async function () {
    try {
        await bindDropdown('StoreWeighbridgeEntry', 'DocType', '#ddlDocType', '-- Select Doc Type --', null, null, true, null, false);
        await bindDropdown('StoreWeighbridgeEntry', 'DocStatus', '#ddlStatus', '-- Select Status --', null, null, true, null, false);
        await bindDropdown('StoreWeighbridgeEntry', 'Party', '#DdlPartyName', '-- Select Party Name --', null, null, false, null, true);
        //$('#ddlDocType').prop('selectedIndex', 0);
        handleDocLoad();
        docTypeInOut = $('#ddlDocType').val();
        toggleControls(docTypeInOut);
        if (!docId) {
            addItemRecordRow();
            //const $row = addItemRecordRow();

            await bindRowDropdowns();
        }
    } catch (error) {
        showToast('Failed to load document types: ', { type: "error" });
    }
});
$(document).ready(function () {
    GetGateEntryList();
    setEnterKeyFocus(allFieldIds);

    /* ================= DOC TYPE CHANGE ================= */
    $('#ddlDocType').on('change', function () {
        const VType = $(this).val();
        if (VType) {
            GetDocid(VType);
            docTypeInOut = VType;
            toggleControls(docTypeInOut);
        }
    });

    $('#ddlGateNo').on('change', function () {
        if (isFillingData) return;

        var gateNo = $(this).val();
        var selectedOption = $(this).find('option:selected');

        if (gateNo) {
            var partyCode = selectedOption.data('party');
            var vType = selectedOption.data('vtype');
            var partyName = selectedOption.data('partynm');

            ensureOption($('#DdlPartyName'), partyCode, partyName);
            $('#DdlPartyName').val(partyCode).trigger('change');

            GetGateEntryDetailList(gateNo, vType);
        } else {
            $('#DdlPartyName').val('');
        }
    });
    /* ================= SAVE ================= */
    $('#btn-save').on('click', async function (e) {
        e.preventDefault();
        const docType = $('#ddlDocType').val();
        const gateNo = $('#ddlGateNo').val();

        if (!validateRequiredField('#ddlDocType', 'Doc Type') || !validateRequiredField('#NumDocNo', 'Doc No') || !validateRequiredField('#DtDocDate', 'Doc Date')) return;

        if (docType === "KSIN" && !gateNo) {
            showToast('Gate No is required for KSIN documents.', { type: "warning" });
            $('#ddlGateNo').focus();
            return;
        }

        if (!validateBigWeighbridgeTable()) {
            return;
        }

        const $btn = $(this);
        $btn.prop('disabled', true);

        try {
            const tableData = await collectFormData();
            
            if (docId) {
                UpdateData(tableData);
            } else {
                SaveData(tableData);
            }
        } catch (error) {
            console.error('Error during save:', error);
            showToast('An error occurred while saving the data.', { type: "error" });
        } finally {
            $btn.prop('disabled', false);
        }
    });
});

/* ========= Gate and Party TOGGLE CONTROLS ================= */
function toggleControls(docTypeInOut) {
    if (docTypeInOut == "KSIN") {
        $('#ddlGateNo').prop('disabled', false);
        $('#DdlPartyName').prop('disabled', true);
    } else {
        $('#ddlGateNo').prop('disabled', true);
        $('#DdlPartyName').prop('disabled', false);
    }
}

function validateBigWeighbridgeTable() {
    let isValid = true;
    let errorMessage = '';

    const rows = $('#tblStoreWeighbridge tbody tr');
    const docTypeInOut = $('#ddlDocType').val();

    rows.each(function (index) {
        const $row = $(this);
        const weight = $row.find('.txt-weight').val()?.trim();
        const tweight = $row.find('.txt-twgt').val()?.trim();
        const nweight = $row.find('.txt-nwgt').val()?.trim();
        const datetime = $row.find('.txt-datetime').val()?.trim();
        const fromDept = $row.find('.ddlFromPlace').val()?.trim();
        const toDept = $row.find('.ddlToPlace').val()?.trim();
        const item = $row.find('.txt-item').val()?.trim();

        const parsedWeight = parseFloat(weight) || 0;


        if (docTypeInOut === "KSIN") {
            if (item && (!weight || weight <= 0 || !tweight || !nweight || !datetime)) {
                isValid = false;
                errorMessage = `Please fill Weight, T.Weight, N.Weight, and DateTime in row ${index + 1} where Item Name is filled. and weight > 0 .`;
                return false; // exit loop early
            }
            if (!fromDept || !toDept || fromDept === toDept) {
                isValid = false;
                errorMessage = `Please ensure 'From Department' and 'To Department' are filled and not the same in row ${index + 1}.`;
                return false; // exit loop early
            }

        } else {
            if (index === 0) {
                const allFilled = weight && nweight && datetime && fromDept && toDept && item;
                if (!allFilled && weight <= 0) {
                    isValid = false;
                    errorMessage = 'First row is mandatory. Please fill all fields: Weight, N.Weight, DateTime, From, To, and Item. and weight > 0 .';
                    return false;
                }
                if (!fromDept || !toDept || fromDept === toDept) {
                    isValid = false;
                    errorMessage = `Please ensure 'From Department' and 'To Department' are filled and not the same in row ${index + 1}.`;
                    return false; // exit loop early
                }
            } else {
                if (parsedWeight > 0) {
                    const allFilled = nweight && datetime && fromDept && toDept && item;
                    if (!allFilled) {
                        isValid = false;
                        errorMessage = `Please fill all required fields in row ${index + 1} where Weight > 0.`;
                        return false;
                    }
                    if (!fromDept || !toDept || fromDept === toDept) {
                        isValid = false;
                        errorMessage = `Please ensure 'From Department' and 'To Department' are filled and not the same in row ${index + 1}.`;
                        return false; // exit loop early
                    }
                }
            }
        }
    });

    if (!isValid) {
        //toastr.error(errorMessage);
        showToast(errorMessage, { type: "warning" });
    }

    return isValid;
}

async function handleDocLoad() {
    docId = getQueryParam('id');
    readOnly = getQueryParam('readOnly');
    if (docId) {
        $('#ddlDocType').prop('disabled', true);
        await GetDocData(docId, readOnly);
        docTypeInOut = $('#ddlDocType').val();
        toggleControls(docTypeInOut);
    } else {
        const Vtype = $('#ddlDocType').val();
        if (Vtype) {
            GetDocid(Vtype);
        }
        const today = new Date();
        const todayDate = today.getFullYear() + '-' + (today.getMonth() + 1).toString().padStart(2, '0') + '-' + today.getDate().toString().padStart(2, '0');
        $('#DtDocDate').val(todayDate);
    }
}

function SaveData(saveDt) {
    $.ajax({
        url: '/StoreWeighbridgeEntry/SaveOrUpdateStoreWeighBridgeEntry',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(saveDt),
        success: function (response) {
            if (response?.status) {
                //toastr.success("Data saved successfully.");
                showToast("Data saved successfully.", { type: "success" });
                setTimeout(() => {
                    window.location.href = '/StoreWeighbridgeEntryList/Index';
                }, 1500);
            } else {
                //toastr.error(response?.message || "Save failed. Please try again.");
                showToast(response?.message || "Save failed. Please try again.", { type: "error" });
            }
        },
        error: function () {
            //toastr.error("An error occurred while saving. Please contact the administrator.");
            showToast("An error occurred while saving. Please contact the administrator.", { type: "error" });
        }
    });
}

function UpdateData(UpdateDt) {
    $.ajax({
        url: '/StoreWeighbridgeEntry/SaveOrUpdateStoreWeighBridgeEntry',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(UpdateDt),
        dataType: 'json',
        success: function (response) {
            if (response?.status) {
                //toastr.success("Data updated successfully");
                showToast("Data updated successfully", { type: "success" });
                setTimeout(() => {
                    window.location.href = '/StoreWeighbridgeEntryList/Index';
                }, 1500);
            } else {
                //toastr.error("Update failed: " + (response?.message || "Unknown error."));
                showToast("Update failed: " + (response?.message || "Unknown error."), { type: "error" });
            }
        },
        error: function (xhr, status, error) {
            //toastr.error("Data not updated: " + error);
            showToast("Data not updated: ", { type: "error" });
        }
    });
}

async function collectFormData() {
    const id = toNullableString(docId);
    const itemRecords = await collectItemsDetail();
    return {
        V_NO: parseIntSafe(document.getElementById("NumDocNo")?.value),
        V_TYPE: toNullableString(document.getElementById("ddlDocType")?.value),
        V_DATE: toNullableDate(document.getElementById("DtDocDate")?.value),
        GATE_NO: parseIntSafe(document.getElementById("ddlGateNo")?.value),
        PARTY_CODE: parseIntSafe(document.getElementById("DdlPartyName")?.value),
        DOC_ID: toNullableString(document.getElementById("TxtDocId")?.value),
        PARTY_QTY: null,
        STATUS: parseIntSafe(document.getElementById("ddlStatus")?.value),
        VEHICLE_NO: null,
        V_SHIFT: null,
        WB_TYPE: null,
        //GATE_TYPE: null,
        GATE_TYPE: $('#ddlGateNo option:selected').data('vtype'),
        GROSS_NO: null,
        TARE_NO: null,
        REMARKS: null,
        STATUS_DATE: null,
        NET_WGT: null,
        FINAL_TYPE: null,
        FINAL_REM: null,
        PARTY_GROSSWT: parseDecimalSafe(document.getElementById("TxtPartyWeight")?.value),
        PARTY_TRWT: null,
        PARTY_WBNO: null,
        SMALL_BAG: null,
        MEDIUM_BAG: null,
        LARGE_BAG: null,
        SaveOrUpdate: (!id || id === 0) ? 'Save' : 'Update',
        WB2Data: itemRecords

    };
}

async function collectItemsDetail() {
    const items = [];
    let SNo = 1;

    $('#tblStoreWeighbridge tbody tr').each(function () {

        const row = $(this);

        // Weight
        const weight = parseDecimalSafe(row.find('.txt-weight').val()) || 0;

        // Skip empty rows
        if (weight <= 0) {
            return true; // continue next row
        }

        // Date & Time
        const wgtDateRaw = row.find('.txt-datetime').val();

        let wgtDate = null;
        let wgtTime = null;

        if (wgtDateRaw && wgtDateRaw.includes('T')) {

            const [datePart, timePart] = wgtDateRaw.split('T');

            wgtDate = datePart || null;
            wgtTime = timePart || null;
        }

        // Selected text safely
        const itemText = row.find('.txt-item').find("option:selected").text() || '';
        const fromText = row.find('.ddlFromPlace').find("option:selected").text() || '';
        const toText = row.find('.ddlToPlace').find("option:selected").text() || '';

        // Object
        const item = {

            SNO: SNo,

            ITEM_NAME: itemText && itemText !== 'Select Item'
                ? itemText
                : null,

            ITEM_CODE: parseIntSafe(row.find('.txt-item').val()),
            
            WEIGHT: weight,

            TARE_WGT: parseDecimalSafe(row.find('.txt-twgt').val()),

            NET_WGT: parseDecimalSafe(row.find('.txt-nwgt').val()),

            WGT_DATE: wgtDate,

            WGT_TIME: wgtTime,

            FROM_NAME: fromText && fromText !== 'From'
                ? fromText
                : null,

            TO_NAME: toText && toText !== 'To'
                ? toText
                : null,

            FROM_PLACE: parseIntSafe(row.find('.ddlFromPlace').val()),
            
            TO_PLACE: parseIntSafe(row.find('.ddlToPlace').val()),
            
            REMARKS: toNullableString(row.find('.txt-remarks').val()),

            V_SHIFT: null,
            TYPE: null,
            STATUS: null,
            Ref_type: null,
            Ref_no: null,
            wb_time: null,
            COND: null,
            MOIS_PER: null,
            MOIS_WT: null
        };

        // Push only if item selected
        //if (item.ITEM_CODE && item.ITEM_CODE > 0) {

            items.push(item);

            SNo++;
        //}

    });

    
    return items;
}

async function fillHeaderData(headdata) {
    if (!Array.isArray(headdata) || headdata.length === 0) return;
    const data = headdata[0];

    $("#TxtDocId").val(data.DOC_ID ?? "");
    $("#ddlDocType").val(data.V_TYPE ?? "");
    $("#NumDocNo").val(data.V_NO ?? "");
    $("#DtDocDate").val((data.V_DATE ?? '').substring(0, 10));
    $("#TxtPartyWeight").val(data.PARTY_GROSSWT ?? "");
    $("#ChkCrystalReport").prop('checked', data.CRYSTAL_REPORT ?? false);
    $("#Txtlabel12").val(data.TXT_LABEL12 ?? "");

    GetGateEntryList(data.GATE_NO, data.GATE_TYPE);
    // GetPartyList(data.PARTY_CODE);           
    await bindDropdown('StoreWeighbridgeEntry', 'Party', '#DdlPartyName', '-- Select Party Name --', data.PARTY_CODE, null, false, null, true);
    $('#ddlStatus').val(data.STATUS).trigger('change');
}

async function fillItemDetailTable(itemsData) {
    
    if (!itemsData || itemsData.length === 0) {
        addItemRecordRow(); // add at least one row
        return;
    }

    for (const item of itemsData) {
        // Prepare datetime
        const Vdate = item.WGT_DATE ?? '';
        const Vtime = item.WGT_TIME ?? '';
        if (Vdate && Vtime) {
            item.dateTimeValue = `${Vdate.slice(0, 10)}T${Vtime.slice(0, 5)}`;
        }

        // Step 1: Add row
        addItemRecordRow(item);

        // Step 2: Get the newly added row
        const $row = $('#tblStoreWeighbridge tbody').find('tr').last();

        // Step 3: Bind dropdowns for this specific row **awaiting each async call**
        await bindDropdown('StoreWeighbridgeEntry', 'Place', $row.find('.ddlFromPlace'), '- From Place -', item.FROM_PLACE, null, false, null, true);
        await bindDropdown('StoreWeighbridgeEntry', 'Place', $row.find('.ddlToPlace'), '- To Place -', item.TO_PLACE, null, false, null, true);
        await bindDropdown('StoreWeighbridgeEntry', 'Items', $row.find('.txt-item'), '- Items -', item.ITEM_CODE, null, false, null, true);
    }
}
function GetDocid(VType) {
    $.ajax({
        url: '/StoreWeighbridgeEntry/GetMaxVNo',
        type: 'GET',
        data: { V_type: VType },
        success: function (response) {
            if (response.status === true && response.data) {
                $('#NumDocNo').val(response.data.vNo || '');
                $('#TxtDocId').val(response.data.docId || '');
            } else {
                $('#NumDocNo').val('');
                $('#TxtDocId').val('');
            }
        },
        error: function (xhr, status, error) {
            //toastr.error('Error fetching Doc ID:', error);
            showToast('Error fetching Doc ID:', { type: "error" });
        }
    });
}

function GetGateEntryList(selectedValue = null, gateType = null) {
    $.ajax({
        //url: '/StoreWeighbridgeEntry/GetGateNo',
        url: '/StoreWeighbridgeEntry/GetGateNo',
        type: 'GET',
        dataType: 'json',
        success: function (response) {
            if (response.status) {
                const $DropdownId = $('#ddlGateNo');
                $DropdownId.empty();
                $DropdownId.append('<option value="">- Select Gate No. -</option>');

                // Clear and refill global GateList
                GateList = [];

                $.each(response.data, function (index, item) {
                    // Add item to dropdown
                    $DropdownId.append(
                        `<option
                                data-vtype="${item.V_TYPE}"
                                data-party="${item.PARTY_CODE}"
                                data-partynm="${item.partyName}"
                                value="${item.V_NO}">
                                ${item.V_NO} || ${item.V_TYPE} || ${item.partyName}
                            </option>`
                    );

                    // Add item to GateList array
                    GateList.push(item);
                });

                $DropdownId.select2({
                    placeholder: "- Select -",
                    allowClear: true
                });

                // Set selected value if provided and exists
                if (selectedValue && gateType) {
                    let $optionToSelect = $DropdownId.find(`option[data-vtype="${gateType}"][value="${selectedValue}"]`);
                    if ($optionToSelect.length > 0) {
                        $optionToSelect.prop('selected', true); // manually select it
                        $DropdownId.trigger('change');
                    }
                }
            } else {
                //toastr.error("Gate No. Load failed");
                showToast("Gate No. Load failed", { type: "error" });
            }
        },
        error: function (xhr, status, error) {
            //toastr.error("Gate No. Load failed", xhr.error);
            showToast("Gate No. Load failed", { type: "error" });
        }
    });
}

async function GetGateEntryDetailList(VNo, VType) {
    try {
        const response = await $.ajax({
            url: '/StoreWeighbridgeEntry/GetGateEntryDetailList',
            type: 'GET',
            data: { V_no: VNo, V_type: VType }
        });

        const itemsData = response.data;
        $('#tblStoreWeighbridge tbody').empty();

        if (response.status === true && Array.isArray(itemsData) && itemsData.length > 0) {

            for (let index = 0; index < itemsData.length; index++) {
                const item = itemsData[index];
                
                let dateTimeValue = '';
                if (item.WGT_DATE && item.WGT_TIME) {
                    dateTimeValue = `${item.WGT_DATE.slice(0, 10)}T${item.WGT_TIME.slice(0, 5)}`;
                }

                item.dateTimeValue = dateTimeValue;
                const $row = $('#tblStoreWeighbridge tbody').find('tr').last();
                //const $row = addItemRecordRow(item);
                addItemRecordRow(item);

                bindRowDropdowns({}, false);

                // Set the item dropdown value if available
                const $itemDropdown = $row.find('.txt-item');
                if ($itemDropdown.length > 0 && item.ITEM_CODE) {
                    // Add the option if it doesn't exist
                    if ($itemDropdown.find(`option[value="${item.ITEM_CODE}"]`).length === 0) {
                        $itemDropdown.append(new Option(item.ITEM_NAME, item.ITEM_CODE, false, false));
                    }
                    $itemDropdown.val(item.ITEM_CODE).trigger('change.select2');
                }
            }
        }
        else {
            addItemRecordRow();
            bindRowDropdowns();
        }

    } catch (error) {
        //toastr.error('Error fetching Gate Entry details: ' + error);
        showToast('Error fetching Gate Entry details: ' + error, { type: "error" });
        $('#tblStoreWeighbridge tbody').empty();
        addItemRecordRow();
        bindRowDropdowns();
    }
}

async function GetDocData(MasterTblId, readOnly) {
    try {
        const response = await $.ajax({
            url: '/StoreWeighbridgeEntry/GetStoreWeighBridgeById',
            type: 'GET',
            data: { id: MasterTblId }
        });
        if (response.status) {
            isFillingData = true;
            console.log("header:", response.header);
            console.log("detail:", response.detail);
            await fillHeaderData(response.header);
            await fillItemDetailTable(response.detail);
            isFillingData = false;
            if (readOnly === 'true') {
                $('#btn-save, #cancelBtn').hide();
                disableAllFields();
            } else {
                $('#btn-save, #cancelBtn').show();
                enableAllFields();
            }
        } else {
            //toastr.error('No data returned.');
            showToast('No data returned.', { type: "error" });
        }
    } catch (error) {
        //toastr.error('Failed to load data.');
        showToast('Failed to load data.', { type: "error" });
        console.error(error);
    }
}
function addItemRecordRow(data = {}) {
    docTypeInOut = $('#ddlDocType').val();
    const tbody = $('#tblStoreWeighbridge tbody');

    const itemSelect = (docTypeInOut == "KSIN")
        ? `<select class="form-control txt-item" disabled><option value="">Select Item</option></select>`
        : `<select class="form-control txt-item"><option value="">Select Item</option></select>`;

    const row = `
            <tr class="no-border-input weigh-row">

                <td><input type="text" class="form-control txt-weight" placeholder="Weight" value="${data.WEIGHT || ''}"></td>

                <td><button class="form-control btn-get-wgt">Get Weight</button></td>

                <td><input type="text" class="form-control txt-twgt" placeholder="T.Wgt" value="${data.TARE_WGT || ''}"></td>

                <td><input type="text" class="form-control txt-nwgt" placeholder="N.Wgt" readonly value="${data.NET_WGT || ''}"></td>

                <td><input type="datetime-local" class="form-control txt-datetime" value="${data.dateTimeValue || ''}"></td>

                <td><select class="form-control ddlFromPlace"><option value="">From</option></select></td>

                <td><select class="form-control ddlToPlace"><option value="">To</option></select></td>

                <td>${itemSelect}</td>

                <td><input type="text" class="form-control txt-remarks" value="${data.REMARKS || ''}"></td>

                <td>
                    <i class="fa fa-plus btn-add-action text-success"></i>
                    <i class="fa fa-trash btn-delete-action text-danger"></i>
                </td>

            </tr>
    `;

    tbody.append(row);

    //const $row = tbody.find('tr').last();
    //return tbody.find('tr').last();
    
}
/* ================= DROPDOWNS ================= */
async function bindRowDropdowns(selectedValues = {}, bindItems = true) {
    const promises = [
        bindDropdown('StoreWeighbridgeEntry', 'Place', '.ddlFromPlace', '- From Place -', selectedValues.fromPlace, null, false, null, true),
        bindDropdown('StoreWeighbridgeEntry', 'Place', '.ddlToPlace', '- To Place -', selectedValues.toPlace, null, false, null, true)
    ];

    if (bindItems) {
        promises.push(
            bindDropdown('StoreWeighbridgeEntry', 'Items', '.txt-item', '- Items -', selectedValues.itemValue, null, false, null, true)
        );
    }

    await Promise.all(promises);
}
/* Weight calculation */
function calculateNetWeight($row) {
    const weight = parseFloat($row.find('.txt-weight').val()) || 0;
    const tareWeight = parseFloat($row.find('.txt-twgt').val()) || 0;
    const netWeight = weight - tareWeight;
    $row.find('.txt-nwgt').val(netWeight.toFixed(2));
}
$('#tblStoreWeighbridge').on('input', '.txt-weight', function () {
    const $row = $(this).closest('tr');
    calculateNetWeight($row);
});
$('#tblStoreWeighbridge').on('input', '.txt-twgt', function () {
    const $row = $(this).closest('tr');
    calculateNetWeight($row);
    const formattedDateTime = getCurrentFormattedDateTime();
    $row.find('.txt-datetime').val(formattedDateTime);
});
/* Get Weight */
$('#tblStoreWeighbridge').on('click', '.btn-get-wgt', function (e) {
    e.preventDefault();
    const $row = $(this).closest('tr');
    const formattedDateTime = getCurrentFormattedDateTime();
    $row.find('.txt-datetime').val(formattedDateTime);
    $row.find('.txt-weight').val(0);
    calculateNetWeight($row);
    $row.find('.txt-datetime').prop('readonly', true);
});

/* Add Row */
$(document).on('click', '.btn-add-row, .btn-add-action', function () {
    addItemRecordRow();
    //const $row = addItemRecordRow();

    bindRowDropdowns();
});

/* Delete Row */
$(document).on('click', '.btn-delete-action, .btn-Itemdelete-action', function () {
    var tbody = $(this).closest('tbody');
    var rowCount = tbody.find('tr').length;
    if (rowCount > 1) {
        $(this).closest('tr').remove();
    } else {
        alert('Cannot delete the first row.');
    }
});

//==Enable & Disable==
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