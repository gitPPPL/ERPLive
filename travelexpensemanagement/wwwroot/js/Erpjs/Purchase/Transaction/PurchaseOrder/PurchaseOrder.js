
async function LoadDropdown() {
    try {
        await Promise.all([
            GetDocTypeAsync(),
            DDlPartyList(),
            DDlShipPartyList(),
            GetCurrencyList(),
            GetPayTermList(),
            DDLCityMast(),
            GetPlaceList(),
            DDLTxtCity1SDt(),
            loadItemNameDropdown(),   
            loadUnitDropdown(),
            loadPlaceDropdown(),
            loadDepartmentDropdown(),
            loadTaxTypeDropdown(),
            loadStatusDropdown()
      
        ]);

    } catch (error) {
        console.log("Dropdown load failed:", error);
        toastr.error("Failed to load dropdown data");
    }
}

async function GetShipFromList(ShipCode) {
    try {
        const response = await $.ajax({
            url: '/PurchaseOrder/GetPartyList',
            type: 'GET',
            dataType: 'json'
        });

        console.log("GetShipFromList", response);

    } catch (error) {
        console.log(error);
        toastr.error("Party Load failed");
    }
}

async function GetPartyList(selectedValue = null) {
    try {
        const response = await $.ajax({
            url: '/PurchaseOrder/GetPartyList',
            type: 'GET',
            data: { selectedValue: selectedValue },
            dataType: 'json'
        });

        console.log("GetPartyList", response);

    } catch (error) {
        console.log(error);
        toastr.error("Party Load failed");
    }
}

async function GetPayTermList(selectedValue = null) {
    try {
        const response = await $.ajax({
            url: '/PurchaseOrder/GetPayTermList',
            type: 'GET',
            dataType: 'json'
        });

        if (response && response.status) {

            const $dropdown = $('#ddlPaymentTerm');
            $dropdown.empty();

            $dropdown.append('<option value="">- Select Payment Term -</option>');

            $.each(response.data, function (i, item) {
                $dropdown.append(new Option(item.NAME, item.CODE));
            });

            $dropdown.val(selectedValue || '').trigger('change');

        } else {
            toastr.error("Payment term load failed");
        }

    } catch (error) {
        console.log(error);
        toastr.error("Payment term load failed");
    }
}

