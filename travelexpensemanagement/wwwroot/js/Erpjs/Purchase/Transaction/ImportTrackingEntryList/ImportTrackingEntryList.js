let currentPage = 1;
let pageSize = 10;
let ITEPagination;

$(document).ready(async function () {
    ITEPagination = Pagination.create({
        pageSize: 10,
        paginationContainer: '#pageNumbers',
        infoContainer: '#pageInfoText',
        loader: function (params) {
            $.ajax({
                url: '/ImportTrackingEntryList/GetAllImportTrackingList',
                type: 'GET',
                dataType: 'json',
                data: {
                    searchTerm: $('#searchBox').val(),
                    pageNumber: params.pageNumber,
                    pageSize: params.pageSize
                },
                success: function (res) {
                    params.callback({
                        data: res.importList,
                        totalCount: res.totalCount
                    });
                },
                error: function (xhr) {
                    showToast('Error loading data', { type: "error" });
                }
            });
        },
        render: function (docs) {
            const tbody = $('#tblImportTrackingEntry tbody');
            tbody.empty();
            if (!docs.length) {
                tbody.append(`<tr><td colspan="8" class="text-center text-muted">No list found.</td></tr>`);
                return;
            }

            $.each(docs, function (index, doc) {
                tbody.append(`
				<tr>
							<td style="display:none;">${doc.code || doc.doC_CODE || ''}</td>
							<td>${doc.v_NO || ''}</td>
							<td>${formatDate(doc.v_DATE)}</td>
							<td>${doc.saudA_NO || ''}</td>
							<td>${formatDate(doc.saudA_DATE) }</td>
							<td>${doc.supplier || ''}</td>
							<td>${doc.location || ''}</td>
							<td>${doc.invoicE_NO || ''}</td>
							<td class="action-col">
							  <div class="action-icons">
								  <button class="act-btn edit btn-edit permission-edit" title="Edit" style="cursor:pointer;" title="Edit" onclick="editImportTrackingEntry('${doc.v_NO}', '${doc.v_TYPE}')"><i class="fa fa-edit"></i></button>
								  <button class="act-btn view btn-view" title="View" style="cursor:pointer;" onclick="viewImportTrackingEntry('${doc.v_NO}', '${doc.v_TYPE}')"><i class="fa fa-eye"></i></button>
								  <button class="act-btn delete btn-delete  permission-delete" title="Delete" style="cursor:pointer;" onclick="deleteImportTrackingEntry('${doc.v_NO}', '${doc.v_TYPE}')"><i class="fa fa-trash"></i></button>
								  <button class="act-btn document btn-document" title="document" style="cursor:pointer;" onclick="showDocumentPopup('${doc.v_NO}', '${doc.v_TYPE}')"><i class="fa fa-file-alt"></i></button>
							  </div>
							</td>
						</tr>
				`);
            });

        }
    });
    // First Load
    ITEPagination.load();
    // Search
    $('#searchBox').keyup(function () {
        ITEPagination.load();
    });
});

// Page Size Change
function changeRowsPerPage() {
    ITEPagination.setPageSize(parseInt($('#pageSizeSelect').val()));
    ITEPagination.load();
}

// Edit
function editImportTrackingEntry(vNo, vType) {
    window.location.href = `/ImportTrackingEntry/Index?id=${encodeURIComponent(vNo)}&vtype=${encodeURIComponent(vType)}`;
}

// View
function viewImportTrackingEntry(docCode, docType) {
    window.location.href = `/ImportTrackingEntry/Index?id=${encodeURIComponent(docCode)}&vtype=${encodeURIComponent(docType)}&readOnly=true`;
}

// Delete
function deleteImportTrackingEntry(docId, docType) {
    deleteRecordbytype("ImportTrackingEntryList", docId, docType, {
        action: "Delete",
        text: "This will permanently delete the Import Tracking Entry details.",
        successCallback: ITEPagination.load
    });
}

function formatDate(dateStr) {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    if (isNaN(date)) return '';
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}

const btn = document.getElementById("button_export");

if (btn) {
    btn.addEventListener("click", function (e) {
        e.preventDefault();
        window.location.href = "/ImportTrackingEntryList/ExportAllDocs";
    });
}

function showDocumentPopup(docCode, docType) {
    $.ajax({
        url: '/ImportTrackingEntryList/ITEntryDetails',
        type: 'Get',
        dataType: 'json',
        data: { vNo: docCode, vType: docType },
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

