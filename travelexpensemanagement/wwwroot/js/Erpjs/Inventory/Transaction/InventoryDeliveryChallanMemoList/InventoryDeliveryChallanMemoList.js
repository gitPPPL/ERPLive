let currentPage = 1;
let pageSize = 10;
var controllerName = window.location.pathname.split('/')[1];
let DCMPagination;

$(document).ready(async function () {
    checkPermission(controllerName, function () {
        DCMPagination.load();
    });
    DCMPagination = Pagination.create({
        pageSize: 10,
        paginationContainer: '#pageNumbers',
        infoContainer: '#pageInfoText',
        loader: function (params) {
            $.ajax({
                url: '/InventoryDeliveryChallanMemoList/GetAllDeliveryMemoList',
                type: 'GET',
                dataType: 'json',
                data: {
                    searchTerm: $('#searchBox').val(),
                    pageNumber: params.pageNumber,
                    pageSize: params.pageSize
                },
                success: function (res) {
                    console.log("Delivery Challan Memo list : ", res);
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
            const tbody = $('#tblInventoryDeliveryChallanMemoList tbody');
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
							<td>${doc.emP_NAME || ''}</td>
							<td>${doc.vendoR_NAME || ''}</td>
							<td>${doc.transporT_NAME || ''}</td>
							<td>${doc.through || ''}</td>
							<td>${doc.remarks || ''}</td>
							<td class="action-col">
							  <div class="action-icons">
								  <button class="act-btn edit btn-edit permission-edit" title="Edit" style="cursor:pointer;" title="Edit" onclick="checkModificationAllowed('${doc.v_DATE}', '${doc.v_NO}')"><i class="fa fa-edit"></i></button>
								  <button class="act-btn view btn-view" title="View" style="cursor:pointer;" onclick="viewDeliveryChallanMemo('${doc.v_NO}')"><i class="fa fa-eye"></i></button>
								  <button class="act-btn delete btn-delete  permission-delete" title="Delete" style="cursor:pointer;" onclick="deleteDeliveryChallanMemo('${doc.v_NO}')"><i class="fa fa-trash"></i></button>
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
    DCMPagination.load();
    // Search
    $('#searchBox').keyup(function () {
        DCMPagination.load();
    });
});

// Page Size Change
function changeRowsPerPage() {
    DCMPagination.setPageSize(parseInt($('#pageSizeSelect').val()));
    DCMPagination.load();
}
async function checkModificationAllowed(vDate, rowId) {
    checkModificationDays({
        controller: 'InventoryDeliveryChallanMemoList',
        vDate: vDate,
        rowId: rowId,
        onAllowed: function (rowId) {
            editDeliveryChallanMemo(rowId);
        }
    })

}

function editDeliveryChallanMemo(docCode) {
    window.location.href = `/InventoryDeliveryChallanMemo/Index?id=${encodeURIComponent(docCode)}`;
}

function viewDeliveryChallanMemo(docCode) {
    window.location.href = `/InventoryDeliveryChallanMemo/Index?id=${encodeURIComponent(docCode)}&readOnly=true`;
}

async function deleteDeliveryChallanMemo(docId) {
    deleteRecordbytype("InventoryDeliveryChallanMemoList", docId, {
        action: "Delete",
        text: "This will permanently delete the Delivery Challan Memo Details.",
        successCallback: DCMPagination.load
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
        window.location.href = "/InventoryDeliveryChallanMemoList/ExportAllDocs";
    });
}

function showDocumentPopup(docCode) {
    $.ajax({
        url: '/InventoryDeliveryChallanMemoList/PBPEntryDetails',
        type: 'Get',
        dataType: 'json',
        data: { vNo: docCode},
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

