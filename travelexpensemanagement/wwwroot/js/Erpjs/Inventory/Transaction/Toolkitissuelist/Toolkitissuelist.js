let currentPage = 1;
let pageSize = 10;
var controllerName = window.location.pathname.split('/')[1];
let TIPagination;
$(document).ready(async function () {
    checkPermission(controllerName, function () {
        TIPagination.load();
    });
    TIPagination = Pagination.create({
        pageSize: 10,
        paginationContainer: '#pageNumbers',
        infoContainer: '#pageInfoText',
        loader: function (params) {
            $.ajax({
                url: '/ToolkitIssuelist/GetAllToolkitIssue',
                type: 'GET',
                dataType: 'json',
                data: {
                    searchTerm: $('#searchBox').val(),
                    pageNumber: params.pageNumber,
                    pageSize: params.pageSize
                },
                success: function (res) {
                    console.log("Toolkit issue list : ", res);
                    params.callback({
                        data: res.data,
                        totalCount: res.totalcount
                    });
                },
                error: function (xhr) {
                    showToast('Error loading data', { type: "error" });
                }
            });
        },
        render: function (docs) {
            const tbody = $('#tbltoolkitissueList tbody');
            tbody.empty();
            if (!docs.length) {
                tbody.append(`<tr><td colspan="8" class="text-center text-muted">No list found.</td></tr>`);
                return;
            }

            $.each(docs, function (index, doc) {
                tbody.append(`
				<tr>
							<td>${doc.v_TYPE || ''}</td>
							<td>${doc.v_NO || ''}</td>
							<td>${formatDate(doc.v_DATE)}</td>
							<td>${doc.iteM_CODE || ''}</td>
							<td>${doc.iteM_NAME || ''}</td>
							<td>${doc.emP_CODE || ''}</td>
							<td>${doc.emP_NAME || ''}</td>
							<td class="action-col">
							  <div class="action-icons">
								  <button class="act-btn edit btn-edit permission-edit" title="Edit" style="cursor:pointer;" title="Edit" onclick="checkModificationAllowed('${doc.v_DATE}', '${doc.v_NO}', '${doc.v_TYPE}')"><i class="fa fa-edit"></i></button>
								  <button class="act-btn view btn-view" title="View" style="cursor:pointer;" onclick="viewToolkitIssueEntry('${doc.v_NO}', '${doc.v_TYPE}')"><i class="fa fa-eye"></i></button>
								  <button class="act-btn delete btn-delete  permission-delete" title="Delete" style="cursor:pointer;" onclick="deleteToolkitIssueEntry('${doc.v_NO}', '${doc.v_TYPE}')"><i class="fa fa-trash"></i></button>
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
    TIPagination.load();
    // Search
    $('#searchBox').keyup(function () {
        TIPagination.load();
    });
});

async function checkModificationAllowed(vDate, rowId, vType) {
    checkModificationDays({
        controller: 'ToolkitIssuelist',
        vDate: vDate,
        rowId: rowId,
        vType: vType,
        onAllowed: function (rowId) {
            editToolkitIssueEntry(rowId, vType);
        }
    })

}

function editToolkitIssueEntry(docCode, docType) {
    window.location.href = `/ToolkitIssueEntry/Index?id=${encodeURIComponent(docCode)}&vtype=${encodeURIComponent(docType)}`;
}

function viewToolkitIssueEntry(docCode, docType) {
    window.location.href = `/ToolkitIssueEntry/Index?id=${encodeURIComponent(docCode)}&vtype=${encodeURIComponent(docType)}&readOnly=true`;
}

function deleteToolkitIssueEntry(docId, docType) {
    deleteRecordbytype("ToolkitIssueList", docId, docType, {
        action: "Delete",
        text: "This will permanently delete the Toolkit Issue details.",
        successCallback: TIPagination.load
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
        window.location.href = "/ToolkitIssuelist/ExportAllDocs";
    });
}

function showDocumentPopup(docCode, docType) {
    $.ajax({
        url: '/ToolkitIssuelist/PBPEntryDetails',
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