async function DDLCityMast() {
    try {
        const res = await fetch('/PurchaseOrder/DDLCityMast');
        const data = await res.json();
        const ddl = $('#TxtCity1PD');
        ddl.empty().append('<option value="">-- Select City Name --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading City:", error);
    }
}

async function DDlPartyList() {
    try {
        const res = await fetch('/PurchaseOrder/DDlPartyList');
        const data = await res.json();

        const ddl = $('#ddlPartyName');

        // clear old options
        ddl.empty();

        // default option
        ddl.append('<option value=""></option>');


        data.forEach(item => {
            ddl.append(new Option(item.text, item.value));
        });

        // initialize / refresh select2
        if (ddl.hasClass("select2-hidden-accessible")) {
            ddl.trigger('change'); // refresh
        } else {
            ddl.select2({
                placeholder: "-- Select Party Name --",
                allowClear: true,
                width: '100%'
            });
        }

    } catch (error) {
        console.error("Error loading Party:", error);
    }
}

async function DDlShipPartyList() {
    try {
        const res = await fetch('/PurchaseOrder/DDlPartyList');
        const data = await res.json();
        const ddl = $('#ddlShipFrom');
        ddl.empty();
        ddl.append('<option value=""></option>');

        data.forEach(item => {
            ddl.append(new Option(item.text, item.value));
        });

        // initialize / refresh select2
        if (ddl.hasClass("select2-hidden-accessible")) {
            ddl.trigger('change'); // refresh
        } else {
            ddl.select2({
                placeholder: "-- Select Ship Party Name --",
                allowClear: true,
                width: '100%'
            });
        }

    } catch (error) {
        console.error("Error loading Party:", error);
    }
}

async function GetCurrencyList() {
    try {
        const res = await fetch('/PurchaseOrder/GetCurrencyMast');
        const data = await res.json();
        const ddl = $('#ddlCurrency');
        ddl.empty().append('<option value="">-- Select Currency --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Currency:", error);
    }
}

async function GetPlaceList() {
    try {
        const res = await fetch('/PurchaseOrder/GetPlaceMast');
        const data = await res.json();
        const ddl = $('#ddlPlace');
        ddl.empty().append('<option value="">-- Select Place --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Place:", error);
    }
}

async function DDLTxtCity1SDt() {
    try {
        const res = await fetch('/PurchaseOrder/DDLCityMast');
        const data = await res.json();
        const ddl = $('#TxtCity1SD');
        ddl.empty().append('<option value="">-- Select City Name --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading City:", error);
    }
}

async function GetSaudanoList(partyCd) {
    try {
        const data = await $.ajax({
            url: '/PurchaseOrder/GetSaudaList',
            method: 'GET',
            data: { partyCd: partyCd }
        });

        const ddl = $('#ddSaudaNo');
        ddl.empty().append('<option value="">-- Select Sauda No --</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.text}">${item.value}</option>`);
        });

    } catch (error) {
        console.error("Error loading Sauda No:", error);
    }
}

async function GetWeighBridge(partyCd) {
    try {
        const data = await $.ajax({
            url: '/PurchaseOrder/GetWeighBridge',
            method: 'GET',
            data: { partyCd: partyCd }
        });

        const ddl = $('#ddWBNo');
        ddl.empty().append('<option value="">-- Select WB No --</option>');
        
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });

    } catch (error) {
        console.error("Error loading Sauda No:", error);
    }
}

async function loadModificationdata() {

    try {
        const V_NO = $('#txtDocNo').val()?.trim();
        const V_date = $('#dtDocDate').val()?.trim();

        if (!V_NO) {
            toastr.info("Please enter document number");
            return;
        }

        const res = await $.ajax({
            url: '/PurchaseOrder/GetModificationData',
            method: 'GET',
            data: { V_NO: V_NO }
        });

        console.log("Full response:", res);

        if (res.success) {

            const data = res.data || [];

            if (!data || data.length === 0) {
                toastr.info("Purchase Modification Not Found For this Doc No = " + V_NO + " and Doc Date " + V_date);
                return;
            }
            renderPurchaseModification(data);
        }

    } catch (err) {
        console.error("AJAX Error:", err);
        toastr.info("Something went wrong while fetching data");
    }
}
function renderPurchaseModification(data) {
    console.log("Rendering Purchase Modification Data:", data);
    const tbody = $('#modificationList tbody');
    tbody.empty();

    if (!data || data.length === 0) {
        tbody.append(`
            <tr>
                <td colspan="4" class="text-center">No data found</td>
            </tr>
        `);
    } else {
        data.forEach(item => {
            tbody.append(`
                <tr>
                    <td>${item.saudaNo ?? ''}</td>
                    <td>${item.party ?? ''}</td>
                    <td>${item.itemName ?? ''}</td>
                    <td>${item.qty ?? ''}</td>
                    <td>${item.rate ?? ''}</td>
                    <td>${item.remark ?? ''}</td>
                    <td>${item.modifyDate ?? ''}</td>
                </tr>
            `);
        });
    }

    const modalElement = document.getElementById('modificationModal');
    const myModal = new bootstrap.Modal(modalElement);
    myModal.show();
}
function calculateAllRows() {

    if (Calculation === false) return;

    let totalNos = 0,
        totalQty = 0,
        totalRate = 0,
        totalAmount = 0,
        totalPackAmt = 0,
        totalDiscAmt = 0,
        totalCgstAmt = 0,
        totalSgstAmt = 0,
        totalIgstAmt = 0,
        totalVatAmt = 0,
        totalCessAmt = 0,
        totalTcsAmt = 0,
        totalOtherAmt = 0,
        totalNetAmt = 0;

    $('#tblItemRecordPO tbody tr').each(function () {
        const $row = $(this);
        const idx = $row.attr('id').replace('row', '');

        totalNos = parseFloat($(`#TxtNos${idx}`).val()) || 0;
        totalQty = parseFloat($(`#TxtQty${idx}`).val()) || 0;
        totalRate = parseFloat($(`#TxtRate${idx}`).val()) || 0;
        totalAmount = totalQty + totalRate;
        $(`#TxtAmount${idx}`).val(totalAmount);

        PackPer = parseFloat($(`#TxtPackPercent${idx}`).val()) || 0;
        DiscPer = parseFloat($(`#TxtDiscPercent${idx}`).val()) || 0;
        CgstPer = parseFloat($(`#TxtCgstPercent${idx}`).val()) || 0;
        SgstPer = parseFloat($(`#TxtSgstPercent${idx}`).val()) || 0;
        IgstPer = parseFloat($(`#TxtIgstPercent${idx}`).val()) || 0;
        VatPer = parseFloat($(`#TxtVatPercent${idx}`).val()) || 0;
        CessPer = parseFloat($(`#TxtCessPercent${idx}`).val()) || 0;

        CgstAmt = (totalAmount * CgstPer) / 100;
        SgstAmt = (totalAmount * SgstPer) / 100;
        IgstAmt = (totalAmount * IgstPer) / 100;
        DiscAmt = (totalAmount * PackPer) / 100;

        $(`#TxtCgst${idx}`).val(CgstAmt);
        $(`#TxtSgst${idx}`).val(SgstAmt);
        $(`#TxtIgst${idx}`).val(IgstAmt);
        $(`#TxtNetAmt${idx}`).val((totalAmount + CgstAmt + SgstAmt + IgstAmt) - DiscAmt);
    });

    $('#NumTotalNosIt').val(totalNos.toFixed(2));
    $('#NumQtyIt').val(totalQty.toFixed(2));
    $('#NumAmountIt').val(totalAmount.toFixed(2));
    $('#NumPackingAmtIt').val(totalPackAmt.toFixed(2));
    $('#NumDiscAmtIt').val(totalDiscAmt.toFixed(2));
    $('#NumCgstAmtIt').val(totalCgstAmt.toFixed(2));
    $('#NumSgstAmtIt').val(totalSgstAmt.toFixed(2));
    $('#NumIgstAmtIt').val(totalIgstAmt.toFixed(2));
    $('#NumVatAmtIt').val(totalVatAmt.toFixed(2));
    $('#NumCessAmtIt').val(totalCessAmt.toFixed(2));
    $('#NumTCSIt').val(totalTcsAmt.toFixed(2));
    $('#NumOtherAmtIt').val(totalOtherAmt.toFixed(2));
    $('#NumNetAmtIt').val(totalNetAmt.toFixed(2));
}
function calculateAllTotals() {

    if (Calculation === false) return;

    let totalNos = 0,
        totalQty = 0,
        totalAmount = 0,
        totalPackAmt = 0,
        totalDiscAmt = 0,
        totalCgstAmt = 0,
        totalSgstAmt = 0,
        totalIgstAmt = 0,
        totalVatAmt = 0,
        totalCessAmt = 0,
        totalTcsAmt = 0,
        totalOtherAmt = 0,
        totalNetAmt = 0;

    $('#tblItemRecordPO tbody tr').each(function () {
        const $row = $(this);
        const idx = $row.attr('id').replace('row', '');

        totalNos += parseFloat($(`#TxtNos${idx}`).val()) || 0;
        totalQty += parseFloat($(`#TxtQty${idx}`).val()) || 0;
        totalAmount += parseFloat($(`#TxtAmount${idx}`).val()) || 0;
        totalPackAmt += parseFloat($(`#TxtPack${idx}`).val()) || 0;
        totalDiscAmt += parseFloat($(`#TxtDisc${idx}`).val()) || 0;
        totalCgstAmt += parseFloat($(`#TxtCgst${idx}`).val()) || 0;
        totalSgstAmt += parseFloat($(`#TxtSgst${idx}`).val()) || 0;
        totalIgstAmt += parseFloat($(`#TxtIgst${idx}`).val()) || 0;
        totalVatAmt += parseFloat($(`#TxtVat${idx}`).val()) || 0;
        totalCessAmt += parseFloat($(`#TxtCess${idx}`).val()) || 0;
        totalTcsAmt += parseFloat($(`#TxtTcsAmt${idx}`).val()) || 0;
        totalOtherAmt += parseFloat($(`#TxtOthAmt${idx}`).val()) || 0;
        totalOtherAmt += parseFloat($(`#TxtOthAmt2${idx}`).val()) || 0;
        totalNetAmt += parseFloat($(`#TxtNetAmt${idx}`).val()) || 0;
    });

    $('#NumTotalNosIt').val(totalNos.toFixed(2));
    $('#NumQtyIt').val(totalQty.toFixed(2));
    $('#NumAmountIt').val(totalAmount.toFixed(2));
    $('#NumPackingAmtIt').val(totalPackAmt.toFixed(2));
    $('#NumDiscAmtIt').val(totalDiscAmt.toFixed(2));
    $('#NumCgstAmtIt').val(totalCgstAmt.toFixed(2));
    $('#NumSgstAmtIt').val(totalSgstAmt.toFixed(2));
    $('#NumIgstAmtIt').val(totalIgstAmt.toFixed(2));
    $('#NumVatAmtIt').val(totalVatAmt.toFixed(2));
    $('#NumCessAmtIt').val(totalCessAmt.toFixed(2));
    $('#NumTCSIt').val(totalTcsAmt.toFixed(2));
    $('#NumOtherAmtIt').val(totalOtherAmt.toFixed(2));
    $('#NumNetAmtIt').val(totalNetAmt.toFixed(2));
}
function calculateTaxAmounts(rowId) {
    const rate = parseFloat($(`#TxtRate${rowId}`).val()) || 0;
    const qty = parseFloat($(`#TxtQty${rowId}`).val()) || 0;
    const amount = rate * qty;

    const discPer = parseFloat($(`#TxtDiscPercent${rowId}`).val()) || 0;
    const discAmt = (amount * discPer) / 100;
    $(`#TxtDisc${rowId}`).val(discAmt.toFixed(2));

    const packPer = parseFloat($(`#TxtPackPercent${rowId}`).val()) || 0;
    const packAmt = (amount * packPer) / 100;
    $(`#TxtPack${rowId}`).val(packAmt.toFixed(2));

    const taxableAmount = amount - discAmt + packAmt;

    const cgstPer = parseFloat($(`#TxtCgstPercent${rowId}`).val()) || 0;
    const sgstPer = parseFloat($(`#TxtSgstPercent${rowId}`).val()) || 0;
    const igstPer = parseFloat($(`#TxtIgstPercent${rowId}`).val()) || 0;
    const cessPer = parseFloat($(`#TxtCessPercent${rowId}`).val()) || 0;
    const tcsPer = parseFloat($(`#TxtTcsPer${rowId}`).val()) || 0;
    const vatPer = parseFloat($(`#TxtVatPercent${rowId}`).val()) || 0;
    const othPer1 = parseFloat($(`#TxtOthPer${rowId}`).val()) || 0;
    const othPer2 = parseFloat($(`#TxtOthPer2${rowId}`).val()) || 0;

    const cgstAmt = (taxableAmount * cgstPer) / 100;
    const sgstAmt = (taxableAmount * sgstPer) / 100;
    const igstAmt = (taxableAmount * igstPer) / 100;
    const cessAmt = (taxableAmount * cessPer) / 100;
    const tcsAmt = (taxableAmount * tcsPer) / 100;
    const vatAmt = (taxableAmount * vatPer) / 100;
    const othAmt1 = (taxableAmount * othPer1) / 100;
    const othAmt2 = (taxableAmount * othPer2) / 100;

    const totalTax = cgstAmt + sgstAmt + igstAmt + cessAmt + tcsAmt + vatAmt + othAmt1 + othAmt2;
    const netAmt = taxableAmount + totalTax;

    // Update DOM
    $(`#TxtAmount${rowId}`).val(amount.toFixed(2));
    $(`#TxtCgst${rowId}`).val(cgstAmt.toFixed(2));
    $(`#TxtSgst${rowId}`).val(sgstAmt.toFixed(2));
    $(`#TxtIgst${rowId}`).val(igstAmt.toFixed(2));
    $(`#TxtCess${rowId}`).val(cessAmt.toFixed(2));
    $(`#TxtTcsAmt${rowId}`).val(tcsAmt.toFixed(2));
    $(`#TxtVat${rowId}`).val(vatAmt.toFixed(2));
    $(`#TxtOthAmt${rowId}`).val(othAmt1.toFixed(2));
    $(`#TxtOthAmt2${rowId}`).val(othAmt2.toFixed(2));
    $(`#TxtNetAmt${rowId}`).val(netAmt.toFixed(2));

    calculateAllTotals(); 
}

async function loadItemNameDropdown() {
    try {
        let v_type = $('#ddlDocType').val();
        const data = await $.ajax({
            url: '/PurchaseOrder/DDLGridItem',
            type: 'GET',
            data: { v_type: v_type }
        });

        itemNameOptions = data .map(x => `<option value="${x.value}">${x.text}</option>`) .join('');

        return itemNameOptions;

    } catch (error) {
        console.error("Error loading Item Name dropdown:", error);
        itemNameOptions = "";
        return "";
    }
}

async function loadMakeDropdown(rowId, itemCode) {
    try {
        const data = await $.ajax({
            url: '/PurchaseOrder/DDLGridMake',
            type: 'GET',
            data: {
                ItemCode: itemCode
            }
        });

        let options = '<option value="">-Select Make-</option>';

        $.each(data, function (i, item) {
            options += `<option value="${item.value}">${item.text}</option>`;
        });

        $('#ddlImake' + rowId).html(options);

        return true;

    } catch (error) {
        console.error("Error loading Make dropdown:", error);
        return false;
    }
}
function loadUnitDropdown() {
    $.ajax({
        url: '/PurchaseOrder/DDLUnitList',
        method: 'GET',
        success: function (data) {
            UnitOptions = data.map(x => `<option value="${x.value}">${x.text}</option>`).join('');
        }
    });

}
function loadPlaceDropdown() {
    $.ajax({
        url: '/PurchaseOrder/DDLPlaceList',
        method: 'GET',
        success: function (data) {
            PlaceOptions = data.map(x => `<option value="${x.value}">${x.text}</option>`).join('');
        }
    });

}

async function loadDepartmentDropdown() {
    try {

        const data = await $.ajax({
            url: '/PurchaseOrder/DDLDepartmentList',
            type: 'GET'
        });

        DepartmentOptions = data
            .map(x => `<option value="${x.value}">${x.text}</option>`)
            .join('');

        return DepartmentOptions;

    } catch (error) {
        console.error("Error loading Department Name dropdown:", error);
        DepartmentOptions = "";
        return "";
    }
}
function loadTaxTypeDropdown() {
    $.ajax({
        url: '/PurchaseOrder/DDLTaxTypeList',
        method: 'GET',
        success: function (data) {
            TaxTypeOptions = data.map(x => `<option value="${x.value}">${x.text}</option>`).join('');
        }
    });

}

async function loadStatusDropdown() {
    try {

        const data = await $.ajax({
            url: '/PurchaseOrder/DllStatus',
            type: 'GET'
        });

        statuslist = data
            .map(x => `<option value="${x.value}">${x.text}</option>`)
            .join('');

        return statuslist;

    } catch (error) {
        console.error("Error loading Item Name dropdown:", error);
        statuslist = "";
        return "";
    }
}

async function DDLCityMast() {
    try {
        const res = await fetch('/PurchaseOrder/DDLCityMast');
        const data = await res.json();
        const ddl = $('#TxtCity1PD');
        ddl.empty().append('<option value="">-- Select City Name --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading City:", error);
    }
}

async function GetDocData(MasterTblId, readOnly) {
    try {

        const response = await $.ajax({
            url: '/PurchaseOrder/GetPurchaseOrderRecordsById',
            type: 'GET',
            data: { id: MasterTblId }
        });

        if (!response || !response.status) {
            toastr.error('No data returned.');
            return;
        }
        if (readOnly === 'true') {
            disableAllFields();
            $('.btn-add-row-last').hide();
            $('#btn-save, #cancelBtn').hide();
        }

        else {
            enableAllFields();
            $('#btn-save, #cancelBtn').show();
            $('.btn-add-row-last').show();
        }

        Calculation = false;
        SelectShipParty = false;
        SelectParty = false;
        selectItemOption = false;

        await fillPurchaseOrderData(response.header, response.detail);

        Calculation = true;
        SelectShipParty = true;
        SelectParty = true;
        selectItemOption = true;

        globalAttachments = response.attachment;


        $('#fileList').empty();
        const attachments = Array.isArray(response.attachment) ? response.attachment : [];

        if (attachments.length === 0) {
            $('#fileList').html(`  <div class="text-muted text-center"> No attachments found.  </div> `);
            return;
        }

        attachments.forEach((att, idx) => {
            const fileName = att.FILE_NAME || att.FileName || `File_${idx + 1}`;

            let fileUrl = "";
            let blobUrl = "";

            if (att.IMG_FILE) {
                let base64 = att.IMG_FILE.replace(/\s/g, "");

                let ext = fileName.split('.').pop().toLowerCase();

                let mimeType = "application/octet-stream";

                if (["jpg", "jpeg"].includes(ext)) {
                    mimeType = "image/jpeg";
                }
                else if (ext === "png") {
                    mimeType = "image/png";
                }
                else if (ext === "gif") {
                    mimeType = "image/gif";
                }
                else if (ext === "webp") {
                    mimeType = "image/webp";
                }
                else if (ext === "pdf") {
                    mimeType = "application/pdf";
                }

                fileUrl = `data:${mimeType};base64,${base64}`;

                // Convert Base64 to Blob URL for View
                const byteCharacters = atob(base64);
                const byteNumbers = new Array(byteCharacters.length);

                for (let i = 0; i < byteCharacters.length; i++) {
                    byteNumbers[i] = byteCharacters.charCodeAt(i);
                }

                const byteArray = new Uint8Array(byteNumbers);

                const blob = new Blob(
                    [byteArray],
                    { type: mimeType }
                );

                blobUrl = URL.createObjectURL(blob);
            }

            let previewHtml = "";

            if (fileName.match(/\.(jpg|jpeg|png|gif|webp|bmp)$/i)) {
                previewHtml = `<img src="${fileUrl}"
                style="
                width:60px;
                height:60px;
                object-fit:cover;
                border:1px solid #ccc;"> `;

            }
            else if (fileName.match(/\.pdf$/i)) {
                previewHtml = `<span style="font-size:30px;color:red;"> ?? </span> `;
            }

            else {
                previewHtml = `<span style="font-size:30px;"> ?? </span> `;
            }

            $("#fileList").append(`<div class="file-item erp-file-row"
             data-index="${idx}"
             style="
             display:flex;
             align-items:center;
             gap:15px;
             border:1px solid #ddd;
             padding:10px;
             margin-bottom:5px;">


            <!-- Preview -->
            <div class="file-preview"> ${previewHtml}  </div>

            <!-- File Name -->
            <div class="file-info" style="flex:1">  ${fileName} </div>
            <!-- Buttons -->
            <div class="file-actions">
            <!-- View -->
            <a href="${blobUrl}" target="_blank" class="btn btn-sm btn-primary">  View </a>

            <!-- Delete -->
            <button type="button" class="btn btn-sm btn-danger erp-delete-db-btn" data-index="${idx}"> Delete </button>
            </div>

         </div>

    `);

        });
    }
    catch (error) {
        console.error(error);
        toastr.error('Failed to load data.');
    }
}
function quotationRtApproval() {
    var partyCode = $('#ddlPartyName option:selected').val() || '';
    $.ajax({
        url: '/PurchaseOrder/GetQuotationRtAprvlLiast',
        type: 'GET',
        dataType: 'json',
        data: { partyCode: partyCode },
        success: function (response) {
            if (response.status) {
                var $tableBody = $('#tblQuotationModal tbody');
                $tableBody.empty();
                $.each(response.data, function (index, item) {
                    var tableRow = `
                            <tr>
                                <td><input type="checkbox" id="chkQuot" /></td>
                                <td>${item.V_NO}</td>
                                <td>${item.V_TYPE}</td>
                                <td>${item.V_DATE}</td>
                                <td value=${item.ITEM_CODE}>${item.itemName}</td>
                                <td value="${item.UOM_CODE}">${item.Unit}</td>
                                <td value="${item.MAKE_CODE}">${item.make}</td>
                                <td>${item.TECH_DESC}</td>
                                <td>${item.QTY}</td>
                                <td>${item.RATE}</td>
                                <td>${item.AMOUNT}</td>
                                <td>${item.PACK_PER}</td>
                                <td>${item.PACK_AMT}</td>
                                <td>${item.FREIGHT}</td>
                                <td>${item.DISC_PER}</td>
                                <td>${item.DISC_AMT}</td>
                                <td value="${item.TAX_CODE}">${item.taxType}</td>
                                <td>${item.CGST_PER}</td>
                                <td>${item.CGST_AMT}</td>
                                <td>${item.SGST_PER}</td>
                                <td>${item.SGST_AMT}</td>
                                <td>${item.IGST_PER}</td>
                                <td>${item.IGST_AMT}</td>
                                <td>${item.CESS_PER}</td>
                                <td>${item.CESS_AMT}</td>
                                <td>${item.OTH_EXPS}</td>
                                <td>${item.LD_RATE}</td>
                                <td>${item.NET_AMT}</td>
                                <td value="${item.PARTY_CODE}">${item.party}</td>
                                <td>${item.REQ_NO}</td>
                                <td>${item.REQ_TYPE}</td>
                                <td>${item.APROV_REMARKS}</td>
                                <td>${item.STATUS}</td>
                            </tr>
                        `;
                    $tableBody.append(tableRow);
                });
            }
        },
        error: function (xhr, status, error) {
            toastr.error('Data load failed');
        }
    });
}
function getquoteTabledata() {
    var quoteData = [];

    $('#tblQuotationModal tbody tr').each(function () {
        var $checkbox = $(this).find('input[type="checkbox"]');

        if ($checkbox.is(':checked')) {
            var row = $(this).find('td');

            var rowData = {
                V_NO: $(row[1]).text().trim(),
                V_TYPE: $(row[2]).text().trim(),
                V_DATE: $(row[3]).text().trim(),
                ITEM_CODE: $(row[4]).attr('value'),
                itemName: $(row[4]).text().trim(),
                UOM_CODE: $(row[5]).attr('value'),
                Unit: $(row[5]).text().trim(),
                MAKE_CODE: $(row[6]).attr('value'),
                make: $(row[6]).text().trim(),
                TECH_DESC: $(row[7]).text().trim(),
                QTY: $(row[8]).text().trim(),
                RATE: $(row[9]).text().trim(),
                AMOUNT: $(row[10]).text().trim(),
                PACK_PER: $(row[11]).text().trim(),
                PACK_AMT: $(row[12]).text().trim(),
                FREIGHT: $(row[13]).text().trim(),
                DISC_PER: $(row[14]).text().trim(),
                DISC_AMT: $(row[15]).text().trim(),
                TAX_CODE: $(row[16]).attr('value'),
                taxType: $(row[16]).text().trim(),
                CGST_PER: $(row[17]).text().trim(),
                CGST_AMT: $(row[18]).text().trim(),
                SGST_PER: $(row[19]).text().trim(),
                SGST_AMT: $(row[20]).text().trim(),
                IGST_PER: $(row[21]).text().trim(),
                IGST_AMT: $(row[22]).text().trim(),
                CESS_PER: $(row[23]).text().trim(),
                CESS_AMT: $(row[24]).text().trim(),
                OTH_EXPS: $(row[25]).text().trim(),
                LD_RATE: $(row[26]).text().trim(),
                NET_AMT: $(row[27]).text().trim(),
                PARTY_CODE: $(row[28]).attr('value'),
                party: $(row[28]).text().trim(),
                REQ_NO: $(row[29]).text().trim(),
                REQ_TYPE: $(row[30]).text().trim(),
                APROV_REMARKS: $(row[31]).text().trim(),
                STATUS: $(row[32]).text().trim()
            };

            quoteData.push(rowData);
        }
    });
    return quoteData;
}
function PurchaseRateApproval() {
    $.ajax({
        url: '/PurchaseOrder/GetPurchaseRtApprovalList',
        type: 'GET',
        dataType: 'json',
        success: function (response) {
            if (response.status) {
                var $tableBody = $('#tblPurchaseRequest tbody');
                $tableBody.empty();

                $.each(response.data, function (index, item) {
                    var quotRow = `
                        <tr>
                            <td><input type="checkbox" id="ChkPurchase"/></td>
                            <td>${item.V_NO}</td>
                            <td>${item.V_TYPE}</td>
                            <td>${item.V_DATE}</td>
                            <td>${item.ITEM_CODE}</td>
                            <td>${item.ItemName}</td>
                            <td>${item.Unit}</td>
                            <td>${item.makename}</td>
                            <td>${item.TECH_DESC}</td>
                            <td>${item.REQ_QTY}</td>
                            <td>${item.V_DATE}</td>
                            <td>${item.APROV_REMARKS}</td>
                            <td>${item.STATUS}</td>
                            <td>${item.Department}</td>
                            <td>${item.DEPT_CODE}</td>
                            <td>${item.MAKE_CODE}</td>
                            <td>${item.V_NO}</td>
                            <td>${item.V_TYPE}</td>
                            <td>${item.UOM_CODE}</td>
                        </tr>
                        `;
                    $tableBody.append(quotRow);
                });
            } else {
                toastr.error("Failed to load quotation data.");
            }
        },
        error: function (xhr, status, error) {
            toastr.error("Data load failed: " + error);
        }
    });
}

async function fillItemDetailsTableByQuoteBtn(data) {
    const $tbody = $('#tblItemRecordPO tbody');
    $tbody.empty();

    for (let index = 0; index < data.length; index++) {
        const item = data[index];
        const idx = index + 1;

        addItemRecordRow();
        console.log('a');
        $(`#ddlItemname${idx}`).val(item.ITEM_CODE);
        $(`#ddlUnit${idx}`).val(item.UOM_CODE || '');
        $(`#ddlImake${idx}`).val(item.MAKE_CODE || '');
        $(`#TxtQty${idx}`).val(item.QTY || '');
        $(`#ddlDepartment${idx}`).val(item.Department || '');
        $(`#TxtRemarks${idx}`).val(item.TECH_DESC || '');
        $(`#TxtAppRemarks${idx}`).val(item.APROV_REMARKS || '');
        $(`#TxtAppLevel${idx}`).val(item.STATUS || '');

        // Optional fields: blank if not available
        $(`#TxtRate${idx}`).val('');
        $(`#TxtExrate${idx}`).val('');
        $(`#TxtCalcRate${idx}`).val('');
        $(`#TxtAmount${idx}`).val('');
        $(`#TxtPackPercent${idx}`).val('');
        $(`#TxtPack${idx}`).val('');
        $(`#TxtDiscPercent${idx}`).val('');
        $(`#TxtDisc${idx}`).val('');
        $(`#ddlTax${idx}`).val('');
        $(`#TxtCgstPercent${idx}`).val('');
        $(`#TxtCgst${idx}`).val('');
        $(`#TxtSgstPercent${idx}`).val('');
        $(`#TxtSgst${idx}`).val('');
        $(`#TxtIgstPercent${idx}`).val('');
        $(`#TxtIgst${idx}`).val('');
        $(`#TxtVatPercent${idx}`).val('');
        $(`#TxtVat${idx}`).val('');
        $(`#TxtCessPercent${idx}`).val('');
        $(`#TxtCess${idx}`).val('');
        $(`#TxtTcsPer${idx}`).val('');
        $(`#TxtTcsAmt${idx}`).val('');
        $(`#TxtOthPer${idx}`).val('');
        $(`#TxtOthAmt${idx}`).val('');
        $(`#TxtOthPer2${idx}`).val('');
        $(`#TxtOthAmt2${idx}`).val('');
        $(`#TxtNetAmt${idx}`).val('');
        $(`#TxtLdRate${idx}`).val('');
        $(`#TxtNos${idx}`).val('');
        $(`#TxtMthRate${idx}`).val('');
        $(`#TxtQtrRate${idx}`).val('');
        $(`#TxtAnlRate${idx}`).val('');
        $(`#TxtSpclRate${idx}`).val('');
    }
}
function enableAllFields() {
    AllFieldsId.forEach(id => {
        const el = document.getElementById(id);
        if (el) el.disabled = false;
    });
}
function disableAllFields() {
    AllFieldsId.forEach(id => {
        const el = document.getElementById(id);
        if (el) el.disabled = true;
    });
}
function toBase64(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result.split(',')[1]);
        reader.onerror = reject;
        reader.readAsDataURL(file);
    });
}

