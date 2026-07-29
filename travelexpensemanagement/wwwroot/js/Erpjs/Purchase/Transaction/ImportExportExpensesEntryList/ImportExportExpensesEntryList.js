let purchaseReceiptPagination;

$(document).ready(function () {

	purchaseReceiptPagination = Pagination.create({

		pageSize: 10,

		paginationContainer: "#pageNumbers",

		infoContainer: "#pageInfoText",

		loader: function (params) {

			$.ajax({
				url: '/ImportExportExpensesEntryList/GetPurchaseReceiptEntryList',
				type: 'GET',
				dataType: 'json',
				data: {
					searchTerm: $('#searchBox').val(),
					pageNumber: params.pageNumber,
					pageSize: params.pageSize
				},
				success: function (res) {

					params.callback({
						totalCount: res.totalCount,
						data: res.items
					});

				},
				error: function () {
					alert("Error fetching data");
				}
			});

		},

		render: renderPurchaseReceiptList

	});

	purchaseReceiptPagination.load();

	$('#searchBox').on('keyup', function () {
		purchaseReceiptPagination.load();
	});

});

function renderPurchaseReceiptList(docs) {

	const tbody = $('#tblPurchaseReceiptEntryList tbody');

	tbody.empty();

	if (docs.length === 0) {

		tbody.append(`
            <tr>
                <td colspan="19" class="text-center text-muted">
                    No records found.
                </td>
            </tr>
        `);

		return;
	}

	docs.forEach(doc => {

		tbody.append(`
            <tr>
                <td>${doc.vType || ''}</td>
                <td>${doc.vNo || ''}</td>
                <td>${doc.vDate || ''}</td>
                <td>${doc.partyName || ''}</td>
                <td>${doc.billNo || ''}</td>
                <td>${doc.billDate || ''}</td>
                <td>${doc.billAdd1 || ''}</td>
                <td>${doc.billAdd2 || ''}</td>
                <td>${doc.billCity || ''}</td>
                <td>${doc.billGST || ''}</td>
                <td>${doc.shipTo && typeof doc.shipTo === 'object'
				? (doc.shipTo.Name || '')
				: (doc.shipTo || '')}</td>
                <td>${doc.qty || ''}</td>
                <td>${doc.amount || ''}</td>
                <td>${doc.remarks && typeof doc.remarks === 'object'
				? (doc.remarks.Text || '')
				: (doc.remarks || '')}</td>
                <td>${typeof doc.transportName === 'object'
				? (doc.transportName.Name || '')
				: (doc.transportName || '')}</td>
                <td>${doc.gateNo || ''}</td>
                <td>${doc.status || ''}</td>

                <td class="action-col">
                    <div class="action-wrap">

                        <button class="act-btn edit btn-edit"
                            onclick="editDocStage('${doc.vType}','${doc.vNo}')">
                            <i class="fa fa-edit"></i>
                        </button>

                        <button class="act-btn view btn-view"
                            onclick="viewDocStage('${doc.vType}','${doc.vNo}')">
                            <i class="fa fa-eye"></i>
                        </button>

                        <button class="act-btn delete btn-delete"
                            onclick="deleteDocStage('${doc.vType}','${doc.vNo}')">
                            <i class="fa fa-trash"></i>
                        </button>

                        <button class="act-btn document btn-document"
                            onclick="showDocumentPopup('${doc.vNo}')">
                            <i class="fa fa-file"></i>
                        </button>

                    </div>
                </td>

            </tr>
        `);

	});

}

function changeRowsPerPage() {

	purchaseReceiptPagination.setPageSize(
		parseInt($('#pageSizeSelect').val())
	);

}

function prevPage() {
	Pagination.prev();
}

function nextPage() {
	Pagination.next();
}

function editDocStage(vType, vNo) {
	window.location.href = `/ImportExportExpensesEntry/Index?vType=${encodeURIComponent(vType)}&vNo=${encodeURIComponent(vNo)}`;
}

function viewDocStage(vType, vNo) {
	window.location.href = `/ImportExportExpensesEntry/Index?vType=${encodeURIComponent(vType)}&vNo=${encodeURIComponent(vNo)}&readOnly=true`;
}

function deleteDocStage(vType, vNo) {
	Swal.fire({
		title: 'Are you sure?',
		text: "You won't be able to revert this deletion!",
		icon: 'warning',
		showCancelButton: true,
		confirmButtonColor: '#d33',
		cancelButtonColor: '#3085d6',
		confirmButtonText: 'Yes, delete it!'
	}).then((result) => {
		if (result.isConfirmed) {
			$.ajax({
				url: '/ImportExportExpensesEntryList/DeleteDocByCode',
				type: 'POST',
				data: {
					vType: vType,
					vNo: vNo
				},
				success: function (response) {
					if (response.success) {
						toastr.success(response.message || "Record deleted successfully.");
						loadPurchaseReceiptEntryList();
					} else {
						toastr.warning(response.message || "Delete failed.");
					}
				},
				error: function () {
					toastr.error("An error occurred while deleting.");
				}
			});
		}
	});
}
