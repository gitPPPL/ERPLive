const inputSequence = [
	'TxtName',
	'ddlMeshName',
	'NumDenier',
	'TxtPPHD',
	'ddlColor',
	'NumWidth',
	'NumStdGram',
	'NumMinGram',
	'NumMaxGram',
	'NumStdGPD',
	'NumMinGPD',
	'NumMaxGPD',
	'NumStdStrength',
	'NumStrengthMin',
	'NumStrengthMax',
	'NumStdElong',
	'NumElongMin',
	'NumElongMax',
	'NumLamFabStr',
	'NumUnlamFabStr',
	'NumGSM'
];

let PreviousInputDt = "";
let MasterTblId;
var readOnly;

$(document).ready(function () {
	$('#TxtName').focus();

	//=======Bind Dropdowns============
	bindDropdown('TapeAndFabricMaster', 'Color', '#ddlColor', ' Select Color ', null, null, false, null, false)
	bindDropdown('TapeAndFabricMaster', 'Mesh', '#ddlMeshName', ' Select Mesh ', null, null, false, null, false)

	MasterTblId = getQueryParam('id');
	readOnly = getQueryParam('readOnly');

	if (MasterTblId) {
		GetMasterDataList(MasterTblId, readOnly);
	}

	inputSequence.forEach((id, index) => {
		$(`#${id}`).on('keypress', function (e) {
			if (e.key === 'Enter') {
				e.preventDefault();
				if (index + 1 < inputSequence.length) {
					$(`#${inputSequence[index + 1]}`).focus();
				}
			}
		});
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

		if (!validateRequiredField('#TxtName', 'Name')) return;
		if (!validateRequiredField('#ddlMeshName', 'Mesh Name')) return;
		if (!validateRequiredField('#NumDenier', 'Denier')) return;
		if (!validateRequiredField('#TxtPPHD', 'PP/HD')) return;
		if (!validateRequiredField('#ddlColor', 'Color')) return;
		if (!validateRequiredField('#NumWidth', 'Width(MM.)')) return;
		if (!validateRequiredField('#NumStdGram', 'Std. Gram')) return;
		if (!validateRequiredField('#NumMinGram', 'Min. Gram')) return;
		if (!validateRequiredField('#NumMaxGram', 'Max. Gram')) return;
		if (!validateRequiredField('#NumStdGPD', 'Std. GPD')) return;
		if (!validateRequiredField('#NumMinGPD', 'Min. GPD')) return;
		if (!validateRequiredField('#NumMaxGPD', 'Max. GPD')) return;
		if (!validateRequiredField('#NumStdStrength', 'Std. Strength')) return;
		if (!validateRequiredField('#NumStrengthMin', 'Min. Strength')) return;
		if (!validateRequiredField('#NumStrengthMax', 'Max. Strength')) return;
		if (!validateRequiredField('#NumStdElong', 'Std. Elong(%)')) return;
		if (!validateRequiredField('#NumElongMin', 'Min. Elong(%)')) return;
		if (!validateRequiredField('#NumElongMax', 'Max. Elong(%)')) return;
		if (!validateRequiredField('#NumLamFabStr', 'Lam fab Str')) return;
		if (!validateRequiredField('#NumUnlamFabStr', 'Unlam Fab Str')) return;
		if (!validateRequiredField('#NumGSM', 'GSM')) return;


		const masterData = collectFormData();

		if (MasterTblId) {
			EditMasterData(masterData);
		} else {
			SaveMasterData(masterData);
		}
	});
});

