var controllerName = window.location.pathname.split('/')[1];

let transitPagination;
$(document).ready(function () {

	checkPermission(controllerName, function () {
		transitPagination.load();
	});

	//===Yesterday Date for wayBillDate==
	const yestDate = getYesterdayYMD();
	$('#DtEWaybillDate').val(yestDate);

	const today = new Date().toISOString().split('T')[0];
	document.getElementById('DtEWaybillDate').setAttribute('max', today);

	transitPagination = Pagination.create({
		pageSize: 10,
		paginationContainer: '#pageNumbers',
		infoContainer: '#pageInfoText',
		loader: function (params) {
			$.ajax({
				url: '/TransitEntryList/GetList',
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
					// toastr.error('Error loading data');
					showToast('Error loading data', { type: "error" });
				}
			});
		},
		render: function (docs) {
			const tbody = $('#tblTransitEntryList tbody');
			tbody.empty();
			if (!docs.length) {
				tbody.append(`<tr><td class="text-center text-muted">No list found.</td></tr>'`);
				return;
			}

			$.each(docs, function (index, item) {
				//let actions = '';
				//if (window.permissions.canEdit) {
				//	actions += `<button class="act-btn edit btn-edit" title="Edit" style="cursor:pointer;" onclick="AddOrEditFunction('${item.v_NO}','${item.v_TYPE}')"><i class="fa fa-edit"></i></button>`;
				//}
				//actions += `<button class="act-btn view btn-view" title="View" style="cursor:pointer;" onclick="viewMenuDetails('${item.v_NO}', '${item.v_TYPE}')"><i class="fa fa-eye"></i></button>`;
				//if (window.permissions.canDelete) {
				//	actions += `<button class="act-btn delete btn-delete" title="Delete" style="cursor:pointer;" onclick="deleteTransit('${item.v_NO}', '${item.v_TYPE}')"><i class="fa fa-trash"></i></button>`;
				//}
				tbody.append(`
					<tr>
						<td>${item.v_TYPE}</td>
						<td>${item.v_NO}</td>
						<td>${item.forM_NO}</td>
						<td>${item.forM_DATE ? formatDateYMD(item.forM_DATE) : ''}</td>
						<td>${item.expirY_DATE ? formatDateYMD(item.expirY_DATE) : ''}</td>
						<td>${item.partyname}</td>
						<td>${item.partY_GSTIN}</td>
						<td>${item.bilL_NO}</td>
						<td>${item.bilL_DATE ? formatDateYMD(item.bilL_DATE) : ''}</td>
						<td>${item.trucK_NO}</td>
						<td class="action-col"><div class="action-wrap">
							<button class="act-btn edit permission-edit" title="Edit" style="cursor:pointer;" onclick="AddOrEditFunction('${item.v_NO}','${item.v_TYPE}')"><i class="fa fa-edit"></i></button>
							<button class="act-btn view btn-view" title="View" style="cursor:pointer;" onclick="viewMenuDetails('${item.v_NO}', '${item.v_TYPE}')"><i class="fa fa-eye"></i></button>
							<button class="act-btn delete permission-delete" title="Delete" style="cursor:pointer;" onclick="deleteTransit('${item.v_NO}', '${item.v_TYPE}')"><i class="fa fa-trash"></i></button>
						</div></td>
					</tr>
				`);
				applyGridPermission();
			});

		}
	});
	// First Load
	transitPagination.load();
	// Search
	$('#searchBox').keyup(function () {
		transitPagination.load();
	});
	$('#DtEWaybillDate').on('change', function () {
		if (this.value > today) {
			showToast('EWaybill Date cannot be greater than current date.', { type: "warning" });
			this.value = today;
			this.focus();
		}
	})
});

// Page Size Change
function changeRowsPerPage() {
	transitPagination.setPageSize(parseInt($('#pageSizeSelect').val()));
	transitPagination.load();
}

function AddOrEditFunction(code, vtype) {
	window.location.href = `/TransitEntry/Index?id=${encodeURIComponent(code)}&vtype=${encodeURIComponent(vtype)}`;
}
function viewMenuDetails(code, vtype) {
	window.location.href = '/TransitEntry/Index?id=' + encodeURIComponent(code) + '&vtype=' + encodeURIComponent(vtype) + '&mode=view';
}
function deleteTransit(code, vType) {
	deleteRecordbytype("TransitEntryList", code, vType, {
		action: "Delete",
		text: "This will permanently delete the Transit entry.",
		successCallback: transitPagination.load
	});
}


//===Import and save EwayBill Data
$('#btnEWayBillImportData').on('click', function () {
	GetEwaybillno();
})
async function GetEwaybillno() {
	try {
		const res = await $.ajax({
			url: '/TransitEntry/GetEWayBillDatacall',
			type: 'GET',
			data: { edate: $('#DtEWaybillDate').val(), inoutdata: "IN" },
			dataType: 'json'
		});

		if (res.success == true) {
			showToast(res.message, { type: "success" });
			transitPagination.load();
		}
		else {
			showToast(res.message, { type: "warning" });
		}

	} catch (error) {
		showToast(error, { type: "error" });
	}
}

function crystalDate(dateStr) {

	if (!dateStr) return "";

	// handle ISO format: yyyy-MM-dd
	var parts = dateStr.includes('-')
		? dateStr.split('-')
		: dateStr.split('/');

	if (parts.length !== 3) return "";

	// detect format
	var year, month, day;

	if (dateStr.includes('-') && parts[0].length === 4) {
		// yyyy-MM-dd
		year = parts[0];
		month = parts[1];
		day = parts[2];
	} else {
		// dd/MM/yyyy
		day = parts[0];
		month = parts[1];
		year = parts[2];
	}

	return `Date(${year},${parseInt(month)},${parseInt(day)})`;
}
function TransitReport() {

	var reportName = "TRANSIT";
	var d1 = $('#DtEWaybillDate').val();
	var d2 = $('#DtEWaybillDate').val();


	// Crystal Report Formula
	var formula =
		"{waybill1.comp_code} = " + window.globalVariables.compCode +
		" and {waybill1.year_code} = " + window.globalVariables.yearCode +
		" and {waybill1.branch_code} = " + window.globalVariables.branchCode +
		" and {WAYBILL1.FORM_DATE} in " +
		crystalDate(d1) + " to " + crystalDate(d2);

	var formulaFields = {
		Reportname: reportName,
		selectionFormula: formula,
		Database: window.database.db,
		Parameters: {
			comp_name: window.globalVariables.companyName,
			comp_add1: window.globalVariables.add1,
			comp_add2: window.globalVariables.add2,
			RPTNAME: "TRANSIT REPORT",
			F1: `From Date ${formatDateddmmyyyy(d1)} to ${formatDateddmmyyyy(d2)}`
		}
	};

	var now = new Date();
	var day = String(now.getDate()).padStart(2, '0');
	var month = String(now.getMonth() + 1).padStart(2, '0');
	var year = String(now.getFullYear()).slice(-2);
	var hours = String(now.getHours()).padStart(2, '0');
	var minutes = String(now.getMinutes()).padStart(2, '0');
	var seconds = String(now.getSeconds()).padStart(2, '0');
	var timestamp = `${day}${month}${year}_${hours}${minutes}${seconds}`;

	$.ajax({
		url: 'http://localhost:34088/Report/PendingQCReport',
		type: 'POST',
		data: JSON.stringify(formulaFields),
		contentType: "application/json",
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

// ================= Download Excel =================
document.getElementById("button_export").addEventListener("click", function (e) {
	e.preventDefault();
	window.location.href = "/TransitEntryList/ExportAllDocs";
});