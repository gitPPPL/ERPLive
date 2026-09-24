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
				url: '/SalesSaudaList/LoadListData',
				type: 'GET',
				data: {
					searchTerm: searchTerm,
					pageNo: params.pageNumber,
					pageSize: params.pageSize
				},
				  
				success: function (res) {
					console.log("Store Inventory Response:", res);
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

					console.error('Load Store Inentory List Error:', xhr);

					params.callback({
						data: [],
						totalCount: 0
					});
					showToast("Error while loading Store Inventory List", { type: "error" });

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

	const tbody = $('#tblSalesSaudaList tbody');

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
						${item.docId ?? ''}
					</td>

					<td>${item.vNo ?? ''}</td>

					<td>${formatDate(item.vDate)}</td>

					<td>${item.customerName ?? ''}</td>

					<td>${item.cityName ?? ''}</td>

					<td>${item.phone ?? ''}</td>

					<td>${item.itemName ?? ''}</td>

					<td>${item.itemType ?? ''}</td>

					<td>${item.remark ?? ''}</td>
					
					<td class="action-col">
					  <div class="action-wrap">
							<button class="act-btn edit btn-edit permission-edit" title="Edit Row" style="cursor:pointer;" onclick="editSaleSaudaEntry('${item.vNo}', '${item.vType}')"><i class="fa fa-edit"></i></button>
							<button class="act-btn view btn-view" title="View Row" style="cursor:pointer;" onclick="viewSaleSaudaEntry('${item.vNo}', '${item.vType}')"><i class="fa fa-eye"></i></button>
							<button class="act-btn delete btn-delete permission-delete" title="Delete Row" style="cursor:pointer;" onclick="deleteSaleSaudaEntry('${item.vNo}', '${item.vType}')"><i class="fa fa-trash"></i></button>
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

function editSaleSaudaEntry(vNo, vType) {

	$.ajax({
		url: '/SalesSaudaList/ValidateEdit',
		type: 'GET',
		data: {
			vNo: vNo,
			vType: vType
		},
		success: function (res) {

			// Approval is in process
			if (!res.success) {

				Swal.fire({
					icon: 'error',
					title: 'Stop Modification',
					text: res.message,
					confirmButtonText: 'OK'
				});

				return;
			}

			// SALE2 warning
			if (res.saleWarning) {

				Swal.fire({
					icon: 'warning',
					title: 'Warning',
					text: res.saleWarning,
					confirmButtonText: 'OK'
				}).then(function () {

					if (res.orderWarning) {

						Swal.fire({
							icon: 'warning',
							title: 'Warning',
							text: res.orderWarning,
							confirmButtonText: 'OK'
						}).then(function () {

							window.location.href ='/SalesSaudaEntry/Index?id=' + encodeURIComponent(vNo) + '&vType=' + encodeURIComponent(vType);
						});

					} else {

						window.location.href ='/SalesSaudaEntry/Index?id=' + encodeURIComponent(vNo) + '&vType=' + encodeURIComponent(vType);
					}
				});

				return;
			}

			// ORDER2 warning
			if (res.orderWarning) {

				Swal.fire({
					icon: 'warning',
					title: 'Warning',
					text: res.orderWarning,
					confirmButtonText: 'OK'
				}).then(function () {

					window.location.href ='/SalesSaudaEntry/Index?id=' + encodeURIComponent(vNo) + '&vType=' +encodeURIComponent(vType);
				});

				return;
			}

			window.location.href ='/SalesSaudaEntry/Index?id=' + encodeURIComponent(vNo) + '&vType=' +encodeURIComponent(vType);
		},

		error: function (xhr) {

			console.error('Validate Edit Error:', xhr);

			showToast('Error while validating document edit',{ type: 'error' });
		}
	});
}

function viewSaleSaudaEntry(vNo, vType) {
	window.location.href ='/SalesSaudaEntry/Index?id=' + encodeURIComponent(vNo) + '&vType=' + encodeURIComponent(vType) +'&readOnly=true';
}

function deleteSaleSaudaEntry(vNo, vType) {

	// Step 1: Validate
	$.ajax({
		url: '/SalesSaudaList/ValidateDelete',
		type: 'GET',
		data: {
			vNo: vNo,
			vType: vType
		},

		success: function (res) {

			if (!res.success) {

				Swal.fire({
					icon: res.warning ? 'warning' : 'error',
					title: res.warning ? 'Warning' : 'Stop Deletion',
					text: res.warning || res.message,
					confirmButtonText: 'OK'
				});

				return;
			}

			Swal.fire({
				icon: 'warning',
				title: 'Confirm Deletion',
				text: 'Are you sure you want to delete this document?',
				showCancelButton: true,
				confirmButtonText: 'Yes, Delete',
				cancelButtonText: 'No'
			}).then(function (result) {
				if (!result.isConfirmed) {
					return;
				}
				// Step 3: Actual Delete
				$.ajax({
					url: '/SalesSaudaList/DeleteData',
					type: 'POST',
					data: {
						vNo: vNo,
						vType: vType
					},

					success: function (deleteRes) {

						if (deleteRes.success) {

							Swal.fire({
								icon: 'success',
								title: 'Deleted',
								text: deleteRes.message,
								confirmButtonText: 'OK'
							}).then(function () {

								importPaymentPagination.load();

							});

						} else {

							Swal.fire({
								icon: 'error',
								title: 'Error',
								text: deleteRes.message,
								confirmButtonText: 'OK'
							});
						}
					},

					error: function (xhr) {

						console.error('Delete Error:', xhr);

						showToast('Error while deleting document',{ type: 'error' });
					}
				});
			});
		},

		error: function (xhr) {

			console.error('Validate Delete Error:', xhr);

			showToast('Error while validating document deletion',{ type: 'error' });
		}
	});
}


