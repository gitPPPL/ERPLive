
let qcTemperaturePagination;
$(document).ready(function () {
	qcTemperaturePagination = Pagination.create({
		pageSize: 10,
		paginationContainer: '#pageNumbers',
		infoContainer: '#pageInfoText',
		loader: function (params) {
			$.ajax({
				url: '/QCTemperatureEntryList/GetQcTempratureList',
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
					showToast('Error loading data', { type: "error" });
				}
			});
		},
		render: function (docs) {
			const tbody = $('#tblQCTemperatureList tbody');
			tbody.empty();
			if (!docs.length) {
				tbody.append(`<tr><td colspan="16" class="text-center text-muted">No list found.</td></tr>'`);
				return;
			}

			$.each(docs, function (index, item) {
				let actions = '';
				if (window.permissions.canEdit) {
					//actions += `<button class="act-btn edit" title="Edit" style="cursor:pointer;" onclick="AddOrEditFunction('${item.DOC_ID}')"><i class="fa fa-edit"></i></button>`;
					actions += `<button class="act-btn edit" title="Edit" style="cursor:pointer;" onclick="checkModificationAllowed('${item.V_DATE}', '${item.DOC_ID}')"><i class="fa fa-edit"></i></button>`;
				}
				actions += `<button class="act-btn view" title="View" style="cursor:pointer;" onclick="viewMenuDetails('${item.DOC_ID}')"><i class="fa fa-eye"></i></button>`;

				if (window.permissions.canDelete) {
					actions += `<button class="act-btn delete" title="Delete" style="cursor:pointer;" onclick="deleteQCTemperatureEntry('${item.DOC_ID}')"><i class="fa fa-trash"></i></button>`;
				}
				if (window.permissions.canDocDetail) {
					actions += `<button class="act-btn document" title="document" style="cursor:pointer;" onclick="showImpExpExpensePopup('${item.DOC_ID}')"><i class="fa fa-file-alt"></i></button>`;
				}
				tbody.append(`
				<tr>
				<td class="d-none code">${item.DOC_ID}</td>
				<td>${item.V_NO || ''}</td>
				<td>${item.V_DATE || ''}</td>
				<td>${item.SHIFT || ''}</td>
				<td>${item.V_TIME || ''}</td>
				<td>${item.DenierName || ''}</td>
				<td>${item.PlantName || ''}</td>
				<td class="action-col">${actions}</td>
				</tr>
				`);

			});

		}
	});
	// First Load
	qcTemperaturePagination.load();
	// Search
	$('#searchBox').keyup(function () {
		qcTemperaturePagination.load();
	});
});

// Page Size Change
function changeRowsPerPage() {
	qcTemperaturePagination.setPageSize(parseInt($('#pageSizeSelect').val()));
	qcTemperaturePagination.load();
}

function AddOrEditFunction(rowId) {
	window.location.href = '/QCTemperatureEntry/Index?id=' + encodeURIComponent(rowId);
}

function viewMenuDetails(rowId) {
	window.location.href = '/QCTemperatureEntry/Index?id=' + encodeURIComponent(rowId) + '&readOnly=true';
}

function deleteQCTemperatureEntry(docId) {
	deleteRecord("QCTemperatureEntryList", docId, {
		action: "DeleteQcTempratureEntry",
		text: "This will permanently delete the QC Temperature entry.",
		successCallback: qcTemperaturePagination.load
    });
}


function checkModificationAllowed(vDate, rowId) {
	checkModificationDays({
		controller : 'QCTemperatureEntry',
		vDate: vDate,
		rowId: rowId,
		onAllowed: function (rowId) {
			AddOrEditFunction(rowId);
		}
	})
}

// ================= Download Excel =================
document.getElementById("btn-Export-Excel").addEventListener("click", function (e) {
	e.preventDefault();
	window.location.href = "/QCTemperatureEntryList/ExportAllDocs";
});

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
		url: '/QCTemperatureEntryList/GetQcTempratureEntryDetails',
		type: 'Get',
		dataType: 'json',
		data: { docid: docCode },
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