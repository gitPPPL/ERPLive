

async function DDLVtype() {

    try {

        const res = await fetch('/InventoryOpeningEntry/DDlVType');

        if (!res.ok) {
            throw new Error(`HTTP ${res.status}`);
        }

        const data = await res.json();

        const ddl = $('#ddlDocType');

        ddl.empty();
            

        data.forEach(item => {

            ddl.append(
                `<option value="${item.value}">${item.text}</option>`
            );

        });

    } catch (error) {

        console.error("Error loading VType:", error);

        throw error;
    }
}
function DDlItemName() {

    return $.ajax({
        url: '/InventoryOpeningEntry/DDlItemName',
        method: 'GET',
        dataType: 'json'
    })
        .then(function (data) {

            console.log("DDlItemName:", data);

            if (!Array.isArray(data)) {
                throw new Error("DDlItemName response is not an array");
            }

            ItemNameList = data
                .map(x =>
                    `<option value="${x.value}">${x.text}</option>`
                )
                .join('');

        })
        .catch(function (error) {

            console.error("Error loading ItemName:", error);

            throw error;
        });
}
function DDlUnit() {

    return $.ajax({
        url: '/InventoryOpeningEntry/DDlUnit',
        method: 'GET',
        dataType: 'json'
    })
        .then(function (data) {

            console.log("DDlUnit:", data);

            if (!Array.isArray(data)) {
                throw new Error("DDlUnit response is not an array");
            }

            unitnameList = data
                .map(x =>
                    `<option value="${x.value}">${x.text}</option>`
                )
                .join('');

        })
        .catch(function (error) {

            console.error("Error loading Unit:", error);

            throw error;
        });
}
function DDLItemmake() {

    return $.ajax({
        url: '/InventoryOpeningEntry/DDLItemmake',
        method: 'GET',
        dataType: 'json'
    })
        .then(function (data) {

            console.log("DDLItemmake:", data);

            if (!Array.isArray(data)) {
                throw new Error("DDLItemmake response is not an array");
            }

            ItemmakeList = data
                .map(x =>
                    `<option value="${x.value}">${x.text}</option>`
                )
                .join('');

        })
        .catch(function (error) {

            console.error("Error loading ItemMake:", error);

            throw error;
        });
}
function DDLItemDapt() {

    return $.ajax({
        url: '/InventoryOpeningEntry/DDLItemDapt',
        method: 'GET',
        dataType: 'json'
    })
        .then(function (data) {

            console.log("DDLItemDapt:", data);

            if (!Array.isArray(data)) {

                console.error(
                    "ItemDeptList response is not an array:",
                    data
                );

                throw new Error(
                    "Invalid ItemDeptList response"
                );
            }

            ItemDeptList = data
                .map(x =>
                    `<option value="${x.value}">${x.text}</option>`
                )
                .join('');

        })
        .catch(function (error) {

            console.error("Error loading Item Department:", error);

            throw error;
        });
}

async function GetVNo(Vtype) {

    try {

        const res = await fetch(
            `/InventoryOpeningEntry/GetVNo?Vtype=${encodeURIComponent(Vtype)}`
        );

        if (!res.ok) {
            throw new Error(`HTTP ${res.status}`);
        }

        const data = await res.json();

        if (data.error) {
            throw new Error(data.error);
        }

        if (!data.v_NO) {
            throw new Error('Response missing V_NO');
        }

        $('#NumDocno').val(data.v_NO);

    } catch (error) {

        console.error("Error loading Document Number:", error);

        showToast(
            "Error loading Document Number",
            { type: "error" }
        );

        throw error;
    }
}

