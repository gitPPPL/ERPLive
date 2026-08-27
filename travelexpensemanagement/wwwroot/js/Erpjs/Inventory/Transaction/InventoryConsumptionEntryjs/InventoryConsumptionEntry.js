let itemList = [];
let departmentList = [];
let machineList = [];
let costCatList = [];
let costSubCatList = [];
let costCenterList = [];
let pendingRequestData = [];

const urlParams = new URLSearchParams(window.location.search);
const id = urlParams.get('docId');
const vtype = urlParams.get('vtype');
let isReadOnly = urlParams.get('readOnly') === 'true';

$(document).ready(async function () {

    SetCurrentDate();
    await wireEvent();
    await GetDocTypeDDl();
    await BindAllHeaderDropdown();
    await BindAgainstPlanComplain();

    await LoadAllFooterDropdowns();

    if (id) {
        await LoadEditData();
    }
    else {
        addNewRow();
    }

    if (isReadOnly) {
        setFormReadOnly();
    }

    //=================================
    // Save and Update Data
    //=================================
    $('#btn_save').on('click', async function () {

        try {

            const vType = $('#ddlDocType').val();
            const vNo = $('#NumDocno').val();
            const vDate = $('#DtDocDate').val();
            const shift = $('#ddlShift').val() || "";
            const slipNo = $('#NumSlipNo').val();
            const placeCode = $('#ddlPlace').val();
            const empCode = $('#ddlEmployee').val();
            const remarks = $('#TxtRemarks').val();

            const tbody = $('#tblInventoryConsumption tbody');
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

                const nos = row.find('.nos').val();
                const quantity = row.find('.quantity').val();

                const toDept = row.find('.department').val();
                const machCode = row.find('.machine').val();

                const costCatCode = row.find('.cost-cat').val();
                const costSubCatCode = row.find('.cost-subcat').val();
                const costCenterCode = row.find('.cost-center').val();

                const rate = row.find('.rate').val();
                const amount = row.find('.amount').val();

                const landRate = row.find('.ld-rate').val();
                const landAmt = row.find('.ld-amt').val();

                const freMarks = row.find('.remarks').val();

                footer.push({

                    ITEM_CODE: itemCode ? parseInt(itemCode) : null,
                    ITEM_NAME: itemName || null,
                    MAKE_CODE: makeCode ? parseInt(makeCode) : null,
                    UOM_CODE: uomCode ? parseInt(uomCode) : null,
                    UOM_NAME: row.find('.unit option:selected').text() || null,
                    TO_DEPT: toDept ? parseInt(toDept) : null,
                    NOS: nos ? parseInt(nos) : null,
                    QTY: quantity ? parseFloat(quantity) : null,
                    RATE: rate ? parseFloat(rate) : null,
                    AMOUNT: amount ? parseFloat(amount) : null,
                    LAND_RATE: landRate ? parseFloat(landRate) : null,
                    LAND_AMT: landAmt ? parseFloat(landAmt) : null,
                    KANTA_TYPE: row.find('.ref-wb-type').val() || null,
                    KANTA_NO: row.find('.ref-wb-no').val() || null,
                    REQ_TYPE: row.find('.preq-type').val() || null,
                    REQ_NO: row.find('.preq-no').val() ? parseInt(row.find('.preq-no').val()) : null,
                    MACH_CODE: machCode ? parseInt(machCode) : "",
                    FREMARKS: freMarks || null,
                    COSTCAT_CODE: costCatCode ? parseInt(costCatCode) : null,
                    COSTSCAT_CODE: costSubCatCode ? parseInt(costSubCatCode) : null,
                    COSTCENTER_CODE: costCenterCode ? parseInt(costCenterCode) : null
                });

            });

            const model = {
                V_TYPE: vType,
                V_NO: vNo ? parseInt(vNo) : null,
                V_DATE: vDate,
                SHIFT: shift,
                SLIP_NO: slipNo || null,
                PLACE_CODE: placeCode ? parseInt(placeCode) : null,
                EMP_CODE: empCode ? parseInt(empCode) : null,
                REMARKS: remarks || null,
                InventryConsumptionFooter: footer
            };

            console.log("Save Model:", model);

            const res = await fetch("/InventoryConsumptionEntry/SaveAndUpdateData",
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
            console.error("Save Inventory Consumption Error:", error);
            showToast("Save failed: " + error.message, { type: "error" });
        }

    });

});

