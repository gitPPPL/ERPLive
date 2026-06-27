let quotationPagination;

$(document).ready(function () {

	quotationPagination = Pagination.create({
		pageSize: 10,

		paginationContainer: '#tablePagination',
		infoContainer: '#pageInfoText',

		loader: function (params) {

			const searchTerm = $('#searchBox').val();

			$.ajax({
				url: '/PurchaseQuotationList/GetAllQuotations',
				type: 'GET',
				dataType: 'json',
				data: {
					searchTerm: searchTerm,
					pageNumber: params.pageNumber,
					pageSize: params.pageSize
				},
				success: function (res) {

					params.callback({
						data: res.quotations || [],
						totalCount: res.totalCount || 0
					});

				},
				error: function (xhr) {
					toastr.error('Error loading quotations: ' + xhr.responseText);

					params.callback({
						data: [],
						totalCount: 0
					});
				}
			});
		},

		render: function (quotations) {

			let tbody = $('#tblPurchaseQuotationList tbody');
			tbody.empty();

			if (quotations.length === 0) {

				tbody.append(`
						<tr>
							<td colspan="12" class="text-center text-muted">
								No quotations found.
							</td>
						</tr>
					`);

				return;
			}

			$.each(quotations, function (index, item) {

				let actions = '';

				if (window.permissions.canEdit) {
					actions += `
							<button class="act-btn edit btn-edit"
									onclick="editQuotation(${item.v_NO}, '${item.v_TYPE}')">
								<i class="fa fa-edit"></i>
							</button>`;
				}

				actions += `
						<button class="act-btn view btn-view"
								onclick="viewQuotation(${item.v_NO}, '${item.v_TYPE}')">
							<i class="fa fa-eye"></i>
						</button>`;
                
				if (window.permissions.canDelete) {
					actions += `
							<button class="act-btn delete btn-delete"
									onclick="deleteQuotation(${item.v_NO}, '${item.v_TYPE}')">
								<i class="fa fa-trash"></i>
							</button>`;
				}

				tbody.append(`
						<tr>
							<td style="display:none;">${item.code}</td>
							<td>${item.v_NO || ''}</td>
							<td>${item.v_TYPE || ''}</td>
							<td>${formatDate(item.v_DATE) || ''}</td>
							<td>${item.partY_NAME || ''}</td>
							<td>${item.quotE_NO || ''}</td>
							<td>${formatDate(item.quotE_DATE) || ''}</td>
							<td>${item.conT_PERSON || ''}</td>
							<td>${formatDate(item.valiD_DATE) || ''}</td>
							<td>${item.remarks || ''}</td>
							<td>${item.statuS_NAME || ''}</td>
							<td class="action-col">
								${actions}
							</td>
						</tr>
					`);
			});
		}
	});

	quotationPagination.load();
});

$('#pageSizeSelect').on('change', function () {

	quotationPagination.setPageSize(
		parseInt($(this).val())
	);

});

$('#searchBox').on('keyup', function () {
	quotationPagination.load();
});

$('#prevBtn').on('click', function () {
	Pagination.prev();
});

$('#nextBtn').on('click', function () {
	Pagination.next();
});

function editQuotation(vNo, vType) {
	window.location.href =
		`/PurchaseQuotation/Index?id=${vNo}&vType=${encodeURIComponent(vType)}`;
}

function viewQuotation(vNo, vType) {
	window.location.href =
		`/PurchaseQuotation/Index?id=${vNo}&vType=${encodeURIComponent(vType)}&readOnly=true`;
}

function deleteQuotation(code, vType) {
	Swal.fire({
		title: "Are you sure?",
		text : "This action cannot be undone",
		icon: 'warning',
		showCancelButton: true,
		confirmButtonColor: '#d33',
		cancelButtonColor: '#3085d6',
		confirmButtonText: 'Yes, delete it!',
		cancelButtonText: 'Cancel'
	}).then((result) => {
		if (result.isConfirmed) {
			$.ajax({
				url: '/PurchaseQuotation/DeletePurchaseQuotationByCode',
				type: 'POST',
				data: {
					code: code,
					vType: vType,
					compCode: compCode,
					branchCode: branchCode,
					yearCode: yearCode
				},
				success: function (response) {
					if (response.success) {
						Swal.fire('Deleted!', response.message, 'success').then(() => {
							quotationPagination.load();
						});
					} else {
						Swal.fire('Failed', response.message, 'warning');
					}
				},
				error: function () {
					Swal.fire('Error!', 'An error occurred while deleting.', 'error');
				}
			});
		}
	});
}

function formatDate(dateString) {

	if (!dateString) return '';

	const date = new Date(dateString);

	const day = String(date.getDate()).padStart(2, '0');
	const month = String(date.getMonth() + 1).padStart(2, '0');
	const year = date.getFullYear();

	return `${day}/${month}/${year}`;
}