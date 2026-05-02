let itemList = [];
let deptList = [];
let unitList = [];
let itemMap = {};
let DeptMap = {};
let UnitMap = {};

const urlParams = new URLSearchParams(window.location.search);
const rowId = urlParams.get('id');
const vtype = urlParams.get('vtype');
const $tbody = $("#tblConsumptionEntry tbody");
const mode = urlParams.get('mode');
const isReadOnly = (mode === 'view');

async function Misconsumptioninit() {
    try {

        $("#ddlDocType").focus();
        setCurrentDateTime();
        await initDropdowns();

        if (rowId && vtype) {
            await LoadFormByID(rowId, vtype);
        }

        if (isReadOnly) {
            setFormReadOnly();
        }
       
    } catch (err) {
        showToast("Init Error: " + err.message, { type: "error" });
        console.error(err);
    }

    $(document).on("click", "#btn-save", async function (e) {
        e.preventDefault();
        await saveConsumptionEntry();
    });

    $(document).on("click", ".btn-add-action", function () {
        addRow($tbody);
    });

    $(document).on("click", ".btn-add-row", function () {
        addRow($tbody);
    });

    $(document).on("click", ".btn-delete-action", function () {

        const $row = $(this).closest("tr");
        const wasLast = $row.is(":last-child");

        $row.remove();

        if (wasLast) {
            const $last = $tbody.find("tr:last");

            if ($last.length && !$last.find(".btn-add-action").length) {
                $last.find("td:last").prepend(
                    `<i class="fa fa-plus btn-add-action text-success" title="Add Row"></i>`
                );
            }
        }
    });

    $(document).on("click", "#btn-pending", async function () {
        const partyId = $("#ddlPartyName").val();
        if (!partyId) {
            showToast("Please select a Party.", { type: "warning" });
            return;
        }
        await loadPendingDocuments(partyId);
    });

    $(document).on('click', '#PendngAddRow', function (e) {
        e.preventDefault();

        let selectedRows = getSelectedPendingDocuments();

        if (selectedRows.length === 0) {
            showToast("Please select at least one row.", { type: "warning" });
            return;
        }
        addSelectedToConsumptionTable(selectedRows);

        $('#pendingModal').modal('hide');
    });

    $(document).on("change", "#ddlPartyName", async function () {
        const partyId = $(this).val();

        if (!partyId) {
            $("#TxtAdd1PD, #TxtAdd2PD, #TxtAdd3PD").val("");
            return;
        }

        await fetchPartyAddressDetails(partyId);
    });
}

//===Set Current Date & Time=====
function setCurrentDateTime() {
    const now = new Date();
    const localDate = now.getFullYear() + '-' +
        String(now.getMonth() + 1).padStart(2, '0') + '-' +
        String(now.getDate()).padStart(2, '0');

    $('#DtDocDate').val(localDate);
    $('#TmDocTime').val(now.toTimeString().slice(0, 5));
}

//====Bind Dropdown========
async function initDropdowns() {

    await Promise.all([
        bindDropdown('MiscConsumptionEntry', 'DocType', '#ddlDocType', '', null, null, true),
        bindDropdown('MiscConsumptionEntry', 'Party', '#ddlPartyName', '-- Select Party --')
    ]);

    await Promise.all([
        loadItemMaster(),
        loadDeptMaster(),
        loadUnit()
    ]);
    const defaultDocType = $('#ddlDocType').val();

    if (defaultDocType) {
        await GetVNo(defaultDocType);
    }
    addRow($tbody);
    initSelect2();
   
}

//====For DDlParty====
function initSelect2() {
    $('#ddlPartyName').select2({
        placeholder: "-- Select Party --",
        allowClear: true,
        width: '100%'
    });
}

//=== Get party Address===
async function fetchPartyAddressDetails(partyId) {
    try {
        const data = await MisConsumptionApi.getPartyAddress(partyId);

        console.log("Address API Response:", data);

        if (data && data.length > 0) {
            const d = data[0];

            $("#TxtAdd1PD").val(d.add1 || "");
            $("#TxtAdd2PD").val(d.add2 || "");
            $("#TxtAdd3PD").val(d.add3 || "");
        } else {
            $("#TxtAdd1PD, #TxtAdd2PD, #TxtAdd3PD").val("");
        }

    } catch (err) {
        console.error(err);
        showToast("Error fetching address", { type: "error" });
    }
}

//====Get Vno=======
async function GetVNo(vtype) {
    try {
        const data = await MisConsumptionApi.getVNo(vtype);
        if (data && data.v_NO) {
            $('#NumDocNo').val(data.v_NO);
        } else {
            showToast("No Document Number Received", { type: "warning" });
            console.warn("No document number received");
        }
    } catch (error) {
        showToast("Error fetching VNo", { type: "error" });
        console.error(error);
    }
}

