const urlParams = new URLSearchParams(window.location.search);
const id = urlParams.get('id');
const vtype = urlParams.get('vType');
let isReadOnly = urlParams.get('readOnly') === 'true';
let selectedPendingOrders = [];
let copyDataMode = "all";
let currentFooterRow = null;
  
$(document).ready(async function () {

    await wireEvent();
    if (id && vtype) {
        await loadEditData();
    }
    else {
        await GetVNo();
        await addNewRow();
    }

    $('#btn_save').on('click', async function () {
        await saveDispatchPlanning();
    });

    applyReadOnlyMode();


});

async function wireEvent() {

    SetCurrentDate();

    // Open Copy From Popup
    $('#btnShowAllPendingOrder').on('click', async function () {

        await loadAllPendingOrders();

        bootstrap.Modal.getOrCreateInstance(
            document.getElementById('copyFromModal')
        ).show();

    });

    $(document).on('change', '#selectAllPR', function () {

        $('.selectPendingOrder').prop('checked', this.checked);

    });

    // Copy Selected Pending Orders
    $(document).on('click', '#btnCopyPendingOrders', async function () {
        await copySelectedPendingOrders();
    });

    // Fill Data
    $('#btnItemFooterFillData').on('click', async function () {
        await loadFillData();
    });

    // Reset Form
    $('#btnItemFooterReset').on('click', async function () {
        await resetForm();
    });

    // Maintain currently selected footer row
    // Maintain currently selected footer row
    $(document).on('click', '#tblItemNameFooter tbody tr', function () {

        currentFooterRow = $(this);

        $('#tblItemNameFooter tbody tr').removeClass('selected-row');

        currentFooterRow.addClass('selected-row');

    });

    $(document).on('click', '.act-btn.delete', function () {

        if (isReadOnly) return;

        const row = $(this).closest('tr');
        const tbody = $('#tblItemNameFooter tbody');

        if (tbody.find('tr').length === 1) {
            showToast("At least one row is required.", { type: "warning" });
            return;
        }

        if (currentFooterRow && currentFooterRow[0] === row[0]) {
            currentFooterRow = null;
        }

        row.remove();

        // Update selected row reference
        $('#tblItemNameFooter tbody tr').removeClass('selected-row');

    });

}

