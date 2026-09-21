
// Drop Down

async function LoadDropDown()
{
    try {
        await Promise.all([
            DDLVtype(),
            DDlHDept(),
            DDlPlace(),
            DDlItemName(),
            DDLItemDapt(),
            DDLScrapName(),
            DDLReportItem(),
            DDLReportDept(),
            DDLReportUnit()
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

        const res = await fetch('/ScrapReceivedEntry/DDlVType');

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

async function DDlHDept() {

    try {

        const res = await fetch('/ScrapReceivedEntry/DDlHDept');

        if (!res.ok) {
            throw new Error(`HTTP ${res.status}`);
        }

        const data = await res.json();
        const ddl = $('#ddlACName');
        ddl.empty().append('<option value="">-- Select A/C Name --</option>');           

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

async function DDlPlace()
{
    try {
        const res = await fetch('/ScrapReceivedEntry/DDlPlace');
        if (!res.ok) {
            throw new Error(`HTTP ${res.status}`);
        }
        const data = await res.json();
        const ddl = $('#ddlPlace');
        ddl.empty().append('<option value="">-- Select Place --</option>');      
        
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

async function DDLReportItem() {
    try {
        const res = await fetch('/ScrapReceivedEntry/DDlItemName');
        if (!res.ok) {
            throw new Error(`HTTP ${res.status}`);
        }
        const data = await res.json();
        const ddl = $('#ddlitem');
        ddl.empty().append('<option value="">-- Select Item Name --</option>');

        data.forEach(item => {
            ddl.append(
                `<option value="${item.value}">${item.text}</option>`
            );
        });
    } catch (error) {

        console.error("Error loading Item Name ", error);

        throw error;
    }
}

async function DDLReportDept() {
    try {
        const res = await fetch('/ScrapReceivedEntry/DDLItemDapt');
        if (!res.ok) {
            throw new Error(`HTTP ${res.status}`);
        }
        const data = await res.json();
        const ddl = $('#ddlDepartment');
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

async function DDLReportUnit() {
    try {
        const res = await fetch('/ScrapReceivedEntry/DDLUnitName');
        if (!res.ok) {
            throw new Error(`HTTP ${res.status}`);
        }
        const data = await res.json();
        const ddl = $('#ddlUnit');
        ddl.empty().append('<option value="">-- Select Unit --</option>');

        data.forEach(item => {
            ddl.append(
                `<option value="${item.value}">${item.text}</option>`
            );
        });
    } catch (error) {

        console.error("Error loading Unit:", error);

        throw error;
    }
}
function DDlItemName() {

    return $.ajax({
        url: '/ScrapReceivedEntry/DDlItemName',
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
function DDLItemDapt() {

    return $.ajax({
        url: '/ScrapReceivedEntry/DDLItemDapt',
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
function DDLScrapName()
{
    return $.ajax({
        url: '/ScrapReceivedEntry/DDLScrapName',
        method: 'GET',
        dataType: 'json'
    })
        .then(function (data) {

            if (!Array.isArray(data)) 
            {
                console.error(  "ScrapNameList response is not an array:", data );
                throw new Error( "Invalid ScrapNameList response" );
            }
            ScrapNameList = data .map(x => `<option value="${x.value}">${x.text}</option>` ) .join('');
        })
        .catch(function (error)
        {
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

    let tbody = $('#tblScrapreceivedentry tbody');

    let newRow = `
        <tr class="no-border-input">
            <td> <select class="erppagetable-control ddlItemname">  <option value="">-- Select Item --</option>  ${ItemNameList} </select>  </td>
            <td> <input type="number"  class="erppagetable-control TxtQty" value="${data.qty ?? ''}" oninput="limitMaxLength(this, 8)" /> </td>
            <td> <input type="number"  class="erppagetable-control Txtweight" value="${data.weight ?? ''}" oninput="limitMaxLength(this, 9)" /> </td>
            <td> <select class="erppagetable-control ddlDept">  <option value="">-- Select Item --</option> ${ItemDeptList}  </select>  </td>
            <td> <select class="erppagetable-control ddlScrapName"> <option value="">-- Select Item --</option> ${ScrapNameList}  </select> </td>
            <td>  <input type="text" class="erppagetable-control TxtRemarks" value="${data.remarks ?? ''}" maxlength="250" /> </td>
            <td class="text-center">
                <button type="button" class="act-btn add"  onclick="AddRow()"> <i class="fa fa-plus-circle"></i>  </button>
                <button type="button" class="act-btn delete"  onclick="DeleteRow(this)"> <i class="fa fa-trash"></i>  </button>
            </td>
        </tr>
    `;

    tbody.append(newRow);

    let $row = tbody.find('tr:last');
    $row.find('.ddlItemname').val(data.itemCode ?? '');
    $row.find('.ddlDept').val(data.froM_DEPT ?? '');
    $row.find('.ddlScrapName').val(data.PARTY_CODE ?? '');
    $row.find('.ddlItemname').trigger('change');
    $row.find('.ddlDept').trigger('change');
    $row.find('.ddlScrapName').trigger('change');
}
function GetScrapReceivedEntryData()
{
    let data = [];

    $('#tblScrapreceivedentry tbody tr').each(function (index) {

        let row = $(this);

        let item = {
            SNO: index + 1,

            ITEM_CODE: row.find('.ddlItemname').val() || 0,        
            QTY: row.find('.TxtQty').val() || 0,
            WEIGHT: row.find('.Txtweight').val() || 0,
            DEPT_CODE: row.find('.ddlDept').val() || '',          
            SCRAP_CODE: row.find('.ddlScrapName').val() || 0,
            SCRAP_NAME: row.find('.ddlScrapName option:selected').text() || '',
            REMARK: row.find('.TxtRemarks').val() || ''
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
            url: '/ScrapReceivedList/GetDataByCode',
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

        $('#CODE').val(header.doC_ID);
        $('#ddlDocType').val(header.v_TYPE);
        $('#NumDocno').val(header.v_NO);
        $('#DtDocDate').val(formatDate(header.v_DATE));
        $('#ddlACName').val(header.party);
        $('#ddlPlace').val(header.placE_CODE);
        $('#TxtRemarks').val(header.remark);



        $('#tblScrapreceivedentry tbody').empty();

        if (details && details.length > 0) {
            details.forEach(function (item, index) {
                AddRow({
                    sno: index + 1,
                    itemCode: item.iteM_CODE,
                    qty: item.qty,
                    weight: item.weight,
                    froM_DEPT: item.depT_CODE,
                    PARTY_CODE: item.scraP_CODE,
                    remarks: item.remark                 
                });

            });

        }
        else
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
        const response = await fetch('/ScrapReceivedEntry/CheckValidDate', {
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

function AddPendingRow(data = {})
{
    let tbody = $('#tblpendinglist tbody');
    let newRow = `
        <tr class="no-border-input">
            <td>  <input type="checkbox" class="erppagetable-control chk_box" /> </td>
            <td> <input type="number" class="erppagetable-control txt_itemcode" value="${data.ITEM_CODE}" oninput="limitMaxLength(this, 13)" Readonly /> </td>
            <td>  <input type="text" class="erppagetable-control txt_itemname" value="${data.ItemName}" oninput="limitMaxLength(this, 13)"  Readonly />  </td>
            <td>  <input type="text" class="erppagetable-control Txt_unit" value="${ data.Unit_Name}" maxlength="250" Readonly /> </td>
            <td>  <input type="number"  class="erppagetable-control Txt_Qty" value="${data.open_qty}" oninput="limitMaxLength(this, 13)" Readonly /> </td>
            <td> <input type="text"  class="erppagetable-control Txt_Dept" value="${data.dept_name }"  maxlength="250" Readonly />  </td>
            <td>  <input type="number"  class="erppagetable-control Txt_Deptcode"  value="${data.TO_DEPT }" maxlength="250" Readonly /> </td>
            <td> <input type="text" class="erppagetable-control Txt_ScrapName"  value="${data.scrapname}" maxlength="250" Readonly /> </td>
            <td> <input type="text" class="erppagetable-control Txt_Scrapcode"  value="${data.scrapcode}" maxlength="250" Readonly /> </td>
        </tr>
    `;

    tbody.append(newRow);
}
function GetSelectedPendingRows() {
    let selectedRows = [];

    $('#tblpendinglist tbody tr').each(function () {
        let row = $(this);

        if (row.find('.chk_box').prop('checked')) {
            selectedRows.push({
                ITEM_CODE: Number(row.find('.txt_itemcode').val()) || 0,
                ItemName: row.find('.txt_itemname').val() || '',
                Unit_Name: row.find('.Txt_unit').val() || '',
                open_qty: Number(row.find('.Txt_Qty').val()) || 0,
                dept_name: row.find('.Txt_Dept').val() || '',
                TO_DEPT: Number(row.find('.Txt_Deptcode').val()) || 0,
                scrapname: row.find('.Txt_ScrapName').val() || '',
                PARTY_CODE: Number(row.find('.Txt_Scrapcode').val()) || 0,
            });
        }
    });

    return selectedRows;
}
function validateScrapReceivedDetails() {

    let isValid = true;

    $('#tblScrapreceivedentry tbody tr').each(function (index) {

        let $row = $(this);

        let itemCode = $.trim($row.find('.ddlItemname').val() || '');
        let qty = $.trim($row.find('.TxtQty').val() || '');
        let scrapName = $.trim($row.find('.ddlScrapName').val() || '');
        let department = $.trim($row.find('.ddlDept').val() || '');

        // If Item Code is selected
        if (itemCode !== '') {

            if (qty === '') {
                showToast(`Please enter Qty in row ${index + 1}`, "error");
                $row.find('.TxtQty').focus();
                isValid = false;
                return false;
            }

            if (scrapName === '') {
                showToast(`Please select Scrap Name in row ${index + 1}`, "error");
                $row.find('.ddlScrapName').focus();
                isValid = false;
                return false;
            }

            if (department === '') {
                showToast(`Please select Department in row ${index + 1}`, "error");
                $row.find('.ddlDept').focus();
                isValid = false;
                return false;
            }
        }
    });

    return isValid;
}


// Report
function DailyReportTransitReport() {

    if (!rowId) {
        showToast(`Please save the data before printing the report.`, { type: "info" });
        return;
    }

    let FromData = $('#DtFrom').val();
    let ToDate = $('#DtToDate').val();
    let ItemCdoe = $('#ddlitem').val();
    let DeptCode = $('#ddlDepartment').val();
    let UnitCode = $('#ddlUnit').val();
    var SelForMul = "From " + FromData + " To " + ToDate;
    var reportName = "scrap4";


    console.log('globalVars', globalVars);


    var formula =
        "{TEMP_INV1.COMP_CODE} = " + globalVars.CompCode +
        " and {TEMP_INV1.WSID} = '" + globalVars.wsid + "'" +
        " and {TEMP_INV1.USERID} = " + globalVars.UserId ;

    if (DeptCode)
    {
        formula += " and {TEMP_INV1.TO_DEPT} = " + DeptCode;
    }

    if (ItemCdoe)
    {
        formula += " and {TEMP_INV1.ITEM_CODE} = " + ItemCdoe;
    }

    if (UnitCode)
    {
        formula += " and {TEMP_INV1.UNIT_CODE} = " + UnitCode;
    }

    var payload = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: database,
        Parameters: {
            comp_name: globalVars.CompanyName || "",
            comp_add1: globalVars.Address1 || "",
            comp_add2: globalVars.Address2 || "",
            f1: SelForMul,
            RPTNAME: 'SCRAP ISSUE/RECD DAILY REPORT'
        }
    };


    console.log("payload", payload);



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

function PendingDeptTransitReport() {

    if (!rowId) {
        showToast(`Please save the data before printing the report.`, { type: "info" });
        return;
    }

    let FromData = $('#DtFrom').val();
    let ToDate = $('#DtToDate').val();
    let ItemCdoe = $('#ddlitem').val();
    let DeptCode = $('#ddlDepartment').val();
    let UnitCode = $('#ddlUnit').val();
    var SelForMul = "From " + FromData + " To " + ToDate;
    var reportName = "scrap3";


    console.log('globalVars', globalVars);


    var formula =
        "{TEMP_INV1.COMP_CODE} = " + globalVars.CompCode +
        " and {TEMP_INV1.WSID} = '" + globalVars.wsid + "'" +
        " and {TEMP_INV1.USERID} = " + globalVars.UserId;

    if (DeptCode) {
        formula += " and {TEMP_INV1.TO_DEPT} = " + DeptCode;
    }

    if (ItemCdoe) {
        formula += " and {TEMP_INV1.ITEM_CODE} = " + ItemCdoe;
    }

    if (UnitCode) {
        formula += " and {TEMP_INV1.UNIT_CODE} = " + UnitCode;
    }

    var payload = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: database,
        Parameters: {
            comp_name: globalVars.CompanyName || "",
            comp_add1: globalVars.Address1 || "",
            comp_add2: globalVars.Address2 || "",
            f1: SelForMul,
            RPTNAME: 'SCRAP BALANCE AT DEPARTMENT'
        }
    };


    console.log("payload", payload);



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


function ScrapIssueTransitReport() {

    if (!rowId) {
        showToast(`Please save the data before printing the report.`, { type: "info" });
        return;
    }

    let FromData = $('#DtFrom').val();
    let ToDate = $('#DtToDate').val();
    let ItemCode = $('#ddlitem').val();
    let DeptCode = $('#ddlDepartment').val();
    let UnitCode = $('#ddlUnit').val();

    // Base date formula - same as VB.NET
    let formula =
        "{scrap2.V_DATE} IN DATE(" + FromData + ") TO DATE(" + ToDate + ")";

    // Department
    if (DeptCode) {
        formula += " and {scrap2.dept_code}=" + DeptCode;
    }

    // Item
    if (ItemCode) {
        formula += " and {scrap2.ITEM_CODE}=" + ItemCode;
    }

    // Unit
    if (UnitCode) {
        formula += " and {ITEM_MAST.UNIT_CODE}=" + UnitCode;
    }

    // Fixed conditions - same as VB.NET
    formula += " and {scrap2.V_TYPE}='SCIS'";
    formula += " and {scrap2.COMP_CODE}=" + globalVars.CompCode;
    formula += " and {scrap2.BRANCH_CODE}=" + globalVars.BranchCode;

    console.log("Selection Formula:", formula);

    let SelForMul =
        "{scrap2.V_DATE} IN DATE(" + FromData + ") TO DATE(" + ToDate + ")";

    var reportName = "scrap1";

    var payload = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: database,
        Parameters: {
            comp_name: globalVars.CompanyName || "",
            comp_add1: globalVars.Address1 || "",
            comp_add2: globalVars.Address2 || "",
            f1: SelForMul,
            RPTNAME: "SCRAP BALANCE AT DEPARTMENT"
        }
    };

    console.log("payload", payload);

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
        xhrFields: {
            responseType: 'blob'
        },

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

function RecdPrintTransitReport() {

    if (!rowId) {
        showToast(`Please save the data before printing the report.`, { type: "info" });
        return;
    }

    let FromData = $('#DtFrom').val();
    let ToDate = $('#DtToDate').val();
    let ItemCode = $('#ddlitem').val();
    let DeptCode = $('#ddlDepartment').val();
    let UnitCode = $('#ddlUnit').val();

    let formula = "{scrap2.V_DATE} IN DATE(" + FromData + ") TO DATE(" + ToDate + ")";

    // Department
    if (DeptCode) {
        formula += " and {scrap2.dept_code}=" + DeptCode;
    }

    // Item
    if (ItemCode) {
        formula += " and {scrap2.ITEM_CODE}=" + ItemCode;
    }

    // Unit
    if (UnitCode) {
        formula += " and {ITEM_MAST.UNIT_CODE}=" + UnitCode;
    }

    // Fixed conditions - same as VB.NET
    formula += " and {scrap2.V_TYPE}='SCRD'";
    formula += " and {scrap2.COMP_CODE}=" + globalVars.CompCode;
    formula += " and {scrap2.BRANCH_CODE}=" + globalVars.BranchCode;

    console.log("Selection Formula:", formula);

    let SelForMul =
        "{scrap2.V_DATE} IN DATE(" + FromData + ") TO DATE(" + ToDate + ")";

    var reportName = "scrap1";

    var payload = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: database,
        Parameters: {
            comp_name: globalVars.CompanyName || "",
            comp_add1: globalVars.Address1 || "",
            comp_add2: globalVars.Address2 || "",
            f1: SelForMul,
            RPTNAME: "Scrap Received Report"
        }
    };

    console.log("payload", payload);

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
        xhrFields: {
            responseType: 'blob'
        },

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


function ScrapStocTransitReport() {

    if (!rowId) {
        showToast(`Please save the data before printing the report.`, { type: "info" });
        return;
    }

    let FromData = $('#DtFrom').val();
    let ToDate = $('#DtToDate').val();
    let ItemCode = $('#ddlitem').val();
    let DeptCode = $('#ddlDepartment').val();
    let UnitCode = $('#ddlUnit').val();

    let formula = "{scrap2.V_DATE} IN DATE(" + FromData + ") TO DATE(" + ToDate + ")";

    // Department
    if (DeptCode) {
        formula += " and {scrap2.dept_code}=" + DeptCode;
    }

    // Item
    if (ItemCode) {
        formula += " and {scrap2.ITEM_CODE}=" + ItemCode;
    }

    // Unit
    if (UnitCode) {
        formula += " and {ITEM_MAST.UNIT_CODE}=" + UnitCode;
    }

    // Fixed conditions - same as VB.NET
    formula += " and {scrap2.V_TYPE}='SCRD'";
    formula += " and {scrap2.COMP_CODE}=" + globalVars.CompCode;
    formula += " and {scrap2.BRANCH_CODE}=" + globalVars.BranchCode;

    console.log("Selection Formula:", formula);

    let SelForMul =
        "{scrap2.V_DATE} IN DATE(" + FromData + ") TO DATE(" + ToDate + ")";

    var reportName = "scrap1";

    var payload = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: database,
        Parameters: {
            comp_name: globalVars.CompanyName || "",
            comp_add1: globalVars.Address1 || "",
            comp_add2: globalVars.Address2 || "",
            f1: SelForMul,
            RPTNAME: "Scrap Received Report"
        }
    };

    console.log("payload", payload);

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
        xhrFields: {
            responseType: 'blob'
        },

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





