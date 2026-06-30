let currentPage = 1;
let pageSize = 10;
let IsApprovalBody = false;
let IsFinalApprovalBody = false;
let pubUserLevel = window.userLevel;

let PRPagination;
$(document).ready(async function () {
	await CheckIsApprovalBody();
	await CheckIsFinalApprovalBody();
	PRPagination = Pagination.create({
		pageSize: 10,
		paginationContainer: '#pageNumbers',
		infoContainer: '#pageInfoText',
		loader: function (params) {
			$.ajax({
				url: '/PurchaseRequestList/GetList',
				type: 'GET',
				dataType: 'json',
				data: {
					searchTerm: $('#searchBox').val(),
					pageNumber: params.pageNumber,
					pageSize: params.pageSize
				},
				success: function (res) {
					params.callback({
						data: res.lists,
						totalCount: res.totalCount
					});
				},
				error: function (xhr) {
					showToast('Error loading data', { type: "error" });
				}
			});
		},
		render: function (docs) {
			const tbody = $('#tblPurchaseRequestList tbody');
			tbody.empty();
			if (!docs.length) {
				tbody.append(`<tr><td colspan="10" class="text-center text-muted">No list found.</td></tr>`);
				return;
			}

			$.each(docs, function (index, item) {
				const vNo = item.v_NO || item.V_NO;
				const vDate = item.v_DATE || item.V_DATE;
				const deptCode = item.depT_CODE || item.DEPT_CODE;
				let actions = '';
				if (window.permissions.canEdit) {
					actions += `
								<button class="act-btn edit btn-edit" title="Edit" style="cursor:pointer;"
									data-no="${vNo}"
									data-date="${vDate}"
									data-dept="${deptCode}"
									title="Edit">
									<i class="fa fa-edit"></i>
								</button>`;
				}
				actions += `<button class="act-btn view btn-view" title="View" style="cursor:pointer;" onclick="viewMenuDetails('${vNo}')"><i class="fa fa-eye"></i></button>`;

				if (window.permissions.canDelete) {
					actions += `<button class="act-btn delete btn-delete" title="Delete" style="cursor:pointer;" onclick="deleteTemp('${vNo}')"><i class="fa fa-trash"></i></button>`;
				}
				if (window.permissions.canDocDetail) {
					actions += `<button class="act-btn document btn-document" title="document" style="cursor:pointer;" onclick="showImpExpExpensePopup('${vNo}')"><i class="fa fa-file-alt"></i></button>`;
				}
				tbody.append(`
				<tr>
							<td>${item.v_NO}</td>
							<td>${item.v_TYPE}</td>
							<td>${formatDate(item.v_DATE)}</td>
							<td>${item.depT_NAME}</td>
							<td>${item.owneR_NAME}</td>
							<td>${item.placeName}</td>
							<td>${formatDate(item.targeT_DATE)}</td>
							<td>${item.remarks}</td>
							<td>${item.status === 1 ? 'Open' : item.status === 2 ? 'Cancel' : item.status === 3 ? 'Close' : 'Unknown'}</td>
							<td class="action-col"><div class="action-wrap">${actions}</div></td>
				</tr>
				`);

			});

		}
	});
	// First Load
	PRPagination.load();
	// Search
	$('#searchBox').keyup(function () {
		PRPagination.load();
	});
});

// Page Size Change
function changeRowsPerPage() {
	PRPagination.setPageSize(parseInt($('#pageSizeSelect').val()));
	PRPagination.load();
}

//============Check IsApprovalBody=============
async function CheckIsApprovalBody() {
	try {
		const response = await $.ajax({
			url: '/PurchaseRequest/CheckIsApprovalBody',
			type: 'GET',
			dataType: 'json'
		});

		if (response.exists) {
			IsApprovalBody = true;
		} else {
			IsApprovalBody = false;
		}
	} catch (error) {
		console.error('Error checking approval stage:', error);
		showToast('An error occurred while checking the approval stage.', { type: "error" });
	}
}


