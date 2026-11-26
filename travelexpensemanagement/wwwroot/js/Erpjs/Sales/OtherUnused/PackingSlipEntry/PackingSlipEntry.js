const $tbody = $('#tblProformaPackingSlip tbody');
let isEdit, isCopyFrom = false;

let itemOptions = '';
let tenacityOptions = '';
let statusOptions = '';

let compCode = "";
let yearCode = "";
let branchCode = "";
let companyName = "";
let add1 = "";
let add2 = "";
let db = "";

const urlParams = new URLSearchParams(window.location.search);
const rowId = parseInt(urlParams.get('id'));
const rowIdVType = urlParams.get('vtype');
let isReadOnly = urlParams.get('readOnly') === 'true';

$(document).ready(async function () {
    await getGlobalValues();
    setTableHeaders();

    if (!isReadOnly) {
        $('#DtDocDate').val(getCurrentDateYMD()).focus();
    }

    await bindHeaderDropdowns();
    await loadFooterDropdowns();
    await wireEvents();

    $('#ddlDocType').trigger('change');

    if (rowId && rowIdVType && !isNaN(rowId)) {
        await getDataById();
    } else {
        await addNewRowBelow();
    }
});

// ================= DROPDOWNS =============================
async function bindHeaderDropdowns() {

    return Promise.all([
        bindDropdown("PackingSlipEntry", "doctype", '#ddlDocType', '--Select Document Type--', 'FPSL', null, true, null, false),
        bindDropdown("PackingSlipEntry", "docstatus", '#ddlStatus', '--Select Status--', null, null, true, null, false),
        bindDropdown("PackingSlipEntry", "godown", '#ddlGodown', '--Select Godown--', null, null, true, null, true),
        bindDropdown("PackingSlipEntry", "item", '#ddlItem', '--Select Item--', null, null, false, null, true),
        bindDropdown("PackingSlipEntry", "do", '#ddlDONo', '--Select DO--', null, null, false, null, true),
        bindDropdownWithData('/PackingSlipEntry/GetDropdown?type=party', $('#ddlPartyName'), '--Select Party--'),
        bindDropdown("PackingSlipEntry", "machine", '#ddlIssueMachType', '--Select Issue/Mach Type--', null, null, false, null, true),
        bindDropdown("PackingSlipEntry", "place", '#ddlPlace', '--Select Place--', null, null, true, null, false)
    ]);
}

async function bindDropdownWithData(url, $dropdown, placeholder, selectedValue = null) {
    try {
        const data = await $.ajax({
            url: url,
            type: 'GET',
            dataType: 'json'
        });

        $dropdown.empty().append(`<option value="">${placeholder}</option>`);

        data.forEach(item => {
            const $option = $('<option>', {
                value: item.value,
                text: item.text
            });

            Object.keys(item).forEach(key => {
                if (key !== 'value' && key !== 'text') {
                    $option.attr(`data-${key}`, item[key] ?? '');
                }
            });

            $dropdown.append($option);
        });

        if (selectedValue != null) {
            $dropdown.val(selectedValue);
        }

        initSelect2($dropdown);
    }
    catch (e) {
        console.error("Dropdown binding error:", e.responseText || e.message);
    }
}
function initSelect2($ddl) {
    if (!$ddl.length) return;

    // Already initialized
    if ($ddl.hasClass('select2-hidden-accessible')) return;

    $ddl.select2({
        placeholder: '-- Select --',
        allowClear: true
    });

    $ddl.on('select2:open', function () {
        setTimeout(function () {
            const searchBox = document.querySelector('.select2-container--open .select2-search__field');
            if (searchBox) searchBox.focus();
        }, 0);
    });
}
async function loadFooterDropdowns() {
    const $item = $('<select></select>');
    const $tenacity = $('<select></select>');
    const $status = $('<select></select>');

    await Promise.all([
        bindDropdownWithData('/PackingSlipEntry/GetDropdown?type=footeritem', $item, '-Select Item-'),
        bindDropdown("PackingSlipEntry", "tenacity", $tenacity, '-Select Tenacity-', null, null, false, null, true),
        bindDropdown("PackingSlipEntry", "itemstatus", $status, '-Select Status-', null, null, false, null, true)
    ]);

    itemOptions = $item.html();
    tenacityOptions = $tenacity.html();
    statusOptions = $status.html();
}


