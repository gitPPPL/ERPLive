let currentPage = 1;
let pageSize = 10;
var controllerName = window.location.pathname.split('/')[1];
let DCIPagination;

$(document).ready(async function () {
    checkPermission(controllerName, function () {
        DCIPagination.load();
    });
    DCIPagination = Pagination.create({
        pageSize: 10,
        paginationContainer: '#pageNumbers',
        infoContainer: '#pageInfoText',
        loader: function (params) {
            $.ajax({
                url: '/DeliveryChallanStoreList/GetAllDeliveryChallanList',
                type: 'GET',
                dataType: 'json',
                data: {
                    searchTerm: $('#searchBox').val(),
                    pageNumber: params.pageNumber,
                    pageSize: params.pageSize
                },
                success: function (res) {
                    console.log("Delivery Challan list : ", res);
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
            const tbody = $('#tblDeliverychallanstoreList tbody');
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
							<td>${doc.bilL_NO || ''}</td>
							<td>${formatDate(doc.bilL_DATE)}</td>
							<td>${doc.crName || ''}</td>
							<td class="action-col">
							  <div class="action-icons">
								  <button class="act-btn edit btn-edit permission-edit" title="Edit" style="cursor:pointer;" title="Edit" onclick="edit('${doc.v_DATE}', '${doc.v_NO}', '${doc.v_TYPE}')"><i class="fa fa-edit"></i></button>
								  <button class="act-btn view btn-view" title="View" style="cursor:pointer;" onclick="viewDeliveryChallanStore('${doc.v_NO}', '${doc.v_TYPE}')"><i class="fa fa-eye"></i></button>
								  <button class="act-btn delete btn-delete  permission-delete" title="Delete" style="cursor:pointer;" onclick="deleteDeliveryChallanStore('${doc.v_NO}', '${doc.v_TYPE}')"><i class="fa fa-trash"></i></button>
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
    DCIPagination.load();
    // Search
    $('#searchBox').keyup(function () {
        DCIPagination.load();
    });
});

// Page Size Change
function changeRowsPerPage() {
    DCIPagination.setPageSize(parseInt($('#pageSizeSelect').val()));
    DCIPagination.load();
}

async function edit(vDate, rowId, vType) {
    const isInApproval = await checkAppovStatus(rowId, vType);

    if (!isInApproval) {
        showToast("This Document is already in Approval process!", { type: "warning" });
        return;
    }
    checkModificationDays({
        controller: 'DeliveryChallanStoreList',
        vDate: vDate,
        rowId: rowId,
        vType: vType,
        onAllowed: function (rowId) {
            editDeliveryChallanStore(rowId, vType);
        }
    })
}

async function checkModificationAllowed(vDate, rowId, vType) {
    checkModificationDays({
        controller: 'DeliveryChallanStoreList',
        vDate: vDate,
        rowId: rowId,
        vType: vType,
        onAllowed: function (rowId) {
            editDeliveryChallanStore(rowId, vType);
        }
    })

}

function editDeliveryChallanStore(docCode, docType) {
    window.location.href = `/DeliveryChallanStore/Index?id=${encodeURIComponent(docCode)}&vtype=${encodeURIComponent(docType)}`;
}

function viewDeliveryChallanStore(docCode, docType) {
    window.location.href = `/DeliveryChallanStore/Index?id=${encodeURIComponent(docCode)}&vtype=${encodeURIComponent(docType)}&readOnly=true`;
}

async function deleteDeliveryChallanStore(docId, docType) {
    const isInApproval = await checkAppovStatus(docId, docType);
    if (!isInApproval) {
        Swal.fire({
            icon: 'warning',
            title: `Can't Delete`,
            text: 'This Document Approval is in process, Deletion not allowed.'
        });
        return;
    }
    deleteRecordbytype("DeliveryChallanStoreList", docId, docType, {
        action: "Delete",
        text: "This will permanently delete the Delivery Challan Store Details.",
        successCallback: DCIPagination.load
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
        window.location.href = "/DeliveryChallanStoreList/ExportAllDocs";
    });
}

function showDocumentPopup(docCode, docType) {
    $.ajax({
        url: '/DeliveryChallanStoreList/PBPEntryDetails',
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

async function checkAppovStatus(vNo, vType) {

    try {
        const response = await $.ajax({
            url: '/DeliveryChallanStoreList/GetApprovalStatus',
            type: 'GET',
            data: { vNo: vNo, vType: vType }
        });

        if (response.exists) {
            //showToast("Document is already open for approval by another user.", {type: "warning"});
            return false;
        }

        return true;
    }
    catch (error) {
        console.error(error);
        showToast("Error while checking approval status.", {type: "error"});
        return false;
    }
}
