	let currentVisitorPage = 1;
	let visitorPageSize = 10; 
	let totalVisitorCount = 0;

	$(document).ready(function () {
		loadAllVisitors();

		//===Export====
		$('#btnExport').on('click', function () {
			const searchTerm = $('#tableSearch').val() || '';
			window.location.href = `/VisitorEntryList/ExportVisitorToExcel?searchTerm=${encodeURIComponent(searchTerm)}`;
		});

		//====Print(pdf)====
		$('#btnPrint').on('click', function () {
			const searchTerm = $('#tableSearch').val() || '';
			window.open(`/VisitorEntryList/ExportVisitorToPdf?searchTerm=${encodeURIComponent(searchTerm)}`, '_blank');
		});
	});

	function loadAllVisitors() {
		const searchTerm = $('#tableSearch').val();
		$('.circle-loader').css('display', 'flex');

		$.ajax({
			url: '/VisitorEntryList/GetAllVisitors',
			type: 'GET',
			dataType: 'json',
			data: {
				searchTerm: searchTerm,
				pageNumber: currentVisitorPage,
				pageSize: visitorPageSize
			},
			success: function (res) {

				const visitors = res.visitors || [];
				const totalCount = res.totalCount || 0;

				totalVisitorCount = totalCount; 

				let tbody = $('#tblVisitorEntry tbody');
				tbody.empty();

				if (visitors.length === 0) {
					tbody.append('<tr><td colspan="16" class="text-center text-muted">No visitors found.</td></tr>');
					renderNewPagination();
					return;
				}

				$.each(visitors, function (index, item) {
					tbody.append(`
						<tr>
							<td style="display:none;">${item.doC_ID || ''}</td>
							<td>${item.v_NO || ''}</td>
							<td>${item.v_DATE ? formatDate(item.v_DATE) : ''}</td>
							<td>${item.name || ''}</td>
							<td>${item.organization || ''}</td>
							<td>${item.iN_TIME || ''}</td>
							<td>${item.ouT_TIME || ''}</td>
							<td>${item.meeT_NAME || ''}</td>
							<td>${item.purpose || ''}</td>
							<td>${item.address || ''}</td>
							<td>${item.mobilE_NO || ''}</td>
							<td>${item.vehiclE_NO || ''}</td>
							
							<td class="action-col">
								<button class="act-btn edit" title="Edit" style="cursor:pointer;" onclick="editVisitor('${item.doC_ID}')"><i class="fa fa-edit"></i></button>
								<button class="act-btn view" title="View" style="cursor:pointer;" onclick="viewVisitor('${item.doC_ID}')"><i class="fa fa-eye"></i></button>
								<button class="act-btn delete" title="View" style="cursor:pointer;" onclick="deleteVisitor('${item.doC_ID}')"><i class="fa fa-trash"></i></button>

							</td>
						</tr>
					`);
				});

				renderNewPagination(); 
			},
			error: function (xhr) {
				showCenterToast('Error loading visitors: ' + xhr.responseText, "error");
			},
			complete: function () {
				$('.circle-loader').css('display', 'none');
			}
		});
	}

	//NEW PAGINATION
	function renderNewPagination() {

		const totalPages = Math.ceil(totalVisitorCount / visitorPageSize) || 1;

		let html = '';
		let maxVisible = 5; // kitne buttons dikhane hai
		let startPage = Math.max(1, currentVisitorPage - Math.floor(maxVisible / 2));
		let endPage = startPage + maxVisible - 1;

		if (endPage > totalPages) {
			endPage = totalPages;
			startPage = Math.max(1, endPage - maxVisible + 1);
		}

		// First + dots
		if (startPage > 1) {
			html += `<span class="page-number" onclick="goToVisitorPage(1)">1</span>`;
			if (startPage > 2) {
				html += `<span class="dots">...</span>`;
			}
		}

		// Middle pages
		for (let i = startPage; i <= endPage; i++) {
			html += `<span class="page-number ${i === currentVisitorPage ? 'active' : ''}"
						onclick="goToVisitorPage(${i})">${i}</span>`;
		}

		// Last + dots
		if (endPage < totalPages) {
			if (endPage < totalPages - 1) {
				html += `<span class="dots">...</span>`;
			}
			html += `<span class="page-number" onclick="goToVisitorPage(${totalPages})">${totalPages}</span>`;
		}

		$('#pageNumbers').html(html);

		// Results Text
		let start = (currentVisitorPage - 1) * visitorPageSize + 1;
		let end = Math.min(currentVisitorPage * visitorPageSize, totalVisitorCount);

		if (totalVisitorCount === 0) {
			start = 0;
			end = 0;
		}

		$('#pageInfoText').text(`Results: ${start} - ${end} of ${totalVisitorCount}`);

		$('#prevBtn').prop('disabled', currentVisitorPage === 1);
		$('#nextBtn').prop('disabled', currentVisitorPage === totalPages);
	}

	// Prev / Next
	function prevPage() {
		if (currentVisitorPage > 1) {
			currentVisitorPage--;
			loadAllVisitors();
		}
	}

	function nextPage() {
		const totalPages = Math.ceil(totalVisitorCount / visitorPageSize);
		if (currentVisitorPage < totalPages) {
			currentVisitorPage++;
			loadAllVisitors();
		}
	}

	// Page click
	function goToVisitorPage(page) {
		currentVisitorPage = page;
		loadAllVisitors();
	}

	// Page size change
	function changeRowsPerPage() {
		visitorPageSize = parseInt($('#pageSizeSelect').val());
		currentVisitorPage = 1;
		loadAllVisitors();
	}

	// Search
	$('#tableSearch').on('keyup', function () {
		currentVisitorPage = 1;
		loadAllVisitors();
	});

	// Edit
	function editVisitor(docId) {
		window.location.href = '/VisitorEntry/Index?docId=' + encodeURIComponent(docId);
	}

	// View
	function viewVisitor(docId) {
		window.location.href = '/VisitorEntry/Index?docId=' + encodeURIComponent(docId) + '&readOnly=true';
	}

	function deleteVisitor(docId) {
		deleteRecord('VisitorEntry', docId, {
			action: 'DeleteVisitorEntry',
			title: 'Delete Confirmation',
			text: 'Are you sure you want to delete this entry?',
			successCallback: function () {
				loadAllVisitors();
			}
		});
	}

	// Date format
	function formatDate(dateString) {
		if (!dateString) return '';
		const date = new Date(dateString);
		return date.toLocaleDateString('en-GB');
	}
