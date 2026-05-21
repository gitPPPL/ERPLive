$(document).ready(function () {

	bigWBridgePagination = Pagination.create({
		pageSize: 10,
		paginationContainer: '#pageNumbers',
		infoContainer: '#pageInfoText',

		loader: function (params) {
			const searchTerm = $('#searchBox').val().trim();

			$.ajax({
				url:'/BigWeighbridgeList/GetBigWBridgeList',
				type: 'GET',
				dataType: 'json',
				data: {
					searchTerm: searchTerm,
					pageNumber: params.pageNumber,
					pageSize: params.pageSize
				},
				success: function (res) {
					params.callback(res);
				},
				error: function (xhr) {
					toastr.error('Error loading data: ' + xhr.responseText);
				}
			});
		},

		render: function (data) {
			let tbody = $('#tblBigWeighbridgeList tbody');
			tbody.empty();

			if (!data || data.length === 0) {
				tbody.append(`
						<tr>
							<td colspan="10" class="text-center text-muted">
								No data found.
							</td>
						</tr>
					`);
				return;
			}

			$.each(data, function (index, item) {

				let actions = '';

				if (window.permissions.canEdit) {
					actions += `
							<i class="fa fa-edit btn-edit"
							   title="Edit"
							   onclick="AddOrEditFunction('${item.DOC_ID}')">
							</i>`;
				}

				actions += `
						<i class="fas fa-eye btn-view"
						   title="View"
						   onclick="viewMenuDetails('${item.DOC_ID}')">
						</i>`;      

				if (window.permissions.canDelete) {
					actions += `
							<i class="fas fa-trash btn-delete"
								title="Delete"
								onclick="deleteData('${item.DOC_ID}')">
							</i>`;
				}

				if (window.permissions.canDocDetail) {
					actions += `
							<i class="fa fa-file btn-document btn-details"
							   title="Document Details"
							   onclick="showImpExpExpensePopup('${item.DOC_ID}')">
							</i>`;
				}

				tbody.append(`
						<tr>
							<td class="d-none code">${item.DOC_ID}</td>
							<td>${item.V_TYPE}</td>
							<td>${item.V_NO}</td>
							<td>${item.V_DATE}</td>
							<td>${item.GATE_NO}</td>
							<td>${item.VEHICLE_NO}</td>
							<td>${item.PartyNm}</td>
							<td>${item.WB_TYPE}</td>
							<td>${item.REMARKS}</td>
							<td>${actions}</td>
						</tr>
			    `);
			});
		}
	});

	// Initial Load
	bigWBridgePagination.load();

	// Search
	$('#searchBox').on('keyup', function () {
		bigWBridgePagination.load();
	});

	// Page Size Change
	$('#pageSizeSelect').on('change', function () {
		bigWBridgePagination.setPageSize(parseInt($(this).val()));
	});
});

function AddOrEditFunction(rowId) {
	window.location.href = '/BigWeighbridge/Index?id=' + encodeURIComponent(rowId);
}

function viewMenuDetails(rowId) {
	window.location.href = '/BigWeighbridge/Index?id=' + encodeURIComponent(rowId) + '&readOnly=true';
}

// function deleteData(docId) {
// 	deleteRecord('BigWeighbridgeList', docId, {
// 		action: 'DeleteBigWBridgeEntry',
// 		title: 'Are you sure?',
// 		text: 'This action cannot be undone.',
// 		successCallback: function () {
// 			bigWBridgePagination.load();
// 		}
// 	});
// }

function deleteData(docId) {

	// STEP 1: Validate first
	$.ajax({
		url: '/BigWeighbridgeList/CheckDeleteBigWBridgeEntry',
		type: 'POST',
		data: { docId: docId },

		success: function (response) {

			if (!response.success) {
				Swal.fire('Failed', response.message, 'warning');
				return;
			}

			// STEP 2: Prepare message
			let swalText = 'This will permanently delete the Big Weighbridge entry.';

			if (response.data === "Exists") {
				swalText = response.message;
			}

			// STEP 3: Show confirmation popup
			Swal.fire({
				title: 'Are you sure?',
				html: swalText,
				icon: 'warning',
				showCancelButton: true,
				confirmButtonColor: '#d33',
				cancelButtonColor: '#3085d6',
				confirmButtonText: 'Yes, delete it!',
				cancelButtonText: 'Cancel'
			}).then((result) => {

				if (!result.isConfirmed) return;

				// STEP 4: Delete
				$.ajax({
					url: '/BigWeighbridgeList/DeleteBigWBridgeEntry',
					type: 'POST',
					data: { docid: docId },

					success: function (res) {

						if (res.success) {
							Swal.fire({
								icon: 'success',
								title: 'Deleted!',
								text: res.message || 'Deleted successfully.',
								showConfirmButton: false,
								timer: 2000
							}).then(() => {
								bigWBridgePagination.load();
							});
						}
						else {
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

function exportToExcel() {
	fetch('/BigWeighbridgeList/ExportAllDocs')
		.then(response => {
			if (!response.ok) throw new Error("Network response was not ok");
			return response.json();
		})
		.then(responseData => {
			if (!responseData.status) {
				toastr.error("Failed to fetch data.");
				return;
			}
			const dataArray = responseData.data;
			if (!Array.isArray(dataArray) || dataArray.length === 0) {
				toastr.warning("No data available to export.");
				return;
			}

			const worksheet = XLSX.utils.json_to_sheet(dataArray, { header: Object.keys(dataArray[0]) });
			const colWidths = Object.keys(dataArray[0]).map(key => {
				const maxLen = Math.max(
					key.length,
					...dataArray.map(row => (row[key] ? row[key].toString().length : 0))
				);
				return { wch: maxLen + 2 };
			});
			worksheet['!cols'] = colWidths;


			const headerRowNumber = 1;
			Object.keys(dataArray[0]).forEach((_, idx) => {
				const cellAddress = XLSX.utils.encode_cell({ c: idx, r: headerRowNumber - 1 });
				if (!worksheet[cellAddress]) return;
				worksheet[cellAddress].s = {
					font: { bold: true },
					fill: { fgColor: { rgb: "FFFF00" } }
				};
			});

			const workbook = XLSX.utils.book_new();
			XLSX.utils.book_append_sheet(workbook, worksheet, "AllDocs");

			const pageName = "TransportInward_List";
			const now = new Date();
			const pad = n => String(n).padStart(2, '0');
			const timestamp = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}_${pad(now.getHours())}${pad(now.getMinutes())}${pad(now.getSeconds())}`;
			const fileName = `${pageName}_${timestamp}.xlsx`;

			XLSX.writeFile(workbook, fileName);
		})
		.catch(error => {
			console.error("Export failed:", error);
			toastr.error("Failed to export data.");
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

function showImpExpExpensePopup(docCode) {
	$.ajax({
		url: '/BigWeighbridgeList/GetBigWBridgeEntryDetails',
		type: 'Get',
		dataType: 'json',
		data: { docid: docCode },
		success: function (response) {
			if (response.status) {
				showDocumentPopupjQuery(response.data, docCode);
			} else {
				toastr.error("Failed to get document details.");
			}
		},
		error: function () {
			toastr.error("An error occurred while fetching document details.");
		}
	});
}