async function wireEvent() {
    $("#ddlDocType").focus();

    //-----------------------------------------------
    // Generate new VNo whenever DocType changes
    //-----------------------------------------------
    $('#ddlDocType').on('change', async function () {
        await GetVNo();
    });

    //--------------------------------------------
    // Select VNo on change of Plan drodown
    //--------------------------------------------
    $("#ddlAgainstPlancomplain").on("change", function () {
        const selected = $(this).find(":selected");
        const vNo = selected.val();
        if (!vNo) {
            return;
        }

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
        const tbody = $("#tblInventoryConsumption tbody");
        const rowCount = tbody.find("tr").length;
        if (rowCount <= 1) {
            showToast("At least one row is required.", { type: "warning" });
            return;
        }
        $(this).closest("tr").remove();
    });

    //-------------------------------------------
    // Fill Data on Change of Item
    //-------------------------------------------
    $(document).on("select2:select", ".item-name", async function (e) {

        const row = $(this).closest("tr");

        const item = e.params.data.itemData;

        if (!item) {
            return;
        }

        const itemCode = item.code;

        row.find(".item-code").val(itemCode);

        await Promise.all([
            LoadMakeDetails(itemCode, row),
            LoadUnitDetails(itemCode, row)
        ]);
    });

    //-------------------------------------------
    // Validate manually entered Item Code,
    // set Item Name and load related Make & Unit details
    //-------------------------------------------
    $(document).on("blur", ".item-code", async function () {

        const row = $(this).closest("tr");
        const itemCode = $(this).val().trim();

        if (!itemCode) {

            row.find(".item-name").val(null).trigger("change");
            row.find(".unit").empty().append(
                `<option value="">Select Unit</option>`
            );

            return;
        }

        try {

            const res = await fetch(
                `/InventoryConsumptionEntry/GetItemByCode?itemCode=${encodeURIComponent(itemCode)}`
            );

            if (!res.ok) {
                throw new Error("Failed to get item details.");
            }

            const data = await res.json();

            if (!data.success || !data.item) {

                showToast("Invalid Item Code.", { type: "warning" });
                row.find(".item-code").val("");
                row.find(".item-name").val(null).trigger("change");
                row.find(".unit").empty().append(`<option value="">Select Unit</option>`);
                return;
            }

            const item = data.item;

            row.find(".item-code").val(item.code);

            const itemDropdown = row.find(".item-name");

            const option = new Option(
                item.name,
                item.code,
                true,
                true
            );

            option.dataset.itemData = JSON.stringify(item);

            itemDropdown.append(option).trigger("change");

            // Load Make + Unit
            await Promise.all([
                LoadMakeDetails(item.code, row),
                LoadUnitDetails(item.code, row)
            ]);

        }
        catch (error) {
            console.error("Item Code Error:", error);
            showToast("Item Code validation failed: " + error.message, { type: "error" });
        }

    });

    //---------------------------------
    // Open Model of Pending WB Data
    //---------------------------------
    $('#btn_loadpendingWB').on('click', async function () {
        await LoadCapitalItemData();
    });

    $('#btn_loadpending').on('click', async function (e) {

        e.preventDefault();

        const vType = $('#ddlDocType').val();
        const placeCode = $('#ddlPlace').val();
        const vDate = $('#DtDocDate').val();

        if (vType === 'BFIS') {
            return;
        }

        if (!vDate) {
            showToast('Please select Document Date.', {
                type: 'warning'
            });
            $('#DtDocDate').focus();
            return;
        }

        if (!placeCode || placeCode === '0' || placeCode === '-1') {
            showToast('Please select Place first.', {
                type: 'warning'
            });
            $('#ddlPlace').focus();
            return;
        }

        try {

            const tbody = $('#tblloadpendingrequest tbody');

            tbody.html(`
            <tr>
                <td colspan="30" class="text-center">
                    Loading pending requests...
                </td>
            </tr>
        `);

            const url = `/InventoryConsumptionEntry/GetCopyFromData` + `?vType=${encodeURIComponent(vType)}` + `&placeCode=${encodeURIComponent(placeCode)}` + `&vDate=${encodeURIComponent(vDate)}`;

            console.log("API URL:", url);

            const response = await fetch(url);

            if (!response.ok) {
                throw new Error(`HTTP Error: ${response.status}`);
            }

            const result = await response.json();

            console.log("GetCopyFromData Response:", result);

            if (!result.success) {

                tbody.empty();

                showToast(
                    result.message || 'Unable to load pending requests.',
                    { type: 'warning' }
                );

                return;
            }

            pendingRequestData = result.data || [];

            tbody.empty();

            $('#selectAlllpwb').prop('checked', false);

            if (pendingRequestData.length === 0) {

                tbody.html(`
                <tr>
                    <td colspan="30" class="text-center">
                        No pending request found.
                    </td>
                </tr>
            `);

                return;
            }

            pendingRequestData.forEach((item, index) => {

                tbody.append(`
                <tr data-index="${index}">

                    <td>
                        <input type="checkbox"
                               class="lp-request-check"
                               data-index="${index}">
                    </td>

                    <td>${escapeHtml(item.ReqID ?? '')}</td>
                    <td>${escapeHtml(item.ICode ?? '')}</td>
                    <td>${escapeHtml(item.ItemName ?? '')}</td>
                    <td>${escapeHtml(item.Unit ?? '')}</td>
                    <td>${escapeHtml(item.Nos ?? '')}</td>
                    <td>${escapeHtml(item.Qty ?? '')}</td>
                    <td>${escapeHtml(item.Department ?? '')}</td>
                    <td>${escapeHtml(item.Machine ?? '')}</td>
                    <td>${escapeHtml(item.TO_DEPT ?? '')}</td>
                    <td>${escapeHtml(item.MACH_CODE ?? '')}</td>
                    <td>${escapeHtml(item.MAKE_CODE ?? '')}</td>
                    <td>${escapeHtml(item.ReqType ?? '')}</td>
                    <td>${escapeHtml(item.ReqNo ?? '')}</td>
                    <td></td>
                    <td></td>
                    <td>${escapeHtml(item.Ucode ?? '')}</td>
                    <td>${escapeHtml(item.Make ?? '')}</td>
                    <td>${escapeHtml(item.Remarks ?? '')}</td>
                    <td>${escapeHtml(item.Remarks2 ?? '')}</td>
                    <td>${escapeHtml(item.EMP_CODE ?? '')}</td>
                    <td>${escapeHtml(item.empname ?? '')}</td>
                    <td>${escapeHtml(item.Place_code ?? '')}</td>
                    <td>${escapeHtml(item.CostCat ?? '')}</td>
                    <td>${escapeHtml(item.CostsubCat ?? '')}</td>
                    <td>${escapeHtml(item.Costcenter ?? '')}</td>
                    <td>${escapeHtml(item.COSTCAT_CODE ?? '')}</td>
                    <td>${escapeHtml(item.COSTSCAT_CODE ?? '')}</td>
                    <td>${escapeHtml(item.COSTCENTER_CODE ?? '')}</td>
                    <td class="hidden-col"></td>

                </tr>
               `);
            });

            const modalElement = document.getElementById('loadpendingrequestModal');

            const modal = bootstrap.Modal.getOrCreateInstance(modalElement);

            modal.show();

        }
        catch (error) {

            console.error("GetCopyFromData Error:", error);

            $('#tblloadpendingrequest tbody').empty();

            showToast('Failed to load pending request: ' + error.message, { type: 'error' });
        }
    });

    initPendingPopup({
        searchBox: '#searchBoxlpwb',
        selectAll: '#selectAllPendingWB',
        table: '#tblloadpendingwb',
        rowCheckbox: '.capital-item-check'
    });

    initPendingPopup({
        searchBox: '#searchBoxlprequest',
        selectAll: '#selectAlllpwb',
        table: '#tblloadpendingrequest',
        rowCheckbox: '.lp-request-check',
    });

}