async function CheckIsFinalApprovalBody() {
	$.ajax({
		url: '/PurchaseRequest/CheckIsFinalApprovalBody',
		type: 'GET',
		dataType: 'json',
		success: function (response) {
			if (response.success === false) {
				showToast("An error occurred in checking Final Approval Body!", { type: "error" });
				return;
			}
			if (response.exists) {
				IsFinalApprovalBody = true;
			} else {
				IsFinalApprovalBody = false;
			}
		},
		error: function (xhr, status, error) {
			console.error("AJAX Error:", error);
			showToast("An error occurred in checking Final Approval Body!", { type: "error" });
		}
	});
}

function formatDate(dateStr) {
	if (!dateStr) return null;
	const date = new Date(dateStr);
	if (isNaN(date)) return null;
	const year = date.getFullYear();
	const month = String(date.getMonth() + 1).padStart(2, '0');
	const day = String(date.getDate()).padStart(2, '0');
	return `${year}-${month}-${day}`;
}

//===========Edit Start===========

$(document).on('click', '.btn-edit', function () {
	const no = $(this).data('no');
	const date = $(this).data('date');
	const deptCode = $(this).data('dept');

	checkModificationAllowed(date, no, function (canProceed) {
		if (!canProceed) return;

		getPRApprovalStatus(no, function (canProceed2) {
			if (!canProceed2) return;

			validateDepartmentAccess(deptCode, function (canProceed3) {
				if (!canProceed3) return;
				checkApprovalStatusForEdit(no, function (canProceed4) {
					if (!canProceed4) return;
					AddOrEditFunction(no);
				})
			});
		});
	});
});
function AddOrEditFunction(code) {
	window.location.href = '/PurchaseRequest/Index?id=' + encodeURIComponent(code);
}

function checkModificationAllowed(vDate, rowId, callback) {
	checkModificationDays({
		controller: 'PurchaseRequest',
		vDate: vDate,
		rowId: rowId,
		onAllowed: function (rowId) {
			getPRApprovalStatus(rowId, callback);
		},
		onBlocked: function () {
			callback(false);
		}
	})
}

function getPRApprovalStatus(vNo, callback) {
	$.ajax({
		url: '/PurchaseRequest/GetApprovalStatus',
		type: 'GET',
		data: { VNo: vNo },
		dataType: 'json',
		success: function (response) {
			if (!response.success) {
				showToast("Error in fetching approval status", { type: "error" });
				callback(false);
				return;
			}

			const status = (response.faproV_STATUS || "").toUpperCase();

			// Not approved → allow next check
			//if (status !== "APPROVED") {
			//	callback(true);
			//	return;
			//}

			// Approved + final approval body → allow next check
			//if (IsFinalApprovalBody) {
			//	callback(true);
			//	return;
			//}

			// Approved + not final + non-admin → block
			//if (pubUserLevel !== 1) {
			//	showToast("This Document has been Approved, Edit not allowed.", { type: "warning" });
			//	callback(false);
			//	return;
			//}
			if (status === "APPROVED") {
				if (!IsFinalApprovalBody) {
					showToast("This Document has been Approved, Edit not allowed.", { type: "warning" });
					if (pubUserLevel !== 1) {
						callback(false);
						return;
					}
				}
			}

			// Admin override
			callback(true);
		},
		error: function (xhr, status, error) {
			console.error("Error:", error);
			showToast("An error occurred while fetching approval status.", { type: "error" });
			callback(false);
		}
	});
}

function validateDepartmentAccess(deptCode, callback) {
	$.ajax({
		url: '/PurchaseRequest/ValidateDepartmentAccess',
		type: 'GET',
		data: { deptCode: deptCode },
		success: function (res) {

			if (!res.success) {
				showToast(res.message || "Error while validating access.", {type:"error"});
				callback(false);
				return;
			}

			if (!IsApprovalBody && !res.exists) {
				showToast("You are not allowed to modify this department request.", {type:"warning"});
				callback(false);
				return;
			}

			callback(true);
		},
		error: function () {
			showToast("Server error while validating department access.", {type:"error"});
			callback(false);
		}
	});
}

