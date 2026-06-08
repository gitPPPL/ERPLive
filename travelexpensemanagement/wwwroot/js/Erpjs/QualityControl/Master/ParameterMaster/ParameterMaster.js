let PreviousInputDt = "";
let MasterTblId;
var readOnly;


$(document).ready(function () {
	$('#TxtQCParameterName').focus();
	// getUnitList();
	//=======ddlQCUnit============
	bindDropdown('ParameterMaster', 'QCUnit', '#ddlQCUnit', ' Select Unit ', null, null, false, null, true)

	MasterTblId = getQueryParam('id');
	readOnly = getQueryParam('readOnly');


	if (MasterTblId) {
		GetMasterDataList(MasterTblId, readOnly);
	}

	//=======Events============
	$('#TxtQCParameterName').on('keypress', function (e) {
		if (e.key === 'Enter') {
			e.preventDefault();
			$('#TxtShortName').focus();
		}
	});

	$('#TxtShortName').on('keypress', function (e) {
		if (e.key === 'Enter') {
			e.preventDefault();
			$('#ddlQCUnit').focus();
		}
	});

	$('#ChkQuantity').on('change', function () {
		var quantityValue = $(this).is(':checked') ? 1 : 0;
		$(this).val(quantityValue);
	});

	$('#DdActive').on('change', function () {
		let isChecked = $(this).is(':checked');
		let status = isChecked ? 'Active' : 'Inactive';
		let value = isChecked ? 1 : 0;
		$('#statusText').text(status);
		$(this).val(value);
	});

	$('#btn-save').on('click', function (e) {
		e.preventDefault();

		if (!validateRequiredField('#TxtQCParameterName', 'QC Parameter Name')) return;
		if (!validateRequiredField('#ddlQCUnit', 'QC Unit')) return;

		const masterData = collectFormData();

		console.log(masterData);

		if (MasterTblId) {
			EditMasterData(masterData);
		} else {
			SaveMasterData(masterData);
		}
	});
});

//=======Save============
function SaveMasterData(masterData) {
	const Namee = masterData?.Name?.trim();
	console.log(masterData);
	if (!Namee) {
		showToast("Parameter Name is required.", { type: "warning" });
		return;
	}
	checkExistOrNot(Namee)
		.done(function (data) {
			if (data?.status && data?.exists) {
				showToast("Parameter Name Already Exists.", { type: "warning" });
				return;
			}

			$.ajax({
				url: '/ParameterMaster/SaveQParamMast',
				type: 'POST',
				contentType: 'application/json',
				data: JSON.stringify(masterData),
				success: function (response) {
					if (response?.status) {
						showToast('Data saved successfully.', { type: "success" });
						resetFields();
						setTimeout(() => {
							window.location.href = '/ParameterMasterList/Index';
						}, 1500);
					} else {
						showToast(response?.message || "Save failed. Please try again.", { type: "error" });
					}
				},
				error: function () {
					showToast("Error occurred while saving. Please contact admin.", { type: "error" });
				}
			});
		})
		.fail(function () {
			showToast("Error while checking Parameter name.", { type: "error" });
		});
}

//=======Edit============
function EditMasterData(masterData) {
	if (PreviousInputDt !== masterData.Name) {
		checkExistOrNot(masterData.Name)
			.done(function (data) {
				if (data?.status && data?.exists) {
					showToast("Parameter Name Already Exists.", { type: "warning" });
					return;
				}
				UpdateMasterData(masterData);
			})
			.fail(function () {
				showToast("Error while checking Parameter Name.", { type: "error" });
			});
	} else {
		UpdateMasterData(masterData);
	}
}

//=======Update============
function UpdateMasterData(masterData) {
	$.ajax({
		url: '/ParameterMaster/UpdateQParameterMast',
		type: 'POST',
		contentType: 'application/json',
		data: JSON.stringify(masterData),
		dataType: 'json',
		success: function (response) {
			if (response?.status) {
				showToast('Data updated successfully.', { type: "success" });
				resetFields();
				setTimeout(() => {
					window.location.href = '/ParameterMasterList/Index';
				}, 1500);
			} else {
				showToast("Update failed: " + (response?.message || "Unknown error."), { type: "error" });
			}
		},
		error: function (xhr, status, error) {
			showToast("Data not updated: " + error, { type: "error" });
		}
	});
}

//=======Get By Id============
function GetMasterDataList(MasterTblId, readOnly) {
	$.ajax({
		url: '/ParameterMaster/GetQParameterDetailsById',
		type: 'GET',
		data: { id: MasterTblId },
		success: function (res) {
			if (res.status && res.data) {
				console.log(res.data);
				fillFormFields(res.data);
				if (readOnly === 'true') {
					setFormReadOnly();
				}
			} else {
				showToast('No data found for this ID.', { type: "warning" });
			}
		},
		error: function () {
			showToast('Failed to load data.', { type: "error" });
		}
	});
}

//=======Reset Form============
function resetFields() {
	$('#TxtCode, #TxtQCParameterName, #TxtShortName, #searchInput').val('');
	$('#ddlQCUnit').prop('selectedIndex', 0);
	setActiveStatus(1);
	setCheckboxValue(0);
}

//=======Fill Form============
function fillFormFields(dt) {
	console.log(dt);
	// $('#ddlQCUnit').val(dt.QUNIT_CODE);
	bindDropdown('ParameterMaster', 'QCUnit', '#ddlQCUnit', ' Select Unit ', dt.qUnitCd, null, false, null, true)
	$('#TxtCode').val(dt.code);
	$('#TxtQCParameterName').val(dt.name);
	$('#TxtShortName').val(dt.shortName);
	setCheckboxValue(dt.qty);
	setActiveStatus(dt.active);
	PreviousInputDt = dt.name;
}

//=======Collect Data For Save & Update============
function collectFormData() {
	return {
		code: toNullableInt($('#TxtCode').val()),
		Name: toNullableString($('#TxtQCParameterName').val()),
		ShortName: toNullableString($('#TxtShortName').val()),
		QUnitCd: toNullableInt($('#ddlQCUnit').val()),
		// Qty : $('#ChkQuantity').val(),
		Qty: $('#ChkQuantity').is(':checked') ? 1 : 0,
		active: $('#DdActive').val()
	};
}

//=======CHeck Exist Or Not============
function checkExistOrNot(inputData) {
	return $.ajax({
		url: '/ParameterMaster/getExistOrNot',
		type: 'GET',
		dataType: 'json',
		data: { inputData: inputData }
	});
}

//=======Query Params============
function getQueryParam(param) {
	const urlParams = new URLSearchParams(window.location.search);
	return urlParams.get(param);
}

//=======Set Active============
function setActiveStatus(input) {
	let isActive = input == 1;
	$('#DdActive').prop('checked', isActive);
	$('#DdActive').val(isActive ? 1 : 0);
	$('#statusText').text(isActive ? 'Active' : 'Inactive');
}

//=======Set Checkbox============
function setCheckboxValue(value) {
	if (value == 1) {
		$('#ChkQuantity').prop('checked', true);
	} else {
		$('#ChkQuantity').prop('checked', false);
	}
}

//=======Readonly============
function setFormReadOnly() {
	const form = $('#QCParameterForm');
	$('#statusText').addClass('text-muted');
	$('input, #ddlQCUnit, #ChkQuantity, #DdActive').prop('disabled', true);
	$('#btn-save').hide();
	form.addClass('erppage-readonly');
}