//=============GENERATE VNO=================
async function GetVNo(vtype) {
    try {
        const res = await fetch(`/PackingSlipEntry/GetVNo/?vType=${encodeURIComponent(vtype)}`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();
        if (!data.v_NO) throw new Error('Response missing v_NO');
        $('#NumDocNo').val(data.v_NO);
    }
    catch (e) {
        showToast('Error loading Document Number: ' + e.message, { type: "warning" });
    }
}


//=============EVENTS=================
async function wireEvents() {
    // DocType Change
    $("#ddlDocType").on('change', async function () {
        var vType = $(this).val();
        if (!vType || rowId) return;
        try {
            await GetVNo(vType);
        } catch (err) {
            console.error("Error loading Document No:", err);
        }
    });

    // Item Name Change
    $("#tblProformaPackingSlip").on("change", ".item-name", async function () {
        if (isEdit || isCopyFrom) return;

        const $selectedEl = $(this);
        const $row = $selectedEl.closest("tr");
        const itemCode = $selectedEl.val();
        const $opt = $selectedEl.find("option:selected");

        if (itemCode) {
            $row.find(".item-code").val(itemCode || '');
            $row.find(".unit-no").val($opt.attr("data-ucode") || '');
            $row.find(".unit-name").val($opt.attr("data-unit") || '');

            setLoomFromHeader($row);
            await loadItemRowData($row, $selectedEl, itemCode);
        }
        else {
            $row.find(".item-code").val('');
            $row.find(".unit-no").val('');
            $row.find(".unit-name").val('');
            $row.find('.loom-type').val('');
            $row.find('.loom-no').val('');
        }

    });

    // DO No Change
    $("#ddlDONo").on("change", function () {
        if (isEdit) return;

        const docId = $(this).val();
        if (docId) getDOData(docId);
    });

    // Delete Row Button Click
    $('#tblProformaPackingSlip').on('click', '.btn-delete-action', function () {
        if ($tbody.find('tr').length === 1) return;

        const $row = $(this).closest('tr');
        $row.remove();

        const $lastRow = $tbody.find('tr:last');

        if ($lastRow.length) {
            if ($lastRow.find('.btn-add-action').length === 0) {
                $lastRow.find('.action-wrap').append(`
                <button type="button" class="act-btn add btn-add-action" title="Add Row"><i class="fa fa-plus-circle"></i></button>
            `);
            }
        }

        totalAmt();
    });

    // Add Row Button Click
    $('#tblProformaPackingSlip').on('click', '.btn-add-action', async function () {
        const currentSelect = $(this).closest('tr').find('.item-name')[0];
        //Duplicate
        //if (checkDuplicateItems(currentSelect)) return;

        await addNewRowBelow();
    });

    // Party Change
    $('#ddlPartyName').on('change', function () {
        $('#btnPartyInfo').prop('disabled', !this.value);

        let $opt = $(this).find('option:selected');
        let partyCode = $(this).val();

        if (!partyCode) return;

        $('#TxtAcCode').val(partyCode);
        $('#TxtAcName').val($opt.text().trim());
        $('#TxtCrLimit').val($opt.attr('data-credit_limit'));

        let drBal = $opt.attr('data-data-drbal') || '';
        let crBal = $opt.attr('data-data-crbal') || '';

        if (parseFloat(drBal) > 0) {
            $('#TxtClosingBal').val(drBal);
            $('#lblDrCr').text('Dr');
        }
        else if (parseFloat(crBal) > 0) {
            $('#TxtClosingBal').val(crBal);
            $('#lblDrCr').text('Cr');
        }
        else {
            $('#TxtClosingBal').val(0);
            $('#lblDrCr').text('Cr');
        }
    });

    // Open Party Details modal
    $('#btnPartyInfo').on('click', function () {
        bootstrap.Modal.getOrCreateInstance(document.getElementById('accountInfoModal')).show();
    });

    // Row Gr, Tare, Net weight change
    $('#tblProformaPackingSlip').on('input', '.gross-wgt, .tare-wgt', function () {

        if (isEdit) return;

        let $tr = $(this).closest('tr');
        let gross = parseFloat($tr.find('.gross-wgt').val()) || 0;
        let tare = parseFloat($tr.find('.tare-wgt').val()) || 0;

        $tr.find('.net-wgt').val(formatVal(gross - tare, 'QTY'));
        totalAmt();
    });

    // Save button click
    $('#btn-save').on('click', async function (e) {
        e.preventDefault();

        if (!await Validate()) return;
        savePackingSlip();
    });

    // Copy From
    $('#copyFromModal').on('shown.bs.modal', function () {
        getCopyFromData();
    });

    // Select All
    $(document).on('change', '#chkSelectAllCopyFrom', function () {
        $('.chkCopyFromRow').prop('checked', this.checked);
        updateSelectedRecords();
    });

    // Individual Row Selection
    $(document).on('change', '.chkCopyFromRow', function () {
        const total = $('.chkCopyFromRow').length;
        const selected = $('.chkCopyFromRow:checked').length;

        $('#chkSelectAllCopyFrom').prop('checked', total > 0 && total === selected);
        updateSelectedRecords();
    });

    // Copy into main grid
    $('#btnCopy').on('click', function () {
        copySelectedRows();
    });

    // Search in modal
    $(document).on('input', '#searchBoxCopyFrom', function () {
        const searchText = $(this).val().toLowerCase().trim();

        $('#tblCopyFromModal tbody tr').each(function () {
            const rowText = $(this).text().toLowerCase();
            $(this).toggle(rowText.includes(searchText));
        });
    });
}


//============= Add Footer Rows ==========
function createRowHtml(data = {}) {
    const f = (v, t) => (v === '' || v === null || v === undefined) ? '' : formatVal(v, t);

    return `
        <tr class="no-border-input">
            <td><input class="form-control form-control-sm item-code" type="number" value="${data.iteM_CODE || data.ITEM_CODE || ''}" disabled/></td>
            <td class="freeze-item">
                <select class="form-control form-control-sm item-name">${itemOptions}</select>

                <input class="form-control form-control-sm unit-no" type="hidden" value="${data.uoM_CODE || data.UOM_CODE || ''}"/>
                <input class="form-control form-control-sm unit-name" type="hidden" value="${data.uoM_NAME || data.UOM_NAME || ''}" maxlength="20"/>
            </td>

            <td><input class="form-control form-control-sm bale-no" type="text" value="${data.balE_NO || data.BALE_NO || ''}" maxlength="20"/></td>
            <td><input class="form-control form-control-sm lot-no" type="text" value="${data.loT_NO || data.LOT_NO || ''}" maxlength="20"/></td>

            <!-- For Laminators -->
            <td class="laminator-col"><input class="form-control form-control-sm meter" type="number" value="${f(data.mtr ?? data.MTR ?? '', 'QTY')}" oninput="SetMaxlength(this, 18, 4);"/></td>
            <!-- ---------- -->

            <td><input class="form-control form-control-sm gross-wgt" type="number" value="${f(data.grosS_QTY || data.GROSS_QTY, 'QTY')}" oninput="SetMaxlength(this, 18, 4);"/></td>
            <td><input class="form-control form-control-sm tare-wgt" type="number" value="${f(data.tarE_QTY || data.TARE_QTY, 'QTY')}" oninput="SetMaxlength(this, 18, 4);"/></td>
            <td><input class="form-control form-control-sm net-wgt" type="number" value="${f(data.qty || data.QTY, 'QTY')}" disabled oninput="SetMaxlength(this, 18, 4);"/></td>
            
            <!-- For Laminators -->
            <td class="laminator-col"><input class="form-control form-control-sm avg-wgt" type="number" value="${f(data.avG_QTY ?? data.AVG_QTY ?? '', 'QTY')}" oninput="SetMaxlength(this, 18, 4);"/></td>
            <td class="laminator-col"><input class="form-control form-control-sm gram" type="number" value="${f(data.gram ?? data.GRAM ?? '', 'QTY')}" oninput="SetMaxlength(this, 18, 4);"/></td>
            <!-- ---------- -->

            <td><select class="form-control form-control-sm tenacity">${tenacityOptions}</select ></td >
            <td><select class="form-control form-control-sm status">${statusOptions}</select></td>

            <td><input class="form-control form-control-sm remarks" type="text" value="${data.remarks || data.REMARKS || ''}" maxlength="100"/></td>

            <td><input class="form-control form-control-sm ref-type" type="text" value="${data.reF_TYPE || data.REF_TYPE || ''}" disabled maxlength="4"/></td>
            <td><input class="form-control form-control-sm ref-no" type="text" value="${data.reF_NO || data.REF_NO || ''}" disabled/></td>

            <td><input class="form-control form-control-sm lmrc-type" type="text" value="${data.lmrC_TYPE || data.LMRC_TYPE || ''}" disabled maxlength="4"/></td>
            <td><input class="form-control form-control-sm lmrc-no" type="text" value="${data.lmrC_NO || data.LMRC_NO || ''}" disabled/></td>

            <td><input class="form-control form-control-sm loom-type" type="text" value="${data.maC_TYPE || data.MAC_TYPE || ''}" maxlength="20"/></td>
            <td><input class="form-control form-control-sm loom-no" type="text" value="${data.maC_CODE || data.MAC_CODE || ''}"/></td>

            <td><input class="form-control form-control-sm place-code" type="text" value="${data.placE_CODE || data.PLACE_CODE || ''}"/></td>
            <td><input class="form-control form-control-sm item-size" type="text" value="${data.iteM_SIZE || data.ITEM_SIZE || ''}" oninput="SetMaxlength(this, 12, 4);"/></td>

            <td class="action-col">
                <div class="action-wrap">
                    <button type="button" class="act-btn delete btn-delete-action" title="Delete Row"><i class="fa fa-trash"></i></button>
                    <button type="button" class="act-btn add btn-add-action" title="Add Row"><i class="fa fa-plus-circle"></i></button>
                </div>
            </td>
        </tr>
    `;
}
async function addNewRowBelow(data = null) {
    data = data || {};

    // Remove add button from the current last row
    const $previousLastRow = $("#tblProformaPackingSlip tbody tr:last");
    $previousLastRow.find(".btn-add-action").remove();

    let rowHtml = createRowHtml(data);
    $("#tblProformaPackingSlip tbody").append(rowHtml);

    const $lastRow = $("#tblProformaPackingSlip tbody tr:last");

    $lastRow.find('.laminator-col').toggle(Number(compCode) === 2);

    // Item Dropdown
    $lastRow.find('.item-name').val(data.ITEM_CODE || data.iteM_CODE).trigger('change');

    // Tenacity
    $lastRow.find('.tenacity').val(data.tenacitY_CODE || data.TENACITY_CODE).trigger('change');

    // Status
    $lastRow.find('.status').val(data.statuS_CODE || data.STATUS_CODE).trigger('change');

    initSelect2($lastRow.find('.item-name'));
    initSelect2($lastRow.find('.tenacity'));
    initSelect2($lastRow.find('.status'));
}
function setTableHeaders() {
    const code = Number(compCode);
    const isLaminator = code === 2;

    const header = code === 1 ? 'Bale No' : code === 2 ? 'Roll No' : 'Bag No';

    $('#tblProformaPackingSlip .bale-no-header').text(header);
    $('#tblProformaPackingSlip .laminator-col').css('display', isLaminator ? '' : 'none');
}


//============= DO Data ==========
async function getDOData(docId) {
    try {
        const result = await $.getJSON('/PackingSlipEntry/GetDOData', { docId });

        if (result.data && result.data.length > 0) {
            console.log("DO Data: ", result.data);
            const data = result.data[0];
            $("#ddlPartyName").val(data.BILL_CODE || '').trigger('change');
            $("#ddlItem").val(data.ITEM_CODE || '').trigger('change');
        }
    } catch (error) {
        console.error("Error fetching DO details:", error);
    }
}


//============= FILL LOOM DETAILS FROM HEADER TO FOOTER ==========
function setLoomFromHeader($row) {
    const machCode = $('#ddlIssueMachType').val();
    const machName = machCode ? $('#ddlIssueMachType option:selected').text().trim() : '';

    $row.find('.loom-type').val(machName);
    $row.find('.loom-no').val(machCode || '');
}


//============= FOOTER ITEM DETAILS ==========
async function loadItemRowData($row, $sel, itemCode) {
    const rowIndex = $row.index();
    const prevBaleNo = rowIndex > 0 ? $row.prev().find('.bale-no').val() : '';

    try {
        const r = await $.getJSON('/PackingSlipEntry/GetItemRowData', {
            vType: $('#ddlDocType').val(),
            itemCode: itemCode,
            rowIndex: rowIndex,
            prevBaleNo: prevBaleNo,
            placeCode: $('#ddlPlace').val()
        });

        console.log("Item Details on item change: ", r.data);

        $row.find('.bale-no').val(r.data.baleNo);
        $row.find('.item-size').val(r.data.itemSize ?? '');
    } catch (e) {
        console.error("Error loading item row data:", e);
    }
}


//============= CALCULATIONS ==========
function totalAmt() {
    let tGrosswt = 0, tTarewt = 0, tNetwt = 0;
    let $rows = $('#tblProformaPackingSlip tbody tr');

    $rows.each(function () {
        tGrosswt += parseFloat($(this).find('.gross-wgt').val()) || 0;
        tTarewt += parseFloat($(this).find('.tare-wgt').val()) || 0;
        tNetwt += parseFloat($(this).find('.net-wgt').val()) || 0;
    });

    $('#NumGrossWt').val(formatVal(tGrosswt, 'QTY'));
    $('#NumTareWt').val(formatVal(tTarewt, 'QTY'));
    $('#NumNetWt').val(formatVal(tNetwt, 'QTY'));
    $('#NumNoOfRecords').val($rows.length);
}


//============= VALIDATIONS ==========
async function Validate() {
    // Document Details
    if (!validateRequiredField('#ddlDocType', 'Document Type') || !validateRequiredField('#NumDocNo', 'Document No.')) return false;

    let isValidaDocDate = await checkValidDate();
    if (!isValidaDocDate) return false;

    // Shift
    if (!validateRequiredField('#ddlShift', 'Shift')) return false;

    // Check if grid has any records
    let hasRecords = false;

    $('#tblProformaPackingSlip tbody tr').each(function () {
        const itemCode = parseInt($(this).find('.item-code').val()) || 0;
        if (itemCode > 0) {
            hasRecords = true;
            return false;
        }
    });

    if (!hasRecords) {
        showToast('No Record in grid to save.', { type: 'warning' });
        return false;
    }

    // Server Side Validation
    const rows = [];

    const validationFields = {
        BaleNo: '.bale-no',
        NetWeight: '.gross-wgt',
        Tenacity: '.tenacity',
        AshRange: '.tenacity',
        Status: '.status',
        ItemCode: '.item-name',
        MachineType: '#ddlIssueMachType'
    };

    let duplicateMessage = '';

    $('#tblProformaPackingSlip tbody tr').each(function () {
        const $row = $(this);
        const itemCode = parseInt($row.find('.item-code').val()) || 0;

        if (itemCode > 0 && duplicateItem($row, String(compCode))) {
            const baleNo = $row.find('.bale-no').val()?.trim() || '';
            const lotNo = $row.find('.lot-no').val()?.trim() || '';
            const itemName = $row.find('.item-name option:selected').text().trim();

            if (String(compCode) === '1')
                duplicateMessage = 'Duplicate Roll No record found.';
            else if (String(compCode) === '2')
                duplicateMessage = `Duplicate record found.\nItem : ${itemName} (${itemCode})\nRoll/Bale/Bag No : ${baleNo}`;
            else if (String(compCode) === '4')
                duplicateMessage = `Duplicate record found.\nRoll/Bale/Bag No : ${baleNo}\nLot/Batch No : ${lotNo}`;

            return false;
        }

        rows.push({
            itemCode: itemCode,
            itemName: $row.find('.item-name option:selected').text().trim(),
            baleNo: $row.find('.bale-no').val()?.trim() || '',
            lotNo: $row.find('.lot-no').val()?.trim() || '',
            netWeight: parseFloat($row.find('.net-wgt').val()) || 0,
            tenacity: parseInt($row.find('.tenacity').val()) || 0,
            status: parseInt($row.find('.status').val()) || 0
        });
    });

    if (duplicateMessage) {
        showToast(duplicateMessage, { type: 'warning' });
        return;
    }

    const data = {
        vType: $('#ddlDocType').val(),
        vNo: parseInt($('#NumDocNo').val()) || 0,
        vDate: $('#DtDocDate').val(),
        shift: $('#ddlShift').val(),
        machineType: $('#ddlIssueMachType').val(),
        doId: $('#ddlDONo').val(),
        rows: rows
    };

    try {
        const result = await $.ajax({
            url: '/PackingSlipEntry/ValidateData',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data)
        });

        if (!result.status) {
            const selector = validationFields[result.field];
            
            if (selector) {
                const $el = result.rowIndex !== null
                    ? $('#tblProformaPackingSlip tbody tr').eq(result.rowIndex).find(selector)
                    : $(selector);

                if ($el.length) {
                    setInvalid($el, result.message);
                    return false;
                }
            }

            showToast(result.message, { type: 'warning' });
            return false;
        }

        // Multiple warnings one by one in SweetAlert
        if (result.warnings?.length > 0) {
            for (const message of result.warnings) {
                await Swal.fire({
                    icon: 'warning',
                    title: 'Warning',
                    text: message,
                    confirmButtonText: 'OK'
                });
            }
        }

        return true;
    }
    catch (error) {
        console.error('Validation error:', error);
        showToast(
            error.responseJSON?.message || 'Error occurred while validating Packing Slip.',
            { type: 'warning' }
        );
        return false;
    }
}
async function checkValidDate() {
    const data = {
        vdate: $("#DtDocDate").val(),
        vtype: $("#ddlDocType").val(),
        vno: $("#NumDocNo").val()
    };
    try {
        const response = await fetch('/PackingSlipEntry/CheckValidDate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });
        const result = await response.json();
        if (result.status === false) {
            // toastr.warning(result.message);
            showToast(result.message, { type: "warning" });
            return false;
        }
        return true;
    } catch (error) {
        console.error("Error:", error);
        return false;
    }
}
function duplicateItem($currentRow, compCode) {
    const itemCode = String($currentRow.find('.item-code').val() || '').trim();
    const baleNo = String($currentRow.find('.bale-no').val() || '').trim();
    const lotNo = String($currentRow.find('.lot-no').val() || '').trim();

    let isDuplicate = false;

    $('#tblProformaPackingSlip tbody tr').not($currentRow).each(function () {
        const $row = $(this);
        const existingItem = String($row.find('.item-code').val() || '').trim();
        const existingBale = String($row.find('.bale-no').val() || '').trim();
        const existingLot = String($row.find('.lot-no').val() || '').trim();

        if (compCode === '1' && baleNo === existingBale) isDuplicate = true;
        else if (compCode === '2' && itemCode + baleNo === existingItem + existingBale) isDuplicate = true;
        else if (compCode === '4' && lotNo + baleNo === existingLot + existingBale) isDuplicate = true;

        if (isDuplicate) return false;
    });

    return isDuplicate;
}


//============= SAVE & UPDATE ==========
function collectPackingSlipData() {
    const getInt = value => {
        const n = parseInt(value);
        return Number.isFinite(n) ? n : null;
    };

    const getDecimal = value => {
        if (value === null || value === undefined || value === '') return null;
        const n = parseFloat(value);
        return Number.isFinite(n) ? n : null;
    };

    const doId = $('#ddlDONo').val() || '';
    const doType = doId.length >= 4 ? doId.substring(0, 4) : null;
    const doNo = doId.length >= 13 ? getInt(doId.substring(4, 13)) : null;

    const macCode = $('#ddlIssueMachType').val();
    const macType = macCode ? $('#ddlIssueMachType option:selected').text().trim() : null;

    const Items = [];

    $('#tblProformaPackingSlip tbody tr').each(function () {
        const $row = $(this);
        const itemCode = getInt($row.find('.item-code').val());

        if (!itemCode) return;

        Items.push({
            PLACE_CODE: getInt($row.find('.place-code').val()),
            ITEM_CODE: itemCode,
            ITEM_NAME: $row.find('.item-name option:selected').text().trim(),
            ITEM_SIZE: getDecimal($row.find('.item-size').val()),
            NOS: null,
            WASTE: null,
            BALE_NO: $row.find('.bale-no').val()?.trim() || null,
            LOT_NO: $row.find('.lot-no').val()?.trim() || null,
            GROSS_QTY: getDecimal($row.find('.gross-wgt').val()),
            TARE_QTY: getDecimal($row.find('.tare-wgt').val()),
            QTY: getDecimal($row.find('.net-wgt').val()),
            MTR: getDecimal($row.find('.meter').val()),
            AVG_QTY: getDecimal($row.find('.avg-wgt').val()),
            GRAM: getDecimal($row.find('.gram').val()),
            PACK_QTY: null,
            REMARKS: $row.find('.remarks').val()?.trim() || null,
            TENACITY_CODE: getInt($row.find('.tenacity').val()),
            STATUS_CODE: getInt($row.find('.status').val()),
            UOM_CODE: getInt($row.find('.unit-no').val()),
            UOM_NAME: $row.find('.unit-name').val() || null,
            MAKE_CODE: null,
            FROM_DEPT: null,
            TO_DEPT: null,
            MAC_TYPE: $row.find('.loom-type').val() || null,
            MAC_CODE: getInt($row.find('.loom-no').val()),
            LAND_RATE: null,
            LAND_AMT: null,
            REF_TYPE: $row.find('.ref-type').val() || null,
            REF_NO: getInt($row.find('.ref-no').val()),
            LMRC_TYPE: $row.find('.lmrc-type').val() || null,
            LMRC_NO: getInt($row.find('.lmrc-no').val()),
            CREEL_NO: null,
            QC_SAMPLE: null,
            flg: null,
            LOG_FLG: null
        });
    });

    return {
        V_TYPE: $('#ddlDocType').val() || null,
        V_NO: getInt($('#NumDocNo').val()),
        V_DATE: $('#DtDocDate').val() || null,
        PARTY_CODE: getInt($('#ddlPartyName').val()),
        PLACE_CODE: getInt($('#ddlPlace').val()),
        PORD_TYPE: doType,
        PORD_NO: doNo,
        REF_TYPE: null,
        REF_NO: null,
        SHIFT: $('#ddlShift').val() || null,
        SLIP_NO: null,
        REMARKS: $('#TxtRemarks').val()?.trim() || null,
        MAC_TYPE: macType,
        MAC_CODE: getInt(macCode),
        GODOWN_CODE: getInt($('#ddlGodown').val()),
        STATUS: getInt($('#ddlStatus').val()),
        Items: Items,

        ACTION: (rowId && !isNaN(rowId)) ? 'UPDATE' : 'INSERT',
    };
}
function savePackingSlip() {
    const data = collectPackingSlipData();

    console.log("Save request data: ", data);

    $.ajax({
        url: '/PackingSlipEntry/SaveData',
        type: 'POST',
        contentType: 'application/json; charset=utf-8',
        data: JSON.stringify(data),
        beforeSend: function () {
            $('#btn-save').prop('disabled', true);
        },
        success: function (result) {
            if (result.status) {
                showToast(result.message || 'Packing Slip saved successfully.', { type: 'success' });
            } else {
                showToast(result.message || 'Unable to save Packing Slip.', { type: 'warning' });
            }
        },
        error: function (xhr) {
            showToast(xhr.responseJSON?.message || 'Error while saving Packing Slip.', { type: 'error' });
        },
        complete: function () {
            $('#btn-save').prop('disabled', false);
        }
    });
}


//============= EDIT & VIEW ==========
async function getDataById() {

    $.ajax({
        url: '/PackingSlipEntry/GetById',
        type: 'GET',
        data: { id: rowId, vtype: rowIdVType },
        success: async function (result) {
            if (result.status) {
                console.log("Packing Slip Data By Id:", result.data);
                await fillPackingSlipData(result.data);
            } else {
                showToast(result.message || 'Unable to fetch Packing Slip data.', { type: 'warning' });
            }
        },
        error: function (xhr) {
            showToast(xhr.responseJSON?.message || 'Error while fetching Packing Slip data.', { type: 'error' });
        }
    })
}
async function fillPackingSlipData(data) {
    try {
        isEdit = true;

        $('#ddlDocType').val(data.v_TYPE);
        $('#NumDocNo').val(data.v_NO);
        $('#DtDocDate').val(data.v_DATE.split('T')[0]);
        $('#ddlStatus').val(data.status);
        if (data.maC_CODE == null || data.maC_CODE === "" || data.maC_CODE === 0) {
            $('#ddlIssueMachType').val('1').trigger('change');
        } else {
            $('#ddlIssueMachType').val(data.maC_CODE).trigger('change');
        }

        if (data.shift == null || data.shift === "" || data.shift === 0) {
            $('#ddlShift').val('A');
        } else {
            $('#ddlShift').val(data.shift);
        }
        $('#ddlGodown').val(data.godowN_CODE).trigger('change');
        $('#ddlDONo').val(data.porD_TYPE + data.porD_NO).trigger('change');
        $('#ddlPartyName').val(data.partY_CODE).trigger('change');
        $('#TxtRemarks').val(data.remarks);
        $('#ddlPlace').val(data.placE_CODE);

        const items = data.items || [];
        $tbody.empty();
        for (const item of items) {
            await addNewRowBelow(item);
        }
    }
    catch (e) {
        console.error("Data binding error:", e);
    }
    finally {
        totalAmt();

        if (isReadOnly) {
            setFormReadonly();
        } else if (rowId && !isNaN(rowId)) {
            enableEditMode();
        }

        isEdit = false;
    }
}
function setFormReadonly() {
    const form = $('#ProformaPackingSlipForm');
    form.addClass('erppage-readonly');
    $('#btn-save').hide();
    $('#btnCopyFrom').prop('disabled', true)
        .css({
            'pointer-events': 'none',
            'cursor': 'not-allowed'
        });
    $('#tblProformaPackingSlip .btn-delete-action, #tblProformaPackingSlip .btn-add-action')
        .prop('disabled', true)
        .css({
            'pointer-events': 'none',
            'cursor': 'not-allowed'
        });
}
function enableEditMode() {
    if (isReadOnly) return;

    isEdit = true;
    $('#ProformaPackingSlipForm').removeClass('erppage-readonly');

    // Apply row-wise edit restrictions
    if (Number(compCode) !== 2) {
        $tbody.find('tr').each(function () {
            applyRowEditRestrictions($(this));
        });
    }
}

function applyRowEditRestrictions($row) {
    if (!$row.find('.item-code').val()) return;

    const hasRefNo = !!$row.find('.ref-no').val();

    if (hasRefNo) {
        $row.find('input, select').prop('disabled', true);
        $row.find('.remarks').prop('disabled', false);
    } else {
        $row.find('input, select').prop('disabled', false);
        $row.find('.item-code, .net-wgt, .ref-type, .ref-no, .lmrc-type, .lmrc-no')
            .prop('disabled', true);
    }
}



//==============Global Variables=========
async function getGlobalValues() {
    try {
        const response = await $.ajax({
            url: "/PackingSlipEntry/GetGlobalValues",
            type: "GET",
            dataType: "json"
        });

        if (response.success) {
            const d = response.data;
            compCode = d.compCode;
            yearCode = d.yearCode;
            branchCode = d.branchCode;
            companyName = d.companyName;
            add1 = d.add1;
            add2 = d.add2;
            db = d.db;
        } else {
            showToast(response.message || "Failed to load global values.", { type: "error" });
        }
    } catch (error) {
        console.error("Error loading global values:", error);
        showToast("An error occurred while loading global values.", { type: "error" });
    }
}


//==============Copy From=========
function getCopyFromData() {
    $.ajax({
        url: '/PackingSlipEntry/GetCopyFromList',
        type: 'GET',
        dataType: 'json',
        data: {
            itemCode: $('#ddlItem').val()  || 0,
            macCode: $('#ddlIssueMachType').val() || 0,
            vDate: $('#DtDocDate').val()
        },
        success: function (response) {
            if (response.success) {
                bindCopyFromData(response.data);
            } else {
                console.error(response.message);
                showToast("Error in fetching copy from list.", { type: "error" });
            }
        },
        error: function (xhr) {
            console.error(xhr.responseText);
            showToast("Error in fetching copy from list.", { type: "error" });
        }
    });
}
function bindCopyFromData(data) {
    const tbody = $('#tblCopyFromModal tbody');
    tbody.empty();

    $.each(data, function (i, item) {
        const row = $(`
            <tr>
                <td><input type="checkbox" class="chkCopyFromRow"></td>
                <td>${item.balE_NO ?? ''}</td>
                <td>${item.v_NO ?? ''}</td>
                <td>${item.v_TYPE ?? ''}</td>
                <td>${item.v_DATE ?? ''}</td>
                <td>${item.iteM_CODE ?? ''}</td>
                <td>${item.iteM_NAME ?? ''}</td>
                <td>${item.uoM_NAME ?? ''}</td>
                <td>${item.loT_NO ?? ''}</td>
                <td>${item.grosS_QTY ?? ''}</td>
                <td>${item.tarE_QTY ?? ''}</td>
                <td>${item.qty ?? ''}</td>
                <td>${item.mtr ?? ''}</td>
                <td>${item.avG_QTY ?? ''}</td>
                <td>${item.gram ?? ''}</td>
                <td>${item.tenacity ?? ''}</td>
                <td>${item.status ?? ''}</td>
                <td>${item.remarks ?? ''}</td>
                <td>${item.lmrC_TYPE ?? ''}</td>
                <td>${item.lmrC_NO ?? ''}</td>
                <td>${item.maC_TYPE ?? ''}</td>
                <td>${item.maC_CODE ?? ''}</td>
                <td>${item.placE_CODE ?? ''}</td>
                <td>${item.uniT_CODE ?? ''}</td>
                <td>${item.tenacitY_CODE ?? ''}</td>
                <td>${item.statuS_CODE ?? ''}</td>
                <td>${item.inch ?? ''}</td>
            </tr>
        `);

        row.data('item', item);
        tbody.append(row);
    });

    $('#chkSelectAllCopyFrom').prop('checked', false);

    const $rows = $('#tblCopyFromModal tbody tr');
    $('#lblTotalRecords').text(`Total Records: ${$rows.length}`);

}
async function copySelectedRows() {
    const selectedRows = $('#tblCopyFromModal tbody .chkCopyFromRow:checked');

    if (selectedRows.length === 0) {
        showToast('Please select at least one record.', { type: 'warning' });
        return;
    }

    try {
        isCopyFrom = true;

        for (const checkbox of selectedRows) {
            const item = $(checkbox).closest('tr').data('item');

            // Temporary row for duplicate checking
            const $tempRow = $(createRowHtml({
                ITEM_CODE: item.iteM_CODE,
                BALE_NO: item.balE_NO,
                LOT_NO: item.loT_NO
            }));

            if (duplicateItem($tempRow, String(compCode))) {
                let message = '';

                if (String(compCode) === '1') {
                    message = 'Duplicate Roll No record found.';
                } else if (String(compCode) === '2') {
                    message = `Duplicate record found.\nItem : ${item.iteM_NAME} (${item.iteM_CODE})\nRoll/Bale/Bag No : ${item.balE_NO}`;
                } else if (String(compCode) === '4') {
                    message = `Duplicate record found.\nRoll/Bale/Bag No : ${item.balE_NO}\nLot/Batch No : ${item.loT_NO}`;
                }

                await Swal.fire({
                    icon: 'warning',
                    title: 'Duplicate Record',
                    text: message,
                    confirmButtonText: 'OK'
                });
                continue;
            }

            await addNewRowBelow({
                ITEM_CODE: item.iteM_CODE,
                UOM_CODE: item.uniT_CODE,
                UOM_NAME: item.uoM_NAME,
                BALE_NO: item.balE_NO,
                LOT_NO: item.loT_NO,
                MTR: item.mtr,
                GROSS_QTY: item.grosS_QTY,
                TARE_QTY: item.tarE_QTY,
                QTY: item.qty,
                AVG_QTY: item.avG_QTY,
                GRAM: item.gram,
                TENACITY_CODE: item.tenacitY_CODE,
                STATUS_CODE: item.statuS_CODE,
                REMARKS: item.remarks,
                REF_TYPE: $('#ddlDocType').val() === 'FGIS' ? '' : item.v_TYPE,
                REF_NO: $('#ddlDocType').val() === 'FGIS' ? 0 : item.v_NO,
                MAC_TYPE: item.maC_TYPE,
                MAC_CODE: item.maC_CODE,
                PLACE_CODE: item.placE_CODE,
                ITEM_SIZE: item.inch
            });

            applyRowEditRestrictions($('#tblProformaPackingSlip tbody tr:last'));
        }

        totalAmt();
        $('#chkSelectAllCopyFrom').prop('checked', false);
        $('#tblProformaPackingSlip tbody .bale-no').first().trigger('focus');

    } finally {
        isCopyFrom = false;
        bootstrap.Modal.getOrCreateInstance(document.getElementById('copyFromModal')).hide();
    }
}
function updateSelectedRecords() {
    const $selected = $('#tblCopyFromModal tbody .chkCopyFromRow:checked');
    let totalNetWt = 0;

    $selected.each(function () {
        const item = $(this).closest('tr').data('item');
        totalNetWt += Number(item?.qty || 0);
    });

    $('#lblSelectedRecords, #lblTotalNetWt').show();

    $('#lblSelectedRecords').text(`Selected Records: ${$selected.length}`);
    $('#lblTotalNetWt').text(`Total Net Wt: ${totalNetWt.toFixed(2)}`);
}


//==============Report==========
function PackingSlipReport() {

    var reportName = "PRODUCTION5_PROF";
    var vType = ($("#ddlDocType").val() || "").trim();
    var VNO = ($("#NumDocNo").val() || "").trim();

    var formula =
        "{PPRODUCTION1.V_TYPE} = '" + vType + "'" +
        " AND {PPRODUCTION1.V_NO} = " + VNO +
        " AND {PPRODUCTION1.COMP_CODE} = " + compCode +
        " AND {PPRODUCTION1.YEAR_CODE} = " + yearCode +
        " AND {PPRODUCTION1.BRANCH_CODE} = " + branchCode;

    console.log("formula: ", formula);

    var formulaFields = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: db,

        Parameters: {
            comp_name: companyName,
            comp_add1: add1,
            comp_add2: add2,
            RPTNAME: $("#ddlDocType option:selected").text()
        }
    };

    console.log("formulaFields: ", formulaFields);

    // Generate timestamp
    var now = new Date();

    var day = String(now.getDate()).padStart(2, '0');
    var month = String(now.getMonth() + 1).padStart(2, '0');
    var year = String(now.getFullYear()).slice(-2);

    var hours = String(now.getHours()).padStart(2, '0');
    var minutes = String(now.getMinutes()).padStart(2, '0');
    var seconds = String(now.getSeconds()).padStart(2, '0');

    var timestamp = `${day}${month}${year}_${hours}${minutes}${seconds}`;

    $.ajax({
        url: 'http://localhost:34088/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(formulaFields),
        contentType: "application/json",
        xhrFields: { responseType: 'blob' },

        success: function (response) {
            console.log("PDF response:", response);

            var file = new Blob([response], { type: 'application/pdf' });

            var fileName = `${reportName}_${timestamp}.pdf`;
            var link = document.createElement('a');

            link.href = URL.createObjectURL(file);
            link.download = fileName;

            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);

            URL.revokeObjectURL(link.href);
        },

        error: function (xhr, status, error) {
            console.error("Error generating report:", error);
            console.error("Response:", xhr.responseText);
        }
    });
}


//===============SetMaxLength=========
function SetMaxlength(selector, precision, scale) {
    let value = $(selector).val();

    const integerDigits = precision - scale;

    const regex = new RegExp(
        `^\\d{0,${integerDigits}}(\\.\\d{0,${scale}})?$`
    );

    if (!regex.test(value)) {
        $(selector).val(value.slice(0, -1));
    }
}
