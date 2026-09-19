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
					<td> ${item.vNo} </td> 
					<td> ${formatDate(item.vDate)} </td> 
					<td> ${item.ipAddress ?? ''} </td> 
					<td> ${item.assetCat ?? ''} </td> 
					<td> ${item.deviceType ?? ''} </td> 
					<td> ${item.empCode ?? ''} </td> 
					<td> ${item.empName ?? ''} </td> 
					<td> ${item.assetCode ?? ''} </td> 
					<td> ${item.deviceName ?? ''} </td> 
					<td> ${item.deviceModel ?? ''} </td> 
					<td> ${item.unitName ?? ''} </td> 
					<td> ${item.location ?? ''} </td> 
					<td> ${formatDate(item.purchaseDate)} </td> 
					<td> ${item.purchaseFrom ?? ''} </td> 
					<td> ${item.usedBy ?? ''} </td>
					<td> ${item.remarks ?? ''} </td>
					<td class="action-col">
					  <div class="action-wrap">
							<button class="act-btn edit btn-edit permission-edit" title="Edit Row" style="cursor:pointer;" onclick="editITInventory('${item.docId}')"><i class="fa fa-edit"></i></button>
							<button class="act-btn view btn-view" title="View Row" style="cursor:pointer;" onclick="viewITInventory('${item.docId}')"><i class="fa fa-eye"></i></button>
							<button class="act-btn delete btn-delete permission-delete" title="Delete Row" style="cursor:pointer;" onclick="deleteITInventroyEntry('${item.docId}')"><i class="fa fa-trash"></i></button>
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

function editITInventory(docId) {
	window.location.href = '/SalesSaudaEntry/Index?docId=' + encodeURIComponent(docId);
}

function viewITInventory(docId) {
	window.location.href = '/SalesSaudaEntry/Index?docId=' + encodeURIComponent(docId) + '&readOnly=true';
}



