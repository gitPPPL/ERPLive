async function LoadDropDown()
{
    try {
        await Promise.all([
            DDLVtype(),
            DDlStatus(),
            DDlItemName(),
            DDlUnit(),
            DDLItemmake(),
            DDLItemDapt(),
            DDlPlace(),
            DDlHOD(),
            DDlDeptName()  
        ]);
    }
    catch (error)
    {
        showToast("Error loading dropdowns",{ type: "error" });
        throw error;
    }
}

async function DDLVtype() {

    try {

        const res = await fetch('/InventoryTransferRequest/DDlVType');

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

async function DDlStatus() {

    try {

        const res = await fetch('/InventoryTransferRequest/DDlStatus');

        if (!res.ok)
        {
            throw new Error(`HTTP ${res.status}`);
        }

        const data = await res.json();
        const ddl = $('#ddlStatus');
        ddl.empty();

        data.forEach(item => {

            ddl.append(
                `<option value="${item.value}">${item.text}</option>`
            );

        });

    } catch (error) {

        console.error("Error loading status:", error);

        throw error;
    }
}

async function DDlPlace() {

    try {

        const res = await fetch('/InventoryTransferRequest/DDlPlace');

        if (!res.ok) {
            throw new Error(`HTTP ${res.status}`);
        }

        const data = await res.json();
        const ddl = $('#ddlPlace');
        ddl.empty().append('<option value="">-- Select Place--</option>');

        data.forEach(item => {

            ddl.append(
                `<option value="${item.value}">${item.text}</option>`
            );

        });

    } catch (error) {

        console.error("Error loading Place:", error);

        throw error;
    }
}

async function DDlHOD() {

    try {

        const res = await fetch('/InventoryTransferRequest/DDlHOD');

        if (!res.ok) {
            throw new Error(`HTTP ${res.status}`);
        }

        const data = await res.json();
        const ddl = $('#ddlHodName');
        ddl.empty().append('<option value="">-- Select HOD Name --</option>');

        data.forEach(item => {

            ddl.append(
                `<option value="${item.value}">${item.text}</option>`
            );

        });

    } catch (error) {

        console.error("Error loading Hod:", error);

        throw error;
    }
}

async function DDlDeptName() {
    try {
        const res = await fetch('/InventoryTransferRequest/DDlDeptName');

        if (!res.ok) {
            throw new Error(`HTTP ${res.status}`);
        }

        const data = await res.json();

        const ddl = $('#ddlHDepartment, #ddlDepartment');

        ddl.empty().append('<option value="">-- Select Department --</option>');

        data.forEach(item => {
            ddl.append(
                `<option value="${item.value}">${item.text}</option>`
            );
        });

    } catch (error) {
        console.error("Error loading Department:", error);
        throw error;
    }
}
function DDlItemName() {

    return $.ajax({
        url: '/InventoryTransferRequest/DDlItemName',
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
        url: '/InventoryTransferRequest/DDlUnit',
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
        url: '/InventoryTransferRequest/DDLItemmake',
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
        url: '/InventoryTransferRequest/DDLItemDapt',
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

async function GetVNo(Vtype)
{
    try
    {
        const res = await fetch(`/InventoryTransferRequest/GetVNo?Vtype=${encodeURIComponent(Vtype)}`);
        if (!res.ok)
        {
            throw new Error(`HTTP ${res.status}`);
        }
        const data = await res.json();
        if (data.error)
        {
            throw new Error(data.error);
        }
        if (!data.v_NO)
        {
            throw new Error('Response missing V_NO');
        }
        $('#NumDocno').val(data.v_NO);
    }
    catch (error)
    {
        console.error("Error loading Document Number:", error);
        showToast( "Error loading Document Number", { type: "error" });
        throw error;
    }
}
function AddRow(data = {}) {

    let tbody = $('#tblInventoryTransferRequest tbody');

    let newRow = `
        <tr class="no-border-input">
            <td> <input  class="erppagetable-control ItemCode" value="${data.itemCode ?? ''}" /> </td>
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

            <td> <input type="number" class="erppagetable-control TxtNos" value="${data.nos ?? ''}" oninput="limitMaxLength(this, 10)" /> </td>
            <td> <input type="number"  class="erppagetable-control TxtQty" value="${data.qty ?? ''}" oninput="limitMaxLength(this, 13)" /> </td>

            <td>
                <select class="erppagetable-control FromPlace">
                    <option value="">-- Select From Place --</option>
                    ${ItemDeptList}     
                </select>
            </td>

            <td>
                <select class="erppagetable-control ToPlace">
                    <option value="">-- Select To Place --</option>
                    ${ItemDeptList}
                </select>
            </td>

            <td>
                <select class="erppagetable-control maC_CODE">
                    <option value="">-- Select Machine --</option>
                    ${ItemDeptList}
                </select>
            </td>

            <td> <input type="text" class="erppagetable-control TxtRemarks" value="${data.remarks ?? ''}" maxlength="250" />  </td>
            <td class="hidden-col"> <input type="number" class="erppagetable-control TxtLDRate" value="${data.lanD_RATE ?? ''}" maxlength="250" />  </td>
            <td class="hidden-col"> <input type="number" class="erppagetable-control TxtLDAMT" value="${data.lanD_AMT ?? ''}" maxlength="250" />  </td>

            <td class="text-center">
                <button type="button" class="act-btn add"  onclick="AddRow()"> <i class="fa fa-plus-circle"></i>  </button>
                <button type="button" class="act-btn delete"  onclick="DeleteRow(this)"> <i class="fa fa-trash"></i>  </button>
            </td>

        </tr>
    `;

    tbody.append(newRow);

    let $row = tbody.find('tr:last');

    $row.find('.ddlItemname').val(data.itemCode ?? '');
    $row.find('.ItemMake').val(data.makeCode ?? '');
    $row.find('.ddlUnit').val(data.uomCode ?? '');
    $row.find('.FromPlace').val(data.froM_DEPT ?? '');
    $row.find('.ToPlace').val(data.tO_DEPT ?? '');
    $row.find('.maC_CODE').val(data.maC_CODE ?? '');

    $row.find('.ddlItemname').trigger('change');
    $row.find('.ItemMake').trigger('change');
    $row.find('.ddlUnit').trigger('change');
    $row.find('.FromPlace').trigger('change');
    $row.find('.ToPlace').trigger('change');
    $row.find('.maC_CODE').trigger('change');
}
function GetInventoryOpeningData()
{
    let data = [];
    $('#tblInventoryTransferRequest tbody tr').each(function (index) {

        let row = $(this);
        let item = {
            SNO: index + 1,
            ITEM_CODE: row.find('.ddlItemname').val() || 0,
            ITEM_NAME: row.find('.ddlItemname option:selected').text() || '',
            MAKE_CODE: row.find('.ItemMake').val() || 0,
            UOM_CODE: row.find('.ddlUnit').val() || 0,
            UOM_NAME: row.find('.ddlUnit option:selected').text() || '',
            FROM_DEPT: row.find('.FromPlace').val() || '',
            TO_DEPT: row.find('.ToPlace').val() || '',
            MAC_CODE: row.find('.maC_CODE').val() || 0,
            NOS: row.find('.TxtNos').val() || 0,
            QTY: row.find('.TxtQty').val() || 0,
            REMARKS: row.find('.TxtRemarks').val() || '',
            LAND_RATE: row.find('.TxtLDRate').val() || 0,
            LAND_AMT: row.find('.TxtLDAMT').val() || 0
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
            url: '/InventoryTransferRequestList/GetDataByCode',
            type: 'POST',
            data: {
                DocID: rowId
            }
        });

        if (!res.success) {
            console.error("Server Error:", res.message);
            alert(res.message || "Unable to load data.");
            return null;
        }

        const header = res.data.header;
        const details = res.data.details;

        console.log("header:", header);
        console.log("details:", details);

        // =========================
        // HEADER
        // =========================

        $('#CODE').val(header.doC_ID);
        $('#ddlDocType').val(header.v_TYPE);
        $('#NumDocno').val(header.v_NO);
        $('#DtDocDate').val(formatDate(header.v_DATE));
        $('#ddlStatus').val(header.status);
        $('#ddlHDepartment').val(header.depT_CODE);
        $('#ddlShift').val(header.shift);
        $('#ddlPlace').val(header.placE_CODE);
        $('#ddlHodName').val(header.emP_CODE);
        $('#TxtRemarks').val(header.remarks);

        // =========================
        // CLEAR TABLE
        // =========================

        $('#tblInventoryTransferRequest tbody').empty();

        if (details && details.length > 0) {
            details.forEach(function (item, index) {
                AddRow({
                    sno: index + 1,
                    itemCode: item.iteM_CODE,
                    makeCode: item.makE_CODE,
                    uomCode: item.uoM_CODE,
                    nos: item.nos,
                    qty: item.qty,
                    froM_DEPT: item.froM_DEPT,
                    tO_DEPT: item.tO_DEPT,
                    maC_CODE: item.macH_CODE,
                    remarks: item.remarks,
                    lanD_RATE: item.lanD_RATE,
                    lanD_AMT: item.lanD_AMT
                });

            });

        }
        else {

            AddRow();

        }

        return res.data;

    }
    catch (error) {

        console.error("Error loading data:", error);

        if (error.responseJSON) {
            console.error("Server Response:", error.responseJSON);
        }

        alert("Error while loading inventory transfer request data.");

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

    var reportName = "rptStoreRequest";

    var fromdate = $('#DtFromDate').val();
    var todate = $('#DtToDate').val();


    var SelForMul = "From " + fromdate + " To " + todate;

    var formula = "";

    var payload = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: database,

        Parameters: {
            comp_name: globalVars.CompanyName || "",
            comp_add1: globalVars.Address1 || "",
            comp_add2: globalVars.Address2 || "",

            // VB.NET FormulaFields("f1")
            f1: SelForMul,

            RPTNAME: "Store Request Report"
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

            var file = new Blob([response], {
                type: 'application/pdf'
            });

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

            if (xhr.status === 0) {
                console.error("Cannot connect to API. Is the backend running?");
            } else {
                console.error(
                    'Error generating report:',
                    xhr.status,
                    xhr.statusText,
                    error
                );

                if (xhr.responseText) {
                    console.error('Response:', xhr.responseText);
                }
            }
        }
    });
}
function setFormReadOnly() {

    const form = document.getElementById("InventoryTransferRequestform");
    if (!form) return;

    form.classList.add("erppage-readonly");
    form.classList.add("readonly-mode");

    form.querySelectorAll("input").forEach(el => {

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

    form.querySelectorAll("textarea").forEach(el => {
        el.readOnly = true;
    });

    form.querySelectorAll("select").forEach(el => {
        el.disabled = true;
    });

    form.querySelectorAll("button").forEach(btn => {

        const id = (btn.id || "").toLowerCase();
        const txt = (btn.innerText || "").trim().toLowerCase();

        if (
            id === "btn_print" ||
            txt.includes("back") ||
            txt.includes("close")
        ) {
            btn.disabled = false;
            btn.style.pointerEvents = "auto";
            btn.style.opacity = "1";
            return;
        }

        btn.disabled = true;
    });

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

    form.querySelectorAll("[data-bs-toggle='modal']").forEach(el => {

        el.removeAttribute("data-bs-toggle");
        el.removeAttribute("data-bs-target");

        el.style.pointerEvents = "none";
        el.style.opacity = "0.5";
        el.style.cursor = "not-allowed";
    });

    form.querySelectorAll(`
        table input,
        table select,
        table textarea,
        table button,
        table .fa,
        table span
    `).forEach(el => {

        if (
            el.tagName === "INPUT" ||
            el.tagName === "TEXTAREA"
        ) {
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

    const printButton = document.getElementById("btn_print");

    if (printButton) {
        printButton.disabled = false;
        printButton.style.pointerEvents = "auto";
        printButton.style.opacity = "1";
    }
}
function limitMaxLength(input, maxLength) {
    input.value = input.value.replace(/\D/g, '');  
    if (input.value.length > maxLength) {
        input.value = input.value.substring(0, maxLength);
    }
}

async function checkValidDate() {
    const data = {
        vdate: $("#DtDocDate").val(),
        vtype: $("#ddlDocType").val(),
        vno: $("#NumDocno").val()
    };
    try {
        const response = await fetch('/InventoryTransferRequest/CheckValidDate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });

        const result = await response.json();

        if (result.status === false) {
            showToast("result.message", { type: "warning" });
            return false;
        }

        return true;

    } catch (error) {
        showToast("result.message", { type: "warning" });
        return false;
    }
} 
function validateInventoryDetails() {

    let isValid = true;
    let firstInvalidRow = null;
    let hasSelectedItem = false;

    $('#tblInventoryTransferRequest tbody tr').each(function (index) {

        let row = $(this);

        let itemCode = $.trim(row.find('.ddlItemname').val() || '');

        // Check if at least one item is selected
        if (itemCode !== '') {

            hasSelectedItem = true;

            let qty = $.trim(row.find('.TxtQty').val() || '');
            let fromDept = $.trim(row.find('.FromPlace').val() || '');
            let toDept = $.trim(row.find('.ToPlace').val() || '');
            let machineName = $.trim(row.find('.maC_CODE').val() || '');

            if (qty === '')
            {
                showToast(`Please enter Qty in row ${index + 1}`, "Error");
                firstInvalidRow = row;
                isValid = false;
                return false;
            }

            if (fromDept === '')
            {
                showToast(`Please select From Department in row ${index + 1}`, "Error");
                firstInvalidRow = row;
                isValid = false;
                return false;
            }

            if (toDept === '')
            {
                showToast(`Please select To Department in row ${index + 1}`, "Error");
                firstInvalidRow = row;
                isValid = false;
                return false;
            }

            if (machineName === '')
            {
                showToast(`Please select Machine Name in row ${index + 1}`, "Error");
                firstInvalidRow = row;
                isValid = false;
                return false;
            }
        }
    });

    if (!hasSelectedItem)
    {
        showToast("Please select at least one Item.", "Error");
        return false;
    }

    if (firstInvalidRow)
    {
        $('html, body').animate({
            scrollTop: firstInvalidRow.offset().top - 150
        }, 300);
    }

    return isValid;
}



function TransitReportHeader() {

    if (!rowId)
    {
        showToast(`Please save the data before printing the report.`, { type: "info" });
        return;
    }

    var reportName = "RAW11";

    var v_no = $('#NumDocno').val();
    var v_type = $('#ddlDocType').val();
    var v_typetext = $('#ddlDocType option:selected').text();
    var formula =
        " {ISSUE1.V_TYPE} = '" + v_type + "'"
        " and {ISSUE1.V_NO} = " + v_no  +
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
            RPTNAME: v_typetext
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


