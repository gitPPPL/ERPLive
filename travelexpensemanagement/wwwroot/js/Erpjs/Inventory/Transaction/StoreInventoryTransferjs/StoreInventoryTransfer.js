let itemList = [];
let placeList = [];

const urlParams = new URLSearchParams(window.location.search);
const id = urlParams.get('docId');
const vtype = urlParams.get('vtype');
let isReadOnly = urlParams.get('readOnly') === 'true';

$(document).ready(async function () {

    await GetDocTypeDDl();

    await BindAllHeaderDropdown();
    await LoadItemList();
    await LoadPlaceList();

    if (id) {
        await LoadEditData();
    }
    else {
        addNewRow();
    }

    await wireEvent();

    if (isReadOnly) {
        setFormReadOnly();
    }

    //=================================
    // Save and Update Data
    //=================================
    $('#btn_save').on('click', async function (e) {

        e.preventDefault();
        try {

            const isValidDate = await checkValidDate();
            if (!isValidDate) return;

            if (!(await validateData())) {
                return;
            }

            const vType = $('#ddlDocType').val();
            const vNo = $('#NumDocno').val();
            const vDate = $('#DtDocDate').val();
            const shift = $('#ddlShift').val() || "";
            const slipNo = $('#NumSlipNo').val();
            const deptCode = $('#ddlRefDepartment').val();
            const status = $('#ddlStatus').val() || null;
            const remarks = $('#txtreason').val();

            const tbody = $('#tblItemdetail tbody');
            const rows = tbody.find('tr');

            if (rows.length === 0) {
                showToast("At least one item is required.", { type: "warning" });
                return;
            }

            const footer = [];

            rows.each(function (index) {

                const row = $(this);

                const itemCode = row.find('.item-code').val();
                const itemName = row.find('.item-name option:selected').text();

                const makeCode = row.find('.make').val();
                const uomCode = row.find('.unit').val();
                const uomName = row.find('.unit option:selected').text();

                const nos = row.find('.nos').val();
                const quantity = row.find('.quantity').val();
                const binLocation = row.find('.bagNo').val();

                const fromDept = row.find('.fromPlace').val();
                const toDept = row.find('.toPlace').val();

                const landRate = 0;
                const landAmt = 0;
                const kantaType = row.find('.wbType').val();
                const kantaNo = row.find('.wbNo').val();

                const wbDate = row.find('.wbDate').val();
                const reqType = row.find('.pReqType').val();
                const reqNo = row.find('.pReqNo').val();
                
                const pordType = row.find('.pordType').val();
                const pordNo = row.find('.prodNo').val();

                const freMarks = row.find('.remark').val();

                footer.push({

                    ITEM_CODE: itemCode ? parseInt(itemCode) : null,
                    ITEM_NAME: itemName || null,
                    MAKE_CODE: makeCode ? parseInt(makeCode) : null,
                    UOM_CODE: uomCode ? parseInt(uomCode) : null,
                    UOM_NAME: uomCode ? uomName : null,
                    NOS: nos ? parseInt(nos) : null,
                    QTY: quantity ? parseFloat(quantity) : null,
                    BIN_LOCATION: binLocation || null,
                    FROM_DEPT: fromDept ? parseInt(fromDept) : null,
                    TO_DEPT: toDept ? parseInt(toDept) : null,
                    LAND_RATE: landRate ? parseFloat(landRate) : null,
                    LAND_AMT: landAmt ? parseFloat(landAmt) : null,
                    KANTA_TYPE: kantaType || null,
                    KANTA_NO: kantaNo ? parseInt(kantaNo) : null,
                    REQ_TYPE: reqType || null,
                    REQ_NO: reqNo ? parseInt(reqNo) : null,
                    PORD_TYPE: pordType || null,
                    PORD_NO: pordNo ? parseInt(pordNo) : null,
                    WB_DATETIME: wbDate || null,
                    FREMARKS: freMarks || null
                });
            });

            const model = {
                V_TYPE: vType,
                V_NO: vNo ? parseInt(vNo) : null,
                V_DATE: vDate,
                SHIFT: shift,
                SLIP_NO: slipNo || null,
                DEPT_CODE: deptCode ? parseInt(deptCode) : null,
                STATUS: status ? parseInt(status) : null,
                REMARKS: remarks || null,
                StoreInventoryTransferFooter: footer
            };

            console.log("Save Model:", model);

            const res = await fetch("/StoreInventoryTransfer/SaveAndUpdateData",
                {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify(model)
                }
            );

            const data = await res.json();

            if (!res.ok || !data.success) {
                showToast(data.message || "Data saved failed.", { type: "error" });
                return;
            }

            showToast(data.message || "Data saved successfully.", { type: "success" });

            isReadOnly = true;
            setFormReadOnly();

        }
        catch (error) {
            console.error("Save StoreInventory Error:", error);
            showToast("Save failed: " + error.message, { type: "error" });
        }

    });

});