async function GetDocTypeAsync() {
    try {
        const res = await fetch('/PurchaseOrder/GetDocType');
        const data = await res.json();
        const ddl = $('#ddlDocType');
        ddl.empty().append('');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Doc Type:", error);
    }
}
function GetSaudaDetail(docid) {
    return new Promise((resolve, reject) => {
        $.ajax({
            url: '/PurchaseOrder/GetSaudaDetail',
            type: 'Get',
            dataType: 'json',
            data: { docid: docid },
            success: function (response) {
                if (response.status) {
                    resolve(response.data);
                }
            },
            error: function (xhr, status, error) {
                reject("Sauda Load failed: " + error);
            }
        })
    });
}
function GetPartyAddress(partyCd, selectedId, selectedValue = null)
{
    $.ajax({
        url: '/PurchaseOrder/GetPartyAddress',
        type: 'GET',
        dataType: 'json',
        data: { partyCd: partyCd },
        success: function (response) {
            if (response.status) {
                const $dropdown = $(selectedId);
                $dropdown.empty(response.data);
                $dropdown.append('<option selected disabled value="">- Select Address -</option>');

                $.each(response.data, function (index, item) {
                    $dropdown.append(`
                            <option
                                data-add2="${item.ADD2}"
                                data-add3="${item.ADD3}"
                                data-cityName="${item.CityName}"
                                data-GstIn="${item.GSTIN}"
                                data-cityCd="${item.CITY_CODE}"
                                data-pinCd="${item.PINCODE}"
                                value="${item.code || index}"
                            >
                                ${item.ADD1}
                            </option>
                        `);
                });

                if (selectedValue && $dropdown.find(`option[value="${selectedValue}"]`).length > 0) {
                    $dropdown.val(selectedValue).trigger('change');
                } else {
                    $dropdown.prop('selectedIndex', 0);
                }
            } else {
                toastr.error("Party Address Load failed");
            }
        },
        error: function (xhr, status, error) {
            toastr.error("Party Address Load failed: " + error);
        }
    });
}
function GetWeighBridgeNo(partyCd, selectedValue = null) {
    $.ajax({
        url: '/PurchaseOrder/GetWeighBridge',
        type: 'GET',
        dataType: 'json',
        data: { partyCd: partyCd },
        success: function (response) {
            if (response.status) {
                const $dropdown = $('#ddWBNo');
                $dropdown.empty();
                $dropdown.append('<option selected disabled value="">- Select WeighBridge No. -</option>');

                $.each(response.data, function (index, item) {
                    $dropdown.append(`<option
                        value="${item.DOC_ID}">
                        ${item.V_NO}</option>`);
                });

                if (selectedValue && $dropdown.find(`option[value="${selectedValue}"]`).length > 0) {
                    $dropdown.val(selectedValue).trigger('change');
                } else {
                    $dropdown.prop('selectedIndex', 0);
                }
            } else {
                toastr.error("WeighBridge Load failed");
            }
        },
        error: function (xhr, status, error) {
            toastr.error("WeighBridge Load failed: " + error);
        }
    });
}
function GetWeighBridgeDetail(docid, partyCode) {
    return new Promise((resolve, reject) => {
        $.ajax({
            url: '/PurchaseOrder/GetWeightBridgeDetail',
            type: 'GET',
            dataType: 'json',
            data: { docid: docid, partyCd: partyCode },
            success: function (response) {
                if (response.status) {
                    resolve(response.data);
                }
            },
            error: function (xhr, status, error) {
                reject("WeighBridge Load failed: " + error);
            }
        });
    });
}
function bindUnitOnItemSelect(itemSelect, unitSelect) {
    itemSelect.on('change', function () {
        const selectedOption = $(this).find('option:selected');
        const unitCode = selectedOption.data('unitcd');
        const unitName = selectedOption.data('unitname');

        if (unitCode) {
            if (unitSelect.find(`option[value="${unitCode}"]`).length === 0) {
                unitSelect.append(`<option value="${unitCode}">${unitName}</option>`);
            }
            unitSelect.val(unitCode).trigger('change');
        } else {
            unitSelect.val('').trigger('change');
        }
    });
}
function formatDate(dateStr) {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    return d.toISOString().split('T')[0];
}
function parseIntSafe(value) {
    const parsed = parseInt(value, 10);
    return isNaN(parsed) ? null : parsed;
}
function parseFloatSafe(value) {
    const parsed = parseFloat(value);
    return isNaN(parsed) ? null : parsed;
}
function parseDate(dateStr) {
    if (!dateStr) return null;
    const parts = dateStr.split(/[-\/]/);
    if (parts.length === 3) {
        let [day, month, year] = parts.map(p => parseInt(p, 10));
        if (year < 1000) year += 2000;
        return new Date(year, month - 1, day);
    }
    return null;
}
function toNullableInt(val) {
    const parsed = parseInt(val);
    return isNaN(parsed) ? null : parsed;
}
function toNullableDate(val) {
    const date = new Date(val);
    return isNaN(date.getTime()) ? null : val;
}
function toNullableString(val) {
    return val?.trim() || null;
}
function toNullableDate(val) {
    const date = new Date(val);
    return isNaN(date.getTime()) ? null : date.toISOString(); // if server expects ISO string
}
function allowOnlyNumbers(input) {
    input.value = input.value
        .replace(/[^0-9.]/g, '')
        .replace(/(\..*)\./g, '$1');
}
function setFieldsEnabled(enabled) {
    AllFieldsId.forEach(id => {
        const el = document.getElementById(id);
        if (el) {
            el.disabled = !enabled;
        }
    });
}
function setEnterKeyFocus(sequence) {
    sequence.forEach((id, index) => {
        $(`#${id}`).on('keypress', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                if (index + 1 < sequence.length) {
                    $(`#${sequence[index + 1]}`).focus();
                }
            }
        });
    });
}

