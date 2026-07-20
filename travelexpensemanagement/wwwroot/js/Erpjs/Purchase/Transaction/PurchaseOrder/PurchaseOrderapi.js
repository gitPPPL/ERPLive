
async function handleDocLoad() {


    SetFYDate('dtDocDate', LoginDate);

    if (docId) {
        await GetDocData(docId, readOnly);
        $('#SaudaDetail').show();
        $('#ddlDocType').prop('disabled', true);
        $('#ddlStatus').prop('disabled', false);
        var VTpeU = docId.substring(0, 4);
        var VNoU = docId.substring(4);
        $('#txtDocNo').val(VNoU);
        Wb_SaudaDdl_Make_enabledisable(VTpeU);

    } else {
        $('#SaudaDetail').hide();
        $('#ddlStatus').prop('disabled', true);
        const Vtype = $('#ddlDocType').val();
        Wb_SaudaDdl_Make_enabledisable(Vtype);
        GetDocid(Vtype);

        const today = new Date();
        const todayDate = today.getFullYear() + '-' + (today.getMonth() + 1).toString().padStart(2, '0') + '-' + today.getDate().toString().padStart(2, '0');

        $('#DtDeliveryDate').val(todayDate);
        $('#DtValidateDate').val(todayDate);
    }
}
function TransitReport() {

    if (!docId) {
        showToast(`Please save the data before printing the report.`, { type: "info" });
        return;
    }

    var reportName = "";

 
        reportName = "pr_porder_gst";


    var v_no = $('#TxtCode').val();
    var v_type = "PAUD";

    var formula =
        "{ORDER1.COMP_CODE} = " + globalVars.CompCode +
        " and {ORDER1.YEAR_CODE} = " + globalVars.FYearCode +
        " and {ORDER1.BRANCH_CODE} = " + globalVars.BranchCode +
        " and {ORDER1.V_NO} = " + v_no +
        " and {ORDER1.V_TYPE} = '" + v_type + "'";

    // Prepare the payload for the API
    var payload = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: database,
        Parameters: {
            comp_name: globalVars.CompanyName || "",
            comp_add1: globalVars.Address1 || "",
            comp_add2: globalVars.Address2 || "",
            COMP_GST: globalVars.GST || "",
            COMP_PAN: globalVars.PAN || "",
            COMP_CIN: globalVars.COMP_CIN || "",
            COMP_EMAIL: globalVars.Email || "",
            comp_phone: globalVars.Phone || "",
            RPTNAME: "PURCHASE CONTRACT/ORDER"
        }
    };

    // Timestamp for file name
    var now = new Date();
    var timestamp =
        String(now.getDate()).padStart(2, '0') +
        String(now.getMonth() + 1).padStart(2, '0') +
        String(now.getFullYear()).slice(-2) + "_" +
        String(now.getHours()).padStart(2, '0') +
        String(now.getMinutes()).padStart(2, '0') +
        String(now.getSeconds()).padStart(2, '0');

    $.ajax({
        url: 'http://localhost:24085/Report/PendingQCReport', // check port
        type: 'POST',
        data: JSON.stringify(payload),
        contentType: "application/json",
        xhrFields: { responseType: 'blob' }, // Important for PDF

        success: function (response) {
            // Convert response to a Blob
            var file = new Blob([response], { type: 'application/pdf' });
            var fileName = `${reportName}_${timestamp}.pdf`;

            // Trigger download
            var link = document.createElement('a');
            link.href = URL.createObjectURL(file);
            link.download = fileName;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        },

        error: function (xhr, status, error) {
            if (xhr.status === 0) {
                console.error("Cannot connect to API. Is the backend running?");
            } else {
                console.error('Error generating report:', xhr.status, xhr.statusText, error);
                xhr.responseText && console.error('Response:', xhr.responseText);
            }
        }
    });
}

