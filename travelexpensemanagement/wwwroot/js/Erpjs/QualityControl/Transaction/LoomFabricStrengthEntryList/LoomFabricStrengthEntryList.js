let loomFabricPagination;

$(document).ready(function () {
	
	loomFabricPagination = Pagination.create({

		pageSize: 10,

		paginationContainer: '#pageNumbers',

		infoContainer: '#pageInfoText',

		loader: function (params) {

			$.ajax({
				url: '/LoomFabricStrengthEntryList/GetLoomFabricStrengthList',
				type: 'GET',
				dataType: 'json',
				data: {
					searchTerm: $('#searchBox').val().trim(),
					pageNumber: params.pageNumber,
					pageSize: params.pageSize
				},
				success: function (res) {
					params.callback(res);
				},
				error: function () {
					toastr.error('Error loading data');
				}
			});
		},
		render: renderLoomFabricList
	});

	loomFabricPagination.load();

	$('#searchBox').on('keyup', function () {
		loomFabricPagination.load();
	});

});

function renderLoomFabricList(mastListData) {
	let tbody = $('#tblLoomFabricStrengthEntryList tbody');
	tbody.empty();

	if (!mastListData || mastListData.length === 0) {

		tbody.append(`
				<tr>
					<td colspan="9" class="text-center text-muted">
						No Record Found
					</td>
				</tr>`);

		return;
	}

	$.each(mastListData, function (index, item) {

		let actions = '';

		if (window.permissions.canEdit) {
			actions += `
					<i class="fa fa-edit btn-edit"
					   data-bs-toggle="tooltip"
					   title="Edit"
					   onclick="AddOrEditFunction('${item.DOC_ID}')">
					</i>`;
		}

		actions += `
				<i class="fas fa-eye btn-view"
				   data-bs-toggle="tooltip"
				   title="View"
				   onclick="viewMenuDetails('${item.DOC_ID}')">
				</i>`;

		if (window.permissions.canDelete) {
			actions += `
					<i class="fas fa-trash btn-delete"
					   data-bs-toggle="tooltip"
					   title="Delete"
					   onclick="deleteData('${item.DOC_ID}')">
					</i>`;
		}

		if (window.permissions.canDocDetail) {
			actions += `
					<i class="fa fa-file btn-document btn-details"
					   data-bs-toggle="tooltip"
					   data-bs-title="Document Details"
					   onclick="showImpExpExpensePopup('${item.DOC_ID}')">
					</i>`;
		}

		tbody.append(`
				<tr>
					<td class="d-none code">${item.DOC_ID}</td>
					<td>${item.V_NO}</td>
					<td style="display:none;">${item.V_TYPE}</td>
					<td>${item.V_DATE}</td>
					<td>${item.SHIFT}</td>
					<td>${item.EmployeeName}</td>
					<td>${item.PlaceName}</td>
					<td>${item.REMARKS}</td>
					<td>${actions}</td>
				</tr>
	    `);
	});
}

function changeRowsPerPage() {
	let size = $('#pageSizeSelect').val();
	loomFabricPagination.setPageSize(size);
}

function AddOrEditFunction(rowId) {
	window.location.href = '/LoomFabricStrengthEntry/Index?id=' + encodeURIComponent(rowId);
}

function viewMenuDetails(rowId) {
	window.location.href = '/LoomFabricStrengthEntry/Index?id=' + encodeURIComponent(rowId) + '&readOnly=true';
}

function deleteData(docId) {
	deleteRecord('LoomFabricStrengthEntryList', docId, {
		action: 'DeleteLoomFabricStrengthEntry',
		title: 'Are you sure?',
		text: 'This action cannot be undone.',
		successCallback: function () {
			loomFabricPagination.load();
		}
	});
}

function exportToExcel() {
	fetch('/LoomFabricStrengthEntryList/ExportAllDocs')
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

function showImpExpExpensePopup(docCode) {
	$.ajax({
		url: '/LoomFabricStrengthEntryList/GetLoomFabricStrengthEntryDetails',
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

function callLoomQcReportAsPdf(vno) {

	var reportName = "QC_LOOM1";

	var formula =
		"{PROD1_QC.V_TYPE} = 'LMQC' " +
		"and {PROD1_QC.V_NO} = " + vno +
		" and {PROD1_QC.COMP_CODE} = " + window.globalVariables.compCode +
		" and {PROD1_QC.YEAR_CODE} = " + window.globalVariables.yearCode +
		" and {PROD1_QC.BRANCH_CODE} = " + window.globalVariables.branchCode;

	var formulaFields = {
		Reportname: reportName,
		selectionFormula: formula,
		Parameters: {
			RPTNAME: "LOOM FABRIC STRENGTH REPORT",
			comp_name: window.globalVariables.companyName,
			comp_add1: window.globalVariables.add1,
			comp_add2: window.globalVariables.add2
		}
	};

	var now = new Date();
	var timestamp =
		String(now.getDate()).padStart(2, '0') +
		String(now.getMonth() + 1).padStart(2, '0') +
		String(now.getFullYear()).slice(-2) + "_" +
		String(now.getHours()).padStart(2, '0') +
		String(now.getMinutes()).padStart(2, '0') +
		String(now.getSeconds()).padStart(2, '0');
    
	$.ajax({
		url: 'http://localhost:34088/Report/PendingQCReport',
		type: 'POST',
		data: JSON.stringify(formulaFields),
		contentType: "application/json",
		xhrFields: { responseType: 'blob' },

		success: function (response) {
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
