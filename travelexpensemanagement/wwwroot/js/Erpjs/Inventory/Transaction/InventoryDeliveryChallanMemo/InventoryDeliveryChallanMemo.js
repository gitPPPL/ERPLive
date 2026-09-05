const urlParams = new URLSearchParams(location.search);
const rowId = parseInt(urlParams.get('id'));
let isReadOnly = urlParams.get('readOnly') === 'true';
let isLoadById = false;

let compCode = "";
let yearCode = "";
let branchCode = "";
let companyName = "";
let add1 = "";
let add2 = "";
let db = "";
var controllerName = window.location.pathname.split('/')[1];

$(document).ready(async function () {
    checkPermissionForEntryPage(controllerName, function () {
    });
    getGlobalValues();
    await GetVNo();
    const currentDate = getCurrentDateYMD();
    $('#DtDocDate, #DtReturnDate').val(currentDate);
    await bindHeaderDropdowns();
    wireEvents();
    await addNewRowBelow();
    if (rowId && !isNaN(rowId)) {
        GetDataById();
    }
})

//=============GENERATE VNO=================
async function GetVNo() {
    try {
        const res = await fetch('/InventoryDeliveryChallanMemo/GetVNo');
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();
        if (!data.v_NO) throw new Error('Response missing v_NO');
        $('#NumDocno').val(data.v_NO);
    }
    catch (e) {
        showToast('Error loading Document Number: ' + e.message, { type: "warning" });
    }
}

//=============DROPDOWN=================
async function bindHeaderDropdowns() {
    return Promise.all([
        bindDropdown("InventoryDeliveryChallanMemo", "employee", '#ddlEmployeeName', '--Select Employee--', null, null, false, null, true),
        bindDropdown("InventoryDeliveryChallanMemo", "vendor", '#ddlVendorName', '--Select Vendor--', null, null, false, null, true)
    ]);
}
function loadItemList(dropdownId) {
    const ddl = $(dropdownId);
    if (!ddl.length) {
        return;
    }

    if (ddl.hasClass('select2-hidden-accessible')) {
        return;
    }
    ddl.select2({
        placeholder: "-- Select --",
        allowClear: true,
        // 1. Change this to 0 so it triggers as soon as the dropdown opens
        minimumInputLength: 0,
        ajax: {
            url: '/InventoryDeliveryChallanMemo/GetItemList',
            dataType: 'json',
            delay: 250,
            global: false,
            data: function (params) {
                return {
                    // If params.term is undefined (on first click), pass an empty string
                    searchTerm: params.term || "",
                    page: params.page || 1
                };
            },
            processResults: function (data, params) {
                params.page = params.page || 1;

                const results = data.data.map(item => ({
                    id: item.Value,
                    text: item.Text,
                    // Custom data
                    ucode: item.ucode,
                    unit: item.unit
                }));

                return {
                    results: results,
                    pagination: {
                        // Triggers infinite scroll loading if there are more items
                        more: (params.page * 30) < data.totalCount
                    }
                };
            },
            cache: true
        }
    });
    ddl.on('select2:open', function () {

        setTimeout(function () {

            const searchBox =
                document.querySelector(
                    '.select2-container--open .select2-search__field'
                );

            if (searchBox) {
                searchBox.focus();
            }

        }, 0);
    });
}
function setSelect2Value(selector, value, text) {
    const $ddl = $(selector);
    if (!$ddl.length || value == null) return;
    const option = new Option(text || '', value, true, true);
    $ddl.append(option).trigger('change');
}

