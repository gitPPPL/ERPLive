let currentPage = 1;
let pageSize = 10;
//=============Page Load=========
let QCMasterPagination;
$(document).ready(function () {

	//=========QC Group DDL for print report===========
	bindDropdown('QCMaster', 'QCGroup', '#ddlQCGroupPR', ' Select QC Group ', null, null, false, null, false);

	QCMasterPagination = Pagination.create({
		pageSize: 10,
		paginationContainer: '#pageNumbers',
		infoContainer: '#pageInfoText',
		loader: function (params) {
			$.ajax({
				url: '/QCMasterList/GetQCMasterLList',
				type: 'GET',
				dataType: 'json',
				data: {
					searchTerm: $('#searchBox').val(),
					pageNumber: params.pageNumber,
					pageSize: params.pageSize
				},
				success: function (res) {
					params.callback({
						data: res.groups,
						totalCount: res.totalCount
					});
				},
				error: function (xhr) {
					showToast('Error loading data', { type: "error" });
				}
			});
		},
		render: function (docs) {
			console.log(docs);
			const tbody = $('#tblQCMaster tbody');
			tbody.empty();
			if (!docs.length) {
				tbody.append(`<tr><td colspan="6" class="text-center text-muted">No records found.</td></tr>'`);
				return;
			}

			$.each(docs, function (index, item) {
				let actions = '';
				if (permissions.canEdit) {
					actions += `<button class="act-btn edit" title="Edit Row" style="cursor:pointer;" onclick="AddOrEditItemGroup(${item.code})"><i class="fa fa-edit"></i></button>`;
				}
				actions += `<button class="act-btn view" title="View Row" style="cursor:pointer;" onclick="viewItemGroupDetails(${item.code})"><i class="fa fa-eye"></i></button>`;
				if (permissions.canDelete) {
					actions += `<button class="act-btn delete btn-delete" title="Delete Row" style="cursor:pointer;" onclick="deleteQC(${item.code})"><i class="fa fa-trash"></i></button>`;
				}
				tbody.append(`
						 <tr>
                                <td>${item.code}</td>
                                <td>${item.name}</td>
                                <td>${item.shortName}</td>
                                <td>${item.qcGroup}</td>
                                <td>${item.active === 1
				                    ? '<span class="erppagestatus-badge erppagestatus-active"><i class="fa fa-check-circle"></i>&nbsp;Active</span>'
				                    : '<span class="erppagestatus-badge erppagestatus-inactive"><i class="fa fa-times-circle"></i>&nbsp;Inactive</span>'
				                }
				                </td>
				                <td class="action-col">${actions}</td>
				         </tr>
					`);
			});

		}
	});
	// First Load
	QCMasterPagination.load();
	// Search
	$('#searchBox').keyup(function () {
		QCMasterPagination.load();
	});
});

// Page Size Change
function changeRowsPerPage() {
	QCMasterPagination.setPageSize(parseInt($('#pageSizeSelect').val()));
	QCMasterPagination.load();
}

function AddOrEditItemGroup(id) {
    window.location.href = '/QCMaster/Index?id=' + id;
}
function viewItemGroupDetails(id) {
    window.location.href = '/QCMaster/Index?id=' + id + '&mode=view';
}

//=================Delete================
function deleteQC(docId) {

	// STEP 1: Validate first
	$.ajax({
		url: `/QCMasterList/IsQcDeletable`,
		type: 'GET',
		data: { docId: docId },

		success: function (response) {

			if (!response.success) {
				Swal.fire('Failed', response.message, 'warning');
				return;
			}

			// STEP 2: Prepare message
			let swalText = "This will permanently delete the QC Details.";
			let cancelBtn = true;
			let confirmBtn = true;
			let swalTitle = "Are you sure?";
			if (response.isExists) {
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
					url: `/QCMasterList/DeleteQcMaster`,
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

//============REPORT============

//=============Add Row For Report=========
function addRow(item = {}) {
	const tbody = $('#tblPrintReport tbody');
	const code = item.code || '';
	const group = item.name || '';

	if (!code) return;

	// ❌ Check duplicate properly
	let exists = false;
	tbody.find('.TxtCodePR').each(function () {
		if ($(this).val() === code) {
			exists = true;
			showToast("Already Added!", { type: "warning" });
			return false; // ✅ break the loop
		}
	});

	// Stop if duplicate
	if (exists) return;

	// Add new row
	let row = `
        <tr class="no-border-input">
            <td><input type="number" class="form-control TxtCodePR" value="${code}" /></td>
            <td><input type="text" class="form-control TxtQCGroupPR" value="${group}" /></td>
        </tr>`;

	tbody.append(row);
}

$('#ddlQCGroupPR').on('change', function () {
	var selectedCode = $(this).val();
	var selectedName = $(this).find("option:selected").text();
	if (selectedCode) {
		addRow({ code: selectedCode, name: selectedName });
	}
})

//========Generate Report=========

function buildQCGCondition() {
	let codes = [];

	$('#tblPrintReport tbody .TxtCodePR').each(function () {
		let val = $(this).val();
		if (val) {
			codes.push(val);
		}
	});

	if (codes.length === 0) return "";

	let condition = " AND (";
	condition += codes.map(code => `{QC_MAST.QCGROUP_CODE}=${code}`).join(" OR ");
	condition += ")";

	return condition;
}

function QCMasterReport() {
	var reportName = ($('#report1').is(':checked')) ? "rptQCMaster" : "rptQCMaster1";

	let QCGCodeFormula = buildQCGCondition();

	// Crystal Report Formula
	var formula =
		`{QC_MAST.COMP_CODE} = ${window.globalVariables.compCode}`

	if (QCGCodeFormula) {
		formula += QCGCodeFormula;
	}
	var formulaFields = {
		Reportname: reportName,
		selectionFormula: formula,
		Database: window.database.db,
		Parameters: {
			comp_name: window.globalVariables.companyName,
			comp_add1: window.globalVariables.add1,
			comp_add2: window.globalVariables.add2,
			RPTNAME: "'QC Master Report'"
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

//==================Print pdf=============
function QCReport() {
	var reportName = "rptQCMaster";

	// Crystal Report Formula
	var formula =
		`{QC_MAST.COMP_CODE} = ${window.globalVariables.compCode}`

	var formulaFields = {
		Reportname: reportName,
		selectionFormula: formula,
		Database: window.database.db,
		Parameters: {
			comp_name: window.globalVariables.companyName,
			comp_add1: window.globalVariables.add1,
			comp_add2: window.globalVariables.add2,
			RPTNAME: "'QC Master Report'"
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
document.getElementById("btn-Export-Excel").addEventListener("click", function (e) {
	e.preventDefault();
	window.location.href = "/QCMasterList/ExportAllDocs";
});
