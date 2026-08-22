
let importPaymentPagination;
var controllerName = window.location.pathname.split('/')[1];
$(document).ready(function () {

	checkPermission(controllerName, function () {
		importPaymentPagination.load();
	});

	// Initialize Pagination
	importPaymentPagination = Pagination.create({

		pageSize: parseInt($('#pageSizeSelect').val()) || 10,

		paginationContainer: '#tablePagination',
		infoContainer: '#pageInfoText',

		loader: function (params) {

			const searchTerm = $('#searchBox').val().trim();

			$.ajax({
				url:'/ImportPaymentList/LoadListData',
				type: 'GET',
				data: {
					searchTerm: searchTerm,
					pageNumber: params.pageNumber,
					pageSize: params.pageSize
				},

				success: function (res) {
					console.log("Import Payment List Response:", res);
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
						showToast(res.message || "Unable to load data" ,{ type: "error" });
					}
				},
				
				error: function (xhr) {

					console.error('Load Import Payment List Error:', xhr);

					params.callback({
						data: [],
						totalCount: 0
					});
					showToast("Error while loading Import Payment List", { type: "error" });
		 
				}
			});
		},

		render: function (data) {

			renderImportPaymentTable(data);

		}
	});

	// Initial Load
	importPaymentPagination.load();

	// Page Size Change
	$('#pageSizeSelect').on('change', function () {

		const pageSize = parseInt($(this).val());

		importPaymentPagination.setPageSize(pageSize);

	});

	// Search
	let searchTimer;

	$('#searchBox').on('input', function () {

		clearTimeout(searchTimer);

		searchTimer = setTimeout(function () {

			importPaymentPagination.reset();

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

	const tbody = $('#tblImportPaymentList tbody');

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

					<td>
						${item.vNo ?? ''}
					</td>

					<td>
						${item.vType ?? ''}
					</td>

					<td>
						${formatDate(item.vDate)}
					</td>

					<td class="action-col">

					  <div class="action-wrap">
							<button class="act-btn edit btn-edit permission-edit" title="Edit Row" style="cursor:pointer;" onclick="editImportPayment('${item.docId}')"><i class="fa fa-edit"></i></button>
							<button class="act-btn view btn-view" title="View Row" style="cursor:pointer;" onclick="viewImportPayment('${item.docId}')"><i class="fa fa-eye"></i></button>
							<button class="act-btn delete btn-delete permission-delete" title="Delete Row" style="cursor:pointer;" onclick="deleteImportPayment('${item.docId}')"><i class="fa fa-trash"></i></button>
							<button class="act-btn document btn-document" title="Document Details" style="cursor:pointer;" onclick="showDocumentPopup('${item.docId}')"><i class="fa fa-file"></i></button>
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

function editImportPayment(docId) {
	window.location.href ='/ImportPaymentEntry/Index?docId=' +encodeURIComponent(docId);
}

function viewImportPayment(docId) {
	window.location.href ='/ImportPaymentEntry/Index?docId=' + encodeURIComponent(docId) + '&readOnly=true';
}

function deleteImportPayment(docId) {

	deleteRecord('ImportPaymentList', docId, {
		action: 'DeleteImportPaymentEntry',
		title: 'Delete Import Payment?',
		text: 'Are you sure you want to delete this Import Payment Entry?',
		successCallback: function () {

			// Reload current pagination
			importPaymentPagination.load();
		}
	});
}
