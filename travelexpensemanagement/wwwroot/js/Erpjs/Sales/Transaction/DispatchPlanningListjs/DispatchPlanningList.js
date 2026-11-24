let dispatchPlanning;
var controllerName = window.location.pathname.split('/')[1];
$(document).ready(function () {

	checkPermission(controllerName, function () {
		dispatchPlanning.load();
	});

	dispatchPlanning = Pagination.create({

		pageSize: parseInt($('#pageSizeSelect').val()) || 10,

		paginationContainer: '#tablePagination',
		infoContainer: '#pageInfoText',

		loader: function (params) {

			const searchTerm = $('#searchBox').val().trim();

			$.ajax({
				url: '/DispatchPlanningList/LoadListData',
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
	
	dispatchPlanning.load();

	$('#pageSizeSelect').on('change', function () {

		const pageSize = parseInt($(this).val());

		dispatchPlanning.setPageSize(pageSize);

	});

	let searchTimer;

	$('#searchBox').on('input', function () {

		clearTimeout(searchTimer);

		searchTimer = setTimeout(function () {

			dispatchPlanning.load();

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

	const tbody = $('#tblDispatchPlanning tbody');

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

					<td class="action-col">
					  <div class="action-wrap">
							<button class="act-btn edit btn-edit permission-edit" title="Edit Row" style="cursor:pointer;" onclick="editDispatchPlanning('${item.vNo}', '${item.vType}')"><i class="fa fa-edit"></i></button>
							<button class="act-btn view btn-view" title="View Row" style="cursor:pointer;" onclick="viewDispatchPlanning('${item.vNo}', '${item.vType}')"><i class="fa fa-eye"></i></button>
							<button class="act-btn delete btn-delete permission-delete" title="Delete Row" style="cursor:pointer;" onclick="deleteDispatchPlanning('${item.vNo}', '${item.vType}')"><i class="fa fa-trash"></i></button>
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

function editDispatchPlanning(vNo, vType) {
	window.location.href = `/DispatchPlaning/Index?id=${encodeURIComponent(vNo)}&vType=${encodeURIComponent(vType)}`;
}

function viewDispatchPlanning(vNo, vType) {
	window.location.href = '/DispatchPlaning/Index?id=' + encodeURIComponent(vNo) + '&vType=' + encodeURIComponent(vType) + '&readOnly=true';
}

function deleteDispatchPlanning(vNo, vType) {

	Swal.fire({
		title: 'Are you sure?',
		text: "You want to delete this Dispatch Planning?",
		icon: 'warning',
		showCancelButton: true,
		confirmButtonColor: '#d33',
		cancelButtonColor: '#3085d6',
		confirmButtonText: 'Yes, delete it!',
		cancelButtonText: 'Cancel'
	}).then((result) => {

		if (result.isConfirmed) {

			$.ajax({
				url: '/DispatchPlanningList/DeleteData',
				type: 'POST',
				data: {
					vNo: vNo,
					vType: vType
				},

				success: function (res) {

					if (res.success) {

						Swal.fire({
							title: 'Deleted!',
							text: res.message || 'Dispatch Planning deleted successfully.',
							icon: 'success'
						});

						dispatchPlanning.load();

					} else {

						showToast(res.message || 'Unable to delete Dispatch Planning.', { type: 'error' });
					}
				},

				error: function (xhr) {

					console.error('Delete Dispatch Planning Error:', xhr);

					showToast('Error while deleting Dispatch Planning.', { type: 'error' });
				}
			});
		}
	});
}