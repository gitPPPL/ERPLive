let transitPagination;
$(document).ready(function () {
	//===Yesterday Date for wayBillDate==
	const yestDate = getYesterdayYMD();
	$('#DtEWaybillDate').val(yestDate);

	const today = new Date().toISOString().split('T')[0];
	document.getElementById('DtEWaybillDate').setAttribute('max', today);

	transitPagination = Pagination.create({
		pageSize: 10,
		paginationContainer: '#pageNumbers',
		infoContainer: '#pageInfoText',
		loader: function (params) {
			$.ajax({
				url: '/TransitEntryList/GetList',
				type: 'GET',
				dataType: 'json',
				data: {
					searchTerm: $('#searchBox').val(),
					pageNumber: params.pageNumber,
					pageSize: params.pageSize
				},
				success: function (res) {
					params.callback({
						data: res.lists,
						totalCount: res.totalCount
					});
				},
				error: function (xhr) {
					// toastr.error('Error loading data');
					showToast('Error loading data', { type: "error" });
				}
			});
		},
		render: function (docs) {
			const tbody = $('#tblTransitEntryList tbody');
			tbody.empty();
			if (!docs.length) {
				tbody.append(`<tr><td colspan="12" class="text-center text-muted">No list found.</td></tr>'`);
				return;
			}

			$.each(docs, function (index, item) {
				tbody.append(`
					<tr>
						<td>${item.v_TYPE}</td>
						<td>${item.v_NO}</td>
						<td>${item.forM_NO}</td>
						<td>${item.forM_DATE ? formatDateYMD(item.forM_DATE) : ''}</td>
						<td>${item.expirY_DATE ? formatDateYMD(item.expirY_DATE) : ''}</td>
						<td>${item.partyname}</td>
						<td>${item.partY_GSTIN}</td>
						<td>${item.bilL_NO}</td>
						<td>${item.bilL_DATE ? formatDateYMD(item.bilL_DATE) : ''}</td>
						<td>${item.trucK_NO}</td>
						<td class="action-col">

							<button class="act-btn edit" title="Edit" style="cursor:pointer;" onclick="AddOrEditFunction('${item.v_NO}','${item.v_TYPE}')"><i class="fa fa-edit"></i></button>
							<button class="act-btn view" title="View" style="cursor:pointer;" onclick="viewMenuDetails('${item.v_NO}', '${item.v_TYPE}')"><i class="fa fa-eye"></i></button>
							<button class="act-btn delete" title="Delete" style="cursor:pointer;" onclick="deleteTransit('${item.v_NO}', '${item.v_TYPE}')"><i class="fa fa-trash"></i></button>
							
						</td>
					</tr>
				`);
			});

		}
	});
	// First Load
	transitPagination.load();
	// Search
	$('#searchBox').keyup(function () {
		transitPagination.load();
	});
	$('#DtEWaybillDate').on('change', function () {
		if (this.value > today) {
			showToast('EWaybill Date cannot be greater than current date.', { type: "warning" });
			this.value = today;
			this.focus();
		}
	})
});

// Page Size Change
function changeRowsPerPage() {
	transitPagination.setPageSize(parseInt($('#pageSizeSelect').val()));
	transitPagination.load();
}

function AddOrEditFunction(code, vtype) {
	window.location.href = `/TransitEntry/Index?id=${encodeURIComponent(code)}&vtype=${encodeURIComponent(vtype)}`;
}
function viewMenuDetails(code, vtype) {
	window.location.href = '/TransitEntry/Index?id=' + encodeURIComponent(code) + '&vtype=' + encodeURIComponent(vtype) + '&mode=view';
}
function deleteTransit(code, vType) {
	deleteRecordByType("TransitEntryList", code, vType, {
		action: "Delete",
		text: "This will permanently delete the Transit entry.",
		successCallback: transitPagination.load
	});
}

//===Import and save EwayBill Data
$('#btnEWayBillImportData').on('click', function () {
	GetEwaybillno();
})
async function GetEwaybillno() {
	try {
		const res = await $.ajax({
			url: '/TransitEntry/GetEWayBillDatacall',
			type: 'GET',
			data: { edate: $('#DtEWaybillDate').val(), inoutdata: "IN" },
			dataType: 'json'
		});

		if (res.success == true) {
			showToast(res.message, { type: "success" });
			transitPagination.load();
		}
		else {
			showToast(res.message, { type: "warning" });
		}

	} catch (error) {
		showToast(error, { type: "error" });
	}
}