async function fillPendingRequestInFooter(item) {

    const tbody = $('#tblInventoryConsumption tbody');

    addNewRow();

    const row = tbody.find('tr:last');

    console.log("Copying Request:", item);

    row.find('.item-code').val(item.ICode ?? '');

    const itemDropdown = row.find('.item-name');

    itemDropdown.empty();

    const itemOption = new Option(
        item.ItemName ?? '',
        item.ICode ?? '',
        true,
        true
    );

    itemDropdown.append(itemOption).trigger('change');

    if (item.ICode) {

        await LoadMakeDetails(item.ICode, row);

        row.find('.make').val(item.MAKE_CODE ?? '').trigger('change');
    }

    if (item.ICode) {

        await LoadUnitDetails(item.ICode, row);

        row.find('.unit').val(item.Ucode ?? '').trigger('change');
    }

    row.find('.nos').val(item.Nos ?? '');

    row.find('.quantity').val(item.Qty ?? '');

    row.find('.department').val(item.TO_DEPT ?? '').trigger('change');

    row.find('.machine').val(item.MACH_CODE ?? '').trigger('change');

    row.find('.cost-cat').val(item.COSTCAT_CODE ?? '').trigger('change');

    row.find('.cost-subcat').val(item.COSTSCAT_CODE ?? '').trigger('change');

    row.find('.cost-center').val(item.COSTCENTER_CODE ?? '').trigger('change');

    row.find('.remarks').val(item.Remarks ?? '');

    row.find('.preq-type').val(item.ReqType ?? '');

    row.find('.preq-no').val(item.ReqNo ?? '');
}

