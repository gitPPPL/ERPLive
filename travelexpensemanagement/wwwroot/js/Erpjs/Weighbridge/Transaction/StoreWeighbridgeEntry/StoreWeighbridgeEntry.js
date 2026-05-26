var allFieldIds = [
    "ddlDocType",
    "DtDocDate",
    "ddlStatus",
    "ddlGateNo",
    "DdlPartyName",
    "TxtPartyWeight",
    "Txtlabel12",
    "ChkCrystalReport"
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
let gateType = '';
let gateNo = '';
let itemOptions = '';
let fromPlaceOptions = '';
let toPlaceOptions = '';
function getQueryParam(param) {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get(param);
}
//===INIT
$(async function () {
    try {
        await bindDropdown('StoreWeighbridgeEntry', 'DocType', '#ddlDocType', '-- Select Doc Type --', null, null, false, null, false);
        await bindDropdown('StoreWeighbridgeEntry', 'DocStatus', '#ddlStatus', '-- Select Status --', null, null, true, null, false);
        await bindDropdown('StoreWeighbridgeEntry', 'Party', '#DdlPartyName', '-- Select Party Name --', null, null, false, null, true);
        bindRowDropdowns();
        $('#ddlDocType').prop('selectedIndex', 0);
        handleDocLoad();
        docTypeInOut = $('#ddlDocType').val();
        toggleControls(docTypeInOut);
        if (!docId) {
            ddlItems(null, '.ddlItem').done(function () {
                addItemRecordRow();
            });
        }
    } catch (error) {
        showToast('Failed to load document types: ', { type: "error" });
    }
});
$(document).ready(function () {
    $('#ddlDocType').focus();
    GetGateEntryList();

    //===DocType change event
    $('#ddlDocType').on('change', function () {
        const VType = $(this).val();
        if (VType) {
            GetDocid(VType);
            docTypeInOut = VType;
            toggleControls(docTypeInOut);
            $('#DtDocDate').focus();
            $(this).prop('disabled', true);
        }
        $('#tblStoreWeighbridge tbody').empty();
        ddlItems(null, '.ddlItem').done(function () {
            addItemRecordRow();
        });
    });

    //===Gate No change event
    $('#ddlGateNo').on('change', function (e, data) {
        if (isFillingData || (data && data.isInternal)) return;

        var gateNo = $(this).val();
        var selectedOption = $(this).find('option:selected');

        if (gateNo) {
            var vType = selectedOption.data('vtype');
            GetGateEntryDetailList(gateNo, vType);
        }
    });
    //===save
    $('#btn-save').on('click', async function (e) {
        e.preventDefault();
      
        if (!validateRequiredField('#ddlDocType', 'Doc Type')) return;
        if (!validateRequiredField('#NumDocNo', 'Doc No')) return;
        if (!validateRequiredField('#DtDocDate', 'Doc Date')) return;

        const checkValidation = await checkValidDate();
        if (checkValidation == false) {
            return;
        }
        if (!validateBigWeighbridgeTable()) {
            return;
        }

        const $btn = $(this);
        $btn.prop('disabled', true);

        try {
            const tableData = collectFormData();
            if (tableData) {
                if (docId) {
                    UpdateData(tableData);
                } else {
                    SaveData(tableData);
                }
            }
            else return;
            
        } catch (error) {
            console.error('Error during save:', error);
            showToast('An error occurred while saving the data.', { type: "error" });
        } finally {
            $btn.prop('disabled', false);
        }
    });
    //===Ddl Party Enter
    $('#TxtPartyWeight').on('keydown', function (e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            const $row = $('#tblStoreWeighbridge tbody tr').last();
            const $btn = $row.find('.btn-get-wgt');
            if ($btn.length) {
                $btn.focus();
            } else {
                console.warn('Button not found in last row');
            }
        }
    });
    //setEnterKeyFocus(allFieldIds);
});
//===Validate VDate
async function checkValidDate() {
    const data = {
        vdate: $("#DtDocDate").val(),
        vtype: $("#ddlDocType").val(),
        vno: $("#NumDocNo").val()
    };
    try {
        const response = await fetch('/StoreWeighbridgeEntry/CheckValidDate', {
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
        console.error('Error:', error);
        return false;
    }
}
//===Gate and Party toggle
function toggleControls(docTypeInOut) {
    if (docTypeInOut === "KSOT") {
        $('#ddlGateNo').prop('disabled', true);
        $('#TxtPartyWeight').prop('disabled', true);
        $('#DdlPartyName').prop('disabled', true);
    } else {
        $('#ddlGateNo').prop('disabled', false);
        $('#TxtPartyWeight').prop('disabled', false);
        $('#DdlPartyName').prop('disabled', false); 
    }
}
//===Save Or Update
function SaveData(saveDt) {
    $.ajax({
        url: '/StoreWeighbridgeEntry/SaveOrUpdateStoreWeighBridgeEntry',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(saveDt),
        success: function (response) {
            if (response?.status) {
                showToast("Data saved successfully.", { type: "success" });
                $('#btn-save').hide();
                //setTimeout(() => {
                //    window.location.href = '/StoreWeighbridgeEntryList/Index';
                //}, 1500);
            } else {
                showToast(response?.message || "Save failed. Please try again.", { type: "error" });
            }
        },
        error: function () {
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
                showToast("Data updated successfully", { type: "success" });
                $('#btn-save').hide();
                    //setTimeout(() => {
                    //    window.location.href = '/StoreWeighbridgeEntryList/Index';
                    //}, 1500);
            } else {
                showToast("Update failed: " + (response?.message || "Unknown error."), { type: "error" });
            }
        },
        error: function (xhr, status, error) {
            showToast("Data not updated: ", { type: "error" });
        }
    });
}
//===Collect Data
function collectFormData() {
    const id = toNullableString(docId);
    const itemRecords = collectItemsDetail();
    if (itemRecords) {
        return {
            V_NO: parseIntSafe(document.getElementById("NumDocNo")?.value),
            V_TYPE: toNullableString(document.getElementById("ddlDocType")?.value),
            V_DATE: toNullableDate(document.getElementById("DtDocDate")?.value),
            GATE_NO: parseIntSafe(document.getElementById("ddlGateNo")?.value),
            PARTY_CODE: parseIntSafe(document.getElementById("DdlPartyName")?.value),
            DOC_ID: toNullableString(document.getElementById("TxtDocId")?.value),
            PARTY_QTY: parseDecimalSafe(document.getElementById("TxtPartyWeight")?.value),
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
            PARTY_GROSSWT: null,
            PARTY_TRWT: null,
            PARTY_WBNO: null,
            SMALL_BAG: null,
            MEDIUM_BAG: null,
            LARGE_BAG: null,
            SaveOrUpdate: (!id || id === 0) ? 'Save' : 'Update',
            WB2Data: itemRecords,

            oldGateType: gateType,
            oldGateNo: parseIntSafe(gateNo)

        };
    }
    else {
        return null;
    }
}
function collectItemsDetail() {
    let items = [];
    let SNo = 1;

    const rows = document.querySelectorAll('#tblStoreWeighbridge tbody tr.weigh-row');

    for (let i = 0; i < rows.length; i++) {
        const row = rows[i];

        const weightEl = row.querySelector('.txt-weight');
        const weight = weightEl ? (parseDecimalSafe(weightEl.value) || 0) : 0;

        const datetimeEl = row.querySelector('.txt-datetime');
        const wgtDateRaw = datetimeEl ? datetimeEl.value : '';
        let wgtDate = null;
        let wgtTime = null;

        if (wgtDateRaw && wgtDateRaw.includes('T')) {
            const parts = wgtDateRaw.split('T');
            wgtDate = parts[0] || null;
            wgtTime = parts[1] || null;
        }

        const itemSelect = row.querySelector('.txt-item');
        const fromSelect = row.querySelector('.ddlFromPlace');
        const toSelect = row.querySelector('.ddlToPlace');

        const itemCode = itemSelect ? itemSelect.value : '';
        const itemText = itemSelect && itemSelect.selectedIndex !== -1 ? itemSelect.options[itemSelect.selectedIndex].text : '';


        if (isDuplicateItem(i, itemText)) {
            if (itemSelect) {
                $(itemSelect).addClass('is-invalid');
                itemSelect.focus();
            }
            return null;
        }

        const fromPlace = fromSelect ? fromSelect.value : '';
        const fromText = fromSelect && fromSelect.selectedIndex !== -1 ? fromSelect.options[fromSelect.selectedIndex].text : '';

        const toPlace = toSelect ? toSelect.value : '';
        const toText = toSelect && toSelect.selectedIndex !== -1 ? toSelect.options[toSelect.selectedIndex].text : '';

        const tareWgtEl = row.querySelector('.txt-twgt');
        const netWgtEl = row.querySelector('.txt-nwgt');
        const remarksEl = row.querySelector('.txt-remarks');

        const item = {
            SNO: SNo,
            ITEM_NAME: (itemText && itemText !== '- Select Item -' && itemText !== '-Select Item -') ? itemText : null,
            ITEM_CODE: parseIntSafe(itemCode),
            WEIGHT: weight,
            TARE_WGT: parseDecimalSafe(tareWgtEl ? tareWgtEl.value : ''),
            NET_WGT: parseDecimalSafe(netWgtEl ? netWgtEl.value : ''),
            WGT_DATE: wgtDate,
            WGT_TIME: wgtTime,
            FROM_NAME: (fromText && !fromText.includes('From') && !fromText.includes('Select')) ? fromText : null,
            TO_NAME: (toText && !toText.includes('To') && !toText.includes('Select')) ? toText : null,
            FROM_PLACE: parseIntSafe(fromPlace),
            TO_PLACE: parseIntSafe(toPlace),
            REMARKS: toNullableString(remarksEl ? remarksEl.value : ''),
            V_SHIFT: null, TYPE: null, STATUS: null, Ref_type: null,
            Ref_no: null, wb_time: null, COND: null, MOIS_PER: null, MOIS_WT: null
        };

        items.push(item);
        SNo++;

    }

    return items;
}
//===Fill Data
async function fillHeaderData(headdata) {
    if (!Array.isArray(headdata) || headdata.length === 0) return;
    const data = headdata[0];
    $("#TxtDocId").val(data.DOC_ID ?? "");
    $("#ddlDocType").val(data.V_TYPE ?? "");
    $("#NumDocNo").val(data.V_NO ?? "");
    $("#DtDocDate").val((data.V_DATE ?? '').substring(0, 10));
    $("#TxtPartyWeight").val(data.PARTY_QTY ?? "");
    $("#ChkCrystalReport").prop('checked', data.CRYSTAL_REPORT ?? false);
    $("#Txtlabel12").val(data.TXT_LABEL12 ?? "");
    //GetGateEntryList(data.GATE_NO, data.GATE_TYPE);    
    if (data.GATE_NO && data.GATE_TYPE) {
        let $optionToSelect = $('#ddlGateNo').find(`option[data-vtype="${data.GATE_TYPE}"][value="${data.GATE_NO}"]`);
        if ($optionToSelect.length > 0) {
            $optionToSelect.prop('selected', true); // manually select it
            //$DropdownId.trigger('change');
            $('#ddlGateNo').trigger('change', { isInternal: true });
        }
    }
    await bindDropdown('StoreWeighbridgeEntry', 'Party', '#DdlPartyName', '-- Select Party Name --', data.PARTY_CODE, null, false, null, true);
    $('#ddlStatus').val(data.STATUS).trigger('change');

    gateType = data.GATE_TYPE;
    gateNo = data.GATE_NO;

}
async function fillItemDetailTable(itemsData) {
    if (!itemsData || itemsData.length === 0) {
        ddlItems(null, '.ddlItem').done(function () {
            addItemRecordRow(); // add at least one row
        });
        return;
    }
    ddlItems().done(function () {
        itemsData.forEach(function (item) {
            const Vdate = item.WGT_DATE ?? '';
            const Vtime = item.WGT_TIME ?? '';
            if (Vdate && Vtime) {
                item.dateTimeValue = `${Vdate.slice(0, 10)}T${Vtime.slice(0, 5)}`;
            }
            addItemRecordRow(item);
        });
    });
}
//===Gate Entry
function GetGateEntryList(selectedValue = null, gateType = null) {
    $.ajax({
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
                                data-docname="${item.NAME}"
                                value="${item.V_NO}">
                                ${item.V_NO} || ${item.NAME}
                            </option>`
                    );

                    // Add item to GateList array
                    GateList.push(item);
                });

                $DropdownId.select2({
                    placeholder: "- Select -",
                    allowClear: true
                });
                Select2TextboxFocus($DropdownId);
               
            } else {
                showToast("Gate No. Load failed", { type: "error" });
            }
        },
        error: function (xhr, status, error) {
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
        console.log('itemsData: ', itemsData);
        $('#tblStoreWeighbridge tbody').empty();

        if (response.status === true && Array.isArray(itemsData) && itemsData.length > 0) {
            const party = itemsData[0].Party;

            let partyCode = party;
            if (partyCode && partyCode !== 0) {
                $('#DdlPartyName').val(partyCode).trigger('change');
                $('#DdlPartyName').prop('disabled', true);
                $('#TxtPartyWeight').focus();
            } else {
                $('#DdlPartyName').prop('disabled', false);
                $('#DdlPartyName').focus();
            }

            for (let index = 0; index < itemsData.length; index++) {
                const item = itemsData[index];

                let dateTimeValue = '';
                if (item.WGT_DATE && item.WGT_TIME) {
                    dateTimeValue = `${item.WGT_DATE.slice(0, 10)}T${item.WGT_TIME.slice(0, 5)}`;
                }
                //===Duplicate Check
                if (isDuplicateItem(index, item.ITEM_NAME)) {
                    continue; // Skip adding duplicate item 
                }
                item.dateTimeValue = dateTimeValue;
                ddlItems().done(function () {
                    addItemRecordRow(item);
                });
                const $row = $('#tblStoreWeighbridge tbody').find('tr').last();
                const $itemDropdown = $row.find('.txt-item');
                if ($itemDropdown.length > 0 && item.ITEM_CODE) {
                    if ($itemDropdown.find(`option[value="${item.ITEM_CODE}"]`).length === 0) {
                        $itemDropdown.append(new Option(item.ITEM_NAME, item.ITEM_CODE, false, false));
                    }
                    $itemDropdown.val(item.ITEM_CODE).trigger('change.select2');
                }
            }
        }
        else {
            ddlItems(null, '.ddlItem').done(function () {
                addItemRecordRow();
            });
            $('#DdlPartyName').val('');
        }

    } catch (error) {
        showToast('Error fetching Gate Entry details: ' + error, { type: "error" });
        $('#tblStoreWeighbridge tbody').empty();
        ddlItems(null, '.ddlItem').done(function () {
            addItemRecordRow();
        });
    }
}
//===Doc details
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
            showToast('Error fetching Doc ID:', { type: "error" });
        }
    });
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
            await fillHeaderData(response.header);
            await fillItemDetailTable(response.detail);
            isFillingData = false;
            if (readOnly === 'true') {
                setFormReadOnly();
            } else {
                docTypeInOut = $('#ddlDocType').val();
                toggleControls(docTypeInOut);
                $('#ddlDocType').prop('disabled', true);
                $('#btn-save, #cancelBtn').show();
                //enableAllFields();
            }
        } else {
            showToast('No data returned.', { type: "error" });
        }
    } catch (error) {
        showToast('Failed to load data.', { type: "error" });
        console.error(error);
    }
}
async function handleDocLoad() {
    docId = getQueryParam('id');
    readOnly = getQueryParam('readOnly');
    if (docId) {
        $('#ddlDocType').prop('disabled', true);
        await GetDocData(docId, readOnly);
    } else {
        $('#ddlStatus').prop('disabled', true);
        const Vtype = $('#ddlDocType').val();
        if (Vtype) {
            GetDocid(Vtype);
        }
        const today = new Date();
        const todayDate = today.getFullYear() + '-' + (today.getMonth() + 1).toString().padStart(2, '0') + '-' + today.getDate().toString().padStart(2, '0');
        $('#DtDocDate').val(todayDate);
    }
}
//===Add rows in table
function addItemRecordRow(data = {}) {
    const tbody = $('#tblStoreWeighbridge tbody');
    tbody.find('.btn-add-action').remove();
    docTypeInOut = $('#ddlDocType').val();
    const itemSelect = (docTypeInOut == "KSIN")
        ? `<select class="form-control txt-item" disabled>${itemOptions}</select>`
        : `<select class="form-control txt-item">${itemOptions}</select>`;

    const row = `
            <tr class="no-border-input weigh-row">

                <td><input type="number" class="form-control txt-weight" placeholder="Weight" value="${data.WEIGHT || ''}"></td>

                <td><button class="form-control btn-get-wgt erppage-btn-common">Get Weight</button></td>

                <td><input type="number" class="form-control txt-twgt" placeholder="T.Wgt" value="${data.TARE_WGT || ''}"></td>

                <td><input type="number" class="form-control txt-nwgt" placeholder="N.Wgt" readonly value="${data.NET_WGT || ''}"></td>

                <td><input type="datetime-local" class="form-control txt-datetime" value="${data.dateTimeValue || ''}" disabled></td>

                <td><select class="form-control ddlFromPlace">${fromPlaceOptions}</select></td>

                <td><select class="form-control ddlToPlace">${toPlaceOptions}</select></td>

                <td>${itemSelect}</td>

                <td><input type="text" class="form-control txt-remarks" value="${data.REMARKS || ''}"></td>

                <td class="action-col">
                    <button class="act-btn add btn-add-action" title="Add" style="cursor:pointer;"><i class="fa fa-plus"></i></button>
                    <button class="act-btn delete btn-delete-action" title="Delete" style="cursor:pointer;"><i class="fa fa-trash"></i></button>
                </td>

            </tr>
    `;

    tbody.append(row);

    const $currentRow = tbody.find('tr:last');
    const $lastItemSelect = $currentRow.find('.txt-item');
    const $lastFromPlaceSelect = $currentRow.find('.ddlFromPlace');
    const $lastToPlaceSelect = $currentRow.find('.ddlToPlace');
    $lastItemSelect.select2({ placeholder: "- Select Item -", allowClear: true, width: '100%' });
    $lastFromPlaceSelect.select2({ placeholder: "- Select From Place -", allowClear: true, width: '100%' });
    $lastToPlaceSelect.select2({ placeholder: "- Select To Place -", allowClear: true, width: '100%' });
    Select2TextboxFocus($lastItemSelect);
    Select2TextboxFocus($lastFromPlaceSelect);
    Select2TextboxFocus($lastToPlaceSelect);
    if (data.ITEM_CODE) {
        $lastItemSelect.val(data.ITEM_CODE).trigger('change.select2');
    }
    if (data.FROM_PLACE) {
        $lastFromPlaceSelect.val(data.FROM_PLACE).trigger('change.select2');
    }
    if (data.TO_PLACE) {
        $lastToPlaceSelect.val(data.TO_PLACE).trigger('change.select2');
    }
}
//===Table dropdowns 
async function bindRowDropdowns(selectedValues = {}) {
    const $tempFrom = $('<select></select>');
    const $tempTo = $('<select></select>');
    await Promise.all([
        bindDropdown('StoreWeighbridgeEntry', 'Place', $tempFrom, '- From Place -', selectedValues.fromPlace, null, false, null),
        bindDropdown('StoreWeighbridgeEntry', 'Place', $tempTo, '- To Place -', selectedValues.toPlace, null, false, null)
    ]);

    fromPlaceOptions = $tempFrom.html();
    toPlaceOptions = $tempTo.html();
}
//===Add Row
$(document).on('click', '.btn-add-row, .btn-add-action', function () {
    ddlItems(null, '.ddlItem').done(function () {
        addItemRecordRow();
    });
});
//===Delete Row 
$(document).on('click', '.btn-delete-action', function () {
    var tbody = $(this).closest('tbody');
    var rowCount = tbody.find('tr').length;
    if (rowCount > 1) {
        $(this).closest('tr').remove();
    } else {
        showToast('Cannot delete the first row.', { type: "warning" });
    }
});
//===Weight calculation
function calculateNetWeight($row) {
    const weight = parseFloat($row.find('.txt-weight').val()) || 0;
    const tareWeight = parseFloat($row.find('.txt-twgt').val()) || 0;
    const netWeight = weight - tareWeight;
    $row.find('.txt-nwgt').val(netWeight.toFixed(2));
}
//====Table events
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
//===Get Weight
$('#tblStoreWeighbridge').on('click', '.btn-get-wgt', function (e) {
    e.preventDefault();
    const $row = $(this).closest('tr');
    const formattedDateTime = getCurrentFormattedDateTime();
    $row.find('.txt-datetime').val(formattedDateTime);
    $row.find('.txt-weight').val(0);
    calculateNetWeight($row);
    $row.find('.txt-datetime').prop('readonly', true);
});
//===Table Validation
function validateBigWeighbridgeTable() {
    const $rows = $('#tblStoreWeighbridge tbody tr');
    let totalRows = 0;
  
    for (let i = 0; i < $rows.length; i++) {
        const row = $rows[i];

        const weightEl = row.querySelector('.txt-weight');
        const tareEl = row.querySelector('.txt-twgt');
        const fromPlaceEl = row.querySelector('.ddlFromPlace');
        const toPlaceEl = row.querySelector('.ddlToPlace');
        const itemEl = row.querySelector('.txt-item');

        const weight = parseFloat(weightEl.value) || 0;
        const tareWeight = parseFloat(tareEl.value) || 0;
        const fromPlace = fromPlaceEl.value;
        const toPlace = toPlaceEl.value;
        const itemCode = itemEl.value;
        const itemName = itemEl.selectedOptions[0]?.text || '';
        let errorMessage = '';

        // Validation 1: Blank weight for selected item
        if (weight === 0 && itemCode) {
            errorMessage = `Weight should not be blank or 0 for item: ${itemName} !`;
            setInvalid($(weightEl), errorMessage);
            return false;
        }

        if (weight > 0) {
            totalRows = 1
            // Validation 2: Tare Weight > Gross Weight
            if (tareWeight > weight) {
                errorMessage = `Tare Weight should not be greater than Gross Weight for item: ${itemName} !`;
                setInvalid($(tareEl), errorMessage);
                return false;
            }

            // Validation 3: Required Fields
            if (!validateRequiredField(fromPlaceEl, 'From Place') ||
                !validateRequiredField(toPlaceEl, 'To Place') ||
                !validateRequiredField(itemEl, 'Item Name')) {
                return false;
            }
        }

        // Validation 4: Route comparison
        if (fromPlace && toPlace && fromPlace === toPlace) {
            showToast('From place cannot be same as To place!', { type: 'warning' });
            toPlaceEl.classList.add('is-invalid');
            toPlaceEl.focus();
            return false;
        }
    }
    if (totalRows == 0) {
        showToast('No Record in grid to save!', { type: "warning" });
        return false;
    }
    return true;
}
//===Duplicate item 
function isDuplicateItem(currentRowIndex, itemName) {
    let duplicateFound = false;

    $('#tblStoreWeighbridge tbody tr').each(function (index) {
        if (index === currentRowIndex) return; // skip the current row
        const otherItemName = $(this).find('.txt-item option:selected').text()?.trim();
        if (itemName && otherItemName === itemName) {
            showToast('Duplicate item found: ' + itemName, { type: "warning" });
            duplicateFound = true;
            return false; // break the loop
        }
    });

    return duplicateFound;
}
//===Set Focus
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
function setEnterKeyFocusOnTable(sequence) {
    sequence.forEach((id, index) => {
        let elementId = `.${id}`;
        $(document).on('keydown', elementId, function (e) {
            if (e.key === 'Enter' || e.key === 'Tab' || e.keyCode === 13 || e.keyCode === 9) {
                e.preventDefault();

                let nextIndex = index + 1;
                if (nextIndex < sequence.length) {
                    let nextElementId = `.${sequence[nextIndex]}`;
                    if ($(nextElementId).length) {
                        $(nextElementId).focus();
                    }
                } else {

                    addItemRecordRow();
                    setEnterKeyFocus(sequence);
                    $(`.${sequence[0]}`).focus();
                }
            }
        });
    });
}
//===Enable & Disable
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
//===Set Readonly
function setFormReadOnly() {
    const form = $('#StoreWeighbridgeEntryForm');
    $('#StoreWeighbridgeEntryForm input[type="checkbox"]').prop('disabled', true);
    $('#btn-save, #cancelBtn').hide();
    //disableAllFields();
    form.addClass('erppage-readonly');
}

function ddlItems(selectedValue = null, dropdownSelector = null) {
    return $.ajax({
        url: '/StoreWeighbridgeEntry/GetDropdown',
        type: 'GET',
        dataType: 'json',
        data: { type: 'Items'},
        success: function (data) {
            itemData = data;
            itemOptions = '<option selected disabled value="">-Select Item -</option>';
            $.each(data, function (index, item) {
                itemOptions += `<option value="${item.value}">${item.text}</option>`;
            });
            const $dropdown = $(dropdownSelector);
            $dropdown.empty(); // Clear existing options
            $dropdown.append(itemOptions);
            if (selectedValue && $dropdown.find(`option[value="${selectedValue}"]`).length > 0) {
                $dropdown.val(selectedValue).trigger('change');
            } else {
                $dropdown.val('').trigger('change');
            }
            $dropdown.on('select2:open', function () {
                $('.select2-container--open .select2-search__field').focus();
            });
        },
        error: function (xhr, status, error) {
            showToast("Item Load failed: " + error, { type: "error" });
        }
    });
};

function Select2TextboxFocus(ddl) {
    ddl.on('select2:open', function () {
        setTimeout(function () {
            let searchBox = document.querySelector(
                '.select2-container--open .select2-search__field'
            );

            if (searchBox) {
                searchBox.focus();
            }
        }, 0);
    });
}