async function LoadDropDown() {

    try {

        await Promise.all([
            DDLVtype(),
            DDlItemName(),
            DDlUnit(),
            DDLItemmake(),
            DDLItemDapt()
        ]);

        console.log("All dropdowns loaded successfully");

    } catch (error) {

        console.error("LoadDropDown Error:", error);

        showToast(
            "Error loading dropdowns",
            { type: "error" }
        );

        throw error;
    }
}
function AddRow(data = {}) {

    let tbody = $('#tblInventoryOpeningEntry tbody');

    let newRow = `
        <tr class="no-border-input">

            <td class="hidden-col"> <input type="hidden" class="ItemCode" value="${data.itemCode ?? ''}" />
            </td>

            <td>
                <select class="erppagetable-control ddlItemname">
                    <option value="">-- Select Item --</option>
                    ${ItemNameList}
                </select>
            </td>

            <td>
                <select class="erppagetable-control ItemMake">
                    <option value="">-- Select Make --</option>
                    ${ItemmakeList}
                </select>
            </td>

            <td>
                <select class="erppagetable-control ddlUnit">
                    <option value="">-- Select Unit --</option>
                    ${unitnameList}
                </select>
            </td>

            <td>
                <input type="number"  class="erppagetable-control TxtNos"  value="${data.nos ?? ''}" oninput="limitMaxLength(this, 10)" />
            </td>

            <td>
                <input type="number" class="erppagetable-control TxtQty" value="${data.qty ?? ''}"  oninput="limitMaxLength(this, 13)" />
            </td>

            <td>
                <input type="number" class="erppagetable-control TxtRate" value="${data.rate ?? ''}"  oninput="limitMaxLength(this, 13)" />
            </td>

            <td>
                <input type="number"   class="erppagetable-control TxtAmount" value="${data.amount ?? ''}" readonly />
            </td>

            <td>
                <select class="erppagetable-control ItemDept">  <option value="">-- Select Department --</option>
                    ${ItemDeptList}
                </select>
            </td>

            <td>
                <input type="text"  class="erppagetable-control TxtRemarks" value="${data.remarks ?? ''}" maxlength="250"  />
            </td>

            <td class="text-center">
                <button type="button" class="act-btn add" onclick="AddRow()">   <i class="fa fa-plus-circle"></i> </button>
                <button type="button" class="act-btn delete"  onclick="DeleteRow(this)"> <i class="fa fa-trash"></i> </button>
            </td>
        </tr>
    `;

    tbody.append(newRow);

    // Get the newly added row
    let $row = tbody.find('tr:last');

    // Set dropdown values
    $row.find('.ddlItemname').val(data.itemCode ?? '');
    $row.find('.ItemMake').val(data.makeCode ?? '');
    $row.find('.ddlUnit').val(data.uomCode ?? '');
    $row.find('.ItemDept').val(data.toDept ?? '');

    // Optional: trigger change if dropdowns use select2/custom events
    $row.find('.ddlItemname').trigger('change');
    $row.find('.ItemMake').trigger('change');
    $row.find('.ddlUnit').trigger('change');
    $row.find('.ItemDept').trigger('change');
}
function GetInventoryOpeningData() {

    let data = [];

    $('#tblInventoryOpeningEntry tbody tr').each(function () {

        let row = $(this);

        let item = {
            ITEM_CODE : row.find('.ddlItemname').val() || '',
            ITEM_NAME: row.find('.ddlItemname option:selected').text() || '',
            MAKE_CODE: row.find('.ItemMake').val() || '',
            UOM_CODE: row.find('.ddlUnit').val() || '',
            UOM_NAME: row.find('.ddlUnit option:selected').text() || '',
            NOS: row.find('.TxtNos').val() || '',
            QTY: row.find('.TxtQty').val() || '',
            RATE: row.find('.TxtRate').val() || '',
            AMOUNT: row.find('.TxtAmount').val() || '',
            TO_DEPT: row.find('.ItemDept').val() || '',
            REMARKS: row.find('.TxtRemarks').val() || ''
        };

        data.push(item);
    });

    return data;
}

function DeleteRow(button) {

    let row = $(button).closest('tr');

    if (row.length === 0) {
        return;
    }

    row.remove();
}

async function LoadData() {

    try {

        const res = await $.ajax({
            url: '/InventoryOpeningList/GetDataByCode',
            type: 'POST',
            data: {
                DocID: rowId
            }
        });

        console.log("API Response:", res);

        if (!res.success) {
            console.error("Server Error:", res.message);
            alert(res.message || "Unable to load data.");
            return null;
        }

        const header = res.data.header;
        const details = res.data.details;



        $('#CODE').val(header.doC_ID);
        $('#ddlDocType').val(header.v_TYPE);
        $('#NumDocno').val(header.v_NO);
        $('#DtDocDate').val(formatDate(header.v_DATE));
        $('#TxtRemarks').val(header.remarks);

        $('#tblInventoryOpeningEntry tbody').empty();

        if (details && details.length > 0) {

            details.forEach(function (item) {
                AddRow({
                    itemCode: item.iteM_CODE,
                    makeCode: item.makE_CODE,
                    uomCode: item.uoM_CODE,
                    nos: item.nos,
                    qty: item.qty,
                    rate: item.rate,
                    amount: item.amount,
                    toDept: item.tO_DEPT,
                    remarks: item.remarks
                });

            });

        } else
        {

            AddRow();

        }
        return res.data;
    }
    catch (error) {

        console.error("Error loading data:", error);

        if (error.responseJSON) {
            console.error("Server Response:", error.responseJSON);
        }

        alert("Error while loading inventory opening data.");

        return null;
    }
}

function formatDate(dateStr) {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    if (isNaN(d)) return '';

    return d.getFullYear() + '-' +
        String(d.getMonth() + 1).padStart(2, '0') + '-' +
        String(d.getDate()).padStart(2, '0');
}