function SetCurrentDate() {
    const today = new Date();
    const date = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`;

    $('#dtItemFooterDate').val(date);
}

async function GetVNo() {
    try {

        const vType = "DISP";

        const response = await fetch('/DispatchPlaning/GetNextV_NO');

        if (!response.ok) {
            throw new Error('Failed to generate VNo');
        }

        const data = await response.json();

        if (data.error) {
            showToast("Voucher No Error: " + data.error, { type: "error" });
            return;
        }

        $('#txtItemFooterDocNo').val(data.v_NO);

        const docId = vType + data.v_NO;

        console.log('Generated VNo:', docId);

    } catch (error) {
        console.error('Error generating VNo:', error);
        showToast("Error generating vNo: " + error.message, { type: "error" });
    }
}
 
async function loadAllPendingOrders() {

    try {

        const sortBy = $('input[name="itemSearchType"]:checked').val();

        const vNo = $('#txtItemFooterDocNo').val();

        const response = await fetch(
            `/DispatchPlaning/GetAllPendingOrders?sortBy=${encodeURIComponent(sortBy)}&vNo=${encodeURIComponent(vNo)}`
        );

        if (!response.ok) {
            throw new Error('Failed to load pending orders.');
        }

        const data = await response.json();

        const tbody = $('#tblDeliveryOrderPopUp tbody');

        tbody.empty();

        data.forEach(item => {

            const tr = $('<tr>');

            tr.append('<td><input type="checkbox" class="selectPendingOrder"></td>');
            tr.append($('<td>').text(item.docNo));
            tr.append($('<td>').text(item.docDate));
            tr.append($('<td>').text(item.partyName));
            tr.append($('<td>').text(item.itemName));
            tr.append($('<td>').text(item.tenacity));
            tr.append($('<td>').text(item.qty));
            tr.append($('<td>').text(item.rate));
            tr.append($('<td>').text(item.balanceQty));
            tr.append($('<td>').text(item.planQty));

            tr.data('order', item);

            tbody.append(tr);

        });

        $('#selectAllPR').prop('checked', false);

        if (data.length === 0) {
            showToast("No pending orders found.", { type: "info" });
        }

    } catch (error) {

        console.error('Pending Order Error:', error);

        showToast(error.message, { type: "error" });

    }
}


//==============================
// Footer Table
//=============================

async function addNewRow() {

    const tbody = $("#tblItemNameFooter tbody");

    const row = `
        <tr>

            <!-- Col0: Item Code (Hidden) -->
            <td style="display:none;">
                <input type="number" class="erppagetable-control itemCode" readonly>
            </td>

            <!-- Col1: Item Name -->
            <td class="freeze-item">
                <select class="erppagetable-control itemName">
                    <option value="">Select Item</option>
                </select>
            </td>

            <!-- Col2: Tenacity -->
            <td>
                <select class="erppagetable-control tenacity">
                    <option value="">Select Tenacity</option>
                </select>
            </td>

            <!-- Col3: Status -->
            <td>
                <select class="erppagetable-control status">
                    <option value="">Select Status</option>
                </select>
            </td>

            <!-- Col4: Bale No -->
            <td>
                <input type="text" class="erppagetable-control baleNo">
            </td>

            <!-- Col5: Stock -->
            <td>
                <input type="number" class="erppagetable-control stock" step="0.001" readonly>
            </td>

            <!-- Col6: Dispatch Plan Qty -->
            <td>
                <input type="number" class="erppagetable-control dispatchPlanQty" step="0.001">
            </td>

            <!-- Col7: Party Name -->
            <td>
                <input type="text" class="erppagetable-control partyName">
            </td>

            <!-- Col8: Est. Production -->
            <td>
                <input type="number" class="erppagetable-control estProduction" step="0.001">
            </td>

            <!-- Col9: Approx. Amount -->
            <td>
                <input type="number" class="erppagetable-control approxAmount" step="0.01">
            </td>

              <!--Doc No -->
            <td style="display:none;">
                <input type="number" class="erppagetable-control docNo" readonly>
            </td>

             <!--Doc Date-->
            <td style="display:none;">
                <input type="date" class="erppagetable-control docDate" readonly>
            </td>

            <!-- Hidden Party Code -->
            <td style="display:none;">
                <input type="number" class="erppagetable-control partyCode" readonly>
            </td>

            <!-- Action -->
            <td class="action-col">
                <div class="action-wrap">
                    <button type="button" class="act-btn add" title="Add Row">
                        <i class="fa fa-plus"></i>
                    </button>
                    <button type="button" class="act-btn delete" title="Delete Row">
                        <i class="fa fa-trash"></i>
                    </button>
                </div>
            </td>

        </tr>
    `;

    tbody.append(row);

    const lastRow = tbody.find("tr:last");
    currentFooterRow = lastRow;

    $('#tblItemNameFooter tbody tr').removeClass('selected-row');
    lastRow.addClass('selected-row');

    lastRow.find(".itemName").select2({
        placeholder: "Select Item",
        allowClear: true,
        width: "100%",
        ajax: {
            url: '/DispatchPlaning/GetItemList',
            type: 'GET',
            dataType: 'json',
            delay: 250,
            data: function (params) {
                return {
                    page: params.page || 1,
                    search: params.term || ''
                };
            },
            processResults: function (data) {
                return {
                    results: data.results,
                    pagination: data.pagination
                };
            },
            cache: true
        }
    });

    await loadTenacityDropdown(lastRow);

    await loadStatusDropdown(lastRow);

}

async function loadTenacityDropdown(row) {
    try {
        const response = await fetch('/DispatchPlaning/GetTenacityList');

        if (!response.ok) {
            throw new Error('Failed to load Tenacity');
        }

        const data = await response.json();

        const dropdown = row.find(".tenacity");

        dropdown.empty().append('<option value="">Select Tenacity</option>');

        data.forEach(item => {
            dropdown.append( `<option value="${item.value}">${item.text}</option>` );
        });

    } catch (error) {
        console.error('Tenacity Dropdown Error:', error);
        showToast("Unable to load Tenacity.", { type: "error" });
    }
}

async function loadStatusDropdown(row) {
    try {
        const response = await fetch('/DispatchPlaning/GetStatusList');

        if (!response.ok) {
            throw new Error('Failed to load Status');
        }

        const data = await response.json();
        const dropdown = row.find(".status");

        dropdown.empty().append('<option value="">Select Status</option>');

        data.forEach(item => {
            dropdown.append(
                `<option value="${item.value}">${item.text}</option>`
            );
        });

    } catch (error) {
        console.error('Status Dropdown Error:', error);
        showToast("Unable to load Status.", { type: "error" });
    }
}

async function copySelectedPendingOrders() {

    try {

        const selectedRows = $('#tblDeliveryOrderPopUp tbody tr').filter(function () {
            return $(this).find('.selectPendingOrder').is(':checked');
        });

        if (selectedRows.length === 0) {
            showToast("Please select pending orders.", { type: "warning" });
            return;
        }

        // Copy Mode
        copyDataMode = $('#selectAllPR').is(':checked') ? "all" : "single";

        selectedPendingOrders = [];

        const orders = [];

        selectedRows.each(function () { orders.push($(this).data('order')); });

        // Sort by Item Code and Party Code
        orders.sort((a, b) => {

            const itemA = Number(a.itemCode ?? a.ITEM_CODE ?? 0);
            const itemB = Number(b.itemCode ?? b.ITEM_CODE ?? 0);

            if (itemA !== itemB) return itemA - itemB;

            const partyA = Number(a.partyCode ?? a.PARTY_CODE ?? 0);
            const partyB = Number(b.partyCode ?? b.PARTY_CODE ?? 0);

            return partyA - partyB;

        });

        //================================
        // Single Mode 
        //================================

        if (copyDataMode === "single") {

            if (!currentFooterRow || !currentFooterRow.length ||
                !$.contains(document.getElementById('tblItemNameFooter'), currentFooterRow[0])) {

                showToast("Please select a footer row first.", { type: "warning" });
                return;
            }

            const footerBody = $('#tblItemNameFooter tbody');

            // Current row blank hai toh use karo
            let targetFooterRow = currentFooterRow;

            // Agar current row already filled hai toh blank row search karo
            if (!targetFooterRow || !targetFooterRow.length ||
                targetFooterRow.find('.itemCode').val()) {

                targetFooterRow = footerBody.find('tr').filter(function () {
                    return !$(this).find('.itemCode').val();
                }).first();
            }

            // Blank row nahi mili toh new row create karo
            if (!targetFooterRow.length) {
                await addNewRow();
                targetFooterRow = footerBody.find('tr:last');
            }

            currentFooterRow = targetFooterRow;

            // First selected order current selected footer row mein fill hoga.
            // Remaining orders ke liye new footer rows create hongi.
            for (let i = 0; i < orders.length; i++) {

                const order = orders[i];

                let footerRow;

                if (i === 0) {
                    footerRow = targetFooterRow;
                } else {
                    await addNewRow();
                    footerRow = footerBody.find('tr:last');
                }

                const itemCode = order.itemCode ?? order.ITEM_CODE ?? '';
                const itemName = order.itemName ?? order.ITEM_NAME ?? '';

                const tenacityCode = order.tenacityCode ?? order.TENACITY_CODE ?? '';
                const statusCode = order.statusCode ?? order.STATUS_CODE ?? '';

                const partyName = order.partyName ?? order.PARTY_NAME ?? '';
                const partyCode = order.partyCode ?? order.PARTY_CODE ?? '';

                const balQty = Number(order.balanceQty ?? order.BAL_QTY ?? 0);
                const rate = Number(order.rate ?? order.RATE ?? 0);

                // Item
                const itemDropdown = footerRow.find('.itemName');
                itemDropdown.empty();
                itemDropdown.append(
                    new Option(itemName, itemCode, true, true)
                ).trigger('change');

                footerRow.find('.itemCode').val(itemCode);

                // Tenacity
                footerRow.find('.tenacity').val(String(tenacityCode)).trigger('change');

                // Status
                footerRow.find('.status').val(String(statusCode)).trigger('change');

                // Party
                footerRow.find('.partyName').val(partyName);
                footerRow.find('.partyCode').val(partyCode);

                // Quantity and Amount
                footerRow.find('.stock').val(balQty);
                footerRow.find('.dispatchPlanQty').val(balQty.toFixed(3));
                footerRow.find('.approxAmount').val((rate * balQty).toFixed(2));

                // Document Details
                footerRow.find('.docNo').val(order.docNo ?? order.DOC_NO ?? '');

                const docDate = order.docDate ?? order.DOC_DATE ?? '';

                if (docDate) {
                    footerRow.find('.docDate').val(String(docDate).split('T')[0]);
                }

                // Estimated Production
                const stock = Number(footerRow.find('.stock').val() || 0);

                footerRow.find('.estProduction').val(
                    (stock - balQty).toFixed(3)
                );

                // Pending Order Details
                selectedPendingOrders.push({

                    DOC_TYPE: order.docType ?? order.DOC_TYPE ?? '',
                    DOC_NO: order.docNo ?? order.DOC_NO ?? '',
                    DOC_SNO: i + 1,
                    DOC_DATE: docDate || null,

                    ITEM_CODE: itemCode,
                    TENACITY_CODE: tenacityCode,
                    STATUS_CODE: statusCode,

                    PARTY_CODE: partyCode,

                    RATE: rate,
                    SALE_QTY: Number(order.saleQty ?? order.SALE_QTY ?? 0),
                    BAL_QTY: balQty,
                    PLAN_QTY: balQty,

                    V_SNO: footerBody.find('tr').index(footerRow) + 1

                });
            }

            console.log("Single Mode Pending Orders:", selectedPendingOrders);

            $('#selectAllPR').prop('checked', false);
            $('.selectPendingOrder').prop('checked', false);

            bootstrap.Modal.getInstance(
                document.getElementById('copyFromModal')
            )?.hide();

            showToast("Pending Orders copied successfully.", { type: "success" });

            return;
        }


        // ==========================================
        // ALL MODE
        // ==========================================

        const groupedItems = new Map();

        orders.forEach((order, index) => {

            const itemCode = order.itemCode ?? order.ITEM_CODE;
            const partyCode = order.partyCode ?? order.PARTY_CODE ?? '';

            // const planQty = Number(order.planQty ?? order.PLAN_QTY ?? 0);
            const planQty = Number(order.balanceQty ?? order.BAL_QTY ?? 0);
            const rate = Number(order.rate ?? order.RATE ?? 0);

            const itemKey = String(itemCode);

            if (!groupedItems.has(itemKey)) {

                groupedItems.set(itemKey, {

                    itemCode: itemCode,
                    itemName: order.itemName ?? order.ITEM_NAME ?? '',

                    tenacityCode: order.tenacityCode ?? order.TENACITY_CODE ?? '',
                    tenacity: order.tenacity ?? order.TENACITY ?? '',

                    statusCode: order.statusCode ?? order.STATUS_CODE ?? '',
                    status: order.status ?? order.STATUS_NAME ?? '',

                    totalPlanQty: 0,
                    approxAmount: 0,

                    parties: new Map(),
                    orders: []

                });

            }

            const group = groupedItems.get(itemKey);

            group.totalPlanQty += planQty;
            group.approxAmount += rate * planQty;

            // Party-wise quantity
            const partyKey = String(partyCode);

            if (!group.parties.has(partyKey)) {

                group.parties.set(partyKey, {

                    name: order.partyName ?? order.PARTY_NAME ?? '',
                    code: partyCode,
                    qty: 0

                });

            }

            group.parties.get(partyKey).qty += planQty;

            group.orders.push({
                order: order,
                sno: index + 1
            });

        });

        // Prepare DISPATCH_PLAN2 records
        let vSno = 0;

        for (const group of groupedItems.values()) {

            vSno++;

            group.orders.forEach(({ order, sno }) => {

                selectedPendingOrders.push({

                    DOC_TYPE: order.docType ?? order.DOC_TYPE ?? '',
                    DOC_NO: order.docNo ?? order.DOC_NO ?? '',
                    DOC_SNO: sno,
                    DOC_DATE: order.docDate ?? order.DOC_DATE ?? null,

                    ITEM_CODE: group.itemCode,
                    TENACITY_CODE: group.tenacityCode,
                    STATUS_CODE: group.statusCode,

                    PARTY_CODE: order.partyCode ?? order.PARTY_CODE ?? '',

                    RATE: Number(order.rate ?? order.RATE ?? 0),
                    SALE_QTY: Number(order.saleQty ?? order.SALE_QTY ?? 0),
                    BAL_QTY: Number(order.balanceQty ?? order.BAL_QTY ?? 0),
                    //PLAN_QTY: Number(order.planQty ?? order.PLAN_QTY ?? 0),
                    PLAN_QTY: Number(order.balanceQty ?? order.BAL_QTY ?? 0),

                    V_SNO: vSno

                });

            });

        }

        const footerBody = $('#tblItemNameFooter tbody');

        // Item-wise footer binding
        for (const group of groupedItems.values()) {

            let footerRow = footerBody.find('tr').filter(function () {

                return String($(this).find('.itemCode').val()) === String(group.itemCode);

            }).first();

            // Find blank row or create new row
            if (!footerRow.length) {

                const blankRow = footerBody.find('tr').filter(function () {

                    return !$(this).find('.itemName').val() &&  !$(this).find('.itemCode').val();

                }).first();

                if (blankRow.length) {

                    footerRow = blankRow;

                }
                else {

                    await addNewRow();
                    footerRow = footerBody.find('tr:last');

                }

            }

            // Item Name
            const itemDropdown = footerRow.find('.itemName');

            itemDropdown.empty();

            const option = new Option( group.itemName,  group.itemCode, true,  true);

            itemDropdown.append(option).trigger('change');

            footerRow.find('.itemCode').val(group.itemCode);

            // Tenacity
            const tenacityDropdown = footerRow.find('.tenacity');

            if (group.tenacityCode !== '' && group.tenacityCode != null) {
                tenacityDropdown.val(String(group.tenacityCode)).trigger('change');
            }

            // Status
            const statusDropdown = footerRow.find('.status');

            if (group.statusCode !== '' && group.statusCode != null) {
                statusDropdown.val(String(group.statusCode)).trigger('change');
            }

            // Party Details
            let partyName = '';
            let partyCode = '';

            group.parties.forEach(party => {

                const name = String(party.name).substring(0, 10);

                partyName += `${name}-${party.qty},`;
                partyCode += `${party.code},`;

            });

            // Footer Quantities
            footerRow.find('.dispatchPlanQty').val(group.totalPlanQty.toFixed(3));

            footerRow.find('.partyName').val(partyName);
            footerRow.find('.partyCode').val(partyCode);

            footerRow.find('.approxAmount').val(group.approxAmount.toFixed(2) );

            // Estimated Production
            const stock = Number(footerRow.find('.stock').val() || 0);

            footerRow.find('.estProduction').val( (stock - group.totalPlanQty).toFixed(3) );

        }

        console.log("All Mode Grouped Footer Data:", [...groupedItems.values()]);
        console.log("All Mode Pending Orders:", selectedPendingOrders);

        // Reset Popup
        $('#selectAllPR').prop('checked', false);
        $('.selectPendingOrder').prop('checked', false);

        bootstrap.Modal.getInstance(document.getElementById('copyFromModal'))?.hide();

        showToast("Pending Orders copied successfully.", { type: "success" });

    }
    catch (error) {
        console.error("Copy Pending Orders Error:", error);
        showToast("Unable to copy pending orders: " + error.message,  { type: "error" } );

    }

}

async function loadFillData() {
    try {
        const vDate = $('#dtItemFooterDate').val();

        if (!vDate) {
            showToast("Please select date.", { type: "warning" });
            return;
        }

        const sortBy = $('input[name="itemSearchType"]:checked').val();

        const response = await fetch(
            `/DispatchPlaning/GetFillData?vDate=${encodeURIComponent(vDate)}&sortBy=${encodeURIComponent(sortBy)}`
        );

        if (!response.ok) {
            throw new Error('Failed to load stock data.');
        }

        const data = await response.json();

        if (!Array.isArray(data) || data.length === 0) {
            showToast("No stock data found.", { type: "info" });
            return;
        }

        const tbody = $('#tblItemNameFooter tbody');

        // Clear existing footer rows
        tbody.empty();

        for (const item of data) {

            // Create new footer row
            await addNewRow();

            const row = tbody.find('tr:last');

            // -------------------------
            // Item Code
            // -------------------------
            row.find('.itemCode').val(item.itemCode);

            // -------------------------
            // Item Name
            // -------------------------
            const itemDropdown = row.find('.itemName');

            const option = new Option( item.itemName, item.itemCode, true, true );

            itemDropdown.append(option).trigger('change');

            // -------------------------
            // Tenacity
            // -------------------------
            if (item.tenacityCode && item.tenacityCode !== "0") {
                row.find('.tenacity').val(item.tenacityCode).trigger('change');
            }

            // -------------------------
            // Status
            // -------------------------
            if (item.statusCode && item.statusCode !== "0") {
                row.find('.status').val(item.statusCode).trigger('change');
            }

            // -------------------------
            // Bale No
            // -------------------------
            row.find('.baleNo').val(item.openQty);

            // -------------------------
            // Stock
            // -------------------------
            row.find('.stock').val(item.stock);

        }

        showToast("Stock data loaded successfully.", { type: "success" });

    }
    catch (error) {
        console.error("Fill Data Error:", error);
        showToast("Unable to load stock data.", { type: "error" });
    }
}

async function resetForm() {
    try {
        copyDataMode = "all";
        const tbody = $('#tblItemNameFooter tbody');
        tbody.empty();
      
        await addNewRow();

        SetCurrentDate();

        $('#rdoItemName').prop('checked', true);

        $('#tblDeliveryOrderPopUp tbody').empty();

        $('#selectAllPR').prop('checked', false);
        $('#chkAppendPR').prop('checked', false);
        $('#searchBoxPR').val('');

        showToast("Form reset successfully.", { type: "success" });

    }
    catch (error) {
        console.error("Reset Form Error:", error);
        showToast("Unable to reset form.", { type: "error" });
    }
}

//===================Save Data ===============================

async function saveDispatchPlanning() {
    try {
        const vNo = $('#txtItemFooterDocNo').val();
        const vDate = $('#dtItemFooterDate').val();

        if (!vNo) {
            showToast("Document No is required.", { type: "warning" });
            return;
        }

        if (!vDate) {
            showToast("Please select date.", { type: "warning" });
            return;
        }

        // Grid Validation
        if (!chkvalinGrid()) {
            return;
        }

        const footerList = [];

        $('#tblItemNameFooter tbody tr').each(function (index) {

            const row = $(this);

            const itemCode = row.find('.itemCode').val();
            const statusCode = row.find('.status').val();
            if (!itemCode) return;

            const tenacityDropdown = row.find('.tenacity');
            const tenacityCode = tenacityDropdown.val();

            footerList.push({
                SNO: index + 1,

                // Item Details
                ITEM_CODE: itemCode,
                TENACITY_CODE: row.find('.tenacity').val() || null,
                STATUS_CODE: statusCode === "" ? null : Number(statusCode),

                // Quantity Details
                PLAN_QTY: row.find('.dispatchPlanQty').val() || 0,
                OPN_QTY: row.find('.baleNo').val() || 0,
                OPN_WGT: row.find('.stock').val() || 0,
                PROD_PLANQTY: row.find('.estProduction').val() || 0,
                DISP_PLANQTY: row.find('.dispatchPlanQty').val() || 0,

                // Party Details
                PARTY_NAME: row.find('.partyName').val(),
                PARTY_CODE: row.find('.partyCode').val() || null,
                TENACITY: row.find('.tenacity option:selected').text() === "Select Tenacity" ? null : row.find('.tenacity option:selected').text(),

                // Amount
                APPROX_AMT: row.find('.approxAmount').val() || 0,

                // Hidden Footer Fields
                DOC_NO: row.find('.docNo').val() || null,
                DOC_DATE: row.find('.docDate').val() || null,

            });
        });

        if (footerList.length === 0) {
            showToast("Please add at least one record.", { type: "warning" });
            return;
        }

        const formData = new FormData();

        formData.append("V_NO", vNo);
        formData.append("V_DATE", vDate);

        footerList.forEach((item, index) => {
            Object.keys(item).forEach(key => {
                formData.append(
                    `FooterList[${index}].${key}`,
                    item[key] ?? ''
                );
            });
        });

        formData.append("CopyDataMode", copyDataMode);

        selectedPendingOrders.forEach((item, index) => {

            Object.keys(item).forEach(key => {

                formData.append(`PendingOrderList[${index}].${key}`, item[key] ?? '' );

            });

        });

        console.log("Save Payload:", [...formData.entries()]);

        const response = await fetch('/DispatchPlaning/SaveAndUpdateData', {
            method: 'POST',
            headers: {
                'Accept': 'application/json'
            },
            body: formData
        });

        const responseText = await response.text();

        console.log("Save Response Status:", response.status);
        console.log("Save Response:", responseText);

        let result;

        try {
            result = JSON.parse(responseText);
        }
        catch (error) {
            console.error("Invalid JSON Response:", responseText);

            throw new Error(
                "Server returned HTML instead of JSON. Check Network tab and server error."
            );
        }

        if (!response.ok) {
            throw new Error(result.message || "Failed to save Dispatch Planning.");
        }

        if (result.success) {
            showToast(result.message, { type: "success" });
            isReadOnly = true; 
            applyReadOnlyMode();
        } else {
            showToast(result.message, { type: "error" });
        }

    }
    catch (error) {
        console.error("Save Dispatch Planning Error:", error);
        showToast(error.message, { type: "error" });
    }
}

//===================Edit Data ===============================

async function loadEditData() {
    try {

        const response = await fetch(
            `/DispatchPlaning/LoadEditData?vNo=${encodeURIComponent(id)}&vType=${encodeURIComponent(vtype)}`
        );

        if (!response.ok) {
            throw new Error("Failed to load Dispatch Planning data.");
        }

        const result = await response.json();
        console.log("Edit data", result);

        if (result.success === false) {
            showToast(result.message || "Record not found.", { type: "error" });
            return;
        }

        const records = Array.isArray(result) ? result : [result];

        if (!records.length || !records[0]) {
            showToast("Record not found.", { type: "warning" });
            return;
        }

        const header = records[0];

        // Header Details
        $('#txtItemFooterDocNo').val(header.V_NO ?? header.v_NO ?? id);

        if (header.V_DATE) {
            $('#dtItemFooterDate').val(
                String(header.V_DATE).split('T')[0]
            );
        }

        // Clear existing footer
        const tbody = $('#tblItemNameFooter tbody');
        tbody.empty();

        // Footer Details
        for (const item of records) {

            await addNewRow();

            const row = tbody.find('tr:last');

            // Item Code & Name
            // Item Code & Name
            const itemCode = item.ITEM_CODE ?? '';
            const itemName = item.ITEM_NAME ?? '';

            row.find('.itemCode').val(itemCode);

            if (itemCode && itemName) {
                const option = new Option(itemName, itemCode, true, true);
                row.find('.itemName').append(option).trigger('change');
            }

            // Tenacity
            const tenacityCode = item.TENACITY_CODE ?? '';

            const tenacityDropdown = row.find('.tenacity');

            if (tenacityCode !== '') {

                const option = tenacityDropdown.find('option').filter(function () {
                    return String($(this).val()) === String(tenacityCode);
                });

                if (option.length) {
                    tenacityDropdown.val(tenacityCode).trigger('change');
                }
            }
            // Status
            const statusCode = item.STATUS_CODE ?? '';

            const statusDropdown = row.find('.status');

            if (statusCode !== '') {

                const option = statusDropdown.find('option').filter(function () {
                    return String($(this).val()) === String(statusCode);
                });

                if (option.length) {
                    statusDropdown.val(statusCode).trigger('change');
                }
            }

            // Other Footer Fields
            row.find('.baleNo').val(item.OPN_QTY ?? '');
            row.find('.stock').val(item.OPN_WGT ?? '');
            row.find('.dispatchPlanQty').val(item.DISP_PLANQTY ?? item.PLAN_QTY ?? '');
            row.find('.partyName').val(item.PARTY_NAME ?? '');
            row.find('.partyCode').val(item.PARTY_CODE ?? '');
            row.find('.estProduction').val(item.PROD_PLANQTY ?? '');
            row.find('.approxAmount').val(item.APPROX_AMT ?? '');

            // Hidden Document Details
            row.find('.docNo').val(item.DOC_NO ?? '');

            if (item.DOC_DATE) {
                row.find('.docDate').val(
                    String(item.DOC_DATE).split('T')[0]
                );
            }
        }

        await loadPendingOrderDetails();

    }
    catch (error) {
        console.error("Load Dispatch Planning Error:", error);
        showToast(error.message, { type: "error" });
    }
}

function applyReadOnlyMode() {
    if (!isReadOnly) return;

    // Disable all input fields and dropdowns
    $('input, select, textarea, button').prop('disabled', true);

    // Action buttons visible rahenge, bas disabled honge
    $('.act-btn').prop('disabled', true);

    // Back button enabled rahega
    $('.erppage-header-back').prop('disabled', false);

    // Modal close button enabled rahega
    $('.erppagesmodal-close').prop('disabled', false);

    // Save button hide
    $('#btn_save').hide();
}

async function loadPendingOrderDetails() {

    try {

        const vNo = $('#txtItemFooterDocNo').val() || id;

        if (!vNo) return;

        const response = await fetch(
            `/DispatchPlaning/GetDispatchPlan2Data?vNo=${encodeURIComponent(vNo)}`
        );

        if (!response.ok) {
            throw new Error("Failed to load Pending Order Details.");
        }

        const data = await response.json();

        const tbody = $('#tblItemPendingOrder tbody');

        tbody.empty();

        if (!Array.isArray(data) || data.length === 0) {
            tbody.append(`
                <tr>
                    <td colspan="8" class="text-center">No records found.</td>
                </tr>
            `);
            return;
        }

        data.forEach(item => {

            const tr = $('<tr>');

            tr.append($('<td>').text(item.v_TYPE ?? ''));
            tr.append($('<td>').text(item.v_NO ?? ''));
            tr.append($('<td>').text(item.vDate ?? ''));
            tr.append($('<td>').text(item.itemName ?? ''));
            tr.append($('<td>').text(item.tenacity ?? ''));
            tr.append($('<td>').text(item.status ?? ''));
            tr.append($('<td>').text(item.partyName ?? ''));
            tr.append($('<td>').text(
                Number(item.planQty ?? 0).toFixed(3)
            ));

            tbody.append(tr);

        });

        console.log("Pending Order Details:", data);

    }
    catch (error) {

        console.error("Load Pending Order Details Error:", error);

        showToast(error.message, { type: "error" });

    }
}

//=================Validate Data ==========================
function chkvalinGrid() {

    const rows = $('#tblItemNameFooter tbody tr').filter(function () {
        return $(this).find('.itemCode').val();
    });

  
    if (typeof pubCompCode !== 'undefined' &&  (Number(pubCompCode) === 2 || Number(pubCompCode) === 5)) {
        return true;
    }

    // 1. Item Name Validation
    for (let i = 0; i < rows.length; i++) {

        const row = $(rows[i]);

        if (!row.find('.itemName').val()) {

            row.find('.itemName').select2('open');

            showToast("Please check, Item name is blank.", { type: "error" });

            return false;
        }
    }

    // 2. Stock Quantity OR Dispatch Plan Quantity Validation
    for (let i = 0; i < rows.length; i++) {

        const row = $(rows[i]);

        const stock = row.find('.stock').val();
        const dispatchPlanQty = row.find('.dispatchPlanQty').val();

        if ((stock === '' || stock == null) && (dispatchPlanQty === '' || dispatchPlanQty == null)) {

            row.find('.stock').focus();

            showToast("Please fill stock quantity or dispatch plan quantity !", { type: "error" });

            return false;
        }
    }

    // 3. Duplicate Item + Tenacity + Status Validation
    for (let i = 0; i < rows.length; i++) {

        const row1 = $(rows[i]);

        for (let j = i + 1; j < rows.length; j++) {

            const row2 = $(rows[j]);

            const itemCode1 = row1.find('.itemCode').val() || '';
            const itemCode2 = row2.find('.itemCode').val() || '';

            const tenacity1 = row1.find('.tenacity').val() || '';
            const tenacity2 = row2.find('.tenacity').val() || '';

            const status1 = row1.find('.status').val() || '';
            const status2 = row2.find('.status').val() || '';

            if ( itemCode1 === itemCode2 &&  tenacity1 === tenacity2 && status1 === status2 ) {

                row2.find('.itemName').select2('open');

                showToast("Duplicate item or tenacity or status! Please check", { type: "error" });

                return false;
            }
        }
    }

    // 4. Total Plan Quantity Validation
    const sumQty = rows.toArray().reduce((total, element) => {

        const row = $(element);

        const itemCode = Number(row.find('.itemCode').val() || 0);

        if (itemCode > 0) {
            total += Number(row.find('.dispatchPlanQty').val() || 0);
        }

        return total;

    }, 0);

    return true;
}

//========================
// REPORT
//========================
async function DispatchPlanningReport() {

    var reportName = "disp_plan";

    var vType = "DISP";
    var vNo = $('#txtItemFooterDocNo').val();

    if (!vType || !vNo) {
        showToast("V Type and V No are required.", { type: "error" });
        return;
    }

    // =========================
    // SELECTION FORMULA
    // =========================

    var SelForMul =
        "{DISPATCH_PLAN.V_NO}=" + vNo +
        " AND {DISPATCH_PLAN.V_TYPE}='" + vType + "'" +
        " AND {DISPATCH_PLAN.COMP_CODE}=" + window.globalVariables.compCode +
        " AND {DISPATCH_PLAN.BRANCH_CODE}=" + window.globalVariables.branchCode +
        " AND {DISPATCH_PLAN.YEAR_CODE}=" + window.globalVariables.yearCode +
        " AND {DISPATCH_PLAN.DISP_PLANQTY}<>0";

    // =========================
    // REPORT DATA
    // =========================

    var formulaFields = {

        Reportname: reportName,

        selectionFormula: SelForMul,

        Database: window.database.db,

        Parameters: {
            comp_name: window.globalVariables.companyName,
            comp_add1: window.globalVariables.add1,
            comp_add2: window.globalVariables.add2,

            RPTNAME: "Dispatch Plan"
        }
    };

    console.log("Dispatch Planning Report Data:", formulaFields);
    console.log("Selection Formula:", SelForMul);

    // =========================
    // TIMESTAMP
    // =========================

    var now = new Date();

    var day = String(now.getDate()).padStart(2, '0');
    var month = String(now.getMonth() + 1).padStart(2, '0');
    var year = String(now.getFullYear()).slice(-2);

    var hours = String(now.getHours()).padStart(2, '0');
    var minutes = String(now.getMinutes()).padStart(2, '0');
    var seconds = String(now.getSeconds()).padStart(2, '0');

    var timestamp = `${day}${month}${year}_${hours}${minutes}${seconds}`;

    // =========================
    // GENERATE REPORT
    // =========================

    $.ajax({

        url: 'http://localhost:34089/Report/PendingQCReport',
        type: 'POST',

        data: JSON.stringify(formulaFields),

        contentType: "application/json",

        xhrFields: {
            responseType: 'blob'
        },

        success: function (response) {

            var file = new Blob(
                [response],
                { type: 'application/pdf' }
            );

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

            console.error('Error generating report:', error);
            showToast("Error generating Dispatch Plan report.", { type: "error" });

        }
    });
}