async function checkValidDate() {
    const data = {
        vdate: $("#dtDocDate").val(),
        vtype: $('#ddlDocType').val(),
        vno: $("#txtDocNo").val()
    };
    try {
        const response = await fetch('/PurchaseOrder/CheckValidDate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });

        const result = await response.json();

        if (result.status === false) {
            showToast(result.message, { type: "warning" });
            return false;
        }

        return true;

    } catch (error) {
        showToast(result.message, { type: "warning" });
        return false;
    }
}

async function SaveData(model) {
    try {

        const response = await $.ajax({
            url: '/PurchaseOrder/SavedData',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(model),
            dataType: 'json'
        });

        if (response.success) {
            toastr.success( "Data saved successfully.");
            let rowId = model.VType + model.VNo;
            setTimeout(function () {
                window.location.href = '/PurchaseOrder/Index?id=' + encodeURIComponent(rowId) + '&readOnly=true';
            }, 3000);
        }

        else
        {
           toastr.warning("Unable to save data.");
        }

        return response;

    } catch (err) {

        toastr.error(err.responseJSON?.message || err.responseText || "Error while saving.");

        throw err;
    }
} 

async function getPurchaseOrderModel() {

    const attachments = await getAttachmentDetails();

    const model = {
        //================ HEADER ==================
        VNo: parseIntSafe($('#txtDocNo').val()),
        VType: $('#ddlDocType').val(),
        VDate: toNullableDate($('#dtDocDate').val()),
        DocId: $('#TxtDocId').val(),
        PlaceCode: parseIntSafe($('#ddlPlace').val()),
        WbNo: parseIntSafe($('#ddWBNo').val()),
        PartyCode: parseIntSafe($('#ddlPartyName').val()),
        ShipCode: parseIntSafe($('#ddlShipFrom').val()),
        ShipFrom: parseIntSafe($('#ddlPartyName').val()),
        BillAdd1: $('#TxtAdd1PD').val(),
        BillAdd2: $('#TxtAdd2PD').val(),
        BillAdd3: $('#TxtAdd3PD').val(),
        BillCity: parseIntSafe($('#TxtCity1PD').val()),
        BillPincode: $('#NumPincodePD').val(),
        BillGst: $('#TxtGSTPD').val(),
        ShipAdd1: $('#TxtAdd1SD').val(),
        ShipAdd2: $('#TxtAdd2SD').val(),
        ShipAdd3: $('#TxtAdd3SD').val(),
        ShipCity: parseIntSafe($('#TxtCity1SD').val()),
        ShipPincode: $('#NumPincodeSD').val(),
        PriceType: $('#ddlPriceType').val(),
        PartyRef: $('#txtPartyRef').val(),
        ImportCurrency: $('#ddlCurrency').val(),
        ExRate: parseFloatSafe($('#NumExRate').val()),
        Nos: parseFloatSafe($('#NumTotalNosIt').val()),
        Qty: parseFloatSafe($('#NumQtyIt').val()),
        Amount: parseFloatSafe($('#NumAmountIt').val()),
        PackAmt: parseFloatSafe($('#NumPackingAmtIt').val()),
        DiscAmt: parseFloatSafe($('#NumDiscAmtIt').val()),
        CgstAmt: parseFloatSafe($('#NumCgstAmtIt').val()),
        SgstAmt: parseFloatSafe($('#NumSgstAmtIt').val()),
        IgstAmt: parseFloatSafe($('#NumIgstAmtIt').val()),
        OthAmt: parseFloatSafe($('#TxtOtherAmtSod').val()),
        VatAmt: parseFloatSafe($('#NumVatAmtIt').val()),
        CessAmt: parseFloatSafe($('#NumCessAmtIt').val()),
        TcsAmt: parseFloatSafe($('#NumTCSIt').val()),
        NetAmt: parseFloatSafe($('#NumNetAmtIt').val()),
        DeliveryTerm: $('#txtDeliveryTerm').val(),
        DeliveryDate: toNullableDate($('#DtDeliveryDate').val()),
        ValidityDate: toNullableDate($('#DtValidateDate').val()),
        TransportTerm: $('#txtTransportTerm').val(),
        PaytermCode: parseIntSafe($('#ddlPaymentTerm').val()),
        PaymentTerm: $('#txtPaymentTerm').val(),
        PriceTerm: $('#txtPriceTerm').val(),
        SaudaType: $('#ddSaudaNo').val(),
        SaudaNo: parseIntSafe($('#ddSaudaNo option:selected').text()),
        Remarks: $('#txtRemarksTC').val(),
        PartyName: $('#txtPartySauda').val(),
        SaveOrUpdate: docId ? "Update" : "Save",

        //================ DETAIL ==================
        ItemRecords: [],

        Attachments: attachments
    };

    //========== Collect Table Data ==========
    $('#tblItemRecordPO tbody tr').each(function () {

        const idx = this.id.replace('row', '');

        model.ItemRecords.push({

            SNO: parseIntSafe(idx),
            PlaceCode: parseIntSafe($(`#ddlIplaceofUse${idx}`).val()),
            ItemCode: parseIntSafe($(`#ddlItemname${idx}`).val()),
            ItemName: $(`#ddlItemname${idx} option:selected`).text(),
            MakeCode: parseIntSafe($(`#ddlImake${idx}`).val()),
            NOS: parseIntSafe($(`#TxtNos${idx}`).val()),
            Qty: parseFloatSafe($(`#TxtQty${idx}`).val()),
            AdjQty: parseFloatSafe($(`#txtAdjQtySauda${idx}`).val()),
            UomCode: parseIntSafe($(`#ddlUnit${idx}`).val()),
            UomName: $(`#ddlUnit${idx} option:selected`).text(),
            Rate: parseFloatSafe($(`#TxtRate${idx}`).val()),
            ImportRate: parseFloatSafe($(`#TxtExrate${idx}`).val()),
            CalcRate: parseFloatSafe($(`#TxtCalcRate${idx}`).val()),
            Amount: parseFloatSafe($(`#TxtAmount${idx}`).val()),
            PackPer: parseFloatSafe($(`#TxtPackPercent${idx}`).val()),
            PackAmt: parseFloatSafe($(`#TxtPack${idx}`).val()),
            DiscPer: parseFloatSafe($(`#TxtDiscPercent${idx}`).val()),
            DiscAmt: parseFloatSafe($(`#TxtDisc${idx}`).val()),
            TaxCode: parseIntSafe($(`#ddlTax${idx}`).val()),
            CgstPer: parseFloatSafe($(`#TxtCgstPercent${idx}`).val()),
            CgstAmt: parseFloatSafe($(`#TxtCgst${idx}`).val()),
            SgstPer: parseFloatSafe($(`#TxtSgstPercent${idx}`).val()),
            SgstAmt: parseFloatSafe($(`#TxtSgst${idx}`).val()),
            IgstPer: parseFloatSafe($(`#TxtIgstPercent${idx}`).val()),
            IgstAmt: parseFloatSafe($(`#TxtIgst${idx}`).val()),
            VatPer: parseFloatSafe($(`#TxtVatPercent${idx}`).val()),
            VatAmt: parseFloatSafe($(`#TxtVat${idx}`).val()),
            CessPer: parseFloatSafe($(`#TxtCessPercent${idx}`).val()),
            CessAmt: parseFloatSafe($(`#TxtCess${idx}`).val()),

            OthAmt: parseFloatSafe($(`#TxtOthAmt${idx}`).val()),
            NetAmt: parseFloatSafe($(`#TxtNetAmt${idx}`).val()),
            LandRate: parseFloatSafe($(`#TxtLdRate${idx}`).val()),
            PlaceUse: $(`#ddlIplaceofUse${idx} option:selected`).text(),
            DeptCode: parseIntSafe($(`#ddlDepartment${idx}`).val()),
            DeptName: $(`#ddlDepartment${idx} option:selected`).text(),
            Remarks: $(`#TxtRemarks${idx}`).val(),
            PreorityLevel: parseIntSafe($(`#TxtAppLevel${idx}`).val()),
            PreorityRemarks: $(`#TxtAppRemarks${idx}`).val(),
            RateMonthly: parseFloatSafe($(`#TxtMthRate${idx}`).val()),
            RateQuarterly: parseFloatSafe($(`#TxtQtrRate${idx}`).val()),
            RateAnnualy: parseFloatSafe($(`#TxtAnlRate${idx}`).val()),
            RateSpecial: parseFloatSafe($(`#TxtSpclRate${idx}`).val()),
            RequestType: $(`#TxtReqtype${idx}`).val(),
            RequestNo: parseIntSafe($(`#TxtReqno${idx}`).val()),
            ApprovalType: $(`#TxtApptype${idx}`).val(),
            ApprovalNo: parseIntSafe($(`#TxtAppno${idx}`).val()),
            Status: parseIntSafe($(`#TxtStatus${idx}`).val()),
            SaudaType: $('#ddSaudaNo').val(),
            SaudaNo: parseIntSafe($('#ddSaudaNo option:selected').text())




        });

        //================ ATTACHMENT ==============
        Attachments: attachments


    });

    return model;
}

async function getAttachmentDetails() {

    const attachments = [];

    // Read selected files and convert to Base64
    for (const file of selectedFiles) {

        const base64 = await new Promise((resolve, reject) => {

            const reader = new FileReader();

            reader.onload = () => resolve(reader.result.split(',')[1]); // only Base64 part
            reader.onerror = reject;

            reader.readAsDataURL(file);

        });

        attachments.push({
            FileName: file.name,
            FilePath: `/uploads/${file.name}`,
            FileSize: file.size,
            FileType: file.type,
            FileContentBase64: base64
        });
    }

    // Existing files
    globalAttachments.forEach(file => {

        attachments.push({
            FileName: file.fileName,
            FilePath: file.filePath,
            FileSize: file.fileSize,
            FileType: file.fileType,
            FileContentBase64: file.fileContentBase64 || null
        });

    });

    return attachments;
}
function fileToBase64(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();

        reader.onload = () => {
            // Remove "data:image/png;base64," prefix
            const base64String = reader.result.split(',')[1];
            resolve(base64String);
        };

        reader.onerror = reject;

        reader.readAsDataURL(file);
    });
}

