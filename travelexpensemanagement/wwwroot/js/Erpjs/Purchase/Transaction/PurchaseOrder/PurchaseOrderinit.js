
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
        if (!validateRequiredField('#ddlPlace', 'Place')) return;
        if (!validateRequiredField('#ddlPartyName', 'Party Name')) return;
        if (!validateRequiredField('#ddlShipFrom', 'Ship From')) return;
        if (!validateRequiredField('#ddlPriceType', 'Price Type')) return;
        
        const vType = $('#ddlDocType').val();
        const partyRef = $('#txtPartyRef').val().trim();
        const totalAmount = parseFloat($('#NumAmountIt').val()) || 0;

        if (vType === 'PORD' && partyRef === '' && totalAmount >= 50000) {
            toastr.warning("Party Reference is required if total PO Amount is greater than or equal to 50,000.");
            return;
        }

        if (!(await checkValidDate())) {
            return;
        }

        const model = await getPurchaseOrderModel();

        if (!model.ItemRecords || model.ItemRecords.length === 0) {
            toastr.warning("Please enter at least one item.");
            return;
        }

        let hasItem = false;

        for (let i = 0; i < model.ItemRecords.length; i++) {

            const row = model.ItemRecords[i];

            if (!row.ItemCode) continue;

            hasItem = true;

            if (!row.Amount || row.Amount <= 0) {
                toastr.warning(`Amount is required in row ${i + 1}.`);
                return;
            }

            if (!row.LandRate || row.LandRate <= 0) {
                toastr.warning(`Loaded Rate is required in row ${i + 1}.`);
                return;
            }
        }

        if (!hasItem) {
            toastr.warning("Please enter at least one item.");
            return;
        }

        // Save
        await SaveData(model);

    }
    catch (error) {
        console.error(error);
        toastr.error(error.responseText || "An error occurred while saving the data.");
    }
});

$('#ddlPartyName').on('change', async function () {
    let PartyCode = $('#ddlPartyName').val();
    await GetPartyAddress(PartyCode);
    await GetSaudanoList(PartyCode);
    await GetWeighBridge(PartyCode);
    if (SelectParty == false) return;
    if (!PartyCode)
    {
        return;
    }
    await GetDatabbyPartycode();
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
    if (selectItemOption == false) return;
    loadMakeDropdown(rowId, itemCode);
});

$(document).on("click", ".btn-Itemadd-action", function (e) {
    e.preventDefault();
    addItemRecordRow();
});

$(document).on('click', '#BtnCalculation', async function (event) {
    event.preventDefault();

    try {

        const itemRecords = await collectGridDetail();

        const payload = {
            Btn: "BTN",
            SaudaNo: parseInt($('#ddSaudaNo').val()) || 0,
            SaudaType: $('#ddSaudaNo option:selected').text().trim(),
            StateCode: parseInt($('#TxtGSTPD').val()) || 0,
            CityCode: parseInt($('#TxtCity1PD').val()) || 0,
            EffectiveDate: $('#dtDocDate').val(), // yyyy-MM-dd
            Orders: itemRecords
        };

        console.log("Request Payload:", payload);

        const response = await $.ajax({
            url: '/PurchaseOrder/CalculationBySaudaNo',
            type: 'POST',
            contentType: 'application/json',
            dataType: 'json',
            data: JSON.stringify(payload)
        });

        console.log("Calculation Response:", response);

        if (response.status) {

            await fillItemDetailsTableBySaudaNo(response.data);

            calculateAllRows();
            calculateAllTotals();

        } else {
            console.warn(response.message);
        }

    } catch (err) {
        console.error("Calculation Error:", err);

        if (err.responseJSON) {
            console.log(err.responseJSON);
        }
    }
});

$(document).on('click', '#btnPurchaseRequestCopy', async function () {
    const selectedItems = getPurchaseData();
    fillItemDetailsTable(selectedItems);
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
    let index = $(this).data("index");
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

// approval Code

$(document).on('click', '#btn_Sendapproval', function () {
    var FromName = window.location.pathname.split('/')[1];
    $.ajax({
        url: '/Approval/CheckPendingUser',
        type: 'POST',
        data: {
            vNo: rowId,
            vType: vtype
        },
        success: function (response) {
            console.log('Response:', response);
            // Pending with another user
            if (response.success === false) {
                showToast(`Pending With Another User (${response.userCode})`,
                    { type: "warning" });
                return;
            }
            // Approval_Code = 5
            if (response.approvalCode8 === true) {
                OpenApprovalModal({
                    DocType: vtype,
                    DocNo: rowId,
                    TableName: 'GATE1'
                });
                return;
            }
            // Approval_Code != 8
            OpenSendForApprovalModal({
                DocType: vtype,
                DocNo: rowId,
                UserCode: null,
                UserName: null,
                DocDate: null,
                TableName: 'GATE1',
                FromName, FromName
            });

        },
        error: function (xhr, status, error) {
            console.log(error);
            alert('Error while checking approval status.');
        }
    });

});

$(document).on('click', '#btn_Approved', function () {
    OpenApprovalModal({
        DocType: vtype,
        DocNo: rowId,
        TableName: 'GATE1'
    });
});



$(document).on('change', '#ddWBNo', async function () {
    var docid = $('#ddWBNo').find('option:selected').val();
    var partyCode = $('#ddlPartyName').val();
    let data = await GetWeighBridgeDetail(docid, partyCode);

    const $tbody = $('#tblItemRecordPO tbody');
    $tbody.empty();
    console.log(data);

    for (let index = 0; index < data.length; index++) {
        const item = data[index];
        const idx = index + 1;

        addItemRecordRow();

        $(`#ddlItemname${idx}`).val(item.ITEM_CODE).trigger('change');
        $(`#ddlImake${idx}`).val(item.MakeCode).trigger('change');
        $(`#TxtQty${idx}`).val(item.Qty);
        $(`#ddlUnit${idx}`).val(item.UNIT_CODE).trigger('change');
    }
});

$('#ddlDocType').on('change', function () {
    const VType = $(this).val();
    Wb_SaudaDdl_Make_enabledisable(VType);
    GetDocid(VType);
});

$('#selectAllQM').on('change', function () {
    const isChecked = $(this).is(':checked');
    $('#tblQuotationModal tbody .chkQuot').prop('checked', isChecked);
});         