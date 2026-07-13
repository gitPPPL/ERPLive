let docId = "";
let readOnly; 
let RemoveAttachment = false;
var controllerName = window.location.pathname.split('/')[1];
$(async function () {
    checkPermissionForEntryPage(controllerName);
    try {
        docId = VehicleUI.getQueryParam('id');
        readOnly = VehicleUI.getQueryParam('readOnly');
        //==========KeyFocus=====
        VehicleUI.setEnterKeyFocus();
        //=========Dropdown=======
        VehicleApi.GetDocType();
        VehicleApi.GetCustomerList();
        VehicleApi.GetTransportList();
        VehicleApi.GetDONo();
        //=====Date&Time========
        VehicleUI.setCurrentTime();
        VehicleUI.chkDtdisable();
        VehicleUI.toggleDate('#chkValidity', '#DtValidity');
        VehicleUI.toggleDate('#chkFitmentValidity', '#DtFitmentValidity');
        VehicleUI.toggleDate('#chkTaxValidity', '#DtTaxValidity');
        let currentDate = VehicleUI.setCurrentDate();
        $('#DtDocDate').val(currentDate);
        $('#DtReportDate').val(currentDate);
        //========Events==========
        initEventListeners(docId);
        if (docId) {
            VehicleApi.GetDocData(docId, readOnly);
        }
        const mobileInput = document.getElementById("NumDriverMobile");
        VehicleValidation.allowOnlyNumbers(mobileInput);
    } catch (e) {
        console.error("Initialization Error:", e);
        if (typeof showToast === "function") {
            showToast("Init failed: Check console for details", { type: 'error' });
        }
    }
});

function initEventListeners(docId) {
    $('#ddlDocType').on('change', function () {
        const VType = $(this).val();
        if (VType) {
            VehicleApi.GetDocid(VType);
        }
    });
    $('#ddlCustomerName').on('change', function () {
        var partyCode = $(this).val();
        var selectedOption = $(this).find('option:selected');
        if (partyCode) {
            $('#TxtAdd1TIR').val(selectedOption.data('add1') || '');
            $('#TxtAdd2TIR').val(selectedOption.data('add2') || '');
            $('#TxtAdd3PD').val(selectedOption.data('add3') || '');
            var cityCode = selectedOption.data('citycd');
            var cityName = selectedOption.data('cityname');
            VehicleUI.ensureOption($('#ddlCity'), cityCode, cityName);
            $('#ddlCity').val(cityCode).trigger('change');
        } else {
            $('#TxtAdd1TIR, #TxtAdd2TIR, #TxtAdd3PD').val('');
            $('#ddlCity').val('');
        }
    });
    $('#ddlDONo').on('change', function () {
        var doNo = $(this).val();
        var selectedOption = $(this).find('option:selected');
        VehicleUI.bindDataOnDONoEvent(doNo, selectedOption);
    });
    //========Image Event=========
    $('#TxtAttachment').on('change', function () {
        const file = this.files[0];

        if (!file) return;

        if (!file.type.startsWith('image/')) {
            Swal.fire({
                icon: 'warning',
                title: 'Invalid File',
                text: 'Please select a valid image file.'
            });

            $(this).val('');
            $('#previewContainer').hide();
            return;
        }

        const reader = new FileReader();

        reader.onload = function (e) {
            $('#imgPreview').attr('src', e.target.result);
            $('#previewContainer').fadeIn();
        };

        reader.readAsDataURL(file);
    });
    $('#btnRemoveImage').on('click', async function (e) {
        e.preventDefault();

        const result = await Swal.fire({
            title: 'Remove Image?',
            text: 'Do you want to remove this image?',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Yes',
            cancelButtonText: 'No'
        });

        if (!result.isConfirmed) return;

        $('#TxtAttachment').val('');
        $('#imgPreview').attr('src', '');
        $('#previewContainer').fadeOut();
        RemoveAttachment = true;
        console.log("RemoveAttachment value:", RemoveAttachment);
    });
    //=========Save Event=========
    $('#btn-save').on('click', async function (e) {
        e.preventDefault();
        const validate = await VehicleValidation.validateSave();
        if (validate) {
            return;
        }
        const validateDate = await VehicleValidation.validateDate();
        if (validateDate) {
            return;
        }
        const mobileNo = $('#NumDriverMobile').val();
        const validateMobile = VehicleValidation.validateDriverPhone(mobileNo);
        if (validateMobile) {
            return;
        }
        try {
            const tableData = await VehicleUI.collectFormData();
            if (docId) {
                VehicleApi.UpdateData(tableData);
            } else {
                VehicleApi.SaveData(tableData);
            }
        } catch (error) {
            showToast("An error occurred while saving the data.", { type: "error" });
        }

    });
    //=======Mobile Last Focus=========
    $('#NumDriverMobile').on('blur', function () {
        const mobileNo = $(this).val();
        //if (!validateForm('#TransportInwardPassEntryForm')) return;
        if (VehicleValidation.validateDriverPhone(mobileNo)) return;
        if (mobileNo) {
            VehicleApi.getDriverDetails(mobileNo);
        }
    })
    //========Vehicle Last Focus======
    $('#TxtVehicleNo').on('blur', async function () {
        setTimeout(async () => {
            // check if focus moved to the button
            if (document.activeElement.id === 'btnGetVAHANData') {
                return;
            }
            const vehicleNo = $(this).val();
            if (vehicleNo !== "") {
                await VehicleApi.getVehicleDetailsFromDB(vehicleNo);
            } else {
                VehicleUI.clearVehicleData();
            }
        }, 0);
    });
    //======VAHAN button click========
    $('#btnGetVAHANData').on('click', async function () {
        const vehicleNo = $('#TxtVehicleNo').val();
        if (!vehicleNo) {
            showToast("Please enter Vehicle No", { type: "info" });
            return;
        }
        VehicleUI.clearVehicleData();
        VehicleApi.getVehicleDetailsFromApi();
    });
    $('#ddlCustomerName').on('change', function () {
        var partyCode = $(this).val();
        var selectedOption = $(this).find('option:selected');
        VehicleUI.bindDataOnCustomerEvent(partyCode, selectedOption)
    });
}