function escapeHtml(value) {

    if (value === null || value === undefined) {
        return '';
    }

    return String(value)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}

function SetCurrentDate() {

    const today = new Date().toISOString().split("T")[0];
    $("#DtDocDate").val(today);

}

async function GetDocTypeDDl() {

    try {

        const res = await fetch("/InventoryConsumptionEntry/DocType", {
            method: "GET",
        });

        const data = await res.json();

        const ddl = $("#ddlDocType");
        ddl.empty();

        $.each(data, function (i, item) {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });

        if (id) {

            if (vtype) {
                ddl.val(vtype);
            }
            $('#ddlDocType').prop('disabled', true);
        }
        else {
            await GetVNo();
        }

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
        const res = await fetch(`/InventoryConsumptionEntry/GenerateVNo?vType=${encodeURIComponent(vType)}`);

        if (!res.ok) {
            throw new Error("Network response was not ok");
        }
        const data = await res.json();
        if (data.v_NO) {
            $('#NumDocno').val(data.v_NO);
            const docId = vType + data.v_NO;
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

        bindDropdown('InventoryConsumptionEntry', 'Place', '#ddlPlace', 'Select Party', null, null, false, null, true),
        bindDropdown('InventoryConsumptionEntry', 'HOD', '#ddlEmployee', 'Select Employee', null, null, false, null, true),

    ]);
}

async function BindAgainstPlanComplain() {

    try {

        const res = await fetch("/InventoryConsumptionEntry/GetComplainNo");

        if (!res.ok) {
            throw new Error("Failed to load Plan/Complain No.");
        }

        const data = await res.json();

        const ddl = $("#ddlAgainstPlancomplain");

        if (ddl.hasClass("select2-hidden-accessible")) {
            ddl.select2("destroy");
        }

        ddl.off("select2:open");

        ddl.empty();

        ddl.append(
            `<option value="">Select Against Plan/Complain No.</option>`
        );

        $.each(data, function (i, item) {

            ddl.append(`
                <option 
                    value="${item.vNo}"
                    data-vtype="${item.v_Type}"
                    data-dept="${item.deptName}"
                    data-fault="${item.fault}"
                    data-machine="${item.machine}"
                    data-date="${item.complainDate}">
                    ${item.v_Type} | ${item.vNo} | ${item.deptName} | ${item.fault} | ${item.machine} | ${item.complainDate}
                </option>
            `);

        });

        ddl.select2({
            width: "100%",
            placeholder: "Select Against Plan/Complain No.",
            allowClear: true,

            templateResult: function (item) {

                if (!item.id) {
                    return item.text;
                }

                return $(`<span>${item.text}</span>`);
            },

            templateSelection: function (item) {

                if (!item.id) {
                    return item.text;
                }

                return item.id;
            }
        });

        // ==========================================
        // Auto Focus Select2 Search Box
        // ==========================================
        ddl.on("select2:open", function () {

            let attempts = 0;

            const focusSearchBox = setInterval(function () {

                const searchBox = $(".select2-container--open").find(".select2-search__field");

                if (searchBox.length > 0) {

                    searchBox[0].focus();

                    clearInterval(focusSearchBox);
                    return;
                }

                attempts++;

                if (attempts >= 20) {
                    clearInterval(focusSearchBox);
                }

            }, 25);

        });

    }
    catch (error) {

        showToast(
            "Against Plan/Complain No. Load Failed : " + error.message,
            { type: "error" }
        );

        console.error(
            "Error loading Against Plan/Complain No.:",
            error
        );
    }
}

