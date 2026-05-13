var allFieldIds = [
    "TxtDocId", "ddlDocType", "NumDocNo", "DtDocDate", "DtReportDate", "TmInTime", "TmRTime", "ddlTransportName", "TxtDrivername",
    "TxtVehicleNo", "TxtVehicleRCNo", "NumContainerNo", "txtDLNo", "NumDriverMobile", "TxtInsuranceNo", "txtPANNO", "txtPurpose",
    "ddlContainerSize", "ddlDONo", "ddlCustomerName", "TxtAdd1TIR", "TxtAdd2TIR", "TxtAdd3PD", "ddlCity", "TxtRemarks",
    "DtValidity", "DtFitmentValidity", "DtTaxValidity", "chkValidity", "chkFitmentValidity", "chkTaxValidity", "txtVehicleBody", "txtPassWeight",
    "TxtVehicleRemarks", "TxtAttachment"
];
const formatDate = (val) => val ? val.substring(0, 10) : '';
const VehicleUI = {
    //========Query Param=========
    getQueryParam: function getQueryParam(param) {
        const urlParams = new URLSearchParams(window.location.search);
        return urlParams.get(param);
    },
    //=================Date&Time============
    setCurrentTime: function setCurrentTime() {
        const now = new Date();
        const hours = String(now.getHours()).padStart(2, '0');
        const minutes = String(now.getMinutes()).padStart(2, '0');
        const currentTime = `${hours}:${minutes}`;
        document.getElementById('TmInTime').value = currentTime;
    },
    setCurrentDate: function setCurrentDate() {
        const today = new Date();
        const todayDate = today.getFullYear() + '-' +
            (today.getMonth() + 1).toString().padStart(2, '0') + '-' +
            today.getDate().toString().padStart(2, '0');
        return todayDate;
    },
    chkDtdisable: function chkDtdisable() {
        const currentDate = this.setCurrentDate();
        $('#chkValidity, #chkFitmentValidity, #chkTaxValidity').prop('checked', false);
        $('#DtValidity, #DtFitmentValidity, #DtTaxValidity').prop('disabled', true);
        $('#DtValidity, #DtFitmentValidity, #DtTaxValidity').val(currentDate);
    },
    toggleDate: function toggleDate(chk, input) {
        $(chk).on('change', function () {
            $(input).prop('disabled', !$(this).is(':checked'));
        });
    },
    //===================Dropdowns================
    bindCustomerDropdown: (data, selectedValue) => {
        const $dropdown = $('#ddlCustomerName');
        $dropdown.empty();
        $dropdown.append('<option selected disabled value="">- Select -</option>');
        $.each(data, function (index, item) {
            $dropdown.append(`<option
                            data-add1="${item.ADD1}"
                            data-add2="${item.ADD2}"
                            data-add3="${item.ADD3}"
                            data-cityName="${item.CityName}"
                            data-cityCd="${item.CITY_CODE}"
                            value="${item.CODE}">
                            ${item.NAME}
                        </option>`);
        });
        $dropdown.select2({
            placeholder: "- Select -",
            allowClear: true
        });
        if (selectedValue && $dropdown.find(`option[value="${selectedValue}"]`).length > 0) {
            $dropdown.val(selectedValue).trigger('change');
        } else {
            $dropdown.val('').trigger('change');
        }
    },
    bindTransportDropdown: (data, selectedValue) => {
        const $DropdownId = $('#ddlTransportName');
        $DropdownId.empty();
        $DropdownId.append('<option value="">- Select Transport Name -</option>');
        $.each(data, function (index, item) {
            $DropdownId.append(
                `<option
                            data-partyCode="${item.PARTY_CODE}"
                            value="${item.CODE}">${item.NAME}</option>`
            );
        });

        $DropdownId.select2({
            placeholder: "- Select -",
            allowClear: true
        });

        if (selectedValue && $DropdownId.find(`option[value="${selectedValue}"]`).length > 0) {
            $DropdownId.val(selectedValue).trigger('change');
        }
        else {
            $DropdownId.val('').trigger('change');
        }
    },
    formatDoNoOption: function formatDoNoOption(option) {
        if (!option.id) return option.text;

        let el = $(option.element);

        let vno = option.id;
        let transport = el.data('transportname');
        let truckname = el.data('truckno');
        let partyname = el.data('partyname');
        let cityname = el.data('cityname');

        return $(`
            <div style="display:flex; gap:0px; font-size:12px;  border-bottom:1px solid;">
                <span style="flex:0 0 30%; font-weight:bold;">${vno}</span>
                <span style="flex:1; padding-left:8px; white-space:normal;">${transport}</span>
                <span style="flex:1; padding-left:8px; white-space:nowrap;">${truckname}</span>
                <span style="flex:1; padding-left:8px; white-space:normal;">${partyname}</span>
                <span style="flex:1; padding-left:8px; white-space:normal;">${cityname}</span>
            </div>
        `);
    },
    bindDoNoDropdown: function (data, selectedValue) {
        const $dropdown = $('#ddlDONo');
        $dropdown.empty();
        $dropdown.append('<option selected disabled value="">- Select -</option>');
        $.each(data, function (index, item) {
            $dropdown.append(`<option
                             data-truckno="${item.TruckNo}"
                             data-transportname="${item.TransportName}"
                             data-transportcode="${item.TransportCode}"
                             data-add1="${item.Add1}"
                             data-add2="${item.Add2}"
                             data-add3="${item.Add3}"
                             data-partyname="${item.PartyName}"
                             data-citycd="${item.CityCode}"
                             data-cityname="${item.CityName}"
                             data-billcd="${item.BillCode}"
                             value="${item.Code}">
                            ${item.Name}
                        </option>`);
        });

        // Initialize or refresh Select2
        $dropdown.select2({
            placeholder: "- Select -",
            allowClear: true,
            // width: 100,
            width: '600px',
            templateResult: this.formatDoNoOption,
            templateSelection: function (option) {
                if (!option.id) return option.text;

                return option.id;
            }
        });

        if (selectedValue && $dropdown.find(`option[value="${selectedValue}"]`).length > 0) {
            $dropdown.val(selectedValue).trigger('change');
        } else {
            $dropdown.val('').trigger('change');
        }
    },
    //=======DocType&DocNo=============
    bindDocType: (response) => {
        const $dropdown = $('#ddlDocType');
        $dropdown.empty();
        $.each(response.data, function (index, item) {
            $dropdown.append(`<option value="${item.CODE}">${item.NAME}</option>`);
        });
        if (response.data.length > 0) {
            $dropdown.trigger('change');
        }
    },
    bindDocNo: (response) => {
        if (response.status === true && response.data) {
            $('#NumDocNo').val(response.data.vNo || '');
            $('#TxtDocId').val(response.data.docId || '');
        } else {
            $('#txtDocNo').val('');
            $('#TxtDocId').val('');
        }
    },
    //=========KeyFocus==========
    setEnterKeyFocus: function setEnterKeyFocus() {
        allFieldIds.forEach((id, index) => {
            $(`#${id}`).on('keypress', function (e) {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    if (index + 1 < allFieldIds.length) {
                        $(`#${allFieldIds[index + 1]}`).focus();
                    }
                }
            });
        });
    },
    //=======Disable All Fields=======
    disableAllFields: function disableAllFields() {
        allFieldIds.forEach(id => {
            const el = document.getElementById(id);
            if (el) el.disabled = true;
        });
    },
    //===========ReadOnly============
    setReadOnly: function setReadOnly (readOnly) {
        if (readOnly === 'true') {
            const form = $('#TransportInwardPassEntryForm');
            $('#btn-save, #cancelBtn').hide();
            this.disableAllFields();
            var $previewContainer = $('#previewContainer');
            if ($previewContainer.is(':visible')) {
                $('#btnRemoveImage').hide();
            }
            $('#btnGetVAHANData').prop('disabled', true).css('pointer-events', 'none');
            form.addClass('erppage-readonly');
        }
    },
    //==========Clear Vehicle Data=======
    clearVehicleData: function clearVehicleData(){
        $('#txtVehicleBody').val('');
        $('#txtPassWeight').val('');
        $('#TxtInsuranceNo').val('');
        $('#txtPurpose').val('');
        $('#TxtVehicleRCNo').val('');
        $('#TxtVehicleRemarks').val('');
        $('#ddlTransportName').val('');
        this.chkDtdisable();
    },
    //========Image Preview=========
    showImagePreview: function showImagePreview(path) {
        if (path) {
            const imgUrl = `/Attachments/TransportInward/${path}`;
            $('#imgPreview').attr('src', imgUrl);
            $('#previewContainer').show();
        } else {
            $('#imgPreview').attr('src', '');
            $('#previewContainer').hide();
        }
    },
    //=========Collect data for save&Update==========
    collectFormData: function collectFormData() {
        const id = VehicleValidation.toNullableString(docId);
        const formData = new FormData();

        const InsuranceExpDate = $('#chkValidity').is(':checked') ? VehicleValidation.toNullableDate($('#DtValidity').val()) : null;
        const FitmentValidityDate = $('#chkFitmentValidity').is(':checked') ? VehicleValidation.toNullableDate($('#DtFitmentValidity').val()) : null;
        const TaxValidityDate = $('#chkTaxValidity').is(':checked') ? VehicleValidation.toNullableDate($('#DtTaxValidity').val()) : null;

        const file = document.getElementById("TxtAttachment")?.files[0];
        if (file) {
            formData.append("Attachment", file); // key name for backend
        }

        formData.append("V_TYPE", VehicleValidation.toNullableString(document.getElementById("ddlDocType")?.value));
        formData.append("V_NO", VehicleValidation.parseIntSafe(document.getElementById("NumDocNo")?.value));
        formData.append("DOC_ID", VehicleValidation.toNullableString(document.getElementById("TxtDocId")?.value));
        formData.append("TRF_TYPE", "");
        formData.append("TRF_NO", "");
        formData.append("V_DATE", VehicleValidation.toNullableDate(document.getElementById("DtDocDate")?.value));
        formData.append("V_TIME", VehicleValidation.toNullableString(document.getElementById("TmInTime")?.value));
        formData.append("ITEM_TYPE", "");
        formData.append("PARTY_CODE", VehicleValidation.toNullableInt(document.getElementById("ddlCustomerName")?.value));
        formData.append("ADD1", VehicleValidation.toNullableString(document.getElementById("TxtAdd1TIR")?.value));
        formData.append("ADD2", VehicleValidation.toNullableString(document.getElementById("TxtAdd2TIR")?.value));
        formData.append("ADD3", VehicleValidation.toNullableString(document.getElementById("TxtAdd3PD")?.value));
        formData.append("PARTY_CITY", VehicleValidation.toNullableInt(document.getElementById("ddlCity")?.value));
        formData.append("PARTY_GST", "");
        formData.append("PARTY_PINCODE", "");
        formData.append("PARTY_ADDRESSID", "");
        formData.append("BILL_NO", "");
        formData.append("BILL_DATE", "");
        formData.append("CHALL_NO", "");
        formData.append("TRUCK_NO", VehicleValidation.toNullableString(document.getElementById("TxtVehicleNo")?.value));
        formData.append("TRANSPORT_CODE", VehicleValidation.toNullableInt(document.getElementById("ddlTransportName")?.value));
        formData.append("DRIVER_NAME", VehicleValidation.toNullableString(document.getElementById("TxtDrivername")?.value));
        formData.append("DRIVER_NO", VehicleValidation.toNullableString(document.getElementById("NumDriverMobile")?.value));
        formData.append("TRANSIT_NO", "");
        formData.append("WAYBILL_NO", "");
        formData.append("BILL_AMT", "");
        formData.append("REMARKS", VehicleValidation.toNullableString(document.getElementById("TxtRemarks")?.value));
        formData.append("DISP_PLAN_NO", VehicleValidation.toNullableInt(document.getElementById("ddlDONo")?.value));
        formData.append("DISP_PLAN_TYPE", "");
        formData.append("WB_TYPE", "");
        formData.append("WB_NO", "");
        formData.append("MRN_TYPE", "");
        formData.append("MRN_NO", "");
        formData.append("REF_TYPE", "");
        formData.append("REF_NO", "");
        formData.append("FAPROV_STATUS", "");
        formData.append("FAPROV_REMARKS", "");
        formData.append("STATUS", "");
        formData.append("ACTIVE", "");
        formData.append("PARTY_NAME", "");
        formData.append("RC_NO", VehicleValidation.toNullableString(document.getElementById("TxtVehicleRCNo")?.value));
        formData.append("DL_NO", VehicleValidation.toNullableString(document.getElementById("txtDLNo")?.value));
        formData.append("INSU_NO", VehicleValidation.toNullableString(document.getElementById("TxtInsuranceNo")?.value));
        formData.append("PAN_NO", VehicleValidation.toNullableString(document.getElementById("txtPANNO")?.value));
        formData.append("PURPOSE", VehicleValidation.toNullableString(document.getElementById("txtPurpose")?.value));
        formData.append("IMAGEPATH", "");
        formData.append("R_TIME", VehicleValidation.toNullableString(document.getElementById("TmRTime")?.value));
        formData.append("OUT_TIME", "");
        formData.append("R_DATE", VehicleValidation.toNullableDate(document.getElementById("DtReportDate")?.value));
        formData.append("OUT_DATE", "");
        formData.append("RETURN_TYPE", "");
        formData.append("QRCODE_NO", "");
        formData.append("INOUT_ACTIVE", "");
        formData.append("OUT_ALLOWED", "");
        formData.append("OUT_ALLOWEDBY", "");
        formData.append("RETURN_DATE", "");
        formData.append("RESPONSIBLE_PERSON", "");
        formData.append("INSU_EXPDT", InsuranceExpDate);
        formData.append("EWB_DATE", FitmentValidityDate);
        formData.append("CHALL_DATE", TaxValidityDate);
        formData.append("PARTY_WBSLIPNO", VehicleValidation.toNullableString($('#txtVehicleBody').val()));
        formData.append("PARTY_WBGRWT", VehicleValidation.toNullableString($('#txtPassWeight').val()));
        formData.append("Remarks2", VehicleValidation.toNullableString($('#TxtVehicleRemarks').val()));
        formData.append("DL_EXPDT", "");
        formData.append("CONTAINER_NO", VehicleValidation.toNullableString(document.getElementById("NumContainerNo")?.value));
        formData.append("CONTAINER_SIZE", VehicleValidation.toNullableString(document.getElementById("ddlContainerSize")?.value));
        formData.append("SHIP_PARTY", "");
        formData.append("SHIP_BILLNO", "");
        formData.append("SHIP_BILLDATE", "");
        formData.append("EWB_EXPDATE", "");
        formData.append("PARTY_WBTIME", "");
        formData.append("EWB_INVNO", "");
        formData.append("EWB_INVAMT", "");
        formData.append("PARTY_WBTRWT", "");
        formData.append("PARTY_EWBCITY", "");
        formData.append("GR_NO", "");
        formData.append("GR_DATE", "");
        formData.append("SaveOrUpdate", (!id || id == "") ? 'Save' : 'Update');
        return formData;
    },
    //=======Fill Form=========
    fillFormFields: async function fillFormFields(data) {
        if (!Array.isArray(data) || data.length === 0) {
            console.error("Invalid or empty data array");
            return;
        }

        const d = data[0];
        const formatTime = val => val ? val.substring(0, 5) : '';

        document.getElementById("TxtDocId").value = d.doC_ID || '';
        document.getElementById("ddlDocType").value = d.v_TYPE || '';
        document.getElementById("NumDocNo").value = d.v_NO || '';
        document.getElementById("DtDocDate").value = formatDate(d.v_DATE);
        document.getElementById("DtReportDate").value = formatDate(d.r_DATE);
        document.getElementById("TmInTime").value = formatTime(d.v_TIME);
        document.getElementById("TmRTime").value = formatTime(d.r_TIME);
        document.getElementById("TxtDrivername").value = d.driveR_NAME || '';
        document.getElementById("TxtVehicleNo").value = d.trucK_NO || '';
        document.getElementById("TxtVehicleRCNo").value = d.rC_NO || '';
        document.getElementById("NumContainerNo").value = d.containeR_NO || '';
        document.getElementById("txtDLNo").value = d.dL_NO || '';
        document.getElementById("NumDriverMobile").value = d.driveR_NO || '';
        document.getElementById("TxtInsuranceNo").value = d.insU_NO || '';
        document.getElementById("txtPANNO").value = d.paN_NO || '';
        document.getElementById("txtPurpose").value = d.purpose || '';
        document.getElementById("ddlContainerSize").value = d.containeR_SIZE || '';
        document.getElementById("ddlDONo").value = d.disP_PLAN_NO || '';
        document.getElementById("TxtAdd1TIR").value = d.adD1 || '';
        document.getElementById("TxtAdd2TIR").value = d.adD2 || '';
        document.getElementById("TxtAdd3PD").value = d.adD3 || '';
        document.getElementById("ddlCity").value = d.partY_CITY || '';
        document.getElementById("TxtRemarks").value = d.remarks || '';
        VehicleApi.GetCustomerList(d.partY_CODE);
        VehicleApi.GetTransportList(d.transporT_CODE);
        document.getElementById("txtVehicleBody").value = d.partY_WBSLIPNO || '';
        document.getElementById("TxtVehicleRemarks").value = d.remarks2 || '';
        document.getElementById("txtPassWeight").value = d.partY_WBGRWT || '';

        let currentDate = this.setCurrentDate();
        const syncValidity = (value, chkId, dtId) => {
            const hasValue = value !== null && value !== "";
            $(`#${chkId}`).prop('checked', hasValue);
            $(`#${dtId}`).val(hasValue ? formatDate(value) : currentDate)
                .prop('disabled', !hasValue);
        };
        syncValidity(d.insU_EXPDT, 'chkValidity', 'DtValidity');
        syncValidity(d.ewB_DATE, 'chkFitmentValidity', 'DtFitmentValidity');
        syncValidity(d.chalL_DATE, 'chkTaxValidity', 'DtTaxValidity');
        this.showImagePreview(d.imagepath);
    },
    //=========Driver Details=======
    bindDriverDetails: (data) => {
        $('#TxtDrivername').val(data.driverName);
        $('#NumDriverMobile').val(data.driverNo);
        $('#txtDLNo').val(data.dLNo);
        $('#txtPANNO').val(data.pANNo);
    },
    //======Bind Vehicle Info=======
    bindVehicleInfoFromDB: (data) => {
        if (data.bodyType) $('#txtVehicleBody').val(data.bodyType);
        if (data.grossWt) $('#txtPassWeight').val(data.grossWt);
        if (data.insuranceNumber) $('#TxtInsuranceNo').val(data.insuranceNumber);
        if (data.purpose) $('#txtPurpose').val(data.purpose);
        if (data.rcNumber) $('#TxtVehicleRCNo').val(data.rcNumber);
        if (data.vehicleRemarks) $('#TxtVehicleRemarks').val(data.vehicleRemarks);

        if (data.transportCode) {
            VehicleApi.GetTransportList(data.transportCode);
        }
        const validityData = [
            { value: data.insuExp, checkboxId: '#chkValidity', datepickerId: '#DtValidity' },
            { value: data.fitmentupto, checkboxId: '#chkFitmentValidity', datepickerId: '#DtFitmentValidity' },
            { value: data.taxupto, checkboxId: '#chkTaxValidity', datepickerId: '#DtTaxValidity' }
        ];
        validityData.forEach(item => {
            if (item.value) {
                $(item.checkboxId).prop('checked', true);
                $(item.datepickerId).val(formatDate(item.value)).prop('disabled', false);
            }
        });
    },
    bindVehicleInfoFromApi: (res) => {
        let grossWt = 0;
        if (res.vehicleGrossWeight != null && res.unladenWeight != null) {
            grossWt = res.vehicleGrossWeight - res.unladenWeight;
        }
        const vehicleRemarks = `Vehicle category : ${res.vehicleCategory}, Insurance validity : ${formatDate(res.insuranceUpto)}, Pucc validity : ${formatDate(res.puccUpto)}, Permit valid upto : ${formatDate(res.permitValidUpto)}, Blacklist status : ${res.blacklistStatus}, Rc Status : ${res.rcStatus}",`;
        showToast("Vehicle Info found Successfully!", { type: "success" });
        $('#TxtVehicleRCNo').val(res.vehicleChasiNumber || '');
        $('#TxtInsuranceNo').val(res.insurancePolicyNumber || '');
        $('#txtPassWeight').val(grossWt);
        $('#txtVehicleBody').val(res.bodyType || '');
        $('#TxtVehicleRemarks').val(vehicleRemarks || '');
        const validityData = [
            { value: res.insuranceUpto, checkboxId: '#chkValidity', datepickerId: '#DtValidity' },
            { value: res.taxUpto, checkboxId: '#chkTaxValidity', datepickerId: '#DtTaxValidity' },
            { value: res.fitUpTo, checkboxId: '#chkFitmentValidity', datepickerId: '#DtFitmentValidity' }
        ];

        validityData.forEach(item => {
            if (item.value !== null && item.value !== "") {
                $(item.checkboxId).prop('checked', true);
                $(item.datepickerId).val(formatDate(item.value));
                $(item.datepickerId).prop('disabled', false);
            }
        });

        var strValid = "";

        // Check blacklist_status
        if (res.blacklistStatus !== "" && res.blacklistStatus !== null) {
            strValid += "BlackListed Vehicle\n";
        }

        // Check RC status
        if (res.rcStatus !== "" && res.rcStatus !== null) {
            if ((res.rcStatus) !== "ACTIVE") {
                strValid += "Vehicle RC is not active.\n";
            }
        }

        // Check permit validity
        if (res.permitValidUpto !== "" && res.permitValidUpto !== null) {
            var permitValidUpto = new Date(res.permitValidUpto);
            var currentDate = new Date();
            if (permitValidUpto < currentDate) {
                strValid += "Vehicle permit has expired.\n";
            }
        }
        if (strValid !== "") {
            // toastr.warning(strValid);
            showToast(strValid, { type: "warning" });
        }
    },
    //========Ensure Option======
    ensureOption: function ensureOption($dropdown, code, name) {
        if (code && $dropdown.find(`option[value="${code}"]`).length === 0) {
            $dropdown.append(`<option value="${code}">${name}</option>`);
        }
    },
    bindDataOnCustomerEvent: function bindDataOnCustomerEvent(partyCode, selectedOption) {
        if (partyCode) {
            $('#TxtAdd1TIR').val(selectedOption.data('add1') || '');
            $('#TxtAdd2TIR').val(selectedOption.data('add2') || '');
            $('#TxtAdd3PD').val(selectedOption.data('add3') || '');
            var cityCode = selectedOption.data('citycd');
            var cityName = selectedOption.data('cityname');
            this.ensureOption($('#ddlCity'), cityCode, cityName);
            $('#ddlCity').val(cityCode).trigger('change');
        } else {
            $('#TxtAdd1TIR, #TxtAdd2TIR, #TxtAdd3PD').val('');
            $('#ddlCity').val('');
        }
    },
    bindDataOnDONoEvent: function bindDataOnDONoEvent(doNo, selectedOption) {
        if (doNo) {
            $('#TxtAdd1TIR').val(selectedOption.data('add1') || '');
            $('#TxtAdd2TIR').val(selectedOption.data('add2') || '');
            $('#TxtAdd3PD').val(selectedOption.data('add3') || '');
            var cityCode = selectedOption.data('citycd');
            var cityName = selectedOption.data('cityname');
            this.ensureOption($('#ddlCity'), cityCode, cityName);
            $('#ddlCity').val(cityCode).trigger('change');
            var partyCode = selectedOption.data('billcd');
            var partyName = selectedOption.data('partyname');
            this.ensureOption($('#ddlCustomerName'), partyCode, partyName);
            $('#ddlCustomerName').val(partyCode).trigger('change');
            var transportCode = selectedOption.data('transportcode');
            var transportName = selectedOption.data('transportname');
            this.ensureOption($('#ddlTransportName'), transportCode, transportName);
            $('#ddlTransportName').val(transportCode).trigger('change');
            $('#TxtVehicleNo').val(selectedOption.data('truckno') || '');

            $('#ddlTransportName').prop('disabled', true);
            $('#ddlCustomerName').prop('disabled', true);
            setTimeout(function () {
                $('#TxtDrivername').focus();
            }, 100);
        } else {
            $('#TxtAdd1TIR, #TxtAdd2TIR, #TxtAdd3PD, #TxtVehicleNo').val('');
            $('#ddlCity').val('');
            $('#ddlTransportName').val(null).trigger('change');
            $('#ddlCustomerName').val(null).trigger('change');
            $('#ddlTransportName').prop('disabled', false);
            $('#ddlCustomerName').prop('disabled', false);
        }
    }
};