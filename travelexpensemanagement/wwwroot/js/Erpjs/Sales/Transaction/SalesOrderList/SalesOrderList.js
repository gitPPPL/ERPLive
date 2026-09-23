let currentPage = 1;
let pageSize = 10;
var controllerName = window.location.pathname.split('/')[1];
let SOPagination;

//Load
$(document).ready(async function () {
    checkPermission(controllerName, function () {
        SOPagination.load();
    });
    SOPagination = Pagination.create({
        pageSize: 10,
        paginationContainer: '#pageNumbers',
        infoContainer: '#pageInfoText',
        loader: function (params) {
            $.ajax({
                url: '/SalesOrderList/GetSalesOrderList',
                type: 'GET',
                dataType: 'json',
                data: {
                    searchTerm: $('#searchBox').val(),
                    pageNumber: params.pageNumber,
                    pageSize: params.pageSize
                },
                success: function (res) {
                    console.log("Sales Order : ", res);
                    params.callback({
                        data: res.data,
                        totalCount: res.totalCount
                    });
                },
                error: function (xhr) {
                    showToast('Error loading data', { type: "error" });
                }
            });
        },
        render: function (docs) {
            const tbody = $('#tblSalesOrderList tbody');
            tbody.empty();
            if (!docs.length) {
                tbody.append(`<tr><td colspan="6" class="text-center text-muted">No list found.</td></tr>`);
                return;
            }

            $.each(docs, function (index, doc) {
                tbody.append(`
				<tr>
							<td>${doc.v_NO || ''}</td>
							<td>${formatDate(doc.v_DATE)}</td>
							<td>${doc.partyName || ''}</td>
							<td>${doc.deliverY_PERIOD || ''}</td>
							<td>${doc.deliverY_TO || ''}</td>
							<td>${doc.saudA_NO || ''}</td>
							<td class="action-col">
							  <div class="action-icons">
								  <button class="act-btn edit btn-edit permission-edit" title="Edit" style="cursor:pointer;" title="Edit" onclick="checkPurchaseEditStatus('${doc.v_NO}', '${doc.v_TYPE}', '${doc.v_DATE}')"><i class="fa fa-edit"></i></button>
								  <button class="act-btn view btn-view" title="View" style="cursor:pointer;" onclick="viewSalesOrder('${doc.v_NO}', '${doc.v_TYPE}')"><i class="fa fa-eye"></i></button>
								  <button class="act-btn delete btn-delete  permission-delete" title="Delete" style="cursor:pointer;" onclick="deleteSalesOrder('${doc.v_NO}', '${doc.v_TYPE}')"><i class="fa fa-trash"></i></button>
								  <button class="act-btn document btn-document" title="document" style="cursor:pointer;" onclick="showDocumentPopup('${doc.v_NO}')"><i class="fa fa-file-alt"></i></button>
							  </div>
							</td>
						</tr>
				`);
                applyGridPermission();
            });

        }
    });
    // First Load
    SOPagination.load();
    // Search
    $('#searchBox').keyup(function () {
        SOPagination.load();
    });
});

// Page Size Change
function changeRowsPerPage() {
    SOPagination.setPageSize(parseInt($('#pageSizeSelect').val()));
    SOPagination.load();
}
// Edit
function editSalesOrder(docCode, docType) {
    window.location.href = `/SalesOrder/Index?id=${encodeURIComponent(docCode)}&vtype=${encodeURIComponent(docType)}`;
}


async function checkPurchaseEditStatus(vNo, vType, vDate) {
    try {
        const response = await fetch(
            `/SalesOrderList/GetSalesEditStatus?vType=${encodeURIComponent(vType)}&vNo=${vNo}`
        );

        const result = await response.json();

        if (!result.success) {
            showToast(result.message, {type:"error"});
            return false;
        }

        const data = result.data;

        if (data.isApprovalInProcess) {
            showToast("This Document Approval is in process at User: " + data.approvalUser + ".", {type:"warning"});
            return false;
        }

        const isApprovalBody = data.isFinalApprovalBody;

        if (!isApprovalBody) {
            checkModificationAllowed(vDate, vNo, vType)
        }

        return true;
    }
    catch (error) {
        alert("Error while checking edit status.");
        return false;
    }
}

function checkModificationAllowed(vDate, rowId, vType) {
    checkModificationDays({
        controller: 'SalesOrderList',
        vDate: vDate,
        rowId: rowId,
        onAllowed: function (rowId) {
            editSalesOrder(rowId, vType);
        }
    })
}


//View
function viewSalesOrder(docCode, docType) {
    window.location.href = `/SalesOrder/Index?id=${encodeURIComponent(docCode)}&vtype=${encodeURIComponent(docType)}&readOnly=true`;
}

//Delete
async function deleteSalesOrder(docCode, docType) {

    if (!await checkSaleInvoiceBeforeDelete(docType, docCode)) {
        return;
    }

    deleteRecordbytype("SalesOrderList", docCode, docType, {
        action: "DeleteSalesOrderEntry",
        text: "This will permanently delete the Sales Order Details.",
        successCallback: SOPagination.load
    });
}

async function checkSaleInvoiceBeforeDelete(vType, vNo) {
    try {
        const response = await $.ajax({
            url: '/SalesOrderList/CheckSaleInvoiceBeforeDelete',
            type: 'GET',
            data: {
                vType: vType,
                vNo: vNo
            }
        });

        if (!response.status) {
            showToast(response.message || 'Unable to check sale invoice.', {type:"error"});
            return false;
        }

        if (response.exists) {
            await Swal.fire({
                icon: 'warning',
                title: 'Warning',
                text: `This document exists in Sale Invoice No : ${response.billNo} dated : ${response.billDate}`,
                confirmButtonText: 'OK'
            });

            return false;
        }

        return true;
    }
    catch (error) {
        console.error(error);
        showToast('Failed to check sale invoice.', {type:"error"});
        return false;
    }
}

//Format Date
function formatDate(dateStr) {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    if (isNaN(date)) return '';
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}

//Excel
const btn = document.getElementById("button_export");
if (btn) {
    btn.addEventListener("click", function (e) {
        e.preventDefault();
        window.location.href = "/SalesOrderList/ExportAllDocs";
    });
}

//Document Details Pop Up
function showDocumentPopup(docCode) {
    $.ajax({
        url: '/SalesOrderList/GetSalesOrderEntryDetails',
        type: 'Get',
        dataType: 'json',
        data: { docid: docCode },
        success: function (response) {
            if (response.status) {
                console.log(response.data);
                showDocumentPopupjQuery(response.data, docCode);
            } else {
                toastr.error("Failed to get document details.");
            }
        },
        error: function () {
            toastr.error("An error occurred while fetching document details.");
        }
    });
}
