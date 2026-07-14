
$(document).on('input', '[id^=TxtRate], [id^=TxtQty], [id^=TxtCess], [id^=TxtPack], [id^=TxtDisc]', function () {
    const $input = $(this);
    const rowId = $input.closest('tr').attr('id').replace('row', '');
    calculateTaxAmounts(rowId);
});

$(document).on('change', '[id^=ddlTax]', async function () {
    const rowId = $(this).closest('tr').attr('id').replace('row', '');
    const TaxCode = $(this).val();
    if (!TaxCode) return;
    const tax = await fetchDatabyTaxType(TaxCode);
    if (!tax || !tax.partydetails) return;
    const party = tax.partydetails;

    $(`#TxtCgstPercent${rowId}`).val(party.cgsT_PER);
    $(`#TxtSgstPercent${rowId}`).val(party.sgsT_PER);
    $(`#TxtIgstPercent${rowId}`).val(party.igsT_PER);
    $(`#TxtTcsPer${rowId}`).val(party.tcS_PER);
    $(`#TxtVatPercent${rowId}`).val(party.vaT_PER);
    $(`#TxtOthPer${rowId}`).val(party.otH_PER);
    $(`#TxtOthPer2${rowId}`).val(party.otH_PER2);
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

$('#ddlPartyName').on('change', async function () {
    let PartyCode = $('#ddlPartyName').val();
    if (!PartyCode) {
        return;
    }
    await GetSaudanoList(PartyCode)
    await Promise.allSettled([
        GetPartyAddress(PartyCode),     
        GetDatabbyPartycode(),     
 
    ]);

});

$('#ddlShipFrom').on('change', async function () { 

    let PartyCode = $('#ddlShipFrom').val();
    await GetShipPartyAddress(PartyCode);

    if (SelectShipParty == false) return;

    await LoadDatabyShipCode(PartyCode);    
});

$('#btnorders').on('click', async function () {
    if (!validateRequiredField('#ddSaudaNo', 'Sauda No')) return;
    await LoadOrdersModal();
});

$('#ddlAddressbyparty').on('change', async function () {
    const docId = getQueryParam('id');
    if (docId) return;
    let PartyCode = $('#ddlPartyName').val();
    let AddressCode = $('#ddlAddressbyparty').val();
    if (!PartyCode || !AddressCode) {
        return;
    }
     GetPartyAddressDetails(PartyCode, AddressCode);
});

$('#ddlAddressbypartySD').on('change', async function () {
    const docId = getQueryParam('id');
    if (docId) return;
    let PartyCode = $('#ddlShipFrom').val();
    let AddressCode = $('#ddlAddressbypartySD').val();
    if (!PartyCode || !AddressCode) {
        return;
    }
    GetShipPartyAddressDetails(PartyCode, AddressCode);
});

$('#btn_ModificationOrder').on('click', function () {
    loadModificationdata();
});

$(document).on('click', '.btn-delete-action, .btn-Itemdelete-action', function () {
    var tbody = $(this).closest('tbody');
    var rowCount = tbody.find('tr').length;

    if (rowCount > 1) {
        $(this).closest('tr').remove();
    } else {
        alert('Cannot delete the first row.');
    }
});

$(document).on('change', '[id^=ddlItemname]', function () {

    const rowId = this.id.replace('ddlItemname', '');
    const itemCode = $(this).val();

    console.log("Row:", rowId);
    console.log("Item Code:", itemCode);

    loadMakeDropdown(rowId, itemCode);

});

//Attachment

browseBtn.addEventListener("click", function () {
    fileInput.click();
});

fileInput.addEventListener("change", function () {

    Array.from(this.files).forEach(file => {

        if (!isDuplicateFile(file)) {
            selectedFiles.push(file);
        }
    });

    renderFileList();
    this.value = "";
});

$(document).on("click", ".erp-delete-file-btn", function () {

    const index = $(this).data("index");

    selectedFiles.splice(index, 1);

    renderFileList();
});

$(document).on("click", ".erp-delete-db-btn", function () {

    const index = $(this).data("index");

    globalAttachments.splice(index, 1);

    renderFileList();
});

dropZone.addEventListener("dragover", function (e) {
    e.preventDefault();
    dropZone.classList.add("dragover");
});

dropZone.addEventListener("dragleave", function () {
    dropZone.classList.remove("dragover");
});

dropZone.addEventListener("drop", function (e) {
    e.preventDefault();
    dropZone.classList.remove("dragover");

    const files = e.dataTransfer.files;

    Array.from(files).forEach(file => {

        if (!isDuplicateFile(file)) {
            selectedFiles.push(file);
        }
    });

    renderFileList();
});
