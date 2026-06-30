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

    calculateAllTotals(); // Recalculate footer totals
}


async function handleDocLoad() {
    const docId = getQueryParam('id');
    const readOnly = getQueryParam('readOnly');
    console.log("LoginDate", LoginDate);
    SetFYDate('dtDocDate', LoginDate);
    if (docId) {

        await GetDocData(docId, readOnly);

        $('#SaudaDetail').show();

        $('#ddlDocType').prop('disabled', true);
        $('#ddlStatus').prop('disabled', false);
        var VTpeU = docId.substring(0, 4);
        var VNoU = docId.substring(4);
        $('#txtDocNo').val(VNoU);
        console.log(VTpeU, VNoU);
        Wb_SaudaDdl_Make_enabledisable(VTpeU);

    } else {
        $('#SaudaDetail').hide();
        $('#ddlStatus').prop('disabled', true);
        const Vtype = $('#ddlDocType').val();
        Wb_SaudaDdl_Make_enabledisable(Vtype);
        GetDocid(Vtype);

        const today = new Date();
        const todayDate = today.getFullYear() + '-' + (today.getMonth() + 1).toString().padStart(2, '0') + '-' + today.getDate().toString().padStart(2, '0');
        // $('#dtDocDate').val(todayDate);
        $('#DtDeliveryDate').val(todayDate);
        $('#DtValidateDate').val(todayDate);
    }
}


