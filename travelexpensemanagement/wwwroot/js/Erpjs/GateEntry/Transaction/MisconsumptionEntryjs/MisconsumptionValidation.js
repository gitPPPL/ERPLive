//=====================
// FOAM VALIDATION
//=====================
function validateConsumptionForm() {

    if (!validateRequiredField('#ddlDocType', 'Doc Type')) return;
    if (!validateRequiredField('#NumDocNo', 'Doc No')) return;
    if (!validateRequiredField('#DtDocDate', 'Doc Date')) return;
    if (!validateRequiredField('#ddlPartyName', 'Party Name')) return;
    if (!validateRequiredField('#TxtVehicleNo', 'Vehicle No')) return;

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
            setInvalid($(this).find('.itemName'), 'Item Name is required.');
            isValid = false;
            return false;
        }

        // Department
        if (!dept) {
            setInvalid($(this).find('.department'), 'Department is required.');
            isValid = false;
            return false;
        }

        // Unit
        if (!unit) {
            setInvalid($(this).find('.unit'), 'Unit is required.');
            isValid = false;
            return false;
        }

        // Nos
        if (!nos) {
            setInvalid($(this).find('.no'), 'Nos is required.');
            isValid = false;
            return false;
        }

        // Quantity
        if (qty <= 0) {
            setInvalid($(this).find('.quantity'), 'Quantity is Required.');
            isValid = false;
            return false;
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