//=====Save And Update========
async function saveConsumptionEntry() {

    // ===== CALL VALIDATION =====
    if (!validateConsumptionForm()) return;

    const isValidDate = await checkValidDate();
    if (!isValidDate) return;

    // ===== HEADER =====
    const header = {
        DOC_ID: $.trim($('#TxtCode').val()) || null,
        V_TYPE: $('#ddlDocType').val() || null,
        V_NO: parseInt($('#NumDocNo').val()) || null,
        V_DATE: $("#DtDocDate").val(),
        V_TIME:$('#TmDocTime').val(),
        PARTY_CODE: parseInt($('#ddlPartyName').val()) || null,
        TRUCK_NO: $.trim($('#TxtVehicleNo').val()) || null,
        REMARKS: $.trim($('#TxtRemarks').val()) || null,
        Add1: $.trim($('#TxtAdd1PD').val()) || null,
        Add2: $.trim($('#TxtAdd2PD').val()) || null,
        Add3: $.trim($('#TxtAdd3PD').val()) || null,
        ITEM_TYPE: $('#ddlType').val() || null,
        action: $('#TxtCode').val() ? 'UPDATE' : 'INSERT'
    };

    // ===== DETAILS(FOOTER) =====
    const payload = {
        Header: header,
        Deatils: collectTableRowData()
    };

    try {
        $("#btn-save").prop("disabled", true);

        const response = await MisConsumptionApi.saveData(payload);

        if (response && response.success) {

            const msg = header.action === 'UPDATE'
                ? "Data Updated Successfully"
                : "Data Saved Successfully";

            showToast(msg, { type: "success" });

            setTimeout(() => {
                window.location.href = '/MiscConsumptionEntryList/Index';
            }, 1000);

        } else {
            showToast("Error while saving", { type: "error" });
        }

    } catch (err) {
        console.error(err);
        showToast("Server Error while saving", { type: "error" });
    } finally {
        $("#btn-save").prop("disabled", false);
    }
}

//======Load Data On Edit======
async function LoadFormByID(rowId, vtype) {
    try {

        const result = await MisConsumptionApi.getFormById(rowId, vtype); // API call

        if (!result.success || !result.data || !result.data.header) {
            showToast("Invalid or missing response data.", { type: "error" });
            return;
        }

        const header = result.data.header;
        const details = result.data.details;
        console.log("Table Details", details);
        // ========= HEADER FILL =========
        $('#TxtCode').val(header.doC_ID || '');
        $('#ddlDocType').val(header.v_TYPE || '');
        $('#NumDocNo').val(header.v_NO || '');
        $('#TmDocTime').val(header.v_TIME);
        /* $('#DtDocDate').val(formatDate(header.v_DATE));*/
        $('#DtDocDate').val(formatDate(header.v_DATE, true));
        $('#ddlPartyName') .val(header.partY_CODE || '').trigger('change');
        $('#TxtVehicleNo').val(header.trucK_NO || '');
        $('#TxtRemarks').val(header.remarks || '');
        $('#TxtAdd1PD').val(header.add1 || '');
        $('#TxtAdd2PD').val(header.add2 || '');
        $('#TxtAdd3PD').val(header.add3 || '');
        $('#ddlType').val(header.iteM_TYPE || '');

        // ========= TABLE FILL =========
        const $tbody = $("#tblConsumptionEntry tbody");
        $tbody.empty();

        (details || []).forEach(detail => {

            const rowData = {
                code: detail.iteM_CODE || '',
                itemName: detail.iteM_CODE || '',
                department: detail.depT_CODE || '',
                unit: detail.uoM_CODE || '',
                no: detail.nos || '',
                quantity: detail.qty || '',
                remarks: detail.remarks || ''
            };

            addRow($tbody, rowData); //reuse UI function
        });

    } catch (error) {
        console.error(error);
        showToast("Error loading Form Data", { type: "error" });
    }
}

//====Helper Function(Date)====
function formatDate(dateStr, forInput = false) {
    if (!dateStr) return '';

    const d = new Date(dateStr);
    if (isNaN(d)) return '';

    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();

    // INPUT FIELD FORMAT (yyyy-MM-dd)
    if (forInput) {
        return `${year}-${month}-${day}`;
    }

    // DISPLAY FORMAT (dd-MM-yyyy)
    return `${day}-${month}-${year}`;
}


