let currentPage = 1;
let pageSize = 10;
var controllerName = window.location.pathname.split('/')[1];
let PBPPagination;
$(document).ready(async function () {
    checkPermission(controllerName, function () {
        PBPPagination.load();
    });
    PBPPagination = Pagination.create({
        pageSize: 10,
        paginationContainer: '#pageNumbers',
        infoContainer: '#pageInfoText',
        loader: function (params) {
            $.ajax({
                url: '/TradingDirectPurchaseList/GetAllPurchaseBillPassEntry',
                type: 'GET',
                dataType: 'json',
                data: {
                    searchTerm: $('#searchBox').val(),
                    pageNumber: params.pageNumber,
                    pageSize: params.pageSize
                },
                success: function (res) {
                    params.callback({
                        data: res.purchaseBillDirect,
                        totalCount: res.totalCount
                    });
                },
                error: function (xhr) {
                    showToast('Error loading data', { type: "error" });
                }
            });
        },
        render: function (docs) {
            const tbody = $('#tblPurchaseBillPassEntry tbody');
            tbody.empty();
            if (!docs.length) {
                tbody.append(`<tr><td colspan="20" class="text-center text-muted">No list found.</td></tr>`);
                return;
            }

            $.each(docs, function (index, doc) {
                tbody.append(`
				<tr>
							<td style="display:none;">${doc.code || doc.doC_CODE || ''}</td>
							<td>${doc.v_NO || ''}</td>
							<td>${doc.v_TYPE || ''}</td>
							<td>${formatDate(doc.v_DATE)}</td>
							<td>${doc.partY_NAME || ''}</td>
							<td>${doc.shiP_NAME || ''}</td>
							<td>${doc.debiT_AC_NAME || ''}</td>
							<td>${doc.crediT_AC_NAME || ''}</td>
							<td>${doc.bilL_QTY || ''}</td>
							<td>${doc.namount || ''}</td>
							<td>${doc.reF_TYPE || ''}</td>
							<td>${doc.reF_NO || ''}</td>
							<td>${doc.bilL_NO || ''}</td>
							<td>${formatDate(doc.bilL_DATE)}</td>
							<td>${doc.chalL_NO || ''}</td>
							<td>${formatDate(doc.chalL_DATE)}</td>
							<td>${doc.dR_FROM_TPT || ''}</td>
							<td>${doc.remarks || ''}</td>
							<td>${doc.status || ''}</td>
							<td class="action-col">
							  <div class="action-icons">
								  <button class="act-btn edit btn-edit permission-edit" title="Edit" style="cursor:pointer;" title="Edit" onclick="checkModificationAllowed('${doc.v_DATE}', '${doc.v_NO}', '${doc.v_TYPE}')"><i class="fa fa-edit"></i></button>
								  <button class="act-btn view btn-view" title="View" style="cursor:pointer;" onclick="viewPruchaseBillPassEntry('${doc.v_NO}', '${doc.v_TYPE}')"><i class="fa fa-eye"></i></button>
								  <button class="act-btn delete btn-delete  permission-delete" title="Delete" style="cursor:pointer;" onclick="checkPurchaseDelete('${doc.v_NO}', '${doc.v_TYPE}')"><i class="fa fa-trash"></i></button>
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
    PBPPagination.load();
    // Search
    $('#searchBox').keyup(function () {
        PBPPagination.load();
    });
});

async function checkModificationAllowed(vDate, rowId, vType) {
    const isApprovalBody = await checkApprovalBody(vType);
    if (isApprovalBody) {
        // Approval body does not need modification-days check
        GetPurchaseEditStatus(rowId, vType, isApprovalBody);
        return;
    }

    checkModificationDays({
        controller: 'TradingDirectPurchaseList',
        vDate: vDate,
        rowId: rowId,
        vType: vType,
        onAllowed: function (rowId) {
            //editPruchaseBillPassEntry(rowId, vType);
            GetPurchaseEditStatus(rowId, vType, isApprovalBody);
        }
    })

}

async function GetPurchaseEditStatus(vNo, vType, isApprovalBody) {
    $.ajax({
        url: '/TradingDirectPurchaseList/GetPurchaseEditStatus',
        type: 'GET',
        data: {
            vType: vType,
            vNo: vNo
        },
        success: async function (response) {

            if (response.success) {

                var data = response.data;
                console.log("userlevel: ", response.userlevel);
                console.log("Purchase Edit Status:", data);
                console.log("Is Approved:", data.isApproved);
                console.log("Is Approval Body:", isApprovalBody);
                console.log("Is Final Approval Body:", data.isFinalApprovalBody);
                console.log("EA Flag:", data.eaFlag);
                console.log("Approval In Process:", data.isApprovalInProcess);

                // =====================================================
                // 1. Document is Approved
                // =====================================================
                if (!isApprovalBody && data.isApproved) {
                    showToast("This Document has been Approved, Edit not allowed.", { type: "warning" });
                    if (response.userlevel !== '1') {
                        return;
                    }
                }

                // =====================================================
                // 2. Approval is currently in process
                // =====================================================
                if (data.isApprovalInProcess) {
                    showToast("This Document Approval is in process at User: " + data.approvalUser, { type: "warning" });
                    if (response.userlevel !== '1') {
                        return;
                    }
                }

                // =====================================================
                // 3. Approved Document
                // =====================================================
                if (data.isApproved) {
                    if (data.isFinalApprovalBody === true) {

                        if (data.eaFlag === "EA") {
                            editPruchaseBillPassEntry(vNo, vType);
                        }
                        else {
                            showToast("Edit Locked, Document once Approved Not allowed to EDIT.", { type: "warning" });
                        }
                    }
                    return;
                }
                // =====================================================
                // 4. Document is NOT Approved
                // =====================================================
                editPruchaseBillPassEntry(vNo, vType);
            }
            else {
                showToast(response.message, { type: "warning" });
            }
        },
        error: function (xhr) {
            console.log(xhr);
            showToast("Error while checking purchase edit status.", { type: "error" });
        }
    });
}

function editPruchaseBillPassEntry(docCode, docType) {
    window.location.href = `/TradingDirectPurchase/Index?id=${encodeURIComponent(docCode)}&vtype=${encodeURIComponent(docType)}`;
}

function viewPruchaseBillPassEntry(docCode, docType) {
    window.location.href = `/TradingDirectPurchase/Index?id=${encodeURIComponent(docCode)}&vtype=${encodeURIComponent(docType)}&readOnly=true`;
}

function deletePruchaseBillPassEntry(docId, docType) {
    deleteRecordbytype("TradingDirectPurchaseList", docId, docType, {
        action: "Delete",
        text: "This will permanently delete the Purchase Bill Pass Entry details.",
        successCallback: PBPPagination.load
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
        window.location.href = "/TradingDirectPurchaseList/ExportAllDocs";
    });
}

function showDocumentPopup(docCode, docType) {
    $.ajax({
        url: '/TradingDirectPurchaseList/PBPEntryDetails',
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

async function checkApprovalBody(vType) {
    try {
        const response = await $.ajax({
            url: '/TradingDirectPurchaseList/GetApprovalBody',
            type: 'GET',
            data: {
                vType: vType
            }
        });

        if (!response.success) {
            console.error(response.message);
            return false;
        }

        return response.isApprovalBody;

    } catch (error) {
        console.error("AJAX Error:", error);
        return false;
    }
}

async function checkPurchaseDelete(vNo, vType) {

    $.ajax({
        url: '/TradingDirectPurchaseList/GetPurchaseDeleteStatus',
        type: 'GET',
        data: {
            vType: vType,
            vNo: vNo
        },

        success: function (response) {

            if (!response.success) {
                Swal.fire({
                    icon: 'error',
                    title: 'Error',
                    text: response.message
                });
                return;
            }

            const data = response.data;

            // =====================================================
            // 1. Approval is in process
            // =====================================================
            if (data.isApprovalInProcess) {

                Swal.fire({
                    icon: 'warning',
                    title: `Can't Delete`,
                    text: 'This Document Approval is in process, Deletion not allowed.'
                });

                return;
            }

            // =====================================================
            // 2. Document exists in Ledger
            // =====================================================
            if (data.existsInLedger) {

                Swal.fire({
                    icon: 'warning',
                    title: `Can't Delete`,
                    html: 'This document exists in Ledger Serial No :<b>' + data.ledgerNo + '</b> dated :<b>' + data.ledgerDate + '</b>'
                });

                return;
            }

            // =====================================================
            // 3. Continue deletion
            // =====================================================
            deletePruchaseBillPassEntry(vNo, vType);
        },

        error: function (xhr, status, error) {

            console.error(xhr);

            Swal.fire({
                icon: 'error',
                title: 'Error',
                text: 'Error while checking document deletion status.'
            });
        }
    });
}