async function collectGridDetail() {
    const items = [];
    $('#tblItemRecordPO tbody tr').each(function () {
        const idx = this.id.replace('row', '');
        const $r = $(this);

        items.push({
            SNO: parseIntSafe(idx),
            ItemName: $r.find(`#ddlItemname${idx} option:selected`).text(),
            ItemCode: parseIntSafe($r.find(`#ddlItemname${idx}`).val()),
            MakeCode: parseIntSafe($r.find(`#ddlImake${idx}`).val()),
            Qty: parseFloatSafe($r.find(`#TxtQty${idx}`).val()),
            UomName: $r.find(`#ddlUnit${idx} option:selected`).text(),
            UomCode: parseIntSafe($r.find(`#ddlUnit${idx}`).val())
        });
    });

    return items;
}

async function GetDocData(MasterTblId, readOnly) {
    try {
        const response = await $.ajax({
            url: '/PurchaseOrder/GetPurchaseOrderRecordsById',
            type: 'GET',
            data: { id: MasterTblId }
        });

        console.log("response", response);

        if (!response || !response.status) {
            toastr.error('No data returned.');
            return;
        }

        if (readOnly === 'true') {
            disableAllFields();
            $('.btn-add-row-last').hide();
            $('#btn-save, #cancelBtn').hide();
        } else {
            enableAllFields();
            $('#btn-save, #cancelBtn').show();
            $('.btn-add-row-last').show();
        }

        Calculation = false;
        SelectShipParty = false;
        SelectParty = false;
        selectItemOption = false

        await fillPurchaseOrderData(response.header, response.detail);       

        Calculation = true;
        SelectShipParty = true;
        SelectParty = true;
        selectItemOption = true;

        $('#fileList').empty();

        const attachments = Array.isArray(response.attachment) ? response.attachment : [];

        if (attachments.length === 0) {
            $('#fileList').html(`
                <div class="text-muted text-center">
                    No attachments found.
                </div>
            `);
        } else {

            attachments.forEach((att, idx) => {

                const fileName =
                    att.FILE_NAME ||
                    att.FileName ||
                    att.fileName ||
                    `File_${idx + 1}`;

                let filePath =
                    att.FILE_Path ||
                    att.FilePath ||
                    att.filePath ||
                    '';

                // safe URL
                if (filePath && !filePath.startsWith('http') && !filePath.startsWith('/')) {
                    filePath = '/' + filePath;
                }

                const ext = (fileName.split('.').pop() || '').toLowerCase();

                let icon = '📄';

                if (['jpg', 'jpeg', 'png', 'gif', 'webp'].includes(ext)) icon = '🖼️';
                else if (ext === 'pdf') icon = '📕';

                $('#fileList').append(`
                    <div class="file-item erp-file-row"
                         data-id="${att.ID || idx}">

                        <div class="file-icon">
                            ${icon}
                        </div>

                        <div class="file-info">
                            <div class="file-name-text">${fileName}</div>
                        </div>

                        <div class="file-actions">

                            ${filePath ? `
                                <button type="button"
                                        class="view-file"
                                        onclick="window.open('${filePath}', '_blank')">
                                    View
                                </button>
                            ` : ''}

                            <button type="button"
                                    class="delete-file"
                                    data-id="${att.ID || idx}">
                                Delete
                            </button>

                        </div>

                    </div>
                `);
            });
        }

    } catch (error) {
        console.error(error);
        toastr.error('Failed to load data.');
    }
}

