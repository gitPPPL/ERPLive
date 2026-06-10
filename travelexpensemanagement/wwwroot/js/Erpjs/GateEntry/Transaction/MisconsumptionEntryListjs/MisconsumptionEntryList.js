let pager = null;

$(document).ready(function () {

	pager = Pagination.create({
		pageSize: 10,
		paginationContainer: '#pageNumbers',
		infoContainer: '#pageInfoText',

		loader: function ({ pageNumber, pageSize, callback }) {

			const searchTerm = $('#tableSearch').val();

			$.ajax({
				url: '/MiscConsumptionEntryList/GetList',
				type: 'GET',
				dataType: 'json',
				data: { searchTerm, pageNumber, pageSize },

				success: function (res) {

					if (!res.success) {
						toastr.error(res.message || "Failed to load data.");
						callback({ data: [], totalCount: 0 });
						return;
					}

					callback({
						data: res.headers ?? res.lists ?? res.data ?? [],
						totalCount: res.totalCount ?? 0
					});
				},

				error: function (xhr) {
					toastr.error('Error loading list: ' + xhr.responseText);
					callback({ data: [], totalCount: 0 });
				}
			});
		},

		render: function (list) {

			const tbody = $('#tblMiscConsumptionList tbody');
			tbody.empty();

			if (!Array.isArray(list) || list.length === 0) {
				tbody.append(`
			<tr>
				<td colspan="10" class="text-center text-muted">
					No records found.
				</td>
			</tr>`);
				return;
			}

			list.forEach(item => {

				let actions = '';

				// Edit Permission
				if (window.permissions.canEdit) {
					actions += `
					<button class="act-btn edit"
							title="Edit"
							style="cursor:pointer;"
							onclick="AddOrEditFunction('${item.v_NO}','${item.vtypeCode}')">
						<i class="fa fa-edit"></i>
					</button>`;
				}

				// View Always Visible
				actions += `
				<button class="act-btn view"
						title="View"
						style="cursor:pointer;"
						onclick="viewMenuDetails('${item.v_NO}', '${item.vtypeCode}')">
					<i class="fa fa-eye"></i>
				</button>`;

				// Delete Permission
				if (window.permissions.canDelete) {
					actions += `
					<button class="act-btn delete"
							title="Delete"
							style="cursor:pointer;"
							onclick="deleteTemp('${item.v_NO}', '${item.vtypeCode}')">
						<i class="fa fa-trash"></i>
					</button>`;
				}

				tbody.append(`
			<tr>
				<td>${item.v_NO ?? ''}</td>
				<td>${item.v_TYPE ?? ''}</td>
				<td>${formatDate(item.v_DATE)}</td>
				<td>${item.partY_NAME ?? ''}</td>
				<td class="action-col">
					${actions}
				</td>
			</tr>
		`);
			});
			
		}
	});

	pager.load();

});

function formatDate(dateStr) {
	if (!dateStr) return '';

	const d = new Date(dateStr);
	if (isNaN(d)) return '';

	const day = String(d.getDate()).padStart(2, '0');
	const month = String(d.getMonth() + 1).padStart(2, '0');
	const year = d.getFullYear();

	return `${day}-${month}-${year}`; 
}

// Page size change
function changeRowsPerPage() {
	const size = $('#pageSizeSelect').val();
    pager.setPageSize(parseInt(size, 10));
}

// Search (reset to page 1)
$('#tableSearch').on('input', function () {
	pager.setPageSize(parseInt($('#pageSizeSelect').val(), 10));
});

//===Edit Mode=====
function AddOrEditFunction(code, vtype) {
	window.location.href = `/MiscConsumptionEntry/Index?id=${encodeURIComponent(code)}&vtype=${encodeURIComponent(vtype)}`;
}

//===View Mode=====
function viewMenuDetails(code, vtype) {
	window.location.href = '/MiscConsumptionEntry/Index?id=' + encodeURIComponent(code) + '&vtype=' + encodeURIComponent(vtype) + '&mode=view';
}

//===Delete Data====
function deleteTemp(code, vtype) {
	let vNo = code;
	let docType = vtype;
	deleteRecordbytype(
		"MiscConsumptionEntryList", vNo, docType,                      
		{
			action: "Delete",
			title: "Delete Confirmation",
			text: "Are you sure you want to delete this entry?",
			successCallback: function () {
				pager.load(); 
			}
		}
	);
}

function PendingQCReport() {

	// var reportName = "rpt_city_master";
	var reportName = "Rpt_gate_MiscConsumption";
	var now = new Date();
	var day = String(now.getDate()).padStart(2, '0');
	var month = String(now.getMonth() + 1).padStart(2, '0');
	var year = String(now.getFullYear()).slice(-2);
	var hours = String(now.getHours()).padStart(2, '0');
	var minutes = String(now.getMinutes()).padStart(2, '0');
	var seconds = String(now.getSeconds()).padStart(2, '0');
	var timestamp = `${day}${month}${year}_${hours}${minutes}${seconds}`;

	$.ajax({
		url: 'http://localhost:34089/Report/PendingQCReport',
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