function checkApprovalStatusForEdit(vno, callback) {
	$.ajax({
		url: '/PurchaseRequest/CheckApprovalStatus',
		type: 'GET',
		data: { VNO: vno },
		dataType: 'json',
		success: function (res) {

			if (!res.success) {
				showToast(res.message || "Error while checking approval status.", { type: "error" });
				callback(false);
				return;
			}

			if (res.isExist) {
				showToast(`Document already in Approval process at User: ${res.userName}, Edit not allowed.`, { type: "warning" });
				callback(false); 
				return;
			}

			// pending approval exists
			callback(true);
		},
		error: function () {
			showToast("Server error while checking approval status.", { type: "error" });
			callback(false);
		}
	});
}
//===========Edit End===========
function viewMenuDetails(code) {
	window.location.href = '/PurchaseRequest/Index?id=' + encodeURIComponent(code) + '&mode=view';
}

//================Delete==================
function deleteTemp(docId) {

	// STEP 1: Validate first
	$.ajax({
		url: `/PurchaseRequestList/CheckApprovalStatusForDelete`,
		type: 'GET',
		data: { docId: docId },

		success: function (response) {

			if (!response.success) {
				Swal.fire('Failed', response.message, 'warning');
				return;
			}

			// STEP 2: Prepare message
			let swalText = "This will permanently delete the Purchase Request Details.";
			let cancelBtn = true;
			let confirmBtn = true;
			let swalTitle = "Are you sure?";
			if (response.isOpen) {
				swalText = "This document is currently under approval and cannot be deleted.";
				cancelBtn = false;
				confirmBtn = false;
				swalTitle = "Can't delete!!";
			}

			// STEP 3: Show only ONE popup
			Swal.fire({
				title: swalTitle,
				html: swalText,
				icon: 'warning',
				showCancelButton: cancelBtn,
				showConfirmButton: confirmBtn,
				confirmButtonColor: '#d33',
				cancelButtonColor: '#3085d6',
				confirmButtonText: 'Yes, delete it!',
				cancelButtonText: 'Cancel'
			}).then((result) => {

				if (!result.isConfirmed) return;

				// STEP 4: Delete
				$.ajax({
					url: `/PurchaseRequestList/Delete`,
					type: 'POST',
					data: { docId: docId },

					success: function (res) {

						if (res.success) {

							Swal.fire({
								icon: 'success',
								title: 'Deleted!',
								text: res.message || 'Deleted successfully',
								showConfirmButton: false,   // Hide the OK button
								timer: 2000,                 // Auto close
							});
							setTimeout(() => {
								QCMasterPagination.load();
							}, 2000);
						} else {
							Swal.fire('Failed', res.message, 'warning');
						}
					},

					error: function () {
						Swal.fire('Error!', 'Error in deleting.', 'error');
					}
				});

			});
		},

		error: function () {
			Swal.fire('Error!', 'Something went wrong.', 'error');
		}
	});
}
//function deleteTemp(docId) {
//	deleteRecord("PurchaseRequestList", docId, {
//		action: "Delete",
//		text: "This will permanently delete the Purchase Request details.",
//		successCallback: PRPagination.load
//	});
//}
// ================= Download Excel =================
//document.getElementById("btn-Export-Excel").addEventListener("click", function (e) {
//	e.preventDefault();
//	window.location.href = "/PurchaseRequest/ExportAllDocs";
//});

const btn = document.getElementById("btn-Export-Excel");

if (btn) {
	btn.addEventListener("click", function (e) {
		e.preventDefault();
		window.location.href = "/PurchaseRequest/ExportAllDocs";
	});
}

function showImpExpExpensePopup(docCode) {
	$.ajax({
		url: '/PurchaseRequestList/PREntryDetails',
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