function SetCurrentDate() {
    const today = new Date();

    const year = today.getFullYear();
    const month = String(today.getMonth() + 1).padStart(2, '0');
    const day = String(today.getDate()).padStart(2, '0');

    $('#DtDocDate').val(`${year}-${month}-${day}`);
}

async function LoadEditData() {

    if (!id) {
        return;
    }

    try {

        const res = await fetch(
            `/StoreInventoryTransfer/LoadEditData?docId=${encodeURIComponent(id)}`
        );

        if (!res.ok) {
            throw new Error("Failed to load edit data.");
        }

        const data = await res.json();

        if (!data.success) {
            showToast(
                data.message || "Failed to load edit data.",
                { type: "error" }
            );
            return;
        }

        console.log("Edit Data:", data);
        const header = data.header;

        if (header) {
            $('#ddlDocType').val(header.V_TYPE).trigger('change');
            
            $('#NumDocno').val(header.V_NO);
            if (header.V_DATE) {
                $('#DtDocDate').val(
                    formatDateForInput(header.V_DATE)
                );
            }
            $('#ddlShift').val(header.SHIFT).trigger('change');
            $('#NumSlipNo').val(header.SLIP_NO);
            $('#ddlRefDepartment').val(header.DEPT_CODE).trigger('change');
            $('#txtreason').val(header.REMARKS ?? '');
            $('#ddlStatus').val(header.STATUS ?? '');
        }

        // =====================================
        // Footer
        // =====================================

        const tbody = $('#tblItemdetail tbody');

        tbody.empty();

        if (data.footer && data.footer.length > 0) {

            for (const item of data.footer) {

                addNewRow();

                const row = tbody.find('tr:last');
                row.find('.item-code').val(item.ITEM_CODE ?? '');

                const itemDropdown = row.find('.item-name');

                if (item.ITEM_CODE) {

                    const option = new Option(
                        item.ITEM_NAME || '',
                        item.ITEM_CODE,
                        true,
                        true
                    );

                    itemDropdown.append(option).trigger('change');
                }

                if (item.ITEM_CODE) {
                    await LoadMakeList(row, item.ITEM_CODE);
                }

                row.find('.make').val(item.MAKE_CODE ?? '').trigger('change');

                if (item.ITEM_CODE) {
                    await LoadUnitList(row, item.ITEM_CODE);
                }
                row.find('.unit').val(item.UOM_CODE ?? '').trigger('change');
                row.find('.fromPlace').val(item.FROM_DEPT ?? '').trigger('change');
                row.find('.toPlace').val(item.TO_DEPT ?? '').trigger('change');
                row.find('.nos').val(item.NOS ?? '');
                row.find('.quantity').val(item.QTY ?? '');
                row.find('.ld-rate').val(item.LAND_RATE ?? '');
                row.find('.ld-amt').val(item.LAND_AMT ?? '');
                row.find('.bagNo').val(item.BIN_LOCATION ?? '');
                row.find('.wbType').val(item.KANTA_TYPE ?? '').trigger('change');
                row.find('.wbNo').val(item.KANTA_NO ?? '');
                row.find('.wbDate').val(
                    item.WB_DATETIME ? formatDateForInput(item.WB_DATETIME) : ''
                );
                row.find('.pReqType').val(item.REQ_TYPE ?? '').trigger('change');
                row.find('.pReqNo').val(item.REQ_NO ?? '');
                row.find('.pordType').val(item.PORD_TYPE ?? '').trigger('change');
                row.find('.prodNo').val(item.PORD_NO ?? '');
                row.find('.remark').val(item.FREMARKS ?? '');
            }
        } else {
            addNewRow();
        }
    }
    catch (error) {
        console.error("Load Edit Data Error:", error);
        showToast("Edit data load failed: " + error.message, { type: "error" });
    }
}
    
