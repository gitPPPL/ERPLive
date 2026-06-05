let storeWbPagination;
$(document).ready(function () {
	storeWbPagination = Pagination.create({
		pageSize: 10,
		paginationContainer: '#pageNumbers',
		infoContainer: '#pageInfoText',
		loader: function (params) {
			$.ajax({
				url: '/StoreWeighbridgeEntryList/GetStoreWBridgeList',
				type: 'GET',
				dataType: 'json',
				data: {
					searchTerm: $('#searchBox').val(),
					pageNumber: params.pageNumber,
					pageSize: params.pageSize
				},
				success: function (res) {
					params.callback({
						data: res.data,
						totalCount: res.totalCount
					});
				},
				error: function (xhr) {
					// toastr.error('Error loading data');
					showToast('Error loading data', { type: "error" });
				}
			});
		},
		render: function (docs) {
			const tbody = $('#tblStoreWeighbridgeList tbody');
			tbody.empty();
			if (!docs.length) {
				tbody.append(`<tr><td colspan="8" class="text-center text-muted">No list found.</td></tr>'`);
				return;
			}

			$.each(docs, function (index, item) {
				let actions = '';
				if (window.permissions.canEdit) {
					actions += `<button class="act-btn edit" title="Edit" style="cursor:pointer;" onclick="AddOrEditFunction('${item.DOC_ID}')"><i class="fa fa-edit"></i></button>`;
				}
				actions += `<button class="act-btn view" title="View" style="cursor:pointer;" onclick="viewMenuDetails('${item.DOC_ID}')"><i class="fa fa-eye"></i></button>`;

				if (window.permissions.canDelete) {
					actions += `<button class="act-btn delete" title="Delete" style="cursor:pointer;" onclick="deleteStoreWBEntry('${item.DOC_ID}')"><i class="fa fa-trash"></i></button>`;
				}
				if (window.permissions.canDocDetail) {
					actions += `<button class="act-btn document" title="Document Details" style="cursor:pointer;" onclick="showStoreWbDetailsPopup('${item.DOC_ID}')"><i class="fa fa-file"></i></button>`;
				}
				tbody.append(`
					<tr>
						<td class="d-none code">${item.DOC_ID}</td>
						<td>${item.V_TYPE || ''}</td>
						<td>${item.V_NO || ''}</td>
						<td>${item.V_DATE || ''}</td>
						<td>${item.GATE_NO || ''}</td>
						<td>${item.PartyNm || ''}</td>
						<td class="action-col">${actions}</td>
					</tr>
				`);
			});

		}
	});
	// First Load
	storeWbPagination.load();
	// Search
	$('#searchBox').keyup(function () {
		storeWbPagination.load();
	});
});
// Page Size Change
function changeRowsPerPage() {
	storeWbPagination.setPageSize(parseInt($('#pageSizeSelect').val()));
	storeWbPagination.load();
}
function AddOrEditFunction(rowId) {
	window.location.href = '/StoreWeighbridgeEntry/Index?id=' + encodeURIComponent(rowId);
}
function viewMenuDetails(rowId) {
	window.location.href = '/StoreWeighbridgeEntry/Index?id=' + encodeURIComponent(rowId) + '&readOnly=true';
}
function deleteStoreWBEntry(docId) {
	
	// STEP 1: Validate first
	$.ajax({
		url: `/StoreWeighbridgeEntryList/ValidateDeleteStoreWb`,
		type: 'POST',
		data: { docId: docId },

		success: function (response) {

			if (!response.success) {
				Swal.fire('Failed', response.message, 'warning');
				return;
			}

			// STEP 2: Prepare message
			let swalText = "This will permanently delete the Store WeighBridge entry.";
			let cancelBtn = true;
			let confirmBtn = true;
			let swalTitle = "Are you sure?";
			if (response.data === "Exists") {
				swalText = response.message;
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
					url: `/StoreWeighbridgeEntryList/DeleteStoreWBridgeEntry`,
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
								window.location.href = '/StoreWeighbridgeEntryList/Index';
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
function callGetReportAsPdf() {
	var reportName = "rpt_emp_mast";
	var now = new Date();
	var day = String(now.getDate()).padStart(2, '0');
	var month = String(now.getMonth() + 1).padStart(2, '0');
	var year = String(now.getFullYear()).slice(-2);
	var hours = String(now.getHours()).padStart(2, '0');
	var minutes = String(now.getMinutes()).padStart(2, '0');
	var seconds = String(now.getSeconds()).padStart(2, '0');
	var timestamp = `${day}${month}${year}_${hours}${minutes}${seconds}`;

	$.ajax({
		url: 'http://192.168.20.51:8082/Report/GetReportAsPdf',
		type: 'GET',
		data: { Reportname: reportName },
		xhrFields: {
			responseType: 'blob'
		},
		success: function (response) {
			console.log('PDF response:', response);
			var file = new Blob([response], { type: 'application/pdf' });
			var fileName = `${reportName}_${timestamp}.pdf`;

			var link = document.createElement('a');
			link.href = URL.createObjectURL(file);
			link.download = fileName;
			document.body.appendChild(link);
			link.click();
			document.body.removeChild(link);
		},
		error: function (xhr, status, error) {
			console.error('Error generating report:', error);
		}
	});
}
function showStoreWbDetailsPopup(docCode) {
	$.ajax({
		url: '/StoreWeighbridgeEntryList/GetStoreWBridgeEntryDetails',
		type: 'Get',
		dataType: 'json',
		data: { docid: docCode },
		success: function (response) {
			if (response.status) {
				showDocumentPopupjQuery(response.data, docCode);
			} else {
				//toastr.error("Failed to get document details.");
				showToast("Failed to get document details.", { type: "error" })
			}
		},
		error: function () {
			//toastr.error("An error occurred while fetching document details.");
			showToast("An error occurred while fetching document details.", { type: "error" })
		}
	});
}

// ================= Download Excel =================
document.getElementById("btn-Export-Excel").addEventListener("click", function (e) {
	e.preventDefault();
	window.location.href = "/StoreWeighbridgeEntryList/ExportAllDocs";
});