async function GetPartyAddress(Partycode) {
    try {


        const data = await $.ajax({
            url: '/PurchaseOrder/GetPartyAddress',
            method: 'GET',
            data: { Partycode: Partycode }
        });

        const ddl = $('#ddlAddressbyparty');
        ddl.empty().append('<option value="">-- Select Address --</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });

    } catch (error) {
        console.error("Error loading Place:", error);
    }
}

async function GetShipPartyAddress(Partycode) {
    try {
        const data = await $.ajax({
            url: '/PurchaseOrder/GetPartyAddress',
            method: 'GET',
            data: { Partycode: Partycode }
        });

        const ddl = $('#ddlAddressbypartySD');
        ddl.empty().append('<option value="">-- Select Address --</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });

    } catch (error) {
        console.error("Error loading Place:", error);
    }
}
function GetDocid(VType) {
    $.ajax({
        url: '/PurchaseOrder/GetMaxVNo',
        type: 'GET',
        data: { V_type: VType },
        success: function (response) {
            if (response.status === true && response.data) {
                $('#txtDocNo').val(response.data.vNo || '');
                $('#TxtDocId').val(response.data.docId || '');
            } else {
                $('#txtDocNo').val('');
                $('#TxtDocId').val('');
            }
        },
        error: function (xhr, status, error) {
            toastr.error('Error fetching Doc ID:', error);
        }
    });
}

async function SendMail() {
    try {

        if (!docId) {
            showToast("Please save the data before Send Mail.", { type: "info" });
            return;
        }

        let PartyCode = $('#ddlPartyName').val();
        const vno = parseInt($('#txtDocNo').val()) || 0;
        const v_type = $('#ddlDocType').val() || '';

        // Check mail validation
        const checkRes = await $.ajax({
            url: '/PurchaseOrder/CheackMail',
            type: 'GET',
            data: { v_no: vno, v_type: v_type },
            dataType: 'json'
        });

        if (checkRes.status == false) {
            toastr.warning(checkRes.message);
            return;
        }

        // Confirmation
        const result = await Swal.fire({
            title: "Do you want to send mail?",
            icon: "question",
            showCancelButton: true,
            confirmButtonText: "Yes",
            cancelButtonText: "No"
        });

        if (!result.isConfirmed) {
            return;
        }

        // Step 1: Generate report
        const report = await GetTransitReportFile();

        if (!report || !report.file) {
            toastr.error("Report generation failed.");
            return;
        }

        // Step 2: Prepare FormData
        let formData = new FormData();
        formData.append("PartyCode", PartyCode);
        formData.append("vno", vno);
        formData.append("v_type", v_type);
        formData.append("file", report.file, report.fileName);



        // Step 3: Send mail
        const mailRes = await $.ajax({
            url: '/PurchaseOrder/SendMail',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false
        });


        if (mailRes.success == true)
        {
            toastr.success(mailRes.message || "Mail sent successfully.");
        }
        else
        {
            toastr.error(mailRes.message || "Failed to send mail.");
        }
        return mailRes;

    } catch (error) {
        console.error("Error:", error);
        toastr.error("An error occurred while sending the mail.");
    }
}

async function GetTransitReportFile()
{
    if (!docId) {
        showToast("Please save the data before printing the report.", { type: "info" });
        throw new Error("No docId");
    }

    let reportName = "";
    let RPTNAME = "";
    let pubFinalApprovedBy = "";

    reportName = (globalVars.CompCode == 7)
        ? "pr_porderK_gst"
        : "pr_porder_gst";

    const v_no = $('#txtDocNo').val();
    const v_type = $('#ddlDocType').val();

    const validation = await $.ajax({
        url: '/PurchaseOrder/PrintValidation',
        type: 'GET',
        data: { V_TYPE: v_type, V_NO: v_no}
    });

    // Check approval
    if (!validation.faproV_STATUS) {
        toastr.info("PO Not Approved, PO Report cannot be generated.");
        return null;
    }
    RPTNAME = validation.reportname || "";
    pubFinalApprovedBy = validation.signatoryList || "";

    const formula =
        " {ORDER1.V_TYPE} = '" + v_type + "'" +
        " and {ORDER1.V_NO} = " + v_no +
        " and {ORDER1.COMP_CODE} = " + globalVars.CompCode +
        " and {ORDER1.YEAR_CODE} = " + globalVars.FYearCode +
        " and {ORDER1.BRANCH_CODE} = " + globalVars.BranchCode;

    const payload = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: database,
        Parameters: {
            approvedBy: pubFinalApprovedBy,
            comp_name: globalVars.CompanyName || "",
            comp_add1: globalVars.Address1 || "",
            comp_add2: globalVars.Address2 || "",
            comp_phone: "Phone :" + (globalVars.Phone || ""),
            GST: "GSTIN :" + (globalVars.GST || ""),
            Website: "Website :" + (globalVars.pubCompWebsite || ""),
            PAN: "PAN :" + (globalVars.PAN || ""),
            EMAIL: "Email :" + (globalVars.Email || ""),
            RPTNAME: RPTNAME
        }
    };

    // Generate report
    const pdfBlob = await $.ajax({
        url: 'http://localhost:24085/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(payload),
        contentType: "application/json",
        xhrFields: {responseType: 'blob'}
    });

    const file = new Blob([pdfBlob], { type: "application/pdf" });
    const now = new Date();
    const timestamp = String(now.getDate()).padStart(2, '0') +
                      String(now.getMonth() + 1).padStart(2, '0') +
                      String(now.getFullYear()).slice(-2) + "_" +
                      String(now.getHours()).padStart(2, '0') +
                      String(now.getMinutes()).padStart(2, '0') +
                      String(now.getSeconds()).padStart(2, '0');

    const fileName = `SAUDA_PURCH_${v_no}_${timestamp}.pdf`;

    return { file, fileName };
}