async function LoadEditData() {

    if (!id) {
        return;
    }

    try {

        const res = await fetch(
            `/InventoryConsumptionEntry/LoadEditData?docId=${encodeURIComponent(id)}`
        );

        if (!res.ok) {
            throw new Error("Failed to load edit data.");
        }

        const data = await res.json();

        if (!data.success) {
            showToast(data.message || "Failed to load edit data.", { type: "error" });
            return;
        }

        console.log("Edit Data:", data);

        // =========================
        // Header
        // =========================

        const header = data.header;

        if (header) {

            $('#ddlDocType').val(header.V_TYPE);

            $('#NumDocno').val(header.V_NO);

            if (header.V_DATE) {
                $('#DtDocDate').val(formatDateForInput(header.V_DATE));
            }

            $('#ddlShift').val(header.SHIFT).trigger('change');

            $('#NumSlipNo').val(header.SLIP_NO);

            $('#ddlPlace').val(header.PLACE_CODE).trigger('change');

            $('#ddlEmployee').val(header.EMP_CODE).trigger('change');

            $('#TxtRemarks').val(header.REMARKS);
        }

        // =========================
        // Footer
        // =========================

        const tbody = $('#tblInventoryConsumption tbody');

        tbody.empty();

        if (data.footer && data.footer.length > 0) {

            for (const item of data.footer) {

                addNewRow();

                const row = tbody.find('tr:last');

                row.find('.item-code').val(item.ITEM_CODE ?? '');

                const itemDropdown = row.find('.item-name');

                if (item.ITEM_CODE) {

                    const option = new Option(item.ITEM_NAME || '', item.ITEM_CODE, true, true);

                    itemDropdown.append(option).trigger('change');
                }

                await LoadMakeDetails(item.ITEM_CODE, row);

                row.find('.make').val(item.MAKE_CODE ?? '').trigger('change');

                await LoadUnitDetails(item.ITEM_CODE, row);

                row.find('.unit').val(item.UOM_CODE ?? '').trigger('change');

                row.find('.nos').val(item.NOS ?? '');
                row.find('.quantity').val(item.QTY ?? '');
                row.find('.department').val(item.TO_DEPT ?? '').trigger('change');
                row.find('.machine').val(item.MACH_CODE ?? '').trigger('change');
                row.find('.cost-cat').val(item.COSTCAT_CODE ?? '').trigger('change');
                row.find('.cost-subcat').val(item.COSTSCAT_CODE ?? '').trigger('change');
                row.find('.cost-center').val(item.COSTCENTER_CODE ?? '').trigger('change');
                row.find('.rate').val(item.RATE ?? '');
                row.find('.amount').val(item.AMOUNT ?? '');
                row.find('.ld-rate').val(item.LAND_RATE ?? '');
                row.find('.ld-amt').val(item.LAND_AMT ?? '');
                row.find('.remarks').val(item.FREMARKS ?? '');
                row.find('.pord-type').val(item.PORD_TYPE ?? '');
                row.find('.pord-no').val(item.PORD_NO ?? '');
                row.find('.ref-wb-type').val(item.KANTA_TYPE ?? '');
                row.find('.ref-wb-no').val(item.KANTA_NO ?? '');
                row.find('.preq-type').val(item.REQ_TYPE ?? '');
                row.find('.preq-no').val(item.REQ_NO ?? '');
                
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

    const table = $('#tblInventoryConsumption');

    table.find('input').prop('readonly', true);
    table.find('textarea').prop('readonly', true);
    table.find('select').prop('disabled', true);
    table.find('.add, .delete').prop('disabled', true);

    $('#btn_loadpendingWB').prop('disabled', true);
    $('#btn_loadpending').prop('disabled', true);
    $('#btn_print').prop('disabled', true);
    $('#btn_save').prop('disabled', true).hide();
}

//=============================
// Footer Table
//=============================

function addNewRow() {

    const tbody = $("#tblInventoryConsumption tbody");

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
                    <option value="">Select Unit</option>
                </select>
            </td>

            <td>
                <input type="number" class="erppagetable-control nos">
            </td>

            <td>
                <input type="number" class="erppagetable-control quantity">
            </td>

            <td>
                <select class="erppagetable-control department">
                    <option value="">Select Department</option>
                </select>
            </td>

            <td>
                <select class="erppagetable-control machine">
                    <option value="">Select Machine</option>
                </select>
            </td>

            <td>
                <select class="erppagetable-control cost-cat">
                    <option value="">Select Cost Cat</option>
                </select>
            </td>

            <td>
                <select class="erppagetable-control cost-subcat">
                    <option value="">Select Cost SubCat</option>
                </select>
            </td>

            <td>
                <select class="erppagetable-control cost-center">
                    <option value="">Select Cost Center</option>
                </select>
            </td>

            <td>
                <input type="number" class="erppagetable-control rate">
            </td>

            <td>
                <input type="number" class="erppagetable-control amount">
            </td>

            <td>
                <input type="number" class="erppagetable-control ld-rate">
            </td>

            <td>
                <input type="number" class="erppagetable-control ld-amt">
            </td>

            <td>
                <input type="text" class="erppagetable-control remarks">
            </td>

            <td>
                <input type="text" class="erppagetable-control pord-type">
            </td>

            <td>
                <input type="text" class="erppagetable-control pord-no">
            </td>

            <td>
                <input type="text" class="erppagetable-control ref-wb-type">
            </td>

            <td>
                <input type="text" class="erppagetable-control ref-wb-no">
            </td>

            <td>
                <input type="text" class="erppagetable-control preq-type">
            </td>

            <td>
                <input type="text" class="erppagetable-control preq-no">
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

    const itemNameDropdown = newRow.find(".item-name");
    const departmentDropdown = newRow.find(".department");
    const machineDropdown = newRow.find(".machine");
    const costCatDropdown = newRow.find(".cost-cat");
    const costSubCatDropdown = newRow.find(".cost-subcat");
    const costCenterDropdown = newRow.find(".cost-center");

    itemNameDropdown.empty();

    itemNameDropdown.append(`<option value="">Select Item</option>`);

    initItemDropdown(itemNameDropdown);

    departmentDropdown.empty();
    departmentDropdown.append(`<option value="">Select Department</option>`);

    $.each(departmentList, function (i, department) {

        departmentDropdown.append(`
            <option value="${department.text}">${department.value}</option>
        `);

    });

    initSelect2WithAutoFocus(departmentDropdown, "Select Department");

    machineDropdown.empty();
    machineDropdown.append(`<option value="">Select Machine</option>`);

    $.each(machineList, function (i, machine) {

        machineDropdown.append(`
            <option value="${machine.text}">${machine.value}</option>
        `);

    });

    initSelect2WithAutoFocus(machineDropdown, "Select Machine");

    costCatDropdown.empty();
    costCatDropdown.append(`<option value="">Select CostCat</option>`);

    $.each(costCatList, function (i, costCat) {

        costCatDropdown.append(`
            <option value="${costCat.text}">${costCat.value}</option>
        `);

    });

    initSelect2WithAutoFocus(costCatDropdown, "Select CostCat");

    costSubCatDropdown.empty();
    costSubCatDropdown.append(`<option value="">Select Cost SubCat</option>`);

    $.each(costSubCatList, function (i, costSubCat) {

        costSubCatDropdown.append(`
            <option value="${costSubCat.text}">${costSubCat.value}</option>
        `);

    });

    initSelect2WithAutoFocus(costSubCatDropdown, "Select CostSubCat");

    costCenterDropdown.empty();
    costCenterDropdown.append(`<option value="">Select Cost Center</option>`);

    $.each(costCenterList, function (i, costCenter) {

        costCenterDropdown.append(`
            <option value="${costCenter.text}">${costCenter.value}</option>
        `);

    });

    initSelect2WithAutoFocus(costCenterDropdown, "Select Cost Center");

}

//================================
// Footer Table Dropdowns
//================================

async function LoadAllFooterDropdowns() {
    try {
        await Promise.all([
            LoadDepartmentDetails(),
            LoadMachineList(),
            LoadCostCatList(),
            LoadCostSubCatList(),
            LoadCostCenterList()
        ]);

    } catch (error) {
        showToast("Footer dropdown data load failed: " + error.message, { type: "error" });
        console.error("Footer dropdown loading error:", error);
    }
}

function initItemDropdown(dropdown) {

    dropdown.select2({

        width: "100%",
        placeholder: "Select Item",
        allowClear: true,
        minimumInputLength: 0,
        ajax: {
            url: "/InventoryConsumptionEntry/SearchItems",
            dataType: "json",
            delay: 300,
            data: function (params) {
                return {
                    search: params.term || "",
                    page: params.page || 1
                };

            },

            processResults: function (data, params) {

                params.page = params.page || 1;

                return {

                    results: data.items.map(function (item) {

                        return {

                            id: item.code,
                            text: item.name,
                            itemData: item
                        };

                    }),

                    pagination: {

                        more: data.hasMore

                    }

                };

            },

            cache: true
        }
    });

    dropdown.on("select2:open", function () {
        setTimeout(function () {
            const searchBox = $(".select2-container--open").find(".select2-search__field");
            if (searchBox.length) {
                searchBox[0].focus();
            }

        }, 50);
    });
}

async function LoadDepartmentDetails() {
    try {

        const res = await fetch("/InventoryConsumptionEntry/GetDepartmentDetails");

        if (!res.ok) {
            throw new Error("Failed to load Department details.");
        }

        const data = await res.json();

        if (!Array.isArray(data)) {
            throw new Error("Invalid department details response.");
        }
        departmentList = data;

    } catch (error) {
        showToast("Department Load failed: " + error.message, { type: "error" });
    }
}

async function LoadMachineList() {
    try {

        const res = await fetch("/InventoryConsumptionEntry/GetMachineDetails");

        if (!res.ok) {
            throw new Error("Failed to load Machine details.");
        }

        const data = await res.json();

        if (!Array.isArray(data)) {
            throw new Error("Invalid Machine details response.");
        }

        machineList = data;

    } catch (error) {
        showToast("Machine Data Load failed: " + error.message, { type: "error" });
    }
}

async function LoadCostCatList() {
    try {

        const res = await fetch("/InventoryConsumptionEntry/GetCostCatDetails");

        if (!res.ok) {
            throw new Error("Failed to load CostCat Details.");
        }

        const data = await res.json();

        if (!Array.isArray(data)) {
            throw new Error("Invalid CostCat Details");
        }

        costCatList = data;

    } catch (error) {
        showToast("CostCat Data Load failed: " + error.message, { type: "error" });
    }
}

async function LoadCostSubCatList() {
    try {

        const res = await fetch("/InventoryConsumptionEntry/GetCostSubCatDetails");

        if (!res.ok) {
            throw new Error("Failed to load Cost SubCat Details.");
        }

        const data = await res.json();

        if (!Array.isArray(data)) {
            throw new Error("Invalid Cost SubCat Details");
        }

        costSubCatList = data;

    } catch (error) {
        showToast("Cost SubCat Data Load failed: " + error.message, { type: "error" });
    }
}

async function LoadCostCenterList() {
    try {

        const res = await fetch("/InventoryConsumptionEntry/GetCostCenterDetails");

        if (!res.ok) {
            throw new Error("Failed to load Cost Center Details.");
        }

        const data = await res.json();

        if (!Array.isArray(data)) {
            throw new Error("Invalid Cost Center Details");
        }

        costCenterList = data;

    } catch (error) {
        showToast("Cost Center Data Load failed: " + error.message, { type: "error" });
    }
}

async function LoadMakeDetails(itemCode, row) {
    try {

        if (!itemCode) {
            row.find(".make").empty().append(
                `<option value="">Select Make</option>`
            );
            return;
        }

        const res = await fetch(
            `/InventoryConsumptionEntry/GetMakeDetails?itemCode=${encodeURIComponent(itemCode)}`
        );

        if (!res.ok) {
            throw new Error("Failed to load Make details.");
        }

        const data = await res.json();

        if (!Array.isArray(data)) {
            throw new Error("Invalid Make details response.");
        }

        const makeDropdown = row.find(".make");

        makeDropdown.empty();

        // makeDropdown.append(`<option value="">Select Make</option>`);

        $.each(data, function (i, make) {

            makeDropdown.append(`
                <option value="${make.mcode}">
                    ${make.make}
                </option>
            `);

        });
    }
    catch (error) {
        showToast("Make Data Load failed: " + error.message, { type: "error" });
        console.error("Error loading Make details:", error);
    }
}

async function LoadUnitDetails(itemCode, row) {
    try {

        if (!itemCode) {
            row.find(".unit").empty().append(
                `<option value="">Select Unit</option>`
            );
            return;
        }

        const res = await fetch(
            `/InventoryConsumptionEntry/GetUnitDetails?itemCode=${encodeURIComponent(itemCode)}`
        );

        if (!res.ok) {
            throw new Error("Failed to load Unit details.");
        }

        const data = await res.json();

        if (!Array.isArray(data)) {
            throw new Error("Invalid Unit details response.");
        }

        const unitDropdown = row.find(".unit");

        unitDropdown.empty();

        $.each(data, function (i, unit) {
            unitDropdown.append(`<option value="${unit.unitCode}">${unit.unitName}</option>`);
        });

    }
    catch (error) {
        showToast("Unit Data Load failed: " + error.message, { type: "error" });
        console.error("Error loading unit details:", error);
    }
}

function initSelect2WithAutoFocus(dropdown, placeholder) {

    if (dropdown.hasClass("select2-hidden-accessible")) {
        dropdown.select2("destroy");
    }

    dropdown.select2({
        width: "100%",
        placeholder: placeholder,
        allowClear: true
    });

    dropdown.on("select2:open", function () {
        setTimeout(function () {
            document.querySelector(".select2-container--open .select2-search__field")?.focus();
        }, 0);
    });
}

//=====================
// Copy From
//=====================
async function LoadCapitalItemData() {

    try {

        const vDate = $('#DtDocDate').val();

        if (!vDate) {
            showToast("Please select Document Date.", { type: "warning" });
            return;
        }

        const res = await fetch(
            `/InventoryConsumptionEntry/GetCapitalItemData?vDate=${encodeURIComponent(vDate)}`
        );

        if (!res.ok) {
            throw new Error("Failed to load Capital Item data.");
        }

        const data = await res.json();

        if (!Array.isArray(data)) {
            throw new Error("Invalid Capital Item data.");
        }

        const tbody = $('#tblloadpendingwb tbody');

        tbody.empty();

        if (data.length === 0) {
            showToast("No Capital Item data found.", { type: "warning" });
            return;
        }

        $.each(data, function (index, item) {

            tbody.append(`
                <tr>
                    <td>
                       <input type="checkbox" class="capital-item-check"data-doc-id="${item.docId}">
                    </td>
                    <td>${item.docId ?? ''}</td>
                    <td>${item.date ?? ''}</td>
                    <td>${item.itemName ?? ''}</td>
                    <td>${item.qty ?? ''}</td>
                    <td>${item.toPlace ?? ''}</td>
                    <td>${item.grossWt ?? ''}</td>
                    <td>${item.tareWt ?? ''}</td>
                    <td>${item.remarks ?? ''}</td>
                    <td class="hidden-col"></td>
                </tr>
            `);

        });

        $('#loadpendingWBModal').modal('show');

    }
    catch (error) {

        console.error("Capital Item Load Error:", error);

        showToast("Capital Item Load Failed: " + error.message, { type: "error" });
    }
}


//===========================
// Helper functin for popUp
//===========================
function initPendingPopup({ searchBox, selectAll, table, rowCheckbox }) {

    // =========================
    // Select All
    // =========================
    $(document).on('change', selectAll, function () {

        const checked = $(this).is(':checked');

        $(table).find(`tbody ${rowCheckbox}`).prop('checked', checked);
    });


    // =========================
    // Search
    // =========================
    $(document).on('input', searchBox, function () {

        const search = $(this).val().toLowerCase().trim();

        $(table).find('tbody tr').each(function () {

            $(this).toggle(
                $(this).text().toLowerCase().includes(search)
            );

        });
    });

    // =========================
    // Individual Checkbox
    // =========================
    $(document).on('change', `${table} tbody ${rowCheckbox}`,
        function () {
            const total = $(table).find(`tbody ${rowCheckbox}`).length;
            const checked = $(table).find(`tbody ${rowCheckbox}:checked`).length;
            $(selectAll).prop(
                'checked',
                total > 0 && total === checked
            );
        }
    );
}