async function wireEvent() {

    SetCurrentDate();
    //--------------------------------------
    // Fill Data on change of Item Code
    //--------------------------------------
    $(document).on("change", ".item-code", async function () {

        const row = $(this).closest("tr");
        const itemCode = $(this).val().trim();

        if (!itemCode) {

            row.find(".item-name").val("").trigger("change");
            row.find(".unit").val("");

            const make = row.find(".make");
            make.empty();
            make.append(`<option value="">Select Make</option>`);

            return;
        }

        const item = itemList.find(x =>
            String(x.icode) === String(itemCode)
        );

        if (!item) {

            showToast("Item Code not found.", {type: "warning"});

            row.find(".item-name").val("").trigger("change");
            row.find(".unit").val("");

            const make = row.find(".make");
            make.empty();
            make.append(`<option value="">Select Make</option>`);

            return;
        }

        row.find(".item-name").val(item.icode).trigger("change.select2");
        
        await LoadMakeList(row, item.icode);
        await LoadUnitList(row, item.icode);
    });

    //--------------------------------------
    // Fill Data on change of item
    //--------------------------------------
    $(document).on("change", ".item-name", async function () {

        const row = $(this).closest("tr");
        const itemCode = $(this).val();

        if (!itemCode) {

            row.find(".item-code").val("");
            row.find(".unit").val("");

            const make = row.find(".make");
            make.empty();
            make.append(`<option value="">Select Make</option>`);

            return;
        }

        const item = itemList.find(x =>
            String(x.icode) === String(itemCode)
        );

        if (!item) {
            return;
        }

        row.find(".item-code").val(item.icode);
        
        await LoadMakeList(row, item.icode);
        await LoadUnitList(row, item.icode);
    });

    //--------------------------------------------
    // Add Footer Row
    //--------------------------------------------
    $(document).on("click", ".act-btn.add", function () {
        addNewRow();
    });

    //--------------------------------------------
    // Delete Footer Row
    //--------------------------------------------
    $(document).on("click", ".act-btn.delete", function () {
        const tbody = $("#tblItemdetail tbody");
        const rowCount = tbody.find("tr").length;
        if (rowCount <= 1) {
            showToast("At least one row is required.", { type: "warning" });
            return;
        }
        $(this).closest("tr").remove();
    });

    //--------------------------------------------
    // Link Data
    //--------------------------------------------
    $(document).on("click", "#btn_linkWB", async function (e) {

        e.preventDefault();

        await LoadLinkData();

    });
}

async function GetDocTypeDDl() {

    try {

        const res = await fetch("/StoreInventoryTransfer/DocType", {
            method: "GET",
        });

        const data = await res.json();

        const ddl = $("#ddlDocType");
        ddl.empty();

        $.each(data, function (i, item) {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });

        if (data.length > 0) {
            ddl.val(data[0].value);
            await GetVNo();
        }

        //if (id) {

        //    if (vtype) {
        //        ddl.val(vtype);
        //    }
        //    $('#ddlDocType').prop('disabled', true);
        //}
        //else {
        //    await GetVNo();
        //}

    } catch (error) {
        showToast("Doc Type Load Failed :" + error, { type: "error" });
        console.error("Error fetching DocType dropdown data:", error);
    }

}

async function GetVNo() {

    try {
        const vType = $('#ddlDocType').val();
        if (!vType) {
            console.warn("vType is empty");
            return;
        }
        const res = await fetch(`/StoreInventoryTransfer/GenerateVNo?vType=${encodeURIComponent(vType)}`);

        if (!res.ok) {
            throw new Error("Network response was not ok");
        }
        const data = await res.json();
        if (data.v_NO) {
            $('#NumDocno').val(data.v_NO);
            const docId = vType + data.v_NO;
            console.log("DocId", docId);
        } else {
            console.warn("V_NO not found in response");
        }

    } catch (e) {
        showToast("Error in GetVNo :" + e, { type: "error" });
        console.error("Error in GetVNo:", e);
    }
}

async function BindAllHeaderDropdown() {

    await Promise.all([

        bindDropdown('StoreInventoryTransfer', 'Status', '#ddlStatus', 'Select Status', null, null, false, null, false),
        bindDropdown('StoreInventoryTransfer', 'Department', '#ddlRefDepartment', 'Select Department', null, null, false, null, true),

    ]);
}

