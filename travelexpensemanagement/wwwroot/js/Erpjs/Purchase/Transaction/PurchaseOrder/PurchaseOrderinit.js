

$(document).on('input', '[id^=TxtRate], [id^=TxtQty], [id^=TxtCess], [id^=TxtPack], [id^=TxtDisc]', function () {
    const $input = $(this);
    const rowId = $input.closest('tr').attr('id').replace('row', '');
    calculateTaxAmounts(rowId);
});

$(document).on('change', '[id^=ddlTax]', function () {


    if (isLoading == false) {

        isLoading = true;
        return; 
    }


    const $select = $(this);
    const selectedOption = $select.find('option:selected');
    const rowId = $select.closest('tr').attr('id').replace('row', '');
    const csgstPer = selectedOption.data('csgstper') || 0;
    const igstPer = selectedOption.data('igstper') || 0;
    const tdsPer = selectedOption.data('tdsper') || 0;
    const tcsPer = selectedOption.data('tcsper') || 0;
    const vatPer = selectedOption.data('vatper') || 0;
    const othPer1 = selectedOption.data('otherper') || 0;
    const othPer2 = selectedOption.data('otherper2') || 0;

    // ✅ Update matching input fields
    $(`#TxtCgstPercent${rowId}`).val(csgstPer);
    $(`#TxtSgstPercent${rowId}`).val(csgstPer);
    $(`#TxtIgstPercent${rowId}`).val(igstPer);
    $(`#TxtTcsPer${rowId}`).val(tcsPer);
    $(`#TxtVatPercent${rowId}`).val(vatPer);
    $(`#TxtOthPer${rowId}`).val(othPer1);
    $(`#TxtOthPer2${rowId}`).val(othPer2);
    calculateTaxAmounts(rowId);
});


$(document).on('click', '[id^=TxtRate], [id^=TxtQty]', function () {
    const $select = $(this);
    const rowId = $select.closest('tr').attr('id').replace('row', '');
    calculateTaxAmounts(rowId);
});


$('#btn-save').on('click', async function (e) {
    e.preventDefault();

    try {
        if (!validateRequiredField('#ddlPriceType', 'Price Type')) return;
        if (!validateRequiredField('#ddlPlace', 'Place')) return;
        if (!validateRequiredField('#ddlPartyName', 'Party Name')) return;
        if (!validateRequiredField('#ddlShipFrom', 'Ship From')) return;

        var v_type = $('#ddlDocType').val();
        var PartyRef = $('#txtPartyRef').val().trim();


        var totalAmount = parseFloat($('#NumAmountIt').val()) || 0;

        if (v_type === 'PORD') {
            if (PartyRef === '' && totalAmount >= 50000) {
                toastr.warning("Party Reference is required if total PO Amount is greater than or equal to 50,000.");
                return;
            }
        }

        const checkdate = await checkValidDate();
        if (!checkdate) {
            return;
        }

        const tableData = await collectFormData();


        console.log("tableData", tableData);


        const rows = tableData.ItemRecords || [];

        console.log("Rows:", rows);
        console.log("Row Count:", rows.length);

        if (rows.length === 0) {
            toastr.warning("Please enter at least one item.");
            return;
        }

        let hasItem = false;

        for (let i = 0; i < rows.length; i++) {
            const row = rows[i];

            console.log("row", row);

            const itemCode = (row.ItemCode || "").toString().trim();
            const amount = (row.Amount || "").toString().trim();
            const landRate = (row.LandRate || "").toString().trim(); // ✅ FIXED

            // Skip completely blank rows
            if (itemCode === "") {
                continue;
            }

            hasItem = true;

            if (amount === "") {
                toastr.warning(`Amount is required in row ${i + 1}.`);
                return;
            }

            if (landRate === "") {
                toastr.warning(`Loaded Rate is required in row ${i + 1}.`);
                return;
            }
        }

        if (!hasItem) {
            toastr.warning("Please enter at least one item.");
            return;
        }

        SaveData(tableData);          
    
    } catch (error) {
        toastr.error('An error occurred while saving the data.');
    }
});