//=========================
//   FOOTER TABLE 
//=========================
function addRow($tbody, data = {}) {

    $tbody.find(".btn-add-action").remove();

    const row = `
        <tr class="no-border-input">
            <td style="display:none;">${data.code || ""}</td>

            <td>
                <select class="form-control itemName">
                    <option value="">-- Select --</option>
                </select>
            </td>

            <td>
                <select class="form-control department">
                    <option value="">-- Select --</option>
                </select>
            </td>

            <td>
                <select class="form-control unit">
                    <option value="">-- Select --</option>
                </select>
            </td>

            <td>
                <input type="number" class="form-control no" value="${data.no || ''}"/>
            </td>

            <td>
                <input type="number" class="form-control quantity" value="${data.quantity || ''}"/>
            </td>

            <td>
                <input type="text" class="form-control remarks" value="${data.remarks || ''}"/>
            </td>

            <td>
                <i class="fa fa-plus btn-add-action text-success" title="Add Row"></i>
                <i class="fa fa-trash btn-delete-action text-danger" title="Delete Row"></i>
            </td>
        </tr>
    `;

    const $newRow = $(row);
    $tbody.append($newRow);

    bindSelectOptions($newRow.find('.itemName'), itemList);
    bindSelectOptions($newRow.find('.department'), deptList);
    bindSelectOptions($newRow.find('.unit'), unitList);

    if (data.itemName) {
        $newRow.find('.itemName').val(data.itemName);
    }

    if (data.department) {
        $newRow.find('.department').val(data.department);
    }

    if (data.unit) {
        $newRow.find('.unit').val(data.unit);
    }

    // trigger change for select2
    $newRow.find('.itemName').trigger('change');
    $newRow.find('.department').trigger('change');
    $newRow.find('.unit').trigger('change');

    initRowSelect2($newRow);
}

//===helper Function===
function getKeyByValue(map, value) {
    return Object.keys(map).find(key => map[key] === value);
}

function addSelectedToConsumptionTable(selectedRows) {

    const $tbody = $("#tblConsumptionEntry tbody");

    selectedRows.forEach(row => {

        // FAST LOOKUP
        const itemCode = getKeyByValue(itemMap, row.item_name);
        const unitCode = getKeyByValue(UnitMap, row.unit);
        const deptCode = getKeyByValue(DeptMap, row.department);

        // find blank row (better check)
        let $blankRow = null;

        $tbody.find("tr").each(function () {

            const itemVal = $(this).find("select.itemName").val();
            const qtyVal = $(this).find("input.quantity").val();

            const isEmptyRow = !itemVal && (!qtyVal || qtyVal == 0);

            if (isEmptyRow) {
                $blankRow = $(this);
                return false;
            }
        });

        const fillRow = ($row) => {

            $row.find("select.itemName")
                .val(itemCode || "")
                .trigger("change");

            $row.find("select.unit")
                .val(unitCode || "")
                .trigger("change");

            $row.find("input.no").val(row.nos || "");
            $row.find("input.quantity").val(row.quantity || "");
            $row.find("input.remarks").val(row.remarks || "");
        };

        if ($blankRow) {
            fillRow($blankRow);
        } else {
            addRow($tbody);
            const $newRow = $tbody.find("tr:last");
            fillRow($newRow);
        }
    });
}

//=====For Search Box======
function initRowSelect2($row) {
    if (!$row || $row.length === 0) return; 
    $row.find('.itemName').select2({
        placeholder: "-- Select Item --",
        allowClear: true,
        width: '100%'
    });

    $row.find('.department').select2({
        placeholder: "-- Select Department --",
        allowClear: true,
        width: '100%'
    });

    $row.find('.unit').select2({
        placeholder: "-- Select Unit --",
        allowClear: true,
        width: '100%'
    });
}

//===Load Footer DropDown======
function bindSelectOptions($select, data) {
    $select.empty().append('<option value="">-- Select --</option>');

    data.forEach(item => {
        $select.append(
            `<option value="${item.value}">${item.text}</option>`
        );
    });
}

async function loadItemMaster() {
    const data = await MisConsumptionApi.getItemMaster();

    itemList = data; // ✅ store list
    itemMap = {};

    data.forEach(i => {
        itemMap[i.value] = i.text;
    });
}

async function loadDeptMaster() {
    const data = await MisConsumptionApi.getDeptMaster();

    deptList = data;
    DeptMap = {};

    data.forEach(i => {
        DeptMap[i.value] = i.text;
    });
}

async function loadUnit() {
    const data = await MisConsumptionApi.getUnit();

    unitList = data;
    UnitMap = {};

    data.forEach(i => {
        UnitMap[i.value] = i.text;
    });
}