async function fillPurchaseOrderData(headerData, detailData) {

    // ================= HEADER =================
    if (Array.isArray(headerData) && headerData.length > 0) {

        const d = headerData[0];

        $('#txtDocNo').val(d.V_NO || '');
        $('#TxtDocId').val(d.DOC_ID || '');
        $('#dtDocDate').val((d.V_DATE || '').substring(0, 10));
        $('#DtDeliveryDate').val((d.DELIVERY_DATE || '').substring(0, 10));
        $('#DtValidateDate').val((d.VALIDITY_DATE || '').substring(0, 10));
        $('#ddlDocType').val(d.V_TYPE || '');
        $('#ddlPlace').val(d.PLACE_CODE || '');
        $('#ddWBNo').val(d.WB_NO || '');

        $('#ddlPartyName').val(d.PARTY_CODE || '').trigger('change');
        $('#ddlShipFrom').val(d.SHIP_CODE || '').trigger('change');

        // Billing Address
        $('#TxtAdd1PD').val(d.BILL_ADD1 || '');
        $('#TxtAdd2PD').val(d.BILL_ADD2 || '');
        $('#TxtAdd3PD').val(d.BILL_ADD3 || '');
        $('#TxtCity1PD').val(d.BILL_CITY || '');
        $('#NumPincodePD').val(d.BILL_PINCODE || '');
        $('#TxtGSTPD').val(d.BILL_GST || '');

        // Shipping Address
        $('#TxtAdd1SD').val(d.SHIP_ADD1 || '');
        $('#TxtAdd2SD').val(d.SHIP_ADD2 || '');
        $('#TxtAdd3SD').val(d.SHIP_ADD3 || '');
        $('#TxtCity1SD').val(d.SHIP_CITY || '');
        $('#NumPincodeSD').val(d.SHIP_PINCODE || '');

        // Financials
        $('#NumTotalNosIt').val(d.NOS || '');
        $('#NumQtyIt').val(d.QTY || '');
        $('#NumAmountIt').val(d.AMOUNT || '');
        $('#NumPackingAmtIt').val(d.PACK_AMT || '');
        $('#NumDiscAmtIt').val(d.DISC_AMT || '');
        $('#NumCgstAmtIt').val(d.CGST_AMT || '');
        $('#NumSgstAmtIt').val(d.SGST_AMT || '');
        $('#NumIgstAmtIt').val(d.IGST_AMT || '');
        $('#NumVatAmtIt').val(d.VAT_AMT || '');
        $('#NumCessAmtIt').val(d.CESS_AMT || '');
        $('#NumTCSIt').val(d.TCS_AMT || '');
        $('#TxtOtherAmtSod').val(d.OTH_AMT || '');
        $('#NumNetAmtIt').val(d.NET_AMT || '');

        $('#ddlPriceType').val(d.PRICE_TYPE || '');
        $('#txtPartyRef').val(d.PARTY_REF || '');
        $('#ddlCurrency').val(d.IMPORT_CURRENCY || '');
        $('#NumExRate').val(d.EXRATE || '');

        // Terms
        $('#txtDeliveryTerm').val(d.DELIVERY_TERM || '');
        $('#txtTransportTerm').val(d.TRANSPORT_TERM || '');
        $('#ddlPaymentTerm').val(d.PAYTERM_CODE || '');
        $('#txtPaymentTerm').val(d.PAYMENT_TERM || '');
        $('#txtPriceTerm').val(d.PRICE_TERM || '');
        $('#txtRemarksTC').val(d.REMARKS || '');
        $('#txtPartySauda').val(d.PARTY_NAME || '');
    } else {
        toastr.error("Invalid or empty header data.");
    }

    // ================= DETAIL =================
    console.log("Table Data:", detailData);

    const $tbody = $('#tblItemRecordPO tbody');
    $tbody.empty();

    if (!Array.isArray(detailData) || detailData.length === 0)
        return;

    for (let index = 0; index < detailData.length; index++) {
        addItemRecordRow();
        const item = detailData[index];
        const idx = index + 1;

        await loadItemNameDropdown();
        await loadMakeDropdown(idx, item.ITEM_CODE);

        // Dropdowns
        $(`#ddlItemname${idx}`).val(item.ITEM_CODE).trigger('change'); 
        $(`#ddlImake${idx}`).val(item.MAKE_CODE).trigger('change');
        $(`#ddlUnit${idx}`).val(item.UOM_CODE).trigger('change');
        $(`#ddlIplaceofUse${idx}`).val(item.PLACE_CODE).trigger('change');
        $(`#ddlDepartment${idx}`).val(item.DEPT_CODE).trigger('change');
        $(`#ddlTax${idx}`).val(item.TAX_CODE).trigger('change');
        $(`#TxtStatus${idx}`).val(item.STATUS).trigger('change');

        // Inputs
        $(`#TxtCode${idx}`).val(item.ITEM_CODE);
        $(`#TxtNos${idx}`).val(item.NOS);
        $(`#TxtQty${idx}`).val(item.QTY);
        $(`#TxtRate${idx}`).val(item.RATE);
        $(`#TxtExrate${idx}`).val(item.IMPORT_RATE);
        $(`#TxtCalcRate${idx}`).val(item.CALC_RATE);
        $(`#TxtAmount${idx}`).val(item.AMOUNT);
        $(`#TxtPackPercent${idx}`).val(item.PACK_PER);
        $(`#TxtPack${idx}`).val(item.PACK_AMT);
        $(`#TxtDiscPercent${idx}`).val(item.DISC_PER);
        $(`#TxtDisc${idx}`).val(item.DISC_AMT);
        $(`#TxtCgstPercent${idx}`).val(item.CGST_PER);
        $(`#TxtCgst${idx}`).val(item.CGST_AMT);
        $(`#TxtSgstPercent${idx}`).val(item.SGST_PER);
        $(`#TxtSgst${idx}`).val(item.SGST_AMT);
        $(`#TxtIgstPercent${idx}`).val(item.IGST_PER);
        $(`#TxtIgst${idx}`).val(item.IGST_AMT);
        $(`#TxtVatPercent${idx}`).val(item.VAT_PER);
        $(`#TxtVat${idx}`).val(item.VAT_AMT);
        $(`#TxtCessPercent${idx}`).val(item.CESS_PER);
        $(`#TxtCess${idx}`).val(item.CESS_AMT);
        $(`#TxtTcsPer${idx}`).val(item.TCS_PER);
        $(`#TxtTcsAmt${idx}`).val(item.TCS_AMT);
        $(`#TxtOthPer${idx}`).val(item.OTH_PER);
        $(`#TxtOthAmt${idx}`).val(item.OTH_AMT);
        $(`#TxtOthPer2${idx}`).val(item.TOTAL_PER2);
        $(`#TxtOthAmt2${idx}`).val(item.TOTAL_AMT2);
        $(`#TxtNetAmt${idx}`).val(item.NET_AMT);
        $(`#TxtLdRate${idx}`).val(item.LAND_RATE);
        $(`#TxtRemarks${idx}`).val(item.REMARKS);
        $(`#TxtAppLevel${idx}`).val(item.PREORITY_LEVEL);
        $(`#TxtAppRemarks${idx}`).val(item.PREORITY_REMARKS);
        $(`#TxtMthRate${idx}`).val(item.RATE_MONTHLY);
        $(`#TxtQtrRate${idx}`).val(item.RATE_QUARTERLY);
        $(`#TxtAnlRate${idx}`).val(item.RATE_ANNUALY);
        $(`#TxtSpclRate${idx}`).val(item.RATE_SPECIAL);
        $(`#TxtSaudatype${idx}`).val(item.SAUDA_TYPE ?? '');
        $(`#TxtSaudano${idx}`).val(item.SAUDA_NO ?? '');
        $(`#TxtReqtype${idx}`).val(item.REQUEST_TYPE ?? '');
        $(`#TxtReqno${idx}`).val(item.REQUEST_NO ?? '');
        $(`#TxtApptype${idx}`).val(item.APPROVAL_TYPE ?? '');
        $(`#TxtAppno${idx}`).val(item.APPROVAL_NO ?? '');
    }
}
function addItemRecordRow() {
    let tbody = $('#tblItemRecordPO tbody');
    let rowCount = tbody.find('tr').length + 1;

    let newRow = `
        <tr class="no-border-input" id="row${rowCount}"> <td class="d-None"><input class="form-control" id="TxtCode${rowCount}" /></td>         
        <td class="freeze-item">

        <select style="min-width:500px;" class="form-control" id="ddlItemname${rowCount}">
         <option value="">-Select Item Name-</option> ${itemNameOptions}
        </select>

        </td>
            <td>
                <select style="min-width: 100px; max-width: 200px;" class="form-control" id="ddlImake${rowCount}">
                    <option value="">-select Make-</option> ${MakeNameOptions}
                </select>
            </td>
            <td>
                <select style="min-width: 100px; max-width: 200px;" class="form-control" id="ddlUnit${rowCount}" >
                    <option value="">-select Unit-</option> ${UnitOptions}
                </select>
            </td>
            <td>
                <select style="min-width: 100px; max-width: 200px;" class="form-control" id="ddlIplaceofUse${rowCount}">
                    <option value="">-select Place of Use-</option>${PlaceOptions}
                </select>
            </td>

            <td>
                <select style="min-width: 100px; max-width: 200px;" class="erppagetable-control" id="ddlDepartment${rowCount}">
                    <option value="">-select department-</option>${DepartmentOptions}
                </select>
            </td>

            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control" oninput="allowOnlyNumbers(this)" id="TxtNos${rowCount}"     maxlength="15"  /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control" oninput="allowOnlyNumbers(this)" id="TxtQty${rowCount}" /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control" oninput="allowOnlyNumbers(this)" id="TxtRate${rowCount}" /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control" oninput="allowOnlyNumbers(this)" id="TxtExrate${rowCount}" /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control" readonly id="TxtAmount${rowCount}" /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control" oninput="allowOnlyNumbers(this)" id="TxtPackPercent${rowCount}" /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control" oninput="allowOnlyNumbers(this)" id="TxtPack${rowCount}" /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control" oninput="allowOnlyNumbers(this)" id="TxtDiscPercent${rowCount}" /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control" oninput="allowOnlyNumbers(this)" id="TxtDisc${rowCount}" /></td>

            <td>
                <select style="min-width: 100px; max-width: 200px;" class="erppagetable-control" id="ddlTax${rowCount}">
                    <option value="">-select Tax Type-</option>${TaxTypeOptions}
                </select>
            </td>

             <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control"   id="TxtCgstPercent${rowCount}" readonly/></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control"   id="TxtCgst${rowCount}" readonly /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control"   id="TxtSgstPercent${rowCount}" readonly/></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control"   id="TxtSgst${rowCount}" readonly /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control"   id="TxtIgstPercent${rowCount}" readonly/></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control"   id="TxtIgst${rowCount}" readonly /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control"  id="TxtVatPercent${rowCount}" readonly/></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control" id="TxtVat${rowCount}" readonly /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control"  oninput="allowOnlyNumbers(this)" id="TxtCessPercent${rowCount}" /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control" oninput="allowOnlyNumbers(this)" id="TxtCess${rowCount}"  /></td>
            <td class="d-none"><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control"   id="TxtTcsPer${rowCount}" readonly/></td>
            <td class="d-none"><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control"   id="TxtTcsAmt${rowCount}" readonly/></td>
            <td class="d-none"><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control"   id="TxtOthPer${rowCount}" readonly/></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control"   id="TxtOthAmt${rowCount}" readonly/></td>
            <td class="d-none"><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control"   id="TxtOthPer2${rowCount}" readonly/></td>
            <td class="d-none"><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control"   id="TxtOthAmt2${rowCount}" readonly/></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control"  id="TxtNetAmt${rowCount}" readonly /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control"   id="TxtLdRate${rowCount}" /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control" id="TxtRemarks${rowCount}" /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control" oninput="allowOnlyNumbers(this)" id="TxtAppLevel${rowCount}" /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control" id="TxtAppRemarks${rowCount}" /></td>

            <td>
                <select style="min-width: 100px; max-width: 200px;" class="erppagetable-control" id="TxtStatus${rowCount}">
                 <option value="">-select Staus-</option>${statuslist}
                </select>
            </td>

         
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control" id="TxtReqtype${rowCount}" readonly  /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control" id="TxtReqno${rowCount}" readonly  /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control" id="TxtApptype${rowCount}"  /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control" id="TxtAppno${rowCount}"  /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control" id="TxtSaudatype${rowCount}"  readonly /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control" id="TxtSaudano${rowCount}"  readonly /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control" id="TxtMthRate${rowCount}"  /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control" id="TxtQtrRate${rowCount}"  /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control" id="TxtAnlRate${rowCount}"  /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control" id="TxtSpclRate${rowCount}"  /></td>
            <td><input style="min-width: 100px; max-width: 200px;" class="erppagetable-control" id="TxtCalcRate${rowCount}"  /></td>
            <td class="action-col">
                <div class="action-wrap">
                    <button class="act-btn add btn-add-action btn-Itemadd-action" title="Add Row" style="cursor:pointer;"><i class="fa fa-plus-circle"></i></button>
                    <button class="act-btn delete btn-delete-action btn-Itemdelete-action" title="Delete Row" style="cursor:pointer;"><i class="fa fa-trash"></i></button>
                </div>
            </td>
        </tr>
        `;
    tbody.append(newRow);

    setEnterKeyFocusOnTable(itemRecords, rowCount);
}
function setEnterKeyFocusOnTable(sequence, rowCount) {
    sequence.forEach((id, index) => {
        let elementId = `#${id}${rowCount}`;
        $(document).on('keydown', elementId, function (e) {
            if (e.key === 'Enter' || e.key === 'Tab' || e.keyCode === 13 || e.keyCode === 9) {
                e.preventDefault();

                let nextIndex = index + 1;
                if (nextIndex < sequence.length) {
                    let nextElementId = `#${sequence[nextIndex]}${rowCount}`;
                    if ($(nextElementId).length) {
                        $(nextElementId).focus();
                    }
                } else {

                    addItemRecordRow();
                    setEnterKeyFocus(sequence, rowCount + 1);
                    $(`#${sequence[0]}${rowCount + 1}`).focus();
                }
            }
        });
    });
}
function getQueryParam(param) {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get(param);
}
function handleBack(redirectUrl, isReadOnly = false) {

    if (isReadOnly) {
        window.location.href = redirectUrl;
        return;
    }

    Swal.fire({
        title: 'Are you sure?',
        text: "Unsaved data will be lost.",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Yes, exit',
        cancelButtonText: 'Stay',
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33'
    }).then((result) => {
        if (result.isConfirmed) {
            window.location.href = redirectUrl;
        }
    });
}
function isDuplicateFile(file) {

    const fileName = file.name;

    // 1. check in newly selected files
    const inNew = selectedFiles.some(f =>
        f.name === file.name &&
        f.size === file.size &&
        f.lastModified === file.lastModified
    );

    // 2. check in existing DB attachments
    const inExisting = globalAttachments.some(att => {
        const name = att.FileName ?? att.fileName;
        return name.toLowerCase() === file.name.toLowerCase();
    });

    if (inNew || inExisting) {
        toastr.warning(`File "${fileName}" already exists in attachment list.`);
        return true;
    }

    return false;
}
function renderFileList() {

    const attachBody = $("#fileList");
    attachBody.empty();

    // ===== EXISTING ATTACHMENTS =====
    if (globalAttachments && globalAttachments.length > 0) {

        globalAttachments.forEach((att, index) => {

            const fileName = att.FileName ?? att.fileName;
            const filePath = att.FilePath ?? att.filePath;

            const extension = fileName.split('.').pop().toLowerCase();

            let previewHtml = "";

            if (["png", "jpg", "jpeg", "gif", "webp", "bmp"].includes(extension)) {
                previewHtml = `<img src="${filePath}" class="erp-file-thumb">`;
            }
            else if (extension === "pdf") {
                previewHtml = `<i class="fa fa-file-pdf-o erp-file-icon text-danger"></i>`;
            }
            else {
                previewHtml = `<i class="fa fa-file-o erp-file-icon"></i>`;
            }

            attachBody.append(`
                    <div class="file-item erp-file-row">

                        <!-- LEFT: ICON / IMAGE -->
                        <div class="erp-file-preview">
                            ${previewHtml}
                        </div>

                        <!-- MIDDLE: NAME -->
                        <div class="erp-file-info">
                            <div class="erp-file-name">${fileName}</div>
                        </div>

                        <!-- RIGHT: ACTIONS -->
                        <div class="erp-file-actions">
                            <a href="${filePath}" target="_blank" class="erp-view-btn">
                                View
                            </a>

                            <button type="button"
                                    class="erp-delete-db-btn"
                                    data-index="${index}">
                                Delete
                            </button>
                        </div>

                    </div>
                `);
        });
    }

    // ===== NEWLY SELECTED FILES =====
    selectedFiles.forEach((file, index) => {

        const extension = file.name.split('.').pop().toLowerCase();
        const fileUrl = URL.createObjectURL(file);

        let previewHtml = "";

        if (["png", "jpg", "jpeg", "gif", "webp", "bmp"].includes(extension)) {
            previewHtml = `<img src="${fileUrl}" class="erp-file-thumb">`;
        }
        else if (extension === "pdf") {
            previewHtml = `<i class="fa fa-file-pdf-o erp-file-icon text-danger"></i>`;
        }
        else {
            previewHtml = `<i class="fa fa-file-o erp-file-icon"></i>`;
        }

        attachBody.append(`
                <div class="file-item erp-file-row">

                    <!-- LEFT: ICON / IMAGE -->
                    <div class="erp-file-preview">
                        ${previewHtml}
                    </div>

                    <!-- MIDDLE: NAME -->
                    <div class="erp-file-info">
                        <div class="erp-file-name">${file.name}</div>
                    </div>

                    <!-- RIGHT: ACTIONS -->
                    <div class="erp-file-actions">
                        <a href="${fileUrl}" target="_blank" class="erp-view-btn">
                            View
                        </a>

                        <button type="button"
                                class="erp-delete-file-btn"
                                data-index="${index}">
                            Delete
                        </button>
                    </div>

                </div>
            `);
    });

    if (globalAttachments.length === 0 && selectedFiles.length === 0) {
        attachBody.html(`
                <div class="erp-empty-state text-center text-muted">
                    No attachments found.
                </div>
            `);
    }
}
function addAttachmentRow(data = {}) {
    const $list = $('#fileList');

    const filePath = data.FILE_Path ? resolveFileUrl(data.FILE_Path) : '';
    const fileName = data.FILE_NAME || '';

    // safer extension logic (use filename, not path)
    const ext = (fileName.split('.').pop() || '').toLowerCase();

    let icon = '📄';

    if (['jpg', 'jpeg', 'png', 'gif', 'webp'].includes(ext)) icon = '🖼️';
    else if (ext === 'pdf') icon = '📕';

    const item = $(`
        <div class="file-item">

            <div class="file-icon">
                ${icon}
            </div>

            <div class="file-info">

                <input type="file" class="file-input" />

                <input type="hidden" class="existing-file-path" value="${filePath}" />
                <input type="hidden" class="existing-file-name" value="${fileName}" />

                <div class="file-name-text">
                    ${fileName || 'No file selected'}
                </div>

                ${filePath ? `
                    <a href="${filePath}" target="_blank" class="file-link">
                        Download
                    </a>
                ` : ''}

            </div>

            <div class="file-actions">
                <i class="fa fa-plus add-file"></i>
                <i class="fa fa-trash delete-file"></i>
            </div>

        </div>
    `);

    $list.append(item);

    const $fileInput = item.find('.file-input');
    const $fileNameText = item.find('.file-name-text');
    const $link = item.find('.file-link');
    const $icon = item.find('.file-icon');

    // If existing file → hide input
    if (filePath) {
        $fileInput.hide();
    }

    // =========================
    // FILE CHANGE HANDLER
    // =========================
    $fileInput.on('change', function () {
        const file = this.files[0];
        if (!file) return;

        const url = URL.createObjectURL(file);

        const newExt = (file.name.split('.').pop() || '').toLowerCase();

        let newIcon = '📄';

        if (['jpg', 'jpeg', 'png', 'gif', 'webp'].includes(newExt)) newIcon = '🖼️';
        else if (newExt === 'pdf') newIcon = '📕';

        $fileNameText.text(file.name);

        if ($link.length) {
            $link.attr('href', url);
        } else {
            $fileNameText.after(`
                <a href="${url}" target="_blank" class="file-link">
                    Preview
                </a>
            `);
        }

        $icon.text(newIcon);
    });

    // =========================
    // DELETE ROW
    // =========================
    item.find('.delete-file').on('click', function () {
        item.remove();
    });
}