//=============ADD ROWS IN FOOTER TABLE=================
function createRowHtml(data = {}) {

    return `
        <tr>

            <td class="freeze-item"><select class="form-control form-control-sm item-name"></select></td>

            <td>
                <input class="form-control form-control-sm uom-code" type="hidden" value="${data.uniT_CODE || data.UNIT_CODE || ''}" disabled/>
                <input class="form-control form-control-sm uom-name" type="text" value="${data.uniT_NAME || data.UNIT_NAME || ''}" disabled/>
            </td>

            <td><input class="form-control form-control-sm nos" type="number" value="${data.nos || data.NOS || ''}"/></td>
            <td><input class="form-control form-control-sm qty" type="number" value="${data.QTY || data.qty || ''}"/></td>
            <td><input class="form-control form-control-sm approx-amount" type="number" value="${data.approX_AMT || data.APPROX_AMT || ''}"/></td>
            <td><input class="form-control form-control-sm remarks" type="text" value="${data.remarks || data.REMARKS || ''}"/></td>

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

    // Remove delete button from the current last row
    const $previousLastRow = $("#tblItemrecord tbody tr:last");
    $previousLastRow.find(".btn-add-action").remove();

    let rowHtml = createRowHtml(data);
    $("#tblItemrecord tbody").append(rowHtml);

    const $lastRow = $("#tblItemrecord tbody tr:last");

    //Item Dropdown
    const itemName = $lastRow[0].querySelector(".item-name");
    loadItemList(itemName);
    setSelect2Value(itemName, data.iteM_CODE || data.ITEM_CODE, data.iteM_NAME || data.ITEM_NAME)
}

//=============EVENTS===============
function wireEvents() {
    //--------- Item Change ---------
    $(document).on("change", ".item-name", function () {

        if (isLoadById) return;
        const currentSelect = $(this).closest('tr').find('.item-name')[0];
        // Duplicate
        if (checkDuplicateItems(currentSelect)) {
            return;
        }

        const item = $(this).select2('data')[0];

        if (!item) return;

        const $row = $(this).closest("tr");

        $row.find(".uom-code").val(item.ucode || "");
        $row.find(".uom-name").val(item.unit || "");
    });
    //---------- Delete Row Button Click -----------
    $(document).on('click', '.btn-delete-action', function () {
        // Prevent deleting if only one row exists
        const $tbody = $('#tblItemrecord tbody');
        if ($tbody.find('tr').length === 1) {
            return;
        }
        const $row = $(this).closest('tr');
        $row.remove();
    });
    //---------- Add Row Button Click -----------
    $(document).on('click', '.btn-add-action', async function () {
        const currentSelect = $(this).closest('tr').find('.item-name')[0];
        // Duplicate
        if (checkDuplicateItems(currentSelect)) {
            return;
        }
        await addNewRowBelow();
    });
    //--------- Save btn Click -----------
    $('#btn_save').on('click', async function (e) {
        e.preventDefault();
        const isValid = await validate();
        if (!isValid) return;
        
        try {
            SaveDeliveryChallanMemo()
        }
        catch (error) {
            console.error(error);
        }
    })
}

//=============VALIDATIONS===============
async function checkValidDate() {
    const data = {
        vdate: $("#DtDocDate").val(),
        vtype: 'GTMO',
        vno: $("#NumDocno").val()
    };
    try {
        const response = await fetch('/InventoryDeliveryChallanMemo/CheckValidDate', {
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
async function validate() {
    //------------- Validate VDate ------------
    const isValidVDate = await checkValidDate();
    if (!isValidVDate) {
        return false;
    }

    if (!validateRequiredField('#ddlEmployeeName', 'Employee Name') || !validateRequiredField('#ddlVendorName', 'Vendor Name')) return false;

    // Item Details Required
    const $rows = $('#tblItemrecord tbody tr');
    let hasItem = false;

    $rows.each(function () {
        const itemCode = $(this).find('.item-name').val();

        if (itemCode && itemCode !== '0') {
            hasItem = true;
            return false;
        }
    });

    if (!hasItem) {
        showToast('Items Details Required, Please Check!', { type: "warning" });
        return false;
    }

    // Return Date should not be less than Doc Date
    const docDate = $('#DtDocDate').val();
    const returnDate = $('#DtReturnDate').val();

    if (docDate && returnDate) {

        const docDateValue = new Date(docDate);
        const returnDateValue = new Date(returnDate);

        if (returnDateValue < docDateValue) {
            setInvalid($('#DtReturnDate'), 'Return Date should not be less than Doc Date, Please Check!');
            return false;
        }
    }


    // 3. Validate Item Rows
    let isValid = true;

    $rows.each(function () {

        const $row = $(this);

        const $item = $row.find('.item-name');
        const $nos = $row.find('.nos');
        const $qty = $row.find('.qty');

        const itemCode = $item.val();
        const itemName = $item.find('option:selected').text();

        const nos = parseFloat($nos.val()) || 0;
        const qty = parseFloat($qty.val()) || 0;


        // Item selected
        if (!itemCode || parseFloat(itemCode) <= 0) {
            setInvalid($item, 'Item Required, Please Check!');
            isValid = false;
            return false;
        }

        const currentSelect = $row.find('.item-name')[0];
        // Duplicate
        if (checkDuplicateItems(currentSelect)) {
            isValid = false;
            return false;
        }

        // Nos required
        if (nos <= 0) {
            setInvalid($nos, `Nos Required for ${itemName}, Please Check!`);
            isValid = false;
            return false;
        }


        // Quantity required
        if (qty <= 0) {
            setInvalid($qty, `Item Quantity Required for ${itemName}, Please Check!`);
            isValid = false;
            return false;
        }

    });

    return isValid;
}
function checkDuplicateItems(currentSelect) {

    const value = currentSelect.value;
    if (!value) return false;

    let duplicate = false;

    document.querySelectorAll('#tblItemrecord .item-name').forEach(el => {
        if (el === currentSelect) return;

        if (el.value === value) {
            duplicate = true;
        }
    });

    if (duplicate) {

        $(currentSelect).addClass("is-invalid");
        showToast("Duplicate item found!", { type: "warning" });
    } else {
        $(currentSelect).removeClass("is-invalid");
    }

    return duplicate;
}
//=============Collect Data For Save & Update==========
function CollectHeaderData() {

    const items = CollectFooterData();
    const request = {
        V_NO: parseInt($('#NumDocno').val()) || 0,
        V_DATE: $('#DtDocDate').val() || "",
        EMP_CODE: parseInt($('#ddlEmployeeName').val()) || 0,
        EMP_NAME: ($('#ddlEmployeeName option:selected').text() || "").split('|')[1]?.trim() || "",
        VENDOR_CODE: parseInt($('#ddlVendorName').val()) || 0,
        VENDOR_NAME: $('#ddlVendorName').val() === "" ? "" : ($('#ddlVendorName option:selected').text() || "").trim(),
        TRANSPORT_CODE: 0,
        TRANSPORT_NAME: $('#TxtTransportcourier').val() || "",
        THROUGH: $('#TxtThrough').val() || "",
        RETURN_DATE: $('#DtReturnDate').val() || "",
        REMARKS: $('#TxtRemarks').val() || "",
        
        ACTION: (rowId && !isNaN(rowId)) ? "UPDATE" : "INSERT",

        items: items

    }

    return request;
}
function CollectFooterData() {
    const items = [];

    $('#tblItemrecord tbody tr').each(function (index) {

        const $row = $(this);
        const item = $row.find('.item-name');

        const data = {
            ITEM_CODE: parseInt(item.val()) || 0,
            ITEM_NAME: item[0].selectedIndex >= 0
                ? item[0].options[item[0].selectedIndex].text.trim()
                : "",
            UNIT_CODE: parseInt($row.find('.uom-code').val()) || 0,
            UNIT_NAME: $row.find('.uom-name').val() || null,
           
            NOS: parseInt($row.find('.nos').val()) || 0,
            QTY: parseFloat($row.find('.qty').val()) || 0,
            APPROX_AMT: parseFloat($row.find('.approx-amount').val()) || 0,

            REMARKS: $row.find('.remarks').val() || ''
        };

        items.push(data);
    });

    return items;
}
function SaveDeliveryChallanMemo() {
    const request = CollectHeaderData();
    console.log("Request Data For Save: ", request);
    $.ajax({
        url: '/InventoryDeliveryChallanMemo/SaveDeliveryChallanMemo',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(request),
        success: function (res) {
            if (res.success) {
                showToast("Saved Successfully!", { type: "success" });
                setFormReadonly();
                isReadOnly = true;
                setTimeout(() => window.location.href = '/InventoryDeliveryChallanMemo/Index?id=' + encodeURIComponent($('#NumDocno').val()) + '&readOnly=true', 2500);
            }
            else {
                showToast(res.message, { type: "warning" });
            }
        },
        error: function (xhr) {
            console.error(xhr);
            showToast("An error occurred while saving!", { type: "error" });
        }
    })
}

//=============EDIT AND VIEW==========
async function GetDataById() {
    try {
        const res = await $.ajax({
            url: '/InventoryDeliveryChallanMemo/GetDataById',
            type: 'GET',
            data: {docId: rowId},
            dataType: 'JSON'
        });

        if (!res.success) {
            showToast(res.message, { type: "warning" });
            return;
        }

        isLoadById = true;

        console.log("Get By Id: ", res.data);

        await bindDataById(res.data);
    }
    catch (xhr) {
        console.error(xhr);
        showToast('An error occurred while fetching data by Id!', { type: "error" });
    }
    finally {
        isLoadById = false;
    }
}
async function bindDataById(data) {

    if (!data) return;

    // HEADER
    $('#NumDocno').val(rowId);
    $('#DtDocDate').val(formatDate(data.v_DATE));
    $('#DtReturnDate').val(formatDate(data.returN_DATE));
    $('#TxtTransportcourier').val(data.transporT_NAME || '');
    $('#TxtThrough').val(data.through || '');
    $('#TxtRemarks').val(data.remarks || '');
    $('#ddlEmployeeName').val(data.emP_CODE || '').trigger('change');
    $('#ddlVendorName').val(data.vendoR_CODE || '').trigger('change');

    // FOOTER ITEMS

    const $tbody = $('#tblItemrecord tbody');

    $tbody.empty();
    if (data.items && data.items.length > 0) {
        for (const item of data.items) {
            await addNewRowBelow(item);
        }
    }
    else {
        await addNewRowBelow();
    }

    if (isReadOnly) {
        setFormReadonly();
    }
}
function setFormReadonly() {
    const form = $('#DeliveryChallanMemoform');
    form.addClass('erppage-readonly');
    $('#btn_save').hide();
    $('#tblItemrecord .btn-delete-action, #tblItemrecord .btn-add-action')
        .prop('disabled', true)
        .css({
            'pointer-events': 'none',
            'cursor': 'not-allowed'
        });
}

//=============HELPERS=================
function formatDate(dateStr) {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    if (isNaN(date)) return '';
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}

//=============REPORT=================
async function getGlobalValues() {
    try {
        const response = await $.ajax({
            url: "/InventoryDeliveryChallanMemo/GetGlobalValues",
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
function DeliveryChallanMemoReport() {

    var reportName = "Memo1";
    var vType = "GTMO";
    var VNO = ($("#NumDocno").val() || "").trim();

    var formula =
        "{GATE_MEMO1.comp_code} = " + compCode +
        " AND {GATE_MEMO1.YEAR_CODE} = " + yearCode +
        " AND {GATE_MEMO1.BRANCH_CODE} = " + branchCode +
        " AND {GATE_MEMO1.V_TYPE} = '" + vType + "'" +
        " AND {GATE_MEMO1.V_NO} = " + VNO;

    var formulaFields = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: db,

        Parameters: {
            comp_name: companyName,
            comp_add1: add1,
            comp_add2: add2,
            RPTNAME: "Memo for Challan",
        }
    };

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