let importPaymentPagination;
var controllerName = window.location.pathname.split('/')[1];
$(document).ready(function () {

	checkPermission(controllerName, function () {
		importPaymentPagination.load();
	});

	importPaymentPagination = Pagination.create({

		pageSize: parseInt($('#pageSizeSelect').val()) || 10,

		paginationContainer: '#tablePagination',
		infoContainer: '#pageInfoText',

		loader: function (params) {

			const searchTerm = $('#searchBox').val().trim();

			$.ajax({
				url: '/InsuranceCoverNoteList/LoadListData',
				type: 'GET',
				data: {
					searchTerm: searchTerm,
					pageNo: params.pageNumber,
					pageSize: params.pageSize
				},

				success: function (res) {
					if (res.success) {

						params.callback({
							data: res.data || [],
							totalCount: res.totalCount || 0
						});

					} else {

						params.callback({
							data: [],
							totalCount: 0
						});
						showToast(res.message || "Unable to load data", { type: "error" });
					}
				},

				error: function (xhr) {

					console.error('Load Insurance List Error:', xhr);

					params.callback({
						data: [],
						totalCount: 0
					});
					showToast("Error while Loading Insurance List", { type: "error" });

				}
			})
		},

		render: function (data) {

			renderImportPaymentTable(data);

		}
	});

	importPaymentPagination.load();

	$('#pageSizeSelect').on('change', function () {

		const pageSize = parseInt($(this).val());

		importPaymentPagination.setPageSize(pageSize);

	});

	let searchTimer;

	$('#searchBox').on('input', function () {

		clearTimeout(searchTimer);

		searchTimer = setTimeout(function () {

			importPaymentPagination.load();

		}, 300);

	});

	// Previous
	$('#prevBtn').on('click', function () {

		Pagination.prev();

	});

	// Next
	$('#nextBtn').on('click', function () {

		Pagination.next();

	});

});

function renderImportPaymentTable(data) {

	const tbody = $('#tblInsuranceCover tbody');

	tbody.empty();

	if (!data || data.length === 0) {

		tbody.append(`
				<tr>
					<td colspan="5" style="text-align:center;">
						No records found
					</td>
				</tr>
		`);

		return;
	}

	$.each(data, function (index, item) {

		tbody.append(`

				<tr>
					<td class="hidden-col">
						${item.vtype ?? ''}
					</td>

					<td>${item.vNo ?? ''}</td>

					<td>${formatDate(item.vDate)}</td>

					<td>${item.insuranceName ?? ''}</td>

					<td>${item.billName ?? ''}</td>

					<td>${item.fromBillNo ?? ''}</td>

					<td>${item.toBillNo ?? ''}</td>

					<td>${item.itemName ?? ''}</td>
					
					<td class="action-col">
					  <div class="action-wrap">
							<button class="act-btn edit btn-edit permission-edit" title="Edit Row" style="cursor:pointer;" onclick="editInsuranceEntry('${item.vNo}', '${item.vType}')"><i class="fa fa-edit"></i></button>
							<button class="act-btn view btn-view" title="View Row" style="cursor:pointer;" onclick="viewInsuranceEntry('${item.vNo}', '${item.vType}')"><i class="fa fa-eye"></i></button>
							<button class="act-btn delete btn-delete permission-delete" title="Delete Row" style="cursor:pointer;" onclick="deleteInsuranceEntry('${item.vNo}', '${item.vType}')"><i class="fa fa-trash"></i></button>
					  </div>

					</td>

				</tr>

		`);
		applyGridPermission();
	});

}

function formatDate(dateValue) {

	if (!dateValue)
		return '';

	const date = new Date(dateValue);

	if (isNaN(date.getTime()))
		return dateValue;

	const day = String(date.getDate()).padStart(2, '0');
	const month = String(date.getMonth() + 1).padStart(2, '0');
	const year = date.getFullYear();

	return `${day}-${month}-${year}`;
}

function editInsuranceEntry(vNo, vType) {
	window.editInsuranceEntry = function (vNo, vType) {
		window.location.href =`/InsuranceCoverNote/Index?id=${encodeURIComponent(vNo)}&vType=${encodeURIComponent(vType)}`;
	};

}

function viewInsuranceEntry(vNo, vType) {
	window.location.href = '/InsuranceCoverNote/Index?id=' + encodeURIComponent(vNo) + '&vType=' + encodeURIComponent(vType) + '&readOnly=true';
}

function deleteInsuranceEntry(vNo, vType) {

	Swal.fire({
		title: 'Are you sure?',
		text: 'Do you want to delete this Insurance Cover Note?',
		icon: 'warning',
		showCancelButton: true,
		confirmButtonText: 'Yes, Delete',
		cancelButtonText: 'Cancel'
	}).then(function (result) {

		if (!result.isConfirmed)
			return;

		$.ajax({
			url: '/InsuranceCoverNoteList/DeleteData',
			type: 'DELETE',
			data: {
				vNo: vNo,
				vType: vType
			},

			success: function (res) {

				if (res.success) {

					Swal.fire({
						title: 'Deleted!',
						text: res.message || 'Data deleted successfully.',
						icon: 'success',
						confirmButtonText: 'OK'
					}).then(function () {

						// Reload list after OK
						importPaymentPagination.load();

					});

				} else {

					Swal.fire({
						title: 'Error',
						text: res.message || 'Unable to delete data.',
						icon: 'error',
						confirmButtonText: 'OK'
					});

				}
			},

			error: function (xhr) {

				console.error('Delete Insurance Cover Note Error:', xhr);

				Swal.fire({
					title: 'Error',
					text: xhr.responseJSON?.message ||
						'Error while deleting Insurance Cover Note.',
					icon: 'error',
					confirmButtonText: 'OK'
				});
			}
		});

	});
}