async function GetDatabbyPartycode() {
    try {

        let PartyCode = $('#ddlPartyName').val();
        let v_type = $('#ddlDocType').val();
        let v_no = $('#ddSaudaNo option:selected').text();
        const docId = getQueryParam('id');

        const data = await $.ajax({
            url: '/PurchaseOrder/GetDataByPartyCode',
            method: 'GET',
            data: { PartyCode: PartyCode, v_type: v_type, v_no: v_no }
        });

        $('#TxtAdd1PD').val(data.partydetails.adD1);
        $('#TxtAdd2PD').val(data.partydetails.adD2);
        $('#TxtAdd3PD').val(data.partydetails.adD3);
        $('#NumPincodePD').val(data.partydetails.pincode);
        $('#TxtCity1PD').val(data.partydetails.citY_CODE);
        $('#TxtGSTPD').val(data.partydetails.gstin);
             
        if (!docId) {           

            $('#ddlShipFrom').val(data.partydetails.code).trigger('change');
            $('#TxtAdd1SD').val(data.partydetails.adD1);
            $('#TxtAdd2SD').val(data.partydetails.adD2);
            $('#TxtAdd3SD').val(data.partydetails.adD3);
            $('#NumPincodeSD').val(data.partydetails.pincode);
            $('#TxtCity1SD').val(data.partydetails.citY_CODE);
            $('#TxtGSTSD').val(data.partydetails.gstin);
        }

        if (v_type == "RORD" && v_no != "") {

                $('#txtPartySauda').val(data.saudaDetails.p_Name);
                $('#txtItemNameSauda').val(data.saudaDetails.shortName);
                $('#txtQuantitySauda').val(data.saudaDetails.qty);
                $('#NumRateSauda').val(data.saudaDetails.rate);
                $('#txtPriceSauda').val(data.saudaDetails.frT_TERM);
                $('#txtAdjQtySauda').val(data.saudaDetails.qty);
                $('#TxtRemarksSauda').val(data.saudaDetails.remark);
        }
         
        if (data.partyCountry === "Import") {

            $('#ddlCurrency').prop('disabled', true);
            $('#NumExRate').prop('readonly', true);
        } else
        {
            $('#ddlCurrency').prop('disabled', false);
            $('#NumExRate').prop('readonly', false);
        }
    } catch (error) {
        console.error("Error loading Place:", error);
    }
}
function Wb_SaudaDdl_Make_enabledisable(VType) {
    if (VType == "RORD") {
        document.getElementById("SaudaDetail").style.display = "block";
    }
    else {
        document.getElementById("SaudaDetail").style.display = "none";
    }
}