async function checkValidDate() {

    const data = {
        vdate: $("#DtDocDate").val(),
        vtype: $("#ddlDocType").val(),
        vno: $("#NumDocno").val()
    };

    try {

        const response = await fetch('/StoreInventoryTransfer/CheckValidDate', {
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

function formatDateForInput(dateValue) {

    if (!dateValue)
        return '';

    const date = new Date(dateValue);

    if (isNaN(date.getTime()))
        return '';

    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');

    return `${year}-${month}-${day}`;
}

function setFormReadOnly() {

    const page = $('.erppage-fieldset');
    page.addClass('erppage-readonly');

    page.find('input, textarea').prop('readonly', true);
    page.find('input[type="checkbox"], input[type="radio"]').prop('disabled', true);
    page.find('select').prop('disabled', true);
    page.find('select.select2-hidden-accessible').each(function () {

        const select = $(this);

        select.prop('disabled', true);

        select.next('.select2-container')
            .addClass('select2-readonly');
    });

    const table = $('#tblItemdetail');

    table.find('input').prop('readonly', true);
    table.find('textarea').prop('readonly', true);
    table.find('select').prop('disabled', true);
    table.find('.add, .delete').prop('disabled', true);

    $('#btn_print').prop('disabled', false).show();
    $('#btn_save').prop('disabled', true).hide();
}

async function LoadLinkData() {

    try {

        const vDate = $('#DtDocDate').val();

        if (!vDate) {
            showToast("Please select document date.", {type: "warning"});
            return;
        }

        const res = await fetch(
            `/StoreInventoryTransfer/GetLinkData?vDate=${encodeURIComponent(vDate)}`
        );

        if (!res.ok) {
            throw new Error("Failed to load link data.");
        }

        const result = await res.json();

        console.log("Link Data:", result);

        if (!result.success) {
            showToast(result.message || "Link data loading failed.",{ type: "error" });
            return;
        }

        const data = result.data || [];

        if (data.length === 0) {
            showToast("No data found for selected date.",{ type: "warning" });
            return;
        }

        const tbody = $('#tblItemdetail tbody');

        tbody.empty();

        for (const item of data) {

            addNewRow();

            const row = tbody.find('tr:last');

            row.find('.item-code').val(item.itemCode);

            const itemDropdown = row.find('.item-name');

            itemDropdown.val(item.itemCode).trigger('change');

            if (item.itemCode) {
                await LoadMakeList(row,item.itemCode);
            }

            if (item.itemCode) {
                await LoadUnitList(row, item.itemCode);
            }

            row.find('.unit').val(item.uomCode).trigger('change');
            row.find('.quantity').val(item.qty);
            row.find('.fromPlace').val(item.fromDept).trigger('change');
            row.find('.toPlace').val(item.toDept).trigger('change');
        }

        showToast( `${data.length} item(s) loaded successfully.`,{ type: "success" });

    }
    catch (error) {
        console.error("Load Link Data Error:", error );
        showToast( "Link data load failed: " + error.message, { type: "error" });
    }
}

//=============================
// Footer Table
//=============================

function addNewRow() {

    const tbody = $("#tblItemdetail tbody");

    const row = `
        <tr>

            <td>
                <input type="text" class="erppagetable-control item-code">
            </td>

            <td>
                <select class="erppagetable-control item-name ">
                    <option value="">Select Item</option>
                </select>
            </td>

            <td>
                <select class="erppagetable-control make">
                    <option value="">Select Make</option>
                </select>
            </td>

            <td>
                <select class="erppagetable-control unit">
                    <option value="">Select unit</option>
                </select>
            </td>

            <td>
                <input type="number" class="erppagetable-control nos">
            </td>

            <td>
                <input type="number" class="erppagetable-control quantity">
            </td>

            <td>
                <input type="text" class="erppagetable-control bagNo">
            </td>

            <td>
                <select class="erppagetable-control fromPlace">
                    <option value="">Select From Place</option>
                </select>
            </td>

            <td>
                <select class="erppagetable-control toPlace">
                    <option value="">Select To Place</option>
                </select>
            </td>

            <td>
                 <input type="text" class="erppagetable-control remark">
            </td>

            <td>
                 <input type="Date" class="erppagetable-control wbDate">
            </td>

            <td>
                <input type="text" class="erppagetable-control wbType">
            </td>

            <td>
                <input type="number" class="erppagetable-control wbNo">
            </td>

            <td>
                <input type="text" class="erppagetable-control pordType">
            </td>

            <td>
                <input type="number" class="erppagetable-control prodNo">
            </td>

            <td>
                <input type="text" class="erppagetable-control pReqType">
            </td>

            <td>
                <input type="number" class="erppagetable-control pReqNo">
            </td>

            <td class="action-col">
                <div class="action-wrap">
                     <button class="act-btn add" title="Add Row"><i class="fa fa-plus"></i></button>
                     <button class="act-btn delete"><i class="fa fa-trash"></i></button>
                </div>
            </td>

        </tr>
    `;

    tbody.append(row);
    const newRow = tbody.find("tr:last");

    BindItemDropdown(newRow);
    BindPlaceDropdown(newRow);
}

async function LoadItemList() {

    try {

        const res = await fetch("/StoreInventoryTransfer/GetItemList");

        if (!res.ok) {
            throw new Error("Failed to load item list.");
        }

        const result = await res.json();

        if (!result.success) {
            throw new Error(result.message || "Item list loading failed.");
        }

        itemList = result.data || [];

    } catch (error) {
        showToast("Item List Load Failed: " + error.message, {type: "error"});
        console.error("Error loading Item List:", error);
    }
}

function BindItemDropdown(row) {

    const ddl = row.find(".item-name");

    ddl.empty();

    ddl.append(`<option value="">Select Item</option>`);

    $.each(itemList, function (i, item) {

        ddl.append(`
            <option value="${item.icode}">
                ${item.itemName}
            </option>
        `);

    });
    initializeSelect2(ddl, "Select Item");
}

async function LoadPlaceList() {

    try {

        const res = await fetch("/StoreInventoryTransfer/GetDepartment");

        if (!res.ok) {
            throw new Error("Failed to load place list.");
        }

        const result = await res.json();
        placeList = result || [];

        console.log("Place List:", placeList);

    } catch (error) {

        showToast("Place List Load Failed: " + error.message, {
            type: "error"
        });

        console.error("Error loading Place List:", error);
    }
}

function BindPlaceDropdown(row) {

    const fromPlace = row.find(".fromPlace");
    const toPlace = row.find(".toPlace");

    fromPlace.empty();
    toPlace.empty();

    fromPlace.append(`<option value="">Select From Place</option>`);
    toPlace.append(`<option value="">Select To Place</option>`);

    $.each(placeList, function (i, place) {

        fromPlace.append(`
            <option value="${place.value}">
                ${place.text}
            </option>
        `);

        toPlace.append(`
            <option value="${place.value}">
                ${place.text}
            </option>
        `);

    });

    initializeSelect2(fromPlace, "Select From Place");
    initializeSelect2(toPlace, "Select To Place");
}

async function LoadMakeList(row, itemCode) {

    try {

        const res = await fetch(
            `/StoreInventoryTransfer/GetMakeList?itemCode=${encodeURIComponent(itemCode)}`
        );

        if (!res.ok) {
            throw new Error("Failed to load make list.");
        }

        const result = await res.json();
        console.log("MAke", result);
        const ddl = row.find(".make");

        ddl.empty();

        //ddl.append(`<option value="">Select Make</option>`);

        $.each(result, function (i, make) {

            ddl.append(`
                <option value="${make.value}">
                    ${make.text}
                </option>
            `);

        });

    } catch (error) {

        console.error("Error loading Make List:", error);

        showToast("Make List Load Failed: " + error.message, {
            type: "error"
        });
    }
}

async function LoadUnitList(row, itemCode) {

    try {

        const res = await fetch(
            `/StoreInventoryTransfer/GetUnitDetails?itemCode=${encodeURIComponent(itemCode)}`
        );

        if (!res.ok) {
            throw new Error("Failed to load Unit.");
        }

        const result = await res.json();
        console.log("Unit", result);
        const ddl = row.find(".unit");

        ddl.empty();

        //ddl.append(`<option value="">Select Make</option>`);

        $.each(result, function (i, unit) {

            ddl.append(`
                <option value="${unit.unitCode}">
                    ${unit.unitName}
                </option>
            `);

        });

    } catch (error) {
        console.error("Error loading Unit DropDown:", error);
        showToast("Unit DropDown Load Failed: " + error.message, { type: "error" });
    }
}

function initializeSelect2(dropdown, placeholder = "Select") {

    if (dropdown.hasClass("select2-hidden-accessible")) {
        dropdown.select2("destroy");
    }

    dropdown.select2({
        width: "100%",
        placeholder: placeholder,
        allowClear: true,
        dropdownAutoWidth: false
    });

    dropdown.on("select2:open", function () {

        setTimeout(function () {

            const searchBox = document.querySelector(
                ".select2-container--open .select2-search__field"
            );

            if (searchBox) {
                searchBox.focus();
            }

        }, 0);
    });
}

//=======================
// Print
//=======================
async function StorePrint() {

    var reportName = "RAW11";

    var vType = $('#ddlDocType').val();
    var vNo = $('#NumDocno').val();
    var rptName = $('#ddlDocType option:selected').text();

    if (!vType || !vNo) {
        showToast("V Type and V No are required.", { type: "error" });
        return;
    }

    var SelForMul =
        "{ISSUE1.V_TYPE}='" + vType + "'" +
        " AND {ISSUE1.V_NO}=" + vNo +
        " AND {ISSUE1.COMP_CODE}=" + window.globalVariables.compCode +
        " AND {ISSUE1.YEAR_CODE}=" + window.globalVariables.yearCode +
        " AND {ISSUE1.BRANCH_CODE}=" + window.globalVariables.branchCode;

    var formulaFields = {

        Reportname: reportName,

        selectionFormula: SelForMul,

        Database: window.database.db,

        Parameters: {
            RPTNAME: rptName,
            comp_name: window.globalVariables.companyName,
            comp_add1: window.globalVariables.add1,
            comp_add2: window.globalVariables.add2
        }
    };

    console.log("Store Inventory ReportData:", formulaFields);

    var now = new Date();

    var day = String(now.getDate()).padStart(2, '0');
    var month = String(now.getMonth() + 1).padStart(2, '0');
    var year = String(now.getFullYear()).slice(-2);

    var hours = String(now.getHours()).padStart(2, '0');
    var minutes = String(now.getMinutes()).padStart(2, '0');
    var seconds = String(now.getSeconds()).padStart(2, '0');

    var timestamp = `${day}${month}${year}_${hours}${minutes}${seconds}`;

    $.ajax({
        url: 'http://localhost:34089/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(formulaFields),
        contentType: "application/json",
        xhrFields: {
            responseType: 'blob'
        },
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
            console.error('Error generating report:', error);
        }
    });
}

async function validateData() {

    if (!validateRequiredField('#ddlDocType', 'Document Type')) {
        return false;
    }

    if (!validateRequiredField('#NumDocno', 'Document Number')) {
        return false;
    }

    if (!validateRequiredField('#ddlShift', 'Shift')) {
        return false;
    }

    const tbody = $('#tblItemdetail tbody');
    const rows = tbody.find('tr');

    if (rows.length === 0) {
        showToast("At least one item is required.", { type: "warning" });
        return false;
    }

    let validItemCount = 0;

    for (let i = 0; i < rows.length; i++) {

        const row = $(rows[i]);

        const itemCode = parseInt(row.find('.item-code').val()) || 0;
        const itemName = row.find('.item-name').val();

        const quantity = parseFloat(row.find('.quantity').val()) || 0;

        const fromDept = parseInt(row.find('.fromPlace').val()) || 0;
        const toDept = parseInt(row.find('.toPlace').val()) || 0;

        if (itemCode > 0) {
            validItemCount++;
        }

        if (itemName && itemCode === 0) {

            showToast("Item code not valid.", {
                type: "warning"
            });

            row.find('.item-code').focus();

            return false;
        }

        if (itemCode === 0) {
            continue;
        }

        if (quantity === 0) {

            showToast("Quantity is 0.", {
                type: "warning"
            });

            row.find('.quantity').focus();

            return false;
        }

        if (fromDept === 0) {

            showToast("From Department not selected.", {
                type: "warning"
            });

            row.find('.fromPlace').focus();

            return false;
        }

        if (toDept === 0) {

            showToast("To Department not selected.", {
                type: "warning"
            });

            row.find('.toPlace').focus();

            return false;
        }

        if (fromDept === toDept) {

            showToast(
                "From Department And To Department are Same.",
                { type: "warning" }
            );

            row.find('.toPlace').focus();

            return false;
        }


        // =========================
        // Stock Validation
        // =========================
        if (itemCode > 0) {

            // Yahan item_stk_qty ka API call
            // baad mein add karenge.

            // Example:
            //
            // const itemQty = await getItemStock(itemCode);
            //
            // if (itemQty < quantity) {
            //     showToast(
            //         `Stock of Item (${row.find('.item-name option:selected').text()}) is ${itemQty}`,
            //         { type: "warning" }
            //     );
            //     row.find('.item-code').focus();
            // }
        }
    }

    if (validItemCount === 0) {

        showToast(
            "No Record in grid to save.",
            { type: "warning" }
        );

        return false;
    }

    return true;
}