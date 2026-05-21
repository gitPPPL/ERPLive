const VehicleValidation = {
    validateSave: async function validateSave() {
        if (!validateRequiredField('#ddlDocType', 'Doc Type')) return true;
        if (!validateRequiredField('#NumDocNo', 'Doc No')) return true;
        if (!validateRequiredField('#DtDocDate', 'Doc Date')) return true;
        if (!validateRequiredField('#TmInTime', 'In Time')) return true;
        if (!validateRequiredField('#TmRTime', 'R Time')) return true;
        if (!validateRequiredField('#ddlTransportName', 'Transport Name')) return true;
        if (!validateRequiredField('#TxtDrivername', 'Driver Name')) return true;
        if (!validateRequiredField('#ddlCustomerName', 'Customer Name')) return true;
        if (!validateRequiredField('#NumDriverMobile', 'Mobile Number')) return true;
        return false;
    },
    validateDate: async function validateDate() {
        const checkValidation = await VehicleApi.checkValidDate();
        if (checkValidation == false) {
            return true;
        }
        return false;
    },
    validateDriverPhone: function validateDriverPhone(mobileNumber) {
        if (mobileNumber) {
            if (!validatePhone(mobileNumber)) {
                showToast('Please enter valid 10 digit mobile number', { type: 'warning' });
                $('#NumDriverMobile').focus();
                return true;
            }
        }
        return false;
    },
    parseIntSafe: function parseIntSafe(value) {
        const parsed = parseInt(value, 10);
        return isNaN(parsed) ? null : parsed;
    },
    toNullableInt: function toNullableInt(val) {
        const parsed = parseInt(val);
        return isNaN(parsed) ? null : parsed;
    },
    toNullableString: function toNullableString(val) {
        return val?.trim() || /* ==correction== null */ "";
    },
    toNullableDate: function toNullableDate(val) {
        const date = new Date(val);
        return isNaN(date.getTime()) ? /* ==correction== null */ "" : val;
    },
    allowOnlyNumbers: function allowOnlyNumbers(input) {
        input.addEventListener("beforeinput", function (e) {
            if (e.data && !/^\d+$/.test(e.data)) {
                e.preventDefault();
            }
        });
    }
};