async function LoadOrdersModal() {
    let V_NO = $('#ddSaudaNo option:selected').text();
    const response = await $.ajax({
        url: '/PurchaseOrder/GetDataByOrder',
        method: 'GET',
        data: { V_NO: V_NO }
    });

    const tbody = $("#tblPOMaster tbody");
    tbody.empty();

    if (response.success && response.data.length > 0) {
        response.data.forEach(item => {
            tbody.append(`
                <tr>
                    <td>${item.orderNo}</td>
                    <td>${item.party}</td>
                    <td>${item.itemName}</td>
                    <td>${item.quantity}</td>
                    <td>${item.rate}</td>
                </tr>
            `);
        });
        const modal = new bootstrap.Modal(document.getElementById("ordersModal"));
        modal.show();
    }
    else
    {
        toastr.info(`Data Not Found For This Sauda No ${V_NO}`);
    }
}

async function LoadDatabyShipCode(PartyCode) {
    try {
        const data = await $.ajax({
            url: '/PurchaseOrder/GetDataByShipPartyCode',
            method: 'GET',
            data: { PartyCode: PartyCode}
        });          
        $('#TxtAdd1SD').val(data.partydetails.adD1);
        $('#TxtAdd2SD').val(data.partydetails.adD2);
        $('#TxtAdd3SD').val(data.partydetails.adD3);
        $('#NumPincodeSD').val(data.partydetails.pincode);
        $('#TxtCity1SD').val(data.partydetails.citY_CODE);
        $('#TxtGSTSD').val(data.partydetails.gstin);
    }    
    
    catch (error)
    {
    console.error("Error loading Place:", error);
    }

}