$('#ddlPartyName').on('change', function () {

    var partyCode = $(this).val();
    var selectedOption = $(this).find('option:selected');
    var shipCode = $('#ddlShipFrom').val();


    console.log("selectedOption", selectedOption);

    if (partyCode) {

        $('#TxtAdd1PD').val(selectedOption.data('add1'));
        $('#TxtAdd2PD').val(selectedOption.data('add2'));
        $('#TxtAdd3PD').val(selectedOption.data('add3'));
        $('#NumPincodePD').val(selectedOption.data('pincode'));
        $('#TxtCity1PD').val(selectedOption.data('citycode'));

        $('#TxtGSTPD').val(selectedOption.data('gstin'));

        GetPartyAddress(partyCode, '#ddlAddressbyparty');
        GetSaudaList(partyCode);
        GetWeighBridgeNo(partyCode);

        if (!shipCode) {

            GetShipFromList(partyCode);

            $('#TxtAdd1SD').val(selectedOption.data('add1'));
            $('#TxtAdd2SD').val(selectedOption.data('add2'));
            $('#TxtAdd3SD').val(selectedOption.data('add3'));
            $('#NumPincodeSD').val(selectedOption.data('pincode'));
            $('#TxtCity1SD').val(selectedOption.data('cityname'));
            $('#TxtGSTSD').val(selectedOption.data('gstin'));

            GetPartyAddress(partyCode, '#ddlAddressbypartySD');
        }

    } else {

        $('#TxtAdd1PD').val('');
        $('#TxtAdd2PD').val('');
        $('#TxtAdd3PD').val('');
        $('#NumPincodePD').val('');
        $('#TxtCity1PD').val('');
        $('#TxtCityID').val('');
        $('#TxtGSTPD').val('');

        $('#ddlAddressbyparty').empty().append('<option value="">-Select Address-</option>');
        $('#ddSaudaNo').empty().append('<option value="">-Select Sauda No.-</option>');
        $('#ddWBNo').empty().append('<option value="">-Select WeighBridge No.-</option>');

        if (!shipCode) {

            $('#TxtAdd1SD').val('');
            $('#TxtAdd2SD').val('');
            $('#TxtAdd3SD').val('');
            $('#NumPincodeSD').val('');
            $('#TxtCity1SD').val('');
            $('#TxtGSTSD').val('');

            $('#ddlAddressbypartySD').empty().append('<option value="">-Select Address-</option>');
        }
    }
});

$('#ddlShipFrom').on('change', function () {
    var shipCode = $(this).val();
    var selectedOption = $(this).find('option:selected');
    if (shipCode) {
        $('#TxtAdd1SD').val(selectedOption.data('add1'));
        $('#TxtAdd2SD').val(selectedOption.data('add2'));
        $('#TxtAdd3SD').val(selectedOption.data('add3'));
        $('#NumPincodeSD').val(selectedOption.data('pincd'));
        $('#TxtCity1SD').val(selectedOption.data('cityname'));
        $('#TxtGSTSD').val(selectedOption.data('gstin'));
        GetPartyAddress(shipCode, '#ddlAddressbypartySD');
    } else {
        $('#TxtAdd1SD').val('');
        $('#TxtAdd2SD').val('');
        $('#TxtAdd3SD').val('');
        $('#NumPincodeSD').val('');
        $('#TxtCity1SD').val('');
        $('#TxtGSTSD').val('');
        $('#ddlAddressbypartySD').empty().append('<option value="">-Select Address-</option>');
    }
});


$(document).on('change', '#ddlAddressbyparty, #ddlAddressbypartySD', function () {
    const selectedOption = $(this).find('option:selected');
    const dropdownId = $(this).attr('id');

    if (dropdownId === 'ddlAddressbyparty') {
        $('#TxtAdd1PD').val(selectedOption.text().trim());
        $('#TxtAdd2PD').val(selectedOption.data('add2'));
        $('#TxtAdd3PD').val(selectedOption.data('add3'));
        $('#NumPincodePD').val(selectedOption.data('pincd'));
        $('#TxtCity1PD').val(selectedOption.data('citycd'));
        $('#TxtGSTPD').val(selectedOption.data('gstin'));

    } else {
        $('#TxtAdd1SD').val(selectedOption.text().trim());
        $('#TxtAdd2SD').val(selectedOption.data('add2'));
        $('#TxtAdd3SD').val(selectedOption.data('add3'));
        $('#NumPincodeSD').val(selectedOption.data('pincd'));
        $('#TxtCity1SD').val(selectedOption.data('cityname'));
        $('#TxtGSTSD').val(selectedOption.data('gstin'));
    }
});