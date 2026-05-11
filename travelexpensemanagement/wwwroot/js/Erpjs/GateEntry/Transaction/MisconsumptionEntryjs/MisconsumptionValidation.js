//=====================
// FOAM VALIDATION
//=====================
function validateConsumptionForm() {

    // ========= DOC TYPE =========
    const docType = $('#ddlDocType').val();
    if (!docType) {
        showToast("Doc Type is required.", { type: "warning" });
        $('#ddlDocType').addClass('is-invalid').focus();
        return false;
    } else {
        $('#ddlDocType').removeClass('is-invalid');
    }

    // ========= DOC NO =========
    const docNo = $('#NumDocNo').val();
    if (!docNo || parseInt(docNo) === 0) {
        showToast("Invalid Doc No.", { type: "warning" });
        $('#NumDocNo').addClass('is-invalid').focus();
        return false;
    } else {
        $('#NumDocNo').removeClass('is-invalid');
    }

    // ========= DOC DATE =========
    const docDate = $('#DtDocDate').val();
    if (!docDate) {
        showToast("Doc Date is required.", { type: "warning" });
        $('#DtDocDate').addClass('is-invalid').focus();
        return false;
    } else {
        $('#DtDocDate').removeClass('is-invalid');
    }

    // ========= PARTY =========
    const party = $('#ddlPartyName').val();
    if (!party) {
        showToast("Party Name is required.", { type: "warning" });
        $('#ddlPartyName').addClass('is-invalid').focus();
        return false;
    } else {
        $('#ddlPartyName').removeClass('is-invalid');
    }

    // ========= VEHICLE =========
    const vehicle = $('#TxtVehicleNo').val().trim();
    if (!vehicle) {
        showToast("Vehicle No is required.", { type: "warning" });
        $('#TxtVehicleNo').addClass('is-invalid').focus();
        return false;
    } else {
        $('#TxtVehicleNo').removeClass('is-invalid');
    }

    // ========= FOOTER(TABLE) VALIDATION =========
    const rows = $("#tblConsumptionEntry tbody tr");

    if (rows.length === 0) {
        showToast("Please add at least one item.", { type: "warning" });
        return false;
    }

    let isValid = true;

    rows.each(function () {

        const item = $(this).find(".itemName").val();
        const dept = $(this).find(".department").val();
        const unit = $(this).find(".unit").val();
        const nos = parseFloat($(this).find(".no").val() || 0);
        const qty = parseFloat($(this).find(".quantity").val() || 0);

        // Item
        if (!item) {
            showToast("Item Name is required.", { type: "warning" });
            $(this).find(".itemName").addClass("is-invalid").focus();
            isValid = false;
            return false;
        } else {
            $(this).find(".itemName").removeClass("is-invalid");
        }

        // Department
        if (!dept) {
            showToast("Department is required.", { type: "warning" });
            $(this).find(".department").addClass("is-invalid").focus();
            isValid = false;
            return false;
        } else {
            $(this).find(".department").removeClass("is-invalid");
        }

        // Unit
        if (!unit) {
            showToast("Unit is required.", { type: "warning" });
            $(this).find(".unit").addClass("is-invalid").focus();
            isValid = false;
            return false;
        } else {
            $(this).find(".unit").removeClass("is-invalid");
        }

        // Nos
        if (!nos) {
            showToast("Nos is required.", { type: "warning" });
            $(this).find(".no").addClass("is-invalid").focus();
            isValid = false;
            return false;
        } else {
            $(this).find(".no").removeClass("is-invalid");
        }

        // Quantity
        if (qty <= 0) {
            showToast("Quantity is Required.", { type: "warning" });
            $(this).find(".quantity").addClass("is-invalid").focus();
            isValid = false;
            return false;
        } else {
            $(this).find(".quantity").removeClass("is-invalid");
        }
    });

    return isValid;
}

//===========================
// GLOBAL DATE VALIDATION
//===========================
async function checkValidDate() {

    const data = {
        vdate: $("#DtDocDate").val(),
        vtype: "MICO",
        vno: $("#NumDocNo").val()
    };

    try {

        const result = await MisConsumptionApi.checkValidDate(data); 
        if (result.status === false) {
            showToast(result.message, { type: "warning" });
            return false;
        }
        return true;

    } catch (error) {
        console.error(error);
        showToast("Date validation failed", { type: "error" });
        return false;
    }
}