function TransitReport() {

    if (!rowId) {
        showToast(`Please save the data before printing the report.`, { type: "info" });
        return;
    }

    var reportName = "";

    if (globalVars.CompCode == "7") {
        reportName = "pr_porderK_gst";
    }
    else {
        reportName = "pr_porder_gst";
    }

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



async function SaveData(saveDt) {

    try {

        console.log("Request Payload:", saveDt);

        const response = await $.ajax({
            url: '/PurchaseOrder/SaveOrUpdatePurchaseOrder',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(saveDt),
            dataType: 'json'
        });

        const v = response;

        if (v?.status == "success") {

            toastr.success("Data Insert Successfully");

            setTimeout(() => {
                window.location.href = '/PurchaseOrderList/Index';
            }, 500);

        }

        else if (v?.status == false)
        {
            toastr.warning(v.message);
        }
        else {
            toastr.error(v?.message || "Save failed");
        }

    } catch (error) {

        console.log("===== AJAX ERROR START =====");
        console.log("Error Object:", error);
        console.log("Response Text:", error?.responseText);
        console.log("Status:", error?.status);
        console.log("===== AJAX ERROR END =====");

        toastr.error(
            error?.responseText ||
            "Error occurred while saving. Please contact admin."
        );
    }
}



async function collectFormData() {
    const id = toNullableString(docId);
    const itemRecords = await collectOrder2Items();
    const attachment = await getAttachmentDetails();

    console.log("id", docId);


    const docid = toNullableString(document.getElementById("TxtDocId")?.value);

    console.log("docid", docid);

    return {
        VNo: parseIntSafe(document.getElementById("txtDocNo")?.value),
        VType: toNullableString(document.getElementById("ddlDocType")?.value),
        VDate: toNullableDate(document.getElementById("dtDocDate")?.value),
        DeliveryDate: toNullableDate(document.getElementById("DtDeliveryDate")?.value),
        ValidityDate: toNullableDate(document.getElementById("DtValidateDate")?.value),
        DocId: toNullableString(document.getElementById("TxtDocId")?.value),
        PlaceCode: parseIntSafe(document.getElementById("ddlPlace")?.value),
        WbType: null,
        WbNo: parseIntSafe(document.getElementById("ddWBNo")?.value),
        PartyCode: parseIntSafe(document.getElementById("ddlPartyName")?.value),
        ShipCode: parseIntSafe(document.getElementById("ddlShipFrom")?.value),
        ShipFrom: parseIntSafe(document.getElementById("ddlPartyName")?.value),
        BillAdd1: toNullableString(document.getElementById("TxtAdd1PD")?.value),
        BillAdd2: toNullableString(document.getElementById("TxtAdd2PD")?.value),
        BillAdd3: toNullableString(document.getElementById("TxtAdd3PD")?.value),
        BillCity: parseIntSafe(document.getElementById("TxtCity1PD")?.value),
        BillPincode: toNullableString(document.getElementById("NumPincodePD")?.value),
        BillGst: toNullableString(document.getElementById("TxtGSTPD")?.value),
        ShipAdd1: toNullableString(document.getElementById("TxtAdd1SD")?.value),
        ShipAdd2: toNullableString(document.getElementById("TxtAdd2SD")?.value),
        ShipAdd3: toNullableString(document.getElementById("TxtAdd3SD")?.value),
        ShipCity: parseIntSafe(document.getElementById("TxtCity1SD")?.value),
        ShipPincode: toNullableString(document.getElementById("NumPincodeSD")?.value),
        ShipGst: null,
        PriceType: toNullableString(document.getElementById("ddlPriceType")?.value),
        PartyRef: toNullableString(document.getElementById("txtPartyRef")?.value),
        ImportCurrency: toNullableString(document.getElementById("ddlCurrency")?.value),
        ExRate: parseFloatSafe(document.getElementById("NumExRate")?.value),
        Nos: parseFloatSafe(document.getElementById("NumTotalNosIt")?.value),
        Qty: parseFloatSafe(document.getElementById("NumQtyIt")?.value),
        Amount: parseFloatSafe(document.getElementById("NumAmountIt")?.value),
        PackAmt: parseFloatSafe(document.getElementById("NumPackingAmtIt")?.value),
        DiscAmt: parseFloatSafe(document.getElementById("NumDiscAmtIt")?.value),
        CgstAmt: parseFloatSafe(document.getElementById("NumCgstAmtIt")?.value),
        SgstAmt: parseFloatSafe(document.getElementById("NumSgstAmtIt")?.value),
        IgstAmt: parseFloatSafe(document.getElementById("NumIgstAmtIt")?.value),
        OthAmt: parseFloatSafe(document.getElementById("TxtOtherAmtSod")?.value),
        VatAmt: parseFloatSafe(document.getElementById("NumVatAmtIt")?.value),
        CessPer: null,
        CessAmt: parseFloatSafe(document.getElementById("NumCessAmtIt")?.value),
        TcsPer: null,
        TcsAmt: parseFloatSafe(document.getElementById("NumTCSIt")?.value),
        NetAmt: parseFloatSafe(document.getElementById("NumNetAmtIt")?.value),
        DeliveryTerm: toNullableString(document.getElementById("txtDeliveryTerm")?.value),
        TransportTerm: toNullableString(document.getElementById("txtTransportTerm")?.value),
        PaytermCode: parseIntSafe(document.getElementById("ddlPaymentTerm")?.value),
        PaymentTerm: toNullableString(document.getElementById("txtPaymentTerm")?.value),
        PriceTerm: toNullableString(document.getElementById("txtPriceTerm")?.value),
        SaudaType: null,
        SaudaNo: null,
        DeliveryPeriod: null,
        DeliveryTo: null,
        Remarks: toNullableString(document.getElementById("txtRemarksTC")?.value),
        PoType: null,
        FAProvStatus: null,
        FAProvRemarks: null,
        MailSend: null,
        CDiscAmt: null,
        AutoGenPo: null,
        PoAcceptFlg: null,
        PoAttachPath: null,
        PoAttachDate: null,
        TaxCode: null,
        ItemType: null,
        SupplyType: null,
        TranType: null,
        FormCode: null,
        VehicleNo: null,
        InvType: null,
        InvNo: null,
        PartyName: toNullableString(document.getElementById("txtPartySauda")?.value),
        ShipName: null,
        Status: null,

        SaveOrUpdate: (!docid || docid === "") ? 'Save' : 'Update',
        ItemRecords: itemRecords,
        Attachments: attachment
    };
}



