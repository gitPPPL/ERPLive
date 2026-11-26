let currentPage = 1;
let pageSize = 10;
var controllerName = window.location.pathname.split('/')[1];
let PSPagination;

//Load
$(document).ready(async function () {
    checkPermission(controllerName, function () {
        PSPagination.load();
    });
    PSPagination = Pagination.create({
        pageSize: 10,
        paginationContainer: '#pageNumbers',
        infoContainer: '#pageInfoText',
        loader: function (params) {
            $.ajax({
                url: '/PackingSlipEntryList/GetPackingSlipList',
                type: 'GET',
                dataType: 'json',
                data: {
                    searchTerm: $('#searchBox').val(),
                    pageNumber: params.pageNumber,
                    pageSize: params.pageSize
                },
                success: function (res) {
                    console.log("Packing Slip List: ", res);
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
            const tbody = $('#tblPackingSlipList tbody');
            tbody.empty();
            if (!docs.length) {
                tbody.append(`<tr><td colspan="11" class="text-center text-muted">No list found.</td></tr>`);
                return;
            }

            $.each(docs, function (index, doc) {
                tbody.append(`
				<tr>
							<td>${doc.v_NO || ''}</td>
							<td>${doc.v_TYPE || ''}</td>
							<td>${formatDate(doc.v_DATE)}</td>
							<td>${doc.party || ''}</td>
							<td>${doc.place || ''}</td>
							<td>${doc.shift || ''}</td>
							<td>${doc.sliP_NO || ''}</td>
							<td>${doc.porD_TYPE || ''}</td>
							<td>${doc.remarks || ''}</td>
							<td>${doc.status || ''}</td>
							<td class="action-col">
							  <div class="action-icons">
								  <button class="act-btn edit btn-edit permission-edit" title="Edit" style="cursor:pointer;" title="Edit" onclick="checkModificationAllowed('${doc.v_DATE}', '${doc.v_NO}', '${doc.v_TYPE}')"><i class="fa fa-edit"></i></button>
								  <button class="act-btn view btn-view" title="View" style="cursor:pointer;" onclick="viewPackingSlip('${doc.v_NO}', '${doc.v_TYPE}')"><i class="fa fa-eye"></i></button>
								  <button class="act-btn delete btn-delete  permission-delete" title="Delete" style="cursor:pointer;" onclick="deletePackingSlip('${doc.v_NO}', '${doc.v_TYPE}')"><i class="fa fa-trash"></i></button>
								  <button class="act-btn document btn-document" title="document" style="cursor:pointer;" onclick="showDocumentPopup('${doc.v_NO}', '${doc.v_TYPE}')"><i class="fa fa-file-alt"></i></button>
							  </div>
							</td>
						</tr>
				`);
                applyGridPermission();
            });
        }
    });
    // First Load
    PSPagination.load();
    // Search
    $('#searchBox').keyup(function () {
        PSPagination.load();
    });
});

// Page Size Change
function changeRowsPerPage() {
    PSPagination.setPageSize(parseInt($('#pageSizeSelect').val()));
    PSPagination.load();
}

// Edit
function editPackingSlip(docCode, docType) {
    window.location.href = `/PackingSlipEntry/Index?id=${encodeURIComponent(docCode)}&vtype=${encodeURIComponent(docType)}`;
}
function checkModificationAllowed(vDate, rowId, vType) {
    checkModificationDays({
        controller: 'PurchaseRequest',
        vDate: vDate,
        rowId: rowId,
        onAllowed: function (rowId) {
            editPackingSlip(rowId, vType);
        }
    })
}

//View
function viewPackingSlip(docCode, docType) {
    window.location.href = `/PackingSlipEntry/Index?id=${encodeURIComponent(docCode)}&vtype=${encodeURIComponent(docType)}&readOnly=true`;
}

//Delete
async function deletePackingSlip(docCode, docType) {
    deleteRecordbytype("PackingSlipEntryList", docCode, docType, {
        action: "Delete",
        text: "This will permanently delete the Packing Slip Details.",
        successCallback: PSPagination.load
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
        window.location.href = "/PackingSlipEntryList/ExportAllDocs";
    });
}

//Document Details Pop Up
function showDocumentPopup(vNo, vType) {
    $.ajax({
        url: '/PackingSlipEntryList/GetSalesEntryEntryDetails',
        type: 'Get',
        dataType: 'json',
        data: { vNo: vNo, vType: vType },
        success: function (response) {
            if (response.status) {
                console.log(response.data);
                showDocumentPopupjQuery(response.data, vNo);
            } else {
                showToast("Failed to get document details.", { type: "error" });
            }
        },
        error: function () {
            showToast("An error occurred while fetching document details.", { type: "error" });
        }
    });
}


