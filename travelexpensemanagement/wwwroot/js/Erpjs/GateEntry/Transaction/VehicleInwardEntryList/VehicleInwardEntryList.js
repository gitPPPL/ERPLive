

let vehiclePagination;
$(document).ready(function () {
	
	vehiclePagination = Pagination.create({
		pageSize: 10,
		paginationContainer: '#pageNumbers',
		infoContainer: '#pageInfoText',
		loader: function (params) {
			$.ajax({
				url: '/VehicleInwardEntryList/GetTransportInwardList',
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
			const tbody = $('#tblPurchaseBillPassEntry tbody');
			tbody.empty();
			if (!docs.length) {
				tbody.append(`<tr><td colspan="10" class="text-center text-muted">No list found.</td></tr>'`);
				return;
			}

			$.each(docs, function (index, item) {
				let actions = '';
								if (window.permissions.canEdit) {
									actions += `<button class="act-btn edit" title="Edit" style="cursor:pointer;" onclick="checkModificationAllowed('${item.vdate}', '${item.docid}')"><i class="fa fa-edit"></i></button>`;
								}
								actions += `<button class="act-btn view" title="View" style="cursor:pointer;" onclick="viewMenuDetails('${item.docid}')"><i class="fa fa-eye"></i></button>`;
								if (window.permissions.canDelete) {
									actions += `<button class="act-btn delete" title="View" style="cursor:pointer;" onclick="deleteVehicleEntry('${item.docid}')"><i class="fa fa-trash"></i></button>`;
								}
								if (window.permissions.canDocDetail) {
									actions += `<button class="act-btn document" title="document" style="cursor:pointer;" onclick="showImpExpExpensePopup('${item.docid}')"><i class="fa fa-file-alt"></i></button>`;
								}
				tbody.append(`
					<tr>
									<td class="d-none code">${item.docid}</td>
									<td>${item.vno}</td>
									<td>${Formatddmmyyyy(item.vdate) || ''}</td>
									<td>${item.vtime}</td>
									<td>${item.dono || 0}</td>
									<td>${item.partyname}</td>
									<td>${item.truckno}</td>
									<td>${item.transport}</td>
									<td class="action-col">${actions}</td>
								</tr>
				`);
			});

		}
	});
	// First Load
	vehiclePagination.load();
	// Search
	$('#searchBox').keyup(function () {
		vehiclePagination.load();
	});
});

// Page Size Change
function changeRowsPerPage() {
	vehiclePagination.setPageSize(parseInt($('#pageSizeSelect').val()));
	vehiclePagination.load();
}

	function AddOrEditFunction(rowId) {
		window.location.href = '/VehicleInwardEntry/Index?id=' + encodeURIComponent(rowId);
	}

	function viewMenuDetails(rowId) {
		window.location.href = '/VehicleInwardEntry/Index?id=' + encodeURIComponent(rowId) + '&readOnly=true';
	}

	function deleteVehicleEntry(docId) {
		deleteRecord("VehicleInwardEntryList", docId, {
			action: "DeleteVehicleInwardEntry",
			text: "This will permanently delete the vehicle inward entry.",
			successCallback: vehiclePagination.load
		});
	}

function checkModificationAllowed(vDate, rowId) {
	checkModificationDays({
		controller: 'VehicleInwardEntry',
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
	window.location.href = "/VehicleInwardEntryList/ExportAllDocs";
});

	//=======================Commented as per Tiwari Sir===================
//function VehicleReport() {

//	var reportName = "Rpt_Transport_Inward";

//	var formula =
//		"{Gatepass1.comp_code} = " + window.globalVariables.compCode +
//		" and {GATEPASS1.YEAR_CODE}=" + window.globalVariables.yearCode +
//		" AND {GATEPASS1.BRANCH_CODE}=" + window.globalVariables.branchCode;
//		" AND {GATEPASS1.V_TYPE}= '" + vtype +
//		"' AND {GATEPASS1.V_NO}=" + vNo;

//	var formulaFields = {
//		Reportname: reportName,
//		selectionFormula: formula,
//		Database: window.database.db,
//		Parameters: {
//			comp_name: window.globalVariables.companyName,
//			comp_add1: window.globalVariables.add1,
//			comp_add2: window.globalVariables.add2,
//			RPTNAME: vtype
//		}
//	};

//	var now = new Date();
//	var day = String(now.getDate()).padStart(2, '0');
//	var month = String(now.getMonth() + 1).padStart(2, '0');
//	var year = String(now.getFullYear()).slice(-2);
//	var hours = String(now.getHours()).padStart(2, '0');
//	var minutes = String(now.getMinutes()).padStart(2, '0');
//	var seconds = String(now.getSeconds()).padStart(2, '0');
//	var timestamp = `${day}${month}${year}_${hours}${minutes}${seconds}`;

//	$.ajax({
//		url: 'http://localhost:34088/Report/PendingQCReport',
//		type: 'POST',
//		data: JSON.stringify(formulaFields),
//		contentType: "application/json",
//		xhrFields: {
//			responseType: 'blob'
//		},
//		success: function (response) {
//			console.log('PDF response:', response);
//			var file = new Blob([response], { type: 'application/pdf' });
//			var fileName = `${reportName}_${timestamp}.pdf`;

//			var link = document.createElement('a');
//			link.href = URL.createObjectURL(file);
//			link.download = fileName;
//			document.body.appendChild(link);
//			link.click();
//			document.body.removeChild(link);
//		},
//		error: function (xhr, status, error) {
//			console.error('Error generating report:', error);
//		}
//	});
//}
	

	function showImpExpExpensePopup(docCode) {
		$.ajax({
			url: '/VehicleInwardEntryList/GetVehicleInwardEntryDetails',
			type: 'Get',
			dataType: 'json',
			data: { docid: docCode },
			success: function (response) {
				if (response.status) {
					showDocumentPopupjQuery(response.data, docCode);
				} else {
					// toastr.error("Failed to get document details.");
					showToast("Failed to get document details.", { type: "error" });
				}
			},
			error: function () {
				// toastr.error("An error occurred while fetching document details.");
				showToast("An error occurred while fetching document details.", { type: "error" });
			}
		});
	}
	//======Format Date===========
	function GetDateYYYYMMDD(date){
		let parts = date.split('T');
		let newDate = parts[0];
		return newDate;
	};
	function Formatddmmyyyy(date){
		const input = GetDateYYYYMMDD(date);
		const [year, month, day] = input.split("-");
		const formatted = `${day}/${month}/${year}`;
		return formatted;
	}