function TransitReport() {

    if (!rowId) {
        showToast(`Please save the data before printing the report.`, { type: "info" });
        return;
    }

    var reportName = "Inventories Opening Report";

    var v_no = $('#TxtDocNo').val();
    var v_type = $('#ddlDocType').val();
    var v_typetext = $('#ddlDocType option:selected').text();
    var formula =
        "{ISSUE1.V_TYPE} = '" + v_type + " ' "
        " and {ISSUE1.V_NO} = " + v_no +
        " and {ISSUE1.COMP_CODE} = " + globalVars.CompCode +
        " and {ISSUE1.YEAR_CODE} = " + globalVars.FYearCode +
        " and {ISSUE1.BRANCH_CODE} = " + globalVars.BranchCode + "";

    var payload = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: database,
        Parameters: {
            comp_name: globalVars.CompanyName || "",
            comp_add1: globalVars.Address1 || "",
            comp_add2: globalVars.Address2 || "",
            RPTNAME: 'RAW11'
        }
    };

    var now = new Date();
    var timestamp =
        String(now.getDate()).padStart(2, '0') +
        String(now.getMonth() + 1).padStart(2, '0') +
        String(now.getFullYear()).slice(-2) + "_" +
        String(now.getHours()).padStart(2, '0') +
        String(now.getMinutes()).padStart(2, '0') +
        String(now.getSeconds()).padStart(2, '0');

    $.ajax({
        url: 'http://localhost:24085/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(payload),
        contentType: "application/json",
        xhrFields: { responseType: 'blob' },

        success: function (response) {

            var file = new Blob([response], { type: 'application/pdf' });
            var fileName = `${reportName}_${timestamp}.pdf`;

            var link = document.createElement('a');
            link.href = URL.createObjectURL(file);
            link.download = fileName;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        },

        error: function (xhr, status, error) {
            if (xhr.status === 0) {
                console.error("Cannot connect to API. Is the backend running?");
            } else {
                console.error('Error generating report:', xhr.status, xhr.statusText, error);
                xhr.responseText && console.error('Response:', xhr.responseText);
            }
        }
    });
}

function setFormReadOnly() {

    const form = document.getElementById("InventoryOpeningform");
    if (!form) return;

    form.classList.add("erppage-readonly");
    form.classList.add("readonly-mode");

    // --------------------------------------------------
    // Inputs
    // --------------------------------------------------
    form.querySelectorAll("input").forEach(el => {

        // Hidden fields remain unchanged
        if (el.type === "hidden") return;

        if (
            el.type === "text" ||
            el.type === "date" ||
            el.type === "time" ||
            el.type === "number"
        ) {
            el.readOnly = true;
        }
        else {
            el.disabled = true;
        }
    });

    // --------------------------------------------------
    // Textareas
    // --------------------------------------------------
    form.querySelectorAll("textarea").forEach(el => {
        el.readOnly = true;
    });

    // --------------------------------------------------
    // Selects
    // --------------------------------------------------
    form.querySelectorAll("select").forEach(el => {
        el.disabled = true;
    });

    // --------------------------------------------------
    // Buttons
    // --------------------------------------------------
    form.querySelectorAll("button").forEach(btn => {

        const id = (btn.id || "").toLowerCase();
        const txt = (btn.innerText || "").trim().toLowerCase();

        // Keep Back button enabled
        if (
            id === "button_print" ||
            txt.includes("back") ||
            txt.includes("close")
        ) {
            btn.disabled = false;
            return;
        }

        // Disable all other buttons
        btn.disabled = true;
    });

    // --------------------------------------------------
    // Disable clickable icons / lookup controls
    // --------------------------------------------------
    form.querySelectorAll(`
        .input-icon,
        .fa-search,
        .fa-cog,
        .fa-database,
        .fa-ellipsis-h
    `).forEach(icon => {

        icon.style.pointerEvents = "none";
        icon.style.opacity = "0.5";
        icon.style.cursor = "not-allowed";
    });

    // --------------------------------------------------
    // Disable modal triggers
    // --------------------------------------------------
    form.querySelectorAll("[data-bs-toggle='modal']").forEach(el => {

        el.removeAttribute("data-bs-toggle");
        el.removeAttribute("data-bs-target");

        el.style.pointerEvents = "none";
        el.style.opacity = "0.5";
        el.style.cursor = "not-allowed";
    });

    // --------------------------------------------------
    // Table controls
    // --------------------------------------------------
    form.querySelectorAll(`
        table input,
        table select,
        table textarea,
        table button,
        table .fa,
        table span
    `).forEach(el => {

        if (el.tagName === "INPUT" || el.tagName === "TEXTAREA") {
            el.readOnly = true;
        }
        else if (
            el.tagName === "SELECT" ||
            el.tagName === "BUTTON"
        ) {
            el.disabled = true;
        }

        el.style.pointerEvents = "none";
        el.style.opacity = "0.5";
    });

    // --------------------------------------------------
    // Keep Print button enabled
    // --------------------------------------------------
    const printButton = document.getElementById("button_print");

    if (printButton) {
        printButton.disabled = false;
        printButton.style.pointerEvents = "auto";
        printButton.style.opacity = "1";
    }
}


function limitMaxLength(input, maxLength) {
    // Remove anything except digits
    input.value = input.value.replace(/\D/g, '');

    // Limit maximum digits
    if (input.value.length > maxLength) {
        input.value = input.value.substring(0, maxLength);
    }
}


