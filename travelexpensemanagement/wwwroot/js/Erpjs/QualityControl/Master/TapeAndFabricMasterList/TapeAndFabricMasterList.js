let currentPage = 1;
let pageSize = 10;

//=============Page Load=========
let TapeAndFabricPagination;
$(document).ready(function () {
	TapeAndFabricPagination = Pagination.create({
		pageSize: 10,
		paginationContainer: '#pageNumbers',
		infoContainer: '#pageInfoText',
		loader: function (params) {
			$.ajax({
				url: '/TapeAndFabricMasterList/GetTape_FabricList',
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
			console.log(docs);
			const tbody = $('#tblTapeAndFabricMaster tbody');
			tbody.empty();
			if (!docs.length) {
				tbody.append(`<tr><td colspan="10" class="text-center text-muted">No records found.</td></tr>'`);
				return;
			}

			$.each(docs, function (index, item) {
				let actions = '';
				if (window.permissions.canEdit) {
					actions += `<button class="act-btn edit" title="Edit" style="cursor:pointer;" onclick="AddOrEditFunction('${item.code}')"><i class="fa fa-edit"></i></button>`;
				}
				actions += `<button class="act-btn view" title="View" style="cursor:pointer;" onclick="viewMenuDetails('${item.code}')"><i class="fa fa-eye"></i></button>`;
				if (window.permissions.canDelete) {
					actions += `<button class="act-btn delete" title="Delete" style="cursor:pointer;" onclick="deleteTapeAndFabric('${item.code}')"><i class="fa fa-trash"></i></button>`;
				}
				tbody.append(`
						 <tr>
							<td class="code d-none">${item.code}</td>
							<td>${item.name}</td>
							<td>${item.mesH_NAME}</td>
							<td>${item.coloR_NAME}</td>
							<td>${item.uniT_NAME}</td>
							<td>${item.stD_GRAM}</td>
							<td>${item.denier}</td>
							<td>${item.gsm}</td>
							<td>${item.gpd}</td>
							<td class="d-none">${item.stD_STRENGTH}</td>
							<td class="d-none">${item.stD_ELONG}</td>
							<td class="d-none">${item.miN_GRAM}</td>
							<td class="d-none">${item.maX_GRAM}</td>
							<td class="d-none">${item.width}</td>
							<td class="d-none">${item.miN_GPD}</td>
							<td class="d-none">${item.maX_GPD}</td>
							<td class="d-none">${item.strengtH_MAX}</td>
							<td class="d-none">${item.strengtH_MIN}</td>
							<td class="d-none">${item.elonG_MAX}</td>
							<td class="d-none">${item.elonG_MIN}</td>
							<td class="d-none">${item.unlaM_FAB}</td>
							<td class="d-none">${item.laM_FAB}</td>
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
	TapeAndFabricPagination.load();
	// Search
	$('#searchBox').keyup(function () {
		TapeAndFabricPagination.load();
	});
});

// Page Size Change
function changeRowsPerPage() {
	TapeAndFabricPagination.setPageSize(parseInt($('#pageSizeSelect').val()));
	TapeAndFabricPagination.load();
}

//==============Edit==============
function AddOrEditFunction(rowId) {
	window.location.href = '/TapeAndFabricMaster/Index?id=' + encodeURIComponent(rowId);
}

//==============View==============
function viewMenuDetails(rowId) {
	window.location.href = '/TapeAndFabricMaster/Index?id=' + encodeURIComponent(rowId) + '&readOnly=true';
}

//=================Delete================
function deleteTapeAndFabric(docId) {

	// STEP 1: Validate first
	$.ajax({
		url: `/TapeAndFabricMasterList/IsTapeFabricDeletable`,
		type: 'GET',
		data: { docId: docId },

		success: function (response) {

			if (!response.success) {
				Swal.fire('Failed', response.message, 'warning');
				return;
			}

			// STEP 2: Prepare message
			let swalText = "This will permanently delete the Tape And Fabric Details.";
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
					url: `/TapeAndFabricMasterList/DelTape_FabricMast`,
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
								TapeAndFabricPagination.load();
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

// ================= Download Excel =================
document.getElementById("btn-Export-Excel").addEventListener("click", function (e) {
	e.preventDefault();
	window.location.href = "/TapeAndFabricMasterList/ExportAllDocs";
});

// ================= Download Pdf =================
function QCTapeAndFabricReport() {

	var reportName = "loomstd";

	// Crystal Report Formula
	var formula =
		"{TAPE_NFABRIC_MAST.COMP_CODE} = " + window.globalVariables.compCode;
	const today = formatDateddmmyyyy(new Date());


	var formulaFields = {
		Reportname: reportName,
		selectionFormula: formula,
		Database: window.database.db,
		Parameters: {
			comp_name: window.globalVariables.companyName,
			comp_add1: window.globalVariables.add1,
			comp_add2: window.globalVariables.add2,
			F1: "'" + today + "'",
			RPTNAME: "TAPE/FABRIC QC STANDARD MASTER"
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