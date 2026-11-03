let deliveryOrderPagination;
var controllerName = window.location.pathname.split('/')[1];
const formMode = new URLSearchParams(window.location.search).get('mode') || 'DOrder';
$(document).ready(function () {

	checkPermission(controllerName, function () {
		deliveryOrderPagination.load();
	});

	deliveryOrderPagination = Pagination.create({

		pageSize: parseInt($('#pageSizeSelect').val()) || 10,

		paginationContainer: '#tablePagination',
		infoContainer: '#pageInfoText',

		loader: function (params) {

			const searchTerm = $('#searchBox').val().trim();

			$.ajax({
				url: '/DeliveryOrderList/LoadListData',
				type: 'GET',
				data: {
					searchTerm: searchTerm,
					pageNo: params.pageNumber,
					pageSize: params.pageSize
				},

				success: function (res) {
					console.log("Delivery Order Response:", res);
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

					console.error('Delivery Order Error:', xhr);

					params.callback({
						data: [],
						totalCount: 0
					});
					showToast("Error while loading Delivery order List", { type: "error" });

				}
			})
		},

		render: function (data) {

			renderImportPaymentTable(data);

		}
	});

	deliveryOrderPagination.load();

	$('#pageSizeSelect').on('change', function () {

		const pageSize = parseInt($(this).val());

		deliveryOrderPagination.setPageSize(pageSize);

	});

	let searchTimer;

	$('#searchBox').on('input', function () {

		clearTimeout(searchTimer);

		searchTimer = setTimeout(function () {

			deliveryOrderPagination.load();

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

	const tbody = $('#tblDeliveryOrder tbody');

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
					<td>
						${item.vType ?? ''}
					</td>

					<td>${item.vNo ?? ''}</td>

					<td>${formatDate(item.vDate)}</td>

					<td>${formatDate(item.delDate)}</td>

					<td>${item.billName ?? ''}</td>

					<td>${item.billAdd1 ?? ''}</td>

					<td>${item.billAdd2 ?? ''}</td>

					<td>${item.billAdd3 ?? ''}</td>

					<td>${item.billCity ?? ''}</td>

					<td>${item.agentCode ?? ''}</td>

					<td>${item.shipName ?? ''}</td>

					<td>${item.shipAdd1 ?? ''}</td>

					<td>${item.shipAdd2 ?? ''}</td>

					<td>${item.shipAdd3 ?? ''}</td>

					<td>${item.shipCity ?? ''}</td>

					<td>${item.taxCode ?? ''}</td>

					<td>${item.itemType ?? ''}</td>

					<td>${item.amount ?? ''}</td>

					<td>${item.packPer ?? ''}</td>

					<td>${item.packAmt ?? ''}</td>

					<td>${item.totNet ?? ''}</td>

					<td class="action-col">
						<div class="action-wrap">
							<button class="act-btn edit btn-edit permission-edit" title="Edit Row" style="cursor:pointer;" onclick="editDeliveryOrder('${item.vNo}', '${item.vType}')"><i class="fa fa-edit"></i></button>
							<button class="act-btn view btn-view" title="View Row" style="cursor:pointer;" onclick="viewDeliveryOrder('${item.vNo}', '${item.vType}')"> <i class="fa fa-eye"></i> </button>
							<button class="act-btn delete btn-delete permission-delete" title="Delete Row" style="cursor:pointer;" onclick="deleteDeliveryOrder('${item.vNo}', '${item.vType}')"> <i class="fa fa-trash"></i> </button>
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

function editDeliveryOrder(vNo, vType) {
	window.location.href =
		'/DeliveryOrder/Index?id=' + encodeURIComponent(vNo) +
		'&vType=' + encodeURIComponent(vType) +
		'&mode=' + encodeURIComponent(formMode);
}
function viewDeliveryOrder(vNo, vType) {
	window.location.href =
		'/DeliveryOrder/Index?id=' + encodeURIComponent(vNo) +
		'&vType=' + encodeURIComponent(vType) +
		'&mode=' + encodeURIComponent(formMode) +
		'&readOnly=true';
}