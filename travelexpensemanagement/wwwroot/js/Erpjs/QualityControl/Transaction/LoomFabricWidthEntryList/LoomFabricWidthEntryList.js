let pagination;

$(document).ready(function () {

	pagination = Pagination.create({
		pageSize: 10,

		paginationContainer: "#pageNumbers",
		infoContainer: "#pageInfoText",

		loader: function ({ pageNumber, pageSize, callback }) {

			$.ajax({
				url: '/LoomFabricWidthEntryList/GetLoomFabricStrengthList',
				type: 'GET',
				dataType: 'json',
				data: {
					searchTerm: $('#searchBox').val().trim(),
					pageNumber: pageNumber,
					pageSize: pageSize
				},
				success: function (res) {

					callback({
						data: res.data || [],
						totalCount: res.totalCount || 0
					});

				},
				error: function (xhr) {
					toastr.error('Error loading data: ' + xhr.responseText);
				}
			});
		},

		render: function (mastListData) {

			let tbody = $('#tblLoomFabricWidthEntryList tbody');
			tbody.empty();

			if (mastListData.length === 0) {
				tbody.append(`
                    <tr>
                        <td colspan="10" class="text-center text-muted">
                            No PO found.
                        </td>
                    </tr>
                `);
				return;
			}

			$.each(mastListData, function (index, item) {

				let actions = '';

				if (window.permissions.canEdit) {
					actions += `<button class="act-btn edit btn-edit" title="Edit" style="cursor:pointer;" onclick="AddOrEditFunction('${item.DOC_ID}')"><i class="fa fa-edit"></i></button>`;
				}

				actions += `<button class="act-btn view btn-view" title="View" style="cursor:pointer;" onclick="viewMenuDetails('${item.DOC_ID}')"><i class="fa fa-eye"></i></button>`;

				if (window.permissions.canDelete) {
					actions += `<button class="act-btn delete btn-delete" title="Delete Row" style="cursor:pointer;" onclick="deleteData('${item.DOC_ID}')"><i class="fa fa-trash"></i></button>`;
				}

				if (window.permissions.canDocDetail) {
					actions += `<button class="act-btn document btn-document btn-details" title="Document Details" style="cursor:pointer;" onclick="showImpExpExpensePopup('${item.DOC_ID}')"><i class="fa fa-file"></i></button>`;
				}

				tbody.append(`
                    <tr>
                        <td class="d-none code">${item.DOC_ID}</td>
                        <td>${item.V_NO}</td>
                        <td>${item.V_TYPE}</td>
                        <td>${item.V_DATE}</td>
                        <td>${item.SHIFT}</td>
                        <td>${item.PlaceName}</td>
                        <td>${item.EmployeeName}</td>
                       <td>${item.REMARKS ? item.REMARKS : ''}</td>
                        <td class="action-col">${actions}</td>
                    </tr>
                `);
			});
		}
	});

	pagination.load();

	$('#searchBox').on('keyup', function () {
		pagination.load();
	});

});

function AddOrEditFunction(rowId) {
	window.location.href = '/LoomFabricWidthEntry/Index?id=' + encodeURIComponent(rowId);
}

function viewMenuDetails(rowId) {
	window.location.href = '/LoomFabricWidthEntry/Index?id=' + encodeURIComponent(rowId) + '&readOnly=true';
}

function exportToExcel() {
	fetch('/LoomFabricWidthEntryList/ExportAllDocs')
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

			const pageName = "LoomFS_List";
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
		url: '/LoomFabricWidthEntryList/GetLoomFabricStrengthEntryDetails',
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

function deleteData(docId) {
	deleteRecord('LoomFabricWidthEntryList', docId, {
		action: 'DeleteLoomFabricStrengthEntry',
		title: 'Are you sure?',
		text: 'This action cannot be undone.',
		successCallback: function () {
			pagination.load();
		}
	});
}