async function GetPartyAddressDetails(PartyCode, AddressCode) {
    try {
        const data = await $.ajax({
            url: '/PurchaseOrder/GetDataByPartyAddressID',
            method: 'GET',
            data: {
                PartyCode: PartyCode,
                AddressCode: AddressCode
            }
        });

        $('#TxtAdd1PD').val(data.partyAddress.add1);
        $('#TxtAdd2PD').val(data.partyAddress.add2);
        $('#TxtAdd3PD').val(data.partyAddress.add3);
        $('#NumPincodePD').val(data.partyAddress.pincode);
        $('#TxtCity1PD').val(data.partyAddress.city_Code);
        $('#TxtGSTPD').val(data.partyAddress.gstin);

    }
    catch (xhr) {
        console.error("Status:", xhr.status);
        console.error("Response:", xhr.responseJSON || xhr.responseText);
    }
}

async function GetShipPartyAddressDetails(PartyCode, AddressCode) {
    try {
        const data = await $.ajax({
            url: '/PurchaseOrder/GetDataByPartyAddressID',
            method: 'GET',
            data: {
                PartyCode: PartyCode,
                AddressCode: AddressCode
            }
        });

        $('#TxtAdd1SD').val(data.partyAddress.add1);
        $('#TxtAdd2SD').val(data.partyAddress.add2);
        $('#TxtAdd3SD').val(data.partyAddress.add3);
        $('#NumPincodeSD').val(data.partyAddress.pincode);
        $('#TxtCity1SD').val(data.partyAddress.city_Code);
        $('#TxtGSTSD').val(data.partyAddress.gstin);
    }
    catch (xhr) {
        console.error("Status:", xhr.status);
        console.error("Response:", xhr.responseJSON || xhr.responseText);
    }
}

