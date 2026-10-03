let currentPage = 1;
let pageSize = 10;
var controllerName = window.location.pathname.split('/')[1];
let SRPagination;

//Load
$(document).ready(async function () {
    checkPermission(controllerName, function () {
        SRPagination.load();
    });
    SRPagination = Pagination.create({
        pageSize: 10,
        paginationContainer: '#pageNumbers',
        infoContainer: '#pageInfoText',
        loader: function (params) {
            $.ajax({
                url: '/SalesReturnList/GetSalesReturnList',
                type: 'GET',
                dataType: 'json',
                data: {
                    searchTerm: $('#searchBox').val(),
                    pageNumber: params.pageNumber,
                    pageSize: params.pageSize
                },
                success: function (res) {
                    console.log("Sales Return : ", res);
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
            const tbody = $('#tblSalesReturnList tbody');
            tbody.empty();
            if (!docs.length) {
                tbody.append(`<tr><td colspan="6" class="text-center text-muted">No list found.</td></tr>`);
                return;
            }

            $.each(docs, function (index, doc) {
                tbody.append(`
				<tr>
							<td>${doc.v_TYPE || ''}</td>
							<td>${doc.v_NO || ''}</td>
							<td>${formatDate(doc.v_DATE)}</td>
							<td>${doc.bilL_NAME || ''}</td>
							<td>${doc.bilL_CITYNAME || ''}</td>
							<td>${doc.agenT_NAME || ''}</td>
							<td>${doc.shiP_NAME || ''}</td>
							<td>${doc.shiP_CITYNAME || ''}</td>
							<td>${doc.vehiclE_NO || ''}</td>
							<td>${doc.transporT_NAME || ''}</td>
							<td>${doc.remark || ''}</td>
							<td class="action-col">
							  <div class="action-icons">
								  <button class="act-btn edit btn-edit permission-edit" title="Edit" style="cursor:pointer;" title="Edit" onclick="checkSalesEditStatus('${doc.v_NO}', '${doc.v_TYPE}', '${doc.v_DATE}')"><i class="fa fa-edit"></i></button>
								  <button class="act-btn view btn-view" title="View" style="cursor:pointer;" onclick="viewSalesReturn('${doc.v_NO}', '${doc.v_TYPE}')"><i class="fa fa-eye"></i></button>
								  <button class="act-btn delete btn-delete  permission-delete" title="Delete" style="cursor:pointer;" onclick="deleteSalesReturn('${doc.v_NO}', '${doc.v_TYPE}')"><i class="fa fa-trash"></i></button>
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
    SRPagination.load();
    // Search
    $('#searchBox').keyup(function () {
        SRPagination.load();
    });
});

// Page Size Change
function changeRowsPerPage() {
    SRPagination.setPageSize(parseInt($('#pageSizeSelect').val()));
    SRPagination.load();
}

// Edit
function editSalesOrder(docCode, docType) {
    window.location.href = `/SalesReturn/Index?id=${encodeURIComponent(docCode)}&vtype=${encodeURIComponent(docType)}&canEdit=true`;
}
function checkSalesModificationAllowed(vDate) {
    return new Promise((resolve) => {
        $.ajax({
            url: '/SalesReturnList/checkModificationDays',
            type: 'GET',
            dataType: 'json',
            data: { vDate: vDate },

            success: function (response) {
                if (response.success && response.isAllowed !== 0) {
                    resolve(true);
                } else {
                    Swal.fire({
                        icon: "warning",
                        title: "Modification Not Allowed",
                        text: response.message || "Modification is not allowed.",
                        confirmButtonText: "OK",
                        allowOutsideClick: false,
                        allowEscapeKey: false
                    }).then(() => resolve(false));
                }
            },

            error: function () {
                Swal.fire({
                    icon: "error",
                    title: "Error",
                    text: "An error occurred while checking modification days.",
                    confirmButtonText: "OK"
                }).then(() => resolve(false));
            }
        });
    });
}
async function checkSalesEditStatus(vNo, vType, vDate) {
    debugger;
    try {
        const response = await fetch(
            `/SalesReturnList/GetSalesEditStatus?vType=${encodeURIComponent(vType)}&vNo=${vNo}`
        );

        const result = await response.json();

        if (!result.success) {
            await Swal.fire({
                icon: "error",
                title: "Error",
                text: result.message || "Unable to check edit status.",
                confirmButtonText: "OK"
            });
            return false;
        }

        const data = result.data;

        // 1. E-Invoice warning
        if (data.eInvoiceWarning) {
            await Swal.fire({
                icon: "warning",
                title: "Modification Alert!",
                text: data.eInvoiceMessage,
                confirmButtonText: "OK",
                allowOutsideClick: false,
                allowEscapeKey: false
            });
        }

        // 2. Modification days
        if (!data.isFinalApprovalBody) {
            if (!data.isFinalApprovalBody) {
                const allowed = await checkSalesModificationAllowed(vDate);

                if (!allowed)
                    return false;
            }
        }

        // 3. Approved document - Edit Locked
        if (data.isApproved) {
            if (data.isFinalApprovalBody && data.eaFlag?.toUpperCase() === "EA") {
                return true;
            }

            await Swal.fire({
                icon: "error",
                title: "Stop Modification",
                text: "Edit Locked, Document once Approved Not allowed to EDIT.",
                confirmButtonText: "OK",
                allowOutsideClick: false,
                allowEscapeKey: false
            });

            return false;
        }

        // 4. Other blocking validation
        if (!data.canEdit) {
            await Swal.fire({
                icon: "error",
                title: "Stop Modification",
                text: data.message,
                confirmButtonText: "OK",
                allowOutsideClick: false,
                allowEscapeKey: false
            });

            return false;
        }
        else {
            editSalesOrder(vNo, vType);
        }

        return true;
    }
    catch (error) {
        console.error(error);

        await Swal.fire({
            icon: "error",
            title: "Error",
            text: "Error while checking edit status.",
            confirmButtonText: "OK"
        });

        return false;
    }
}

//View
function viewSalesReturn(docCode, docType) {
    window.location.href = `/SalesReturn/Index?id=${encodeURIComponent(docCode)}&vtype=${encodeURIComponent(docType)}&readOnly=true&canEdit=${canEditOnList()}`;
}

//Delete
async function deleteSalesReturn(vNo, vType) {
    try {
        // 1. Check delete status
        const response = await fetch(
            `/SalesReturnList/GetSalesDeleteStatus?vType=${encodeURIComponent(vType)}&vNo=${vNo}`
        );

        const result = await response.json();

        if (!result.success) {
            await Swal.fire({
                icon: "error",
                title: "Error",
                text: result.message || "Unable to check delete status.",
                confirmButtonText: "OK"
            });
            return false;
        }

        const data = result.data;

        // 2. E-Invoice - STOP
        if (!data.canDelete) {
            await Swal.fire({
                icon: "error",
                title: "Stop Delete",
                text: data.message,
                confirmButtonText: "OK",
                allowOutsideClick: false,
                allowEscapeKey: false
            });

            return false;
        }

        // 3. Gate warning
        if (data.gateExists) {
            await Swal.fire({
                icon: "warning",
                title: "Warning",
                text: data.gateMessage,
                confirmButtonText: "OK",
                allowOutsideClick: false,
                allowEscapeKey: false
            });

            return false;
        }

        // 4. Ledger warning
        if (data.ledgerExists) {
            await Swal.fire({
                icon: "warning",
                title: "Warning",
                text: data.ledgerMessage,
                confirmButtonText: "OK",
                allowOutsideClick: false,
                allowEscapeKey: false
            });

            return false;
        }

        // 5. Final confirmation
        const confirm = await Swal.fire({
            icon: "question",
            title: "Confirm Delete",
            text: "Are you sure you want to delete this document?",
            showCancelButton: true,
            confirmButtonText: "Yes, Delete",
            cancelButtonText: "No",
            allowOutsideClick: false
        });

        if (!confirm.isConfirmed)
            return false;

        // 6. Delete
        const deleteResponse = await fetch(`/SalesReturnList/DeleteSales?vType=${encodeURIComponent(vType)}&vNo=${vNo}`,
            {
                method: "POST"
            }
        );

        const deleteResult = await deleteResponse.json();

        if (!deleteResult.success) {
            await Swal.fire({
                icon: "error",
                title: "Error",
                text: deleteResult.message || "Error in Record deleting.",
                confirmButtonText: "OK"
            });

            return false;
        }

        await Swal.fire({
            icon: "success",
            title: "Deleted",
            text: deleteResult.message || "Record deleted successfully.",
            confirmButtonText: "OK"
        });

        SRPagination.load();

        return true;
    }
    catch (error) {
        console.error(error);

        await Swal.fire({
            icon: "error",
            title: "Error",
            text: "Error in Record deleting.",
            confirmButtonText: "OK"
        });

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
        window.location.href = "/SalesReturnList/ExportAllDocs";
    });
}

//Document Details Pop Up
function showDocumentPopup(vNo, vType) {
    $.ajax({
        url: '/SalesReturnList/GetSalesEntryEntryDetails',
        type: 'Get',
        dataType: 'json',
        data: { vNo: vNo, vType: vType },
        success: function (response) {
            if (response.status) {
                console.log(response.data);
                showDocumentPopupjQuery(response.data, vNo);
            } else {
                showToast("Failed to get document details.", {type:"error"});
            }
        },
        error: function () {
            showToast("An error occurred while fetching document details.", {type:"error"});
        }
    });
}

// Edit Permission On Main Form
function canEditOnList() {
    const $b = $('#tblSalesReturnList .btn-edit').first();
    return $b.length > 0 && $b.is(':visible') && !$b.prop('disabled');
}

