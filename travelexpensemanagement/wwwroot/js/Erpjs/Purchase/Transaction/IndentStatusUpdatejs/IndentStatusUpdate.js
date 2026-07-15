
let supplierList = [];

$(document).ready(async function () {

    bindDropdownNew('IndentStatusUpdate', 'SupplierName', '#txtSupplierName', '-- Select Supplier --');

    $('#btnLoaddata').on('click', function () {
        loadStorePurchaseOrderStatus();
    });

    $('#tblStorePurchaseOrderStatus').on('click', '.delete', function () {

        $(this).closest('tr').remove();
 
        if ($('#tblStorePurchaseOrderStatus tbody tr').length === 0) {
            $('#tblStorePurchaseOrderStatus tbody').html(`
                <tr>
                    <td colspan="15" class="text-center">
                        No Record Found
                    </td>
                </tr>
            `);
        }

    });

    $('#btnSave').click(async function () {

        const saveData = [];

        $('#tblStorePurchaseOrderStatus tbody tr').each(function () {

            const row = $(this);

            saveData.push({
                vNo: parseInt(row.attr('data-vno')),
                vDate: row.attr('data-vdate'),
                itemCode: parseInt(row.attr('data-itemcode')),
                sno: parseInt(row.attr('data-sno')),
                dispThrough: row.find('.dispThrough').val(),
                dispRef: row.find('.dispRef').val(),
                dispRemarks: row.find('.dispRemarks').val()
            });

        });

        if (saveData.length === 0) {
            showToast("No Data to update.", { type: "warning" });
            return;
        }

        try {

            const response = await $.ajax({
                url: '/IndentStatusUpdate/SaveIndentStatus',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(saveData)
            });

            if (response.success) {
                showToast(response.message, { type: "success" });
            }
            else {
                showToast(response.message, { type: "warning" });
            }

        }
        catch (ex) {
            console.log(ex);
            showToast("Error while saving.", { type: "error" });
        }

    });
    
});

async function loadStorePurchaseOrderStatus() {

    try {
       
        const fromDate = $('#Dtfromdate').val();
        const toDate = $('#Dttodate').val();
        const supplierCode = $('#hdnSupplierCode').val() || 0;

        if (!fromDate || !toDate) {
            showToast("Please select From Date and To Date", { type: "warning" });
            return;
        }

        if (!isValidDate(fromDate) || !isValidDate(toDate)) {
            showToast("Please enter a valid date.", { type: "warning" });
            return;
        }

        const response = await $.ajax({
            url: '/IndentStatusUpdate/GetStorePurchaseOrderStatus',
            type: 'GET',
            data: {
                fromDate: fromDate,
                toDate: toDate,
                supplierCode: supplierCode
            }
        });
        
        if (response.success) {
            bindStorePurchaseOrderStatus(response.data);
        }
        else {
            showToast(response.message, { type: "warning" });
        }

    }
    catch (ex) {
        console.error(ex);
        showToast("Error loading data : " + ex, { type: "error" });
    }

}

function bindStorePurchaseOrderStatus(data) {

    const tbody = $('#tblStorePurchaseOrderStatus tbody');
    tbody.empty();

    if (!data || data.length === 0) {

        tbody.append(`
            <tr>
                <td colspan="15" class="text-center">
                    No Record Found
                </td>
            </tr>
        `);
        showToast("No Record Found", { type: "warning" });
        return;
    }

    $.each(data, function (i, item) {

        tbody.append(`
            <tr
                data-vno="${item.vNo}"
                data-vdate="${item.vDate}"
                data-itemcode="${item.itemCode}"
                data-sno="${item.sno}">

                <td class="hidden-col">${item.itemCode}</td>

                <td>${item.vType}</td>

                <td>${item.vNo}</td>

                <td>${formatDate(item.vDate)}</td>

                <td>${item.partyName}</td>

                <td>${item.itemCode}</td>

                <td class="hidden-col">${item.sno}</td>

                <td>${item.itemName}</td>

                <td class="text-end">${item.qty}</td>

                <td class="text-end">${item.recdQty}</td>

                <td class="text-end">${item.balQty}</td>

                <td>
                    <input type="text" class="form-control dispThrough" value="${item.dispThrough ?? ''}">
                </td>

                <td>
                    <input type="text" class="form-control dispRef" value="${item.dispRef ?? ''}">
                </td>

                <td>
                    <input type="text" class="form-control dispRemarks" value="${item.dispRemarks ?? ''}">
                </td>

                <td class="action-col">
                    <div class="action-wrap">
                        <button class="act-btn delete">
                            <i class="fa fa-trash"></i>
                        </button>
                    </div>
                </td>

            </tr>
        `);

    });

}

function formatDate(dateString) {

    if (!dateString)
        return '';

    const date = new Date(dateString);

    return date.toLocaleDateString('en-GB');
}

function isValidDate(dateStr) {

    if (!dateStr)
        return false;

    const parts = dateStr.split(/[\/-]/);

    if (parts.length !== 3)
        return false;

    let day, month, year;

    if (parts[0].length === 4) {
        year = parseInt(parts[0], 10);
        month = parseInt(parts[1], 10);
        day = parseInt(parts[2], 10);
    }
    else {
        day = parseInt(parts[0], 10);
        month = parseInt(parts[1], 10);
        year = parseInt(parts[2], 10);
    }

    const date = new Date(year, month - 1, day);

    return date.getFullYear() === year &&
        date.getMonth() === month - 1 &&
        date.getDate() === day;
}