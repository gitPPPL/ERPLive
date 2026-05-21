
	let currentPage = 1;
	let pageSize = 10;
	let totalCount = 0;

	$(document).ready(function () { 
		GetMasterDataList();
		$('#searchBox').on('keyup', function () {
			currentPage = 1;
			GetMasterDataList();
		});
	});

	function GetMasterDataList() {
		const searchTerm = $('#searchBox').val().trim();
		$.ajax({
			url: '/VehicleInwardEntryList/GetTransportInwardList',
			type: 'GET',
			dataType: 'json',
			data: {
				searchTerm: searchTerm,
				pageNumber: currentPage,
				pageSize: pageSize
			},
			success: function (res) {
				const mastListData = res.data || [];
				const count = res.totalCount || 0;
				totalCount = count
				let tbody = $('#tblPurchaseBillPassEntry tbody');
				tbody.empty();
				console.log(mastListData);
				if (mastListData.length === 0) {
					tbody.append('<tr><td colspan="10" class="text-center text-muted">No PO found.</td></tr>');
					return;
				}
				$.each(mastListData, function (index, item) {
					let actions = '';
					if (window.permissions.canEdit) {
						actions += `<button class="act-btn edit" title="Edit" style="cursor:pointer;" onclick="AddOrEditFunction('${item.docid}')"><i class="fa fa-edit"></i></button>`;
					}
					actions += `<button class="act-btn view" title="View" style="cursor:pointer;" onclick="viewMenuDetails('${item.docid}')"><i class="fa fa-eye"></i></button>`;
					if (window.permissions.canDelete) {
						actions += `<button class="act-btn delete" title="View" style="cursor:pointer;" onclick="deleteVehicleEntry('${item.docid}')"><i class="fa fa-trash"></i></button>`;
					}
					if (window.permissions.canDocDetail) {
						actions += `<button class="act-btn document" title="document" style="cursor:pointer;" onclick="showImpExpExpensePopup('${item.docid}')"><i class="fa fa-file-alt"></i></button>`;
					}

					tbody.append(`
								<tr>
									<td class="d-none code">${item.docid}</td>
									<td>${item.vno}</td>
									<td>${Formatddmmyyyy(item.vdate) || ''}</td>
									<td>${item.vtime}</td>
									<td>${item.dono || 0}</td>
									<td>${item.partyname}</td>
									<td>${item.truckno}</td>
									<td>${item.transport}</td>
									<td>${actions}</td>
								</tr>
								`);
				});

				renderNewPagination();
			},
			error: function (xhr) {
				showToast('Error loading Shift Data list: ' + xhr.responseText, { type: "error" });
			}
		});
	}
	function renderNewPagination() {

			const totalPages = Math.ceil(totalCount / pageSize) || 1;

			let html = '';
			let maxVisible = 2;
			let startPage = Math.max(1, currentPage - Math.floor(maxVisible / 2));
			let endPage = startPage + maxVisible - 1;

			if (endPage > totalPages) {
				endPage = totalPages;
				startPage = Math.max(1, endPage - maxVisible + 1);
			}

			// First + dots
			if (startPage > 1) {
				html += `<button class="btn btn-sm mx-1 page-number" onclick="goToPage(1)">1</button>`;
				if (startPage > 2) {
					html += `<button class="btn btn-sm mx-1 dots">...</button>`;
				}
			}

			// Middle pages
			for (let i = startPage; i <= endPage; i++) {
				html += `<button class="btn btn-sm mx-1 page-number ${i === currentPage ? 'active' : ''}"
							onclick="goToPage(${i})">${i}</button>`;
			}

			// Last + dots
			if (endPage < totalPages) {
				if (endPage < totalPages - 1) {
					html += `<button class="btn btn-sm mx-1 dots">...</button>`;
				}
				html += `<button class="btn btn-sm mx-1 page-number" onclick="goToPage(${totalPages})">${totalPages}</button>`;
			}

			$('#pageNumbers').html(html);

			// Results Text
			let start = (currentPage - 1) * pageSize + 1;
			let end = Math.min(currentPage * pageSize, totalCount);

			if (totalCount === 0) {
				start = 0;
				end = 0;
			}

			$('#pageInfoText').text(`Results: ${start} - ${end} of ${totalCount}`);

			// Button disable
			$('#prevBtn').prop('disabled', currentPage === 1);
			$('#nextBtn').prop('disabled', currentPage === totalPages);
	}

		// Prev / Next
	function prevPage() {
		if (currentPage > 1) {
			currentPage--;
			GetMasterDataList();
		}
	}

	function nextPage() {
		const totalPages = Math.ceil(totalCount / pageSize);
		if (currentPage < totalPages) {
			currentPage++;
			GetMasterDataList();
		}
	}

	// Page click
	function goToPage(page) {
		currentPage = page;
		GetMasterDataList();
	}

	// Page size change
	function changeRowsPerPage() {
		pageSize = parseInt($('#pageSizeSelect').val());
		currentPage = 1;
		GetMasterDataList();
	}

	// Search
	$('#tableSearch').on('keyup', function () {
		currentPage = 1;
		GetMasterDataList();
	});
	function AddOrEditFunction(rowId) {
		window.location.href = '/VehicleInwardEntry/Index?id=' + encodeURIComponent(rowId);
	}

	function viewMenuDetails(rowId) {
		window.location.href = '/VehicleInwardEntry/Index?id=' + encodeURIComponent(rowId) + '&readOnly=true';
	}

	function deleteVehicleEntry(docId) {
		deleteRecord("VehicleInwardEntryList", docId, {
			action: "DeleteVehicleInwardEntry",
			text: "This will permanently delete the vehicle inward entry.",
			successCallback: GetMasterDataList
		});
	}

	function exportToExcel() {
		fetch('/VehicleInwardEntryList/ExportAllDocs')
			.then(response => {
				if (!response.ok) throw new Error("Network response was not ok");
				return response.json();
			})
			.then(responseData => {
				if (!responseData.status) {
					// toastr.error("Failed to fetch data.");
					showToast("Failed to fetch data.", { type: "error" });
					return;
				}
				const dataArray = responseData.data;
				if (!Array.isArray(dataArray) || dataArray.length === 0) {
					// toastr.warning("No data available to export.");
					showToast("No data available to export.", { type: "warning" });
					return;
				}

				const worksheet = XLSX.utils.json_to_sheet(dataArray, { header: Object.keys(dataArray[0]) });
				const colWidths = Object.keys(dataArray[0]).map(key => {
					const maxLen = Math.max(
						key.length,
						...dataArray.map(row => (row[key] ? row[key].toString().length : 0))
					);
					return { wch: maxLen + 2 };
				});
				worksheet['!cols'] = colWidths;


				const headerRowNumber = 1;
				Object.keys(dataArray[0]).forEach((_, idx) => {
					const cellAddress = XLSX.utils.encode_cell({ c: idx, r: headerRowNumber - 1 });
					if (!worksheet[cellAddress]) return;
					worksheet[cellAddress].s = {
						font: { bold: true },
						fill: { fgColor: { rgb: "FFFF00" } }
					};
				});

				const workbook = XLSX.utils.book_new();
				XLSX.utils.book_append_sheet(workbook, worksheet, "AllDocs");

				const pageName = "TransportInward_List";
				const now = new Date();
				const pad = n => String(n).padStart(2, '0');
				const timestamp = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}_${pad(now.getHours())}${pad(now.getMinutes())}${pad(now.getSeconds())}`;
				const fileName = `${pageName}_${timestamp}.xlsx`;

				XLSX.writeFile(workbook, fileName);
			})
			.catch(error => {
				console.error("Export failed:", error);
				// toastr.error("Failed to export data.");
				showToast("Failed to export data.", { type: "error" });
			});
	}

	function callGetReportAsPdf() {
		var reportName = "rpt_Vehicle_Inward_Entry";
		var now = new Date();
		var day = String(now.getDate()).padStart(2, '0');
		var month = String(now.getMonth() + 1).padStart(2, '0');
		var year = String(now.getFullYear()).slice(-2);
		var hours = String(now.getHours()).padStart(2, '0');
		var minutes = String(now.getMinutes()).padStart(2, '0');
		var seconds = String(now.getSeconds()).padStart(2, '0');
		var timestamp = `${day}${month}${year}_${hours}${minutes}${seconds}`;

		$.ajax({
			url: 'http://192.168.20.51:8082/Report/GetReportAsPdf',
			type: 'GET',
			data: {Reportname: reportName },
			xhrFields: {
				responseType: 'blob'
			},
			success: function (response) {
				console.log('PDF response:', response);
				var file = new Blob([response], {type: 'application/pdf' });
				var fileName = `${reportName}_${timestamp}.pdf`;

				var link = document.createElement('a');
				link.href = URL.createObjectURL(file);
				link.download = fileName;
				document.body.appendChild(link);
				link.click();
				document.body.removeChild(link);
			},
			error: function (xhr, status, error) {
				console.error('Error generating report:', error);
			}
		});
	}

	function showImpExpExpensePopup(docCode) {
		$.ajax({
			url: '/VehicleInwardEntryList/GetVehicleInwardEntryDetails',
			type: 'Get',
			dataType: 'json',
			data: { docid: docCode },
			success: function (response) {
				if (response.status) {
					showDocumentPopupjQuery(response.data, docCode);
				} else {
					// toastr.error("Failed to get document details.");
					showToast("Failed to get document details.", { type: "error" });
				}
			},
			error: function () {
				// toastr.error("An error occurred while fetching document details.");
				showToast("An error occurred while fetching document details.", { type: "error" });
			}
		});
	}
	//======Format Date===========
	function GetDateYYYYMMDD(date){
		let parts = date.split('T');
		let newDate = parts[0];
		return newDate;
	};
	function Formatddmmyyyy(date){
		const input = GetDateYYYYMMDD(date);
		const [year, month, day] = input.split("-");
		const formatted = `${day}/${month}/${year}`;
		return formatted;
	}
