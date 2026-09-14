let currentPage = 1;
let pageSize = 10;
var controllerName = window.location.pathname.split('/')[1];
let SCLPagination;

//Load
$(document).ready(async function () {
    checkPermission(controllerName, function () {
        SCLPagination.load();
    });
    SCLPagination = Pagination.create({
        pageSize: 10,
        paginationContainer: '#pageNumbers',
        infoContainer: '#pageInfoText',
        loader: function (params) {
            $.ajax({
                url: '/SalesCreditLimitList/GetAllSalesCreditLimitList',
                type: 'GET',
                dataType: 'json',
                data: {
                    searchTerm: $('#searchBox').val(),
                    pageNumber: params.pageNumber,
                    pageSize: params.pageSize
                },
                success: function (res) {
                    console.log("Sales Credit Limit : ", res);
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
            const tbody = $('#tblSalesCreditLimitList tbody');
            tbody.empty();
            if (!docs.length) {
                tbody.append(`<tr><td colspan="8" class="text-center text-muted">No list found.</td></tr>`);
                return;
            }

            $.each(docs, function (index, doc) {
                tbody.append(`
				<tr>
							<td>${doc.v_NO || ''}</td>
							<td>${formatDate(doc.v_DATE)}</td>
							<td>${doc.partY_NAME || ''}</td>
							<td>${doc.cR_LIMIT || ''}</td>
							<td>${doc.cR_DAYS || ''}</td>
							<td>${doc.ourcR_DAYS || ''}</td>
							<td>${doc.remarks || ''}</td>
							<td class="action-col">
							  <div class="action-icons">
								  <button class="act-btn edit btn-edit permission-edit" title="Edit" style="cursor:pointer;" title="Edit" onclick="GetAppStatus('${doc.v_NO}')"><i class="fa fa-edit"></i></button>
								  <button class="act-btn view btn-view" title="View" style="cursor:pointer;" onclick="viewSalesExportCosting('${doc.v_NO}')"><i class="fa fa-eye"></i></button>
								  <button class="act-btn delete btn-delete  permission-delete" title="Delete" style="cursor:pointer;" onclick="deleteSalesExportCosting('${doc.v_NO}')"><i class="fa fa-trash"></i></button>
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
    SCLPagination.load();
    // Search
    $('#searchBox').keyup(function () {
        SCLPagination.load();
    });
});

// Page Size Change
function changeRowsPerPage() {
    SCLPagination.setPageSize(parseInt($('#pageSizeSelect').val()));
    SCLPagination.load();
}

// Edit
function editSalesExportCosting(docCode) {
    window.location.href = `/SalesCreditLimitEntry/Index?id=${encodeURIComponent(docCode)}`;
}

//View
function viewSalesExportCosting(docCode) {
    window.location.href = `/SalesCreditLimitEntry/Index?id=${encodeURIComponent(docCode)}&readOnly=true`;
}

//Delete
async function deleteSalesExportCosting(docId) {
    deleteRecordbytype("SalesCreditLimitList", docId, {
        action: "Delete",
        text: "This will permanently delete the Sales Credit Limit Details.",
        successCallback: SCLPagination.load
    });
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
        window.location.href = "/SalesCreditLimitList/ExportAllDocs";
    });
}

//Document Details Pop Up
function showDocumentPopup(docCode) {
    $.ajax({
        url: '/SalesCreditLimitList/PBPEntryDetails',
        type: 'Get',
        dataType: 'json',
        data: { vNo: docCode },
        success: function (response) {
            if (response.status) {
                showDocumentPopupjQuery(response.data, docCode);
            } else {
                showToast("Failed to get document details.", { type: "error" });
            }
        },
        error: function () {
            showToast("An error occurred while fetching document details.", { type: "error" });
        }
    });
}

async function GetAppStatus(vNo) {
    const response = await fetch(`/SalesCreditLimitList/CheckApprovalStatus?vNo=${encodeURIComponent(vNo)}`, {
        method: 'GET'
    });

    if (!response.ok) {
        throw new Error(`Approval status check failed: ${response.status}`);
    }

    const data = await response.json(); // { isBlocked, message }
    if (!data.isBlocked) {
        editSalesExportCosting(vNo);
    }
    else {
        showToast(data.message, { type: "warning" });
    }
}