async function loadPurchaseHistory() {

    try {
        const V_NO = $('#txtDocNo').val()?.trim();
        const V_date = $('#dtDocDate').val()?.trim();

        if (!V_NO) {
            toastr.info("Please enter document number");
            return;
        }

        const res = await $.ajax({
            url: '/PurchaseSaudaList/GetDataByPurchaseHistory',
            method: 'GET',
            data: { V_NO: V_NO }
        });

        console.log("Full response:", res);

        if (res.success) {

            const data = res.data || [];

            if (!data || data.length === 0) {
                toastr.info("Purchase History Not Found For this Doc No = " + V_NO + " and Doc Date " + V_date);
                return;
            }
            renderPurchaseHistory(data);
        }

    } catch (err) {
        console.error("AJAX Error:", err);
        toastr.info("Something went wrong while fetching data");
    }
}
function renderPurchaseHistory(data) {

    const tbody = $('#tblshowpurchasehistoryList tbody');
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
                    <td>${item.doc_id ?? ''}</td>
                    <td>${item.vdate ?? ''}</td>
                    <td>${item.qty ?? ''}</td>
                    <td>${item.party_name ?? ''}</td>
                </tr>
            `);
        });
    }

    const modalElement = document.getElementById('showpurchasehistoryModal');
    const myModal = new bootstrap.Modal(modalElement);
    myModal.show();
}

async function fillItemDetailsTableBySaudaNo(data) {
    const $tbody = $('#tblItemRecordPO tbody');
    $tbody.empty();
    console.log(data);

    for (let index = 0; index < data.length; index++) {
        const item = data[index];
        const idx = index + 1;

        console.log("Item data", data);

        addItemRecordRow();

        $(`#ddlIplaceofUse${idx}`).val(item.placeUse || '');
        $(`#ddlItemname${idx}`).val(item.itemCode).trigger('change.select2');
        $(`#ddlImake${idx}`).val(item.makeCode).trigger('change.select2');
        $(`#ddlUnit${idx}`).val(item.uomCode || '').trigger('change');

        // $(`#ddlTax${idx}`).val(item.taxCode || '').trigger('change');

        $(`#ddlTax${idx}`).val(item.taxCode || '');

        $(`#ddlDepartment${idx}`).val(item.deptCode || '');
        $(`#TxtNos${idx}`).val(item.nos || '');
        $(`#TxtQty${idx}`).val(item.qty || '');
        $(`#txtAdjQtySauda${idx}`).val(item.adjQty || '');
        $(`#TxtRate${idx}`).val(item.rate || '');
        $(`#TxtExrate${idx}`).val(item.importRate || '');
        $(`#TxtCalcRate${idx}`).val(item.calcRate || '');
        $(`#TxtAmount${idx}`).val(item.amount || '');
        $(`#TxtPackPercent${idx}`).val(item.packPer || '');
        $(`#TxtPack${idx}`).val(item.packAmt || '');
        $(`#TxtDiscPercent${idx}`).val(item.discPer || '');
        $(`#TxtDisc${idx}`).val(item.discAmt || '');
        $(`#TxtCgstPercent${idx}`).val(item.cgstPer || '');
        $(`#TxtCgst${idx}`).val(item.cgstAmt || '');
        $(`#TxtSgstPercent${idx}`).val(item.sgstPer || '');
        $(`#TxtSgst${idx}`).val(item.sgstAmt || '');
        $(`#TxtIgstPercent${idx}`).val(item.igstPer || '');
        $(`#TxtIgst${idx}`).val(item.igstAmt || '');
        $(`#TxtVatPercent${idx}`).val(item.vatPer || '');
        $(`#TxtVat${idx}`).val(item.vatAmt || '');
        $(`#TxtCessPercent${idx}`).val(item.cessPer || '');
        $(`#TxtCess${idx}`).val(item.cessAmt || '');
        $(`#TxtTcsPer${idx}`).val(item.tcsPer || '');
        $(`#TxtTcsAmt${idx}`).val(item.tcsAmt || '');
        $(`#TxtOthPer${idx}`).val(item.othPer || '');
        $(`#TxtOthAmt${idx}`).val(item.othAmt || '');
        $(`#TxtOthPer2${idx}`).val(item.othPer2 || '');
        $(`#TxtOthAmt2${idx}`).val(item.othAmt2 || '');
        $(`#TxtNetAmt${idx}`).val(item.netAmt || '');
        $(`#TxtLdRate${idx}`).val(item.landRate || '');
        $(`#TxtRemarks${idx}`).val(item.remarks || '');
        $(`#TxtAppLevel${idx}`).val(item.preorityLevel || '');
        $(`#TxtAppRemarks${idx}`).val(item.preorityRemarks || '');
        $(`#TxtMthRate${idx}`).val(item.rateMonthly || '');
        $(`#TxtQtrRate${idx}`).val(item.rateQuarterly || '');
        $(`#TxtAnlRate${idx}`).val(item.rateAnnualy || '');
        $(`#TxtSpclRate${idx}`).val(item.rateSpecial || '');
    }
}

async function fetchDatabyTaxType(TaxCode) {
    try {
        const res = await $.ajax({
            url: '/PurchaseOrder/GetDataByTaxType',
            method: 'GET',
            data: { TaxCode: TaxCode }
        });
        console.log("fetchDatabyTaxType res:", res);
        return res;
    }
    catch (error) {
        console.error("AJAX Error:", error);
        return null;
    }
}


function getPurchaseData() {
    let poData = [];

    $('#tblPurchaseRequest tbody tr').each(function () {
        const $checkbox = $(this).find('input[type="checkbox"]');

        if ($checkbox.is(':checked')) {
            const row = $(this).find('td');

            const item = {
                ITEM_CODE: $(row[4]).text().trim(),
                ItemName: $(row[5]).text().trim(),
                Unit: $(row[6]).text().trim(),
                makename: $(row[7]).text().trim(),
                TECH_DESC: $(row[8]).text().trim(),
                QTY: $(row[9]).text().trim(),
                UOM_CODE: $(row[18]).text().trim(),
                MAKE_CODE: $(row[15]).text().trim(),
                APROV_REMARKS: $(row[11]).text().trim(),
                STATUS: $(row[12]).text().trim(),
                Department: $(row[13]).text().trim()
                // Add other fields if needed
            };

            poData.push(item);
        }
    });

    return poData;
}


function SetFYDate(inputId, loginDate) {
    var $input = $('#' + inputId);
    var d = new Date(loginDate);
    var fyStartYear = d.getMonth() >= 3 ? d.getFullYear() : d.getFullYear() - 1;
    var minDate = fyStartYear + '-04-01';
    var maxDate = loginDate;
    $input.attr('min', minDate).attr('max', maxDate).val(maxDate);

    $input.on('change', function () {
        var selectedDate = new Date(this.value);
        var min = new Date(minDate);
        var max = new Date(maxDate);

        if (selectedDate < min || selectedDate > max) {
            toastr.info('Please select a date within the Financial Year and not greater than Login Date.');
            this.value = maxDate;
        }
    });
}




