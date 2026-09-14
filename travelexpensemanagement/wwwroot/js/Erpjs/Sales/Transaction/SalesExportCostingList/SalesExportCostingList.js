let currentPage = 1;
let pageSize = 10;
var controllerName = window.location.pathname.split('/')[1];
let SECPagination;

//Load
$(document).ready(async function () {
    checkPermission(controllerName, function () {
        SECPagination.load();
    });
    SECPagination = Pagination.create({
        pageSize: 10,
        paginationContainer: '#pageNumbers',
        infoContainer: '#pageInfoText',
        loader: function (params) {
            $.ajax({
                url: '/SalesExportCostingList/GetAllSalesExportCostingList',
                type: 'GET',
                dataType: 'json',
                data: {
                    searchTerm: $('#searchBox').val(),
                    pageNumber: params.pageNumber,
                    pageSize: params.pageSize
                },
                success: function (res) {
                    console.log("Sa,es Export Costing : ", res);
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
            const tbody = $('#tblSalesExportCostingList tbody');
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
							<td>${doc.partY_NAME || ''}</td>
							<td>${doc.deliverY_AT || ''}</td>
							<td>${doc.agenT_NAME || ''}</td>
							<td class="action-col">
							  <div class="action-icons">
								  <button class="act-btn edit btn-edit permission-edit" title="Edit" style="cursor:pointer;" title="Edit" onclick="editSalesExportCosting('${doc.v_NO}')"><i class="fa fa-edit"></i></button>
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
    SECPagination.load();
    // Search
    $('#searchBox').keyup(function () {
        SECPagination.load();
    });
});

// Page Size Change
function changeRowsPerPage() {
    SECPagination.setPageSize(parseInt($('#pageSizeSelect').val()));
    SECPagination.load();
}
// Edit
function editSalesExportCosting(docCode) {
    window.location.href = `/SalesExportCosting/Index?id=${encodeURIComponent(docCode)}`;
}

//View
function viewSalesExportCosting(docCode) {
    window.location.href = `/SalesExportCosting/Index?id=${encodeURIComponent(docCode)}&readOnly=true`;
}

//Delete
async function deleteSalesExportCosting(docId) {
    deleteRecordbytype("SalesExportCostingList", docId, {
        action: "Delete",
        text: "This will permanently delete the Sales Export Costing Details.",
        successCallback: SECPagination.load
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
        window.location.href = "/SalesExportCostingList/ExportAllDocs";
    });
}

//Document Details Pop Up
function showDocumentPopup(docCode) {
    $.ajax({
        url: '/SalesExportCostingList/PBPEntryDetails',
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