//====Pending Documents=======
async function loadPendingDocuments(partyId) {
    try {

        const data = await MisConsumptionApi.getPendingDocuments(partyId);
        const tbody = $('#tblPendingDocument tbody');
        tbody.empty();
        if (!data || data.length === 0) {

            showToast("No pending documents found for this party. Please select another party.", { type: "warning" });

            return; 
        }
        data.forEach(item => {
            const row = `
                <tr>
                    <td style="display:none;">${item.v_NO}</td>

                    <td>
                        <input type="checkbox" class="select-row"/>
                    </td>

                    <td>${item.v_type || ''}</td>
                    <td>${item.v_NO || ''}</td>
                    <td>${item.v_date || ''}</td>
                    <td>${item.qty || ''}</td>
                    <td>${item.p_Qty || ''}</td>
                    <td>${item.item_name || ''}</td>
                    <td>${item.remarks || ''}</td>
                    <td>${item.nos || ''}</td>
                    <td>${item.unitname || ''}</td>
                    <td>${item.srno || ''}</td>
                </tr>
            `;
            tbody.append(row);
        });
        $('#pendingModal').modal('show');

    } catch (error) {
        console.error(error);
        showToast("Error loading pending documents", { type: "error" });
    }
}

//===Add pending Documents=====
function getSelectedPendingDocuments() {
    let selectedRows = [];

    $('#tblPendingDocument tbody tr').each(function () {
        let checkbox = $(this).find('.select-row');

        if (checkbox.is(':checked')) {
            let row = $(this);

            let rowData = {
                item_name: row.find('td').eq(7).text(),
                department: "", // abhi blank hi rahega
                unit: row.find('td').eq(10).text(),
                nos: row.find('td').eq(9).text(),
                quantity: row.find('td').eq(5).text(),
                remarks: row.find('td').eq(8).text(),
                code: row.find('td').eq(0).text()
            };

            selectedRows.push(rowData);
        }
    });

    return selectedRows;
}

//====Details(For Save)======
function collectTableRowData() {

    const rows = document.querySelectorAll('#tblConsumptionEntry tbody tr');

    const data = [];

    rows.forEach(row => {

        const item = row.querySelector('select.itemName');
        const dept = row.querySelector('select.department');
        const unit = row.querySelector('select.unit');
        const nos = row.querySelector('input.no');
        const qty = row.querySelector('input.quantity');
        const remarks = row.querySelector('input.remarks');

        // skip completely empty rows (IMPORTANT FIX)
        const isEmpty =
            !item?.value &&
            !dept?.value &&
            !unit?.value &&
            !nos?.value &&
            !qty?.value;

        if (isEmpty) return;

        data.push({
            ITEM_CODE: Number(item?.value) || null,
            ITEM_NAME: item?.selectedOptions[0]?.text || '',
            DEPT_CODE: Number(dept?.value) || null,
            UOM_CODE: Number(unit?.value) || null,
            NOS: Number(nos?.value) || null,
            QTY: parseFloat(qty?.value) || null,
            REMARKS: remarks?.value || ''
        });

    });

    return data;
}

//=====readOnly Mode=========
function setFormReadOnly() {
    const formSelector = '#MiscConsumptionEntryForm';

    $(`${formSelector} input:not([type="hidden"]):not([type="time"])`).prop('readonly', true);
    $(`${formSelector} input[type="time"]`).prop('disabled', true);
    $(`${formSelector} select`).prop('disabled', true);
    $(`${formSelector} textarea`).prop('readonly', true);
    $(`${formSelector} button`).prop('disabled', true);
    $(formSelector).addClass('erppage-readonly');
    $('#btn-save').hide();

    // ===== TABLE FIELDS FIX =====
    $('#tblConsumptionEntry tbody tr').each(function () {

        $(this).find('input').prop('readonly', true).prop('disabled', true);
        $(this).find('select').prop('disabled', true);
        $(this).find('select').each(function () {
            if ($(this).hasClass("select2-hidden-accessible")) {
                $(this).select2('destroy');
            }
        });
        $(this).find('.btn-add-action, .btn-delete-action')
        .css({
            'pointer-events': 'none',
            'opacity': '0.5'
        });
    });

    //ADD THIS (TABLE CSS LOCK - SIMPLE & CLEAN)
    $('#tblConsumptionEntry').css({
        'pointer-events': 'none',
        'opacity': '0.85'
    });

    // Pending table also disable
    $('#tblPendingDocument tbody tr').each(function () {
        $(this).find('input, select, textarea').prop('disabled', true);
    });

    $('#tblPendingDocument').css({
        'pointer-events': 'none',
        'opacity': '0.85'
    });

    // global row buttons
    $('.btn-add-row, .btn-delete-row, #PendngAddRow').css({
        'pointer-events': 'none',
        'opacity': '0.5'
    });

    $('#tablePagination').css({
        'pointer-events': 'none',
        'opacity': '0.5'
    });

    $(`${formSelector} input, ${formSelector} select, ${formSelector} textarea`)
        .attr('tabindex', '-1');
}