function SaveMasterData(masterData) {
	const Namee = masterData?.Name?.trim();
	if (!Namee) {
		showToast("TapeNFabric Name is required!", { type: "warning" });
		return;
	}
	checkExistOrNot(Namee)
		.done(function (data) {
			if (data?.status && data?.exists) {
				showToast("TapeNFabric Name Already Exists!", { type: "warning" });
				return;
			}

			$.ajax({
				url: '/TapeAndFabricMaster/SaveTape_NFabricMast',
				type: 'POST',
				contentType: 'application/json',
				data: JSON.stringify(masterData),
				success: function (response) {
					if (response?.status) {
						showToast('Data saved successfully.', { type: "success" });
						resetFields();
						setTimeout(() => {
							window.location.href = '/TapeAndFabricMasterList/Index';
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
			showToast("Error while checking TapeNFabric name.", { type: "error" });
		});
}

function EditMasterData(masterData) {
	if (PreviousInputDt !== masterData.Name) {
		checkExistOrNot(masterData.Name)
			.done(function (data) {
				if (data?.status && data?.exists) {
					showToast("TapeNFabric Name Already Exists!", { type: "warning" });
					return;
				}
				UpdateMasterData(masterData);
			})
			.fail(function () {
				showToast("Error while checking TapeNFabric Name.", { type: "error" });
			});
	} else {
		UpdateMasterData(masterData);
	}
}

function UpdateMasterData(masterData) {
	$.ajax({
		url: '/TapeAndFabricMaster/UpdateTape_NFabricMast',
		type: 'POST',
		contentType: 'application/json',
		data: JSON.stringify(masterData),
		dataType: 'json',
		success: function (response) {
			if (response?.status) {
				showToast('Data updated successfully.', { type: "success" });
				resetFields();
				setTimeout(() => {
					window.location.href = '/TapeAndFabricMasterList/Index';
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

function GetMasterDataList(MasterTblId, readOnly) {
	$.ajax({
		url: '/TapeAndFabricMaster/GetTape_NFabricDetailsById',
		type: 'GET',
		data: { id: MasterTblId },
		success: function (res) {
			if (res.status && res.data) {
				fillFormFields(res.data);
				if (readOnly === 'true') {
					setFormReadOnly();
				} else {
					$('input, #ddlColor, #ddlMeshName, #DdActive').prop('disabled', false);
					$('#btn-save').show();
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

function resetFields() {
	$('#TxtCode, #TxtName, #NumStdGram, #NumMaxGram, #NumDenier, #NumStdGPD, #NumMaxGPD, #NumStrengthMin, #NumStdElong, #NumElongMax, #NumLamFabStr, #NumMinGram, #NumGSM, #TxtPPHD, #NumWidth, #NumMinGPD, #NumStdStrength, #NumStrengthMax, #NumElongMin, #NumUnlamFabStr').val('');
	$('#ddlColor, #ddlMeshName').prop('selectedIndex', 0);
	setActiveStatus(1);
}

function fillFormFields(dt) {
	console.log("Fill Form Datat", dt);
	$('#TxtCode').val(dt.code);
	$('#TxtName').val(dt.name);
	$('#NumStdGram').val(dt.stdGram);
	$('#NumMinGram').val(dt.minGram);
	$('#NumMaxGram').val(dt.maxGram);
	$('#NumGSM').val(dt.gsm);
	$('#NumDenier').val(dt.denier);
	$('#NumWidth').val(dt.width);
	$('#NumStdGPD').val(dt.gpd);
	$('#NumMinGPD').val(dt.minGpd);
	$('#NumMaxGPD').val(dt.maxGpd);
	$('#NumStdStrength').val(dt.stdStrength);
	$('#NumStrengthMax').val(dt.strengthMax);
	$('#NumStrengthMin').val(dt.strengthMin);
	$('#NumStdElong').val(dt.stdElong);
	$('#NumElongMax').val(dt.elongMax);
	$('#NumElongMin').val(dt.elongMin);
	$('#NumUnlamFabStr').val(dt.unlamFab);
	$('#NumLamFabStr').val(dt.lamFab);
	$('#TxtPPHD').val(dt.unitName);
	setActiveStatus(dt.active);
	PreviousInputDt = dt.name;
	bindDropdown('TapeAndFabricMaster', 'Color', '#ddlColor', ' Select Color ', dt.colorCode, null, false, null, false)
	bindDropdown('TapeAndFabricMaster', 'Mesh', '#ddlMeshName', ' Select Mesh ', dt.meshCode, null, false, null, false)
}

function collectFormData() {
	return {
		Code: toNullableInt($('#TxtCode').val()),
		Name: toNullableString($('#TxtName').val()),
		MeshCode: toNullableInt($('#ddlMeshName').val()),
		StdGram: toNullableDecimal($('#NumStdGram').val()),
		MinGram: toNullableDecimal($('#NumMinGram').val()),
		MaxGram: toNullableDecimal($('#NumMaxGram').val()),
		Gsm: toNullableDecimal($('#NumGSM').val()),
		Denier: toNullableDecimal($('#NumDenier').val()),
		UnitName: toNullableString($('#TxtPPHD').val()),
		ColorCode: toNullableInt($('#ddlColor').val()),
		Width: toNullableDecimal($('#NumWidth').val()),
		Gpd: toNullableDecimal($('#NumStdGPD').val()),
		MinGpd: toNullableDecimal($('#NumMinGPD').val()),
		MaxGpd: toNullableDecimal($('#NumMaxGPD').val()),
		StdStrength: toNullableDecimal($('#NumStdStrength').val()),
		StrengthMax: toNullableDecimal($('#NumStrengthMax').val()),
		StrengthMin: toNullableDecimal($('#NumStrengthMin').val()),
		StdElong: toNullableDecimal($('#NumStdElong').val()),
		ElongMax: toNullableDecimal($('#NumElongMax').val()),
		ElongMin: toNullableDecimal($('#NumElongMin').val()),
		UnlamFab: toNullableDecimal($('#NumUnlamFabStr').val()),
		LamFab: toNullableDecimal($('#NumLamFabStr').val()),
		Active: toNullableInt($('#DdActive').val())
	};
}

function checkExistOrNot(inputData) {
	return $.ajax({
		url: '/TapeAndFabricMaster/getExistOrNot',
		type: 'GET',
		dataType: 'json',
		data: { inputData: inputData }
	});
}

function getQueryParam(param) {
	const urlParams = new URLSearchParams(window.location.search);
	return urlParams.get(param);
}

function setActiveStatus(input) {
	let isActive = input == 1;
	$('#DdActive').prop('checked', isActive);
	$('#DdActive').val(isActive ? 1 : 0);
	$('#statusText').text(isActive ? 'Active' : 'Inactive');
}

function toNullableDecimal(value) {
	if (value === null || value === undefined || value.trim() === '') {
		return null;
	}
	value = value.replace(/[^0-9.]/g, '');
	if ((value.match(/\./g) || []).length > 1) {
		return null;
	}
	var parsed = parseFloat(value);
	return isNaN(parsed) ? null : parsed;
}

//=======Readonly============
function setFormReadOnly() {
	const form = $('#TapeFabricMasterForm');
	$('#statusText').addClass('text-muted');
	$('input, #ddlColor, #ddlMeshName, #DdActive').prop('disabled', true);
	$('#btn-save').hide();
	form.addClass('erppage-readonly');
}
function SetMaxlength(selector) {
	console.log(selector);
	let value = $(selector).val();

	// Allow only 2 decimal places
	if (!/^\d{0,18}(\.\d{0,4})?$/.test(value)) {
		$(selector).val(value.slice(0, -1));
	}
};