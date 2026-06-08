let currentPage = 1;
let pageSize = 10;
let IsApprovalBody = false;
let IsFinalApprovalBody = false;
let pubUserLevel = window.userLevel;

$(document).ready(async function () {
	loadAllMenus();
	await CheckIsApprovalBody();
	await CheckIsFinalApprovalBody();
});

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

function loadAllMenus() {
	const searchTerm = $('#searchBox').val();


	$.ajax({
		url: '/PurchaseRequestList/GetList',
		type: 'GET',
		dataType: 'json',
		data: {
			searchTerm: searchTerm,
			pageNumber: currentPage,
			pageSize: pageSize
		},
		success: function (res) {
			console.log("response: ", res);

			if (!res.success) {
				toastr.error("Failed to load data.");
				return;
			}

			const list = res.lists;
			const totalCount = res.totalCount;
			let tbody = $('#tblPurchaseRequestList tbody');
			tbody.empty();

			if (list.length === 0) {
				tbody.append('<tr><td colspan="11" class="text-center text-muted">No list found.</td></tr>');
			} else {
				$.each(list, function (index, item) {
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
								<td>
								   <i class="fa fa-edit btn-edit"
									   data-no="${item.v_NO}"
									   data-date="${item.v_DATE}"
									   data-dept="${item.depT_CODE}"
									   data-bs-toggle="tooltip"
									   title="Edit"></i>
									<i class="fas fa-eye btn-view" data-bs-toggle="tooltip" title="View" onclick="viewMenuDetails('${item.v_NO}')"></i>
									<i class="fas fa-trash btn-delete" data-bs-toggle="tooltip" title="Delete"onclick="deleteTemp('${item.v_NO}')"></i>
								</td>
							</tr>
						`);
				});
				renderPagination(totalCount, pageSize, currentPage);
			}
		},
		error: function (xhr) {
			toastr.error('Error loading menu list: ' + xhr.responseText);
		}
	});
}

function renderPagination(total, pageSize, currentPage) {
	const totalPages = Math.ceil(total / pageSize);
	const paginationContainer = $('#tablePagination');
	paginationContainer.empty();

	if (total === 0 || totalPages <= 1) return;

	let paginationHtml = '';

	if (currentPage > 1) {
		paginationHtml += `<button class="btn btn-sm btn-light mx-1" onclick="goToPage(1)">First</button>`;
		paginationHtml += `<button class="btn btn-sm btn-light mx-1" onclick="goToPage(${currentPage - 1})">Prev</button>`;
	}

	let startPage = Math.max(1, currentPage - 2);
	let endPage = Math.min(totalPages, currentPage + 2);

	if (currentPage <= 3) endPage = Math.min(5, totalPages);
	if (currentPage >= totalPages - 2) startPage = Math.max(totalPages - 4, 1);

	for (let i = startPage; i <= endPage; i++) {
		paginationHtml += `<button class="btn btn-sm ${i === currentPage ? 'btn-primary' : 'btn-light'} mx-1" onclick="goToPage(${i})">${i}</button>`;
	}

	if (currentPage < totalPages) {
		paginationHtml += `<button class="btn btn-sm btn-light mx-1" onclick="goToPage(${currentPage + 1})">Next</button>`;
		paginationHtml += `<button class="btn btn-sm btn-light mx-1" onclick="goToPage(${totalPages})">Last</button>`;
	}

	paginationContainer.html(paginationHtml);
}

function goToPage(page) {
	currentPage = page;
	loadAllMenus();
}

$('#searchBox').on('keyup', function () {
	currentPage = 1;
	loadAllMenus();
});



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

	getPRApprovalStatus(no, function (canProceed) {
		if (!canProceed) return;

		validateDepartmentAccess(deptCode, function (canProceed2) {
			if (!canProceed2) return;

			checkModificationAllowed(date, no);
		});
	});
});
function AddOrEditFunction(code) {
	window.location.href = '/PurchaseRequest/Index?id=' + encodeURIComponent(code);
}

function checkModificationAllowed(vDate, rowId) {
	checkModificationDays({
		controller: 'PurchaseRequest',
		vDate: vDate,
		rowId: rowId,
		onAllowed: function (rowId) {
			AddOrEditFunction(rowId);
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
			if (status !== "APPROVED") {
				callback(true);
				return;
			}

			// Approved + final approval body → allow next check
			if (IsFinalApprovalBody) {
				callback(true);
				return;
			}

			// Approved + not final + non-admin → block
			if (pubUserLevel !== 1) {
				showToast("This Document has been Approved, Edit not allowed.", { type: "warning" });
				callback(false);
				return;
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
//===========Edit End===========
function viewMenuDetails(code) {
	window.location.href = '/PurchaseRequest/Index?id=' + encodeURIComponent(code) + '&mode=view';
}

function deleteTemp(code) {
	if (!confirm('Are you sure you want to delete this Purchase Request?')) return;
	$.ajax({
		url: '/PurchaseRequestList/Delete',
		type: 'POST',
		data: { code: code },
		success: function (res) {
			if (res.success) {
				toastr.success('Deleted successfully.');
				loadAllMenus();
			} else {
				toastr.error(res.message || 'Failed to delete.');
			}
		},
		error: function () {
			toastr.error('Error deleting Purchase Request .');
		}
	});
}