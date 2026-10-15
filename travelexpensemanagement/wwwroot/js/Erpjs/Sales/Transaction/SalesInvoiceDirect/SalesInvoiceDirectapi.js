
async function LoadDropdown()
{
    try {
        await Promise.all([
            cmbV_type(),
            DDlFormType(),
            DDlMode(), 
            DDlLoadParty(),
            DDlWBParty(),
            DDlDoNo(),
            ddlPartyName(),
            cmbCityName(),
            cmbSalesThrough(),
            cmbTaxType(),
            cmbPurchaseNo(),
            cmbPaymentTerm(),
            cmbProductName(),
            cmbTransportName(),
            DDlProdType(),
            cmbDocStatus()
        ]);
    } catch (error)
    {
        console.log("Dropdown load failed:", error);
        toastr.error("Failed to load dropdown data");
    }
}

async function cmbV_type() {
    try {
        const res = await fetch('/SalesInvoiceDirect/DDlVType');
        const data = await res.json();
        const ddl = $('#ddlDocumentType');
        // ddl.empty().append('<option value="">---Select Party Name---</option>');
        ddl.empty();
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Doc Type:", error);
    }
}

async function DDlFormType()
{
    try {
        const res = await fetch('/SalesInvoiceDirect/DDlFormType');
        const data = await res.json();
        const ddl = $('#ddlFormType');
        ddl.empty().append('<option value="">---Select Form Type---</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Form Type:", error);
    }
}

async function DDlProdType()
{
    try {
        const res = await fetch('/SalesInvoiceDirect/DDlProdType');
        const data = await res.json();
        const ddl = $('#ddlProductType');
        ddl.empty().append('<option value="">---Select Prod Type---</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error)
    {
        console.error("Error loading Prod Type:", error);
    }
}

async function DDlMode()
{
    try {
        const res = await fetch('/SalesInvoiceDirect/DDlMode');
        const data = await res.json();
        const ddl = $('#ddlMode');
        ddl.empty().append('<option value="">---Select Mode Type---</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Mode Type:", error);
    }
}

async function DDlLoadParty()
{
    try {
        const res = await fetch('/SalesInvoiceDirect/DDlLoadParty');
        const data = await res.json();
        const ddl = $('#ddl_LoadParty');
        ddl.empty().append('<option value="">---Select Load Party---</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Load Party:", error);
    }
}

async function DDlWBParty()
{
    try {
        const res = await fetch('/SalesInvoiceDirect/DDlLoadParty');
        const data = await res.json();
        const ddl = $('#ddl_WBParty');
        ddl.empty().append('<option value="">---Select Load Party---</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Load Party:", error);
    }
}

async function GetVNo(Vtype, tableName) {
    const res = await fetch(`/SalesInvoiceDirect/GetVNo?Vtype=${encodeURIComponent(Vtype)}&tableName=${encodeURIComponent(tableName)}`);
    const data = await res.json();
    if (data.v_NO) {
        $('#NumInvoiceNo').val(data.v_NO);
    }
}

async function DDlDoNo()
{
    try
    {
        const res = await fetch('/SalesInvoiceDirect/DDlDoNo');
        const data = await res.json();

        const ddl = $('#ddlDONo');

        ddl.empty();
        ddl.append('<option value="">--- Select Do No ---</option>');

        data.forEach(item => {
            ddl.append( `<option value="${item.value}"> ${item.value} - ${item.text}  </option>` );
        });

        // Initialize searchable dropdown
        ddl.select2({ placeholder: '--- Select Do No ---', allowClear: true, width: '100%' });

    } catch (error) {
        console.error("Error loading Doc Type:", error);
    }
}

async function ddlPartyName() {
    try {
        const res = await fetch('/SalesInvoiceDirect/cmbPartyName');
        const data = await res.json();

        console.log("data", data);

        partyData = data;

        const ddl = $('#ddlPartyName');
        const ddl1 = $('#ddlConsignee');

        ddl.empty();
        ddl1.empty();

        ddl.append('<option value="">---Select Party Name---</option>');
        ddl1.append('<option value="">---Select Party Name---</option>');

        data.forEach(item => {
            const option = `<option value="${item.code}">${item.p_name}</option>`;

            ddl.append(option);
            ddl1.append(option);
        });

        // Initialize searchable dropdown
        ddl.select2({
            placeholder: "---Select Party Name---",
            allowClear: true,
            width: '100%'
        });

        ddl1.select2({
            placeholder: "---Select Party Name---",
            allowClear: true,
            width: '100%'
        });

    } catch (error) {
        console.error("Error loading Party Name:", error);
    }
}
function selectedPartyData() {
    const selectedCode = $('#ddlPartyName').val();

    const party = partyData.find(
        x => String(x.code) === String(selectedCode)
    );

    console.log("Party Data List ", party);

    if (!party) {
        console.log("Party Data not found");
        return;
    }
    console.log("Selected Party:", party);

    $('#TxtAddressL1').val(party.add1 || '');
    $('#TxtAddressL2').val(party.adD2 || '');
    $('#TxtAddressL3').val(party.adD3 || '');
    $('#ddlStation').val(party.citY_CODE || '');
    $('#NumPincode').val(party.pincode || '');
    $('#NumGSTNo').val(party.gstin || '');

    $('#ddlConsignee').val(party.code || '').trigger('change');
    $('#ddlSalesThrough').val(party.agenT_CODE || '').trigger('change');
    $('#TxtSupplyAddressL1').val(party.add1 || '');
    $('#TxtSupplyAddressL2').val(party.adD2 || '');
    $('#TxtSupplyAddressL3').val(party.adD3 || '');
    $('#ddlSupplyStation').val(party.citY_CODE || '');
    $('#NumSupplyPIN').val(party.pincode || '');
    $('#NumGSTNoL').val(party.gstin || '');
}
function selectedConsigneeData() {
    const selectedCode = $('#ddlConsignee').val();

    const party = partyData.find(
        x => String(x.code) === String(selectedCode)
    );
    console.log("Party Data List ", party);

    if (!party) {
        console.log("Party Data not found");
        return;
    }
    console.log("Selected Party:", party);

    $('#ddlSalesThrough').val(party.agenT_CODE || '').trigger('change');
    $('#TxtSupplyAddressL1').val(party.add1 || '');
    $('#TxtSupplyAddressL2').val(party.adD2 || '');
    $('#TxtSupplyAddressL3').val(party.adD3 || '');
    $('#ddlSupplyStation').val(party.citY_CODE || '');
    $('#NumSupplyPIN').val(party.pincode || '');
    $('#NumGSTNoL').val(party.gstin || '');
}

async function cmbCityName() {
    try {
        const res = await fetch('/SalesInvoiceDirect/cmbCityName');

        if (!res.ok) {
            throw new Error(`HTTP error! Status: ${res.status}`);
        }

        const data = await res.json();

        const ddl = $('#ddlStation');
        const ddl1 = $('#ddlSupplyStation');

        ddl.empty();
        ddl1.empty();

        ddl.append('<option value="">--- Select City ---</option>');
        ddl1.append('<option value="">--- Select City ---</option>');

        data.forEach(item => {
            const option = `<option value="${item.value}">${item.text}</option>`;

            ddl.append(option);
            ddl1.append(option);
        });

    } catch (error) {
        console.error("Error loading City:", error);
    }
}

async function cmbPartyAddress(partycode) {
    try {
        const res = await fetch(`/SalesInvoiceDirect/cmbAddress?partycode=${encodeURIComponent(partycode)}`);
        const data = await res.json();

        const ddl = $('#ddladdressL1');
        ddl.empty();

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Address:", error);
    }
}

async function cmbConsigneeAddress(partycode) {
    try {
        const res = await fetch(`/SalesInvoiceDirect/cmbAddress?partycode=${encodeURIComponent(partycode)}`);
        const data = await res.json();

        const ddl = $('#ddlsupplyaddressL1');
        ddl.empty();

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Address:", error);
    }
}

async function cmbSalesThrough() {
    try {
        const res = await fetch('/SalesInvoiceDirect/cmbSalesThrough');
        const data = await res.json();

        const ddl = $('#ddlSalesThrough');

        ddl.empty();
        ddl.append('<option value="">--- Select Sales Through ---</option>');

        data.forEach(item => {
            ddl.append(  `<option value="${item.value}"> ${item.value} - ${item.text}  </option>` );
        });

        // Initialize searchable dropdown
        ddl.select2({ placeholder: '--- Select Sales Through---', allowClear: true, width: '100%' });

    } catch (error) {
        console.error("Error loading Doc Type:", error);
    }
}

async function AddressPartyData(PartyCode, AddressId) {
    try {
        const res = await $.ajax({
            url: '/SalesInvoiceDirect/GetAddressData',
            type: 'GET',
            data: {
                PartyCode: PartyCode,
                AddressId: AddressId
            }
        });

        const data = res[0];

        if (!data) {
            console.log("No address data found");
            return;
        }

        console.log("Address Data", data);


        // Billing Address
        $('#TxtAddressL1').val(data.add1 || '');
        $('#TxtAddressL2').val(data.add2 || '');
        $('#TxtAddressL3').val(data.add3 || '');
        $('#ddlStation').val(data.city_Code || '');
        $('#NumPincode').val(data.pincode || ''); 
        $('#NumGSTNoL').val(data.gstin || '');

        // Shipping / Consignee Address
        $('#TxtSupplyAddressL1').val(data.add1 || '');
        $('#TxtSupplyAddressL2').val(data.add2 || '');
        $('#TxtSupplyAddressL3').val(data.add3 || '');
        $('#ddlSupplyStation').val(data.city_Code || '');
        $('#NumSupplyPIN').val(data.pincode || '');  
        $('#NumGSTNo').val(data.gstin || '');

        console.log("Address Data:", data);

        return data;
    }
    catch (error) {
        console.log("error", error);
    }
}

async function AddressConsigneeData(PartyCode, AddressId) {
    try {
        const res = await $.ajax({
            url: '/SalesInvoiceDirect/GetAddressData',
            type: 'GET',
            data: {
                PartyCode: PartyCode,
                AddressId: AddressId
            }
        });


        const data = res[0];

        if (!data) {
            console.log("No address data found");
            return;
        }
        // Shipping / Consignee Address
        $('#TxtSupplyAddressL1').val(data.add1 || '');
        $('#TxtSupplyAddressL2').val(data.add2 || '');
        $('#TxtSupplyAddressL3').val(data.add3 || '');
        $('#ddlSupplyStation').val(data.city_Code || '');
        $('#NumSupplyPIN').val(data.pincode || '');
        $('#NumGSTNo').val(data.gstin || '');

        console.log("res", res);
        return res;
    }
    catch (error) {
        console.log("error", error);
    }
}

async function cmbTaxType() {
    try {
        const res = await fetch('/SalesInvoiceDirect/cmbTaxType');
        const data = await res.json();
        console.log("Tax Type data", data);
        TaxPercentageData = data;
        const ddl = $('#ddlTaxType');

        ddl.empty();
        ddl.append('<option value="">---Select Tax Type---</option>');

        data.forEach(item => {
            const option = `<option value="${item.code}">${item.name}</option>`;
            ddl.append(option);
        });

        TaxTypeList = data.map(x => `<option value="${x.code}">${x.name}</option>`).join('');

    } catch (error) {
        console.error("Error loading Party Name:", error);
    }
}

async function cmbPurchaseNo() {
    try {
        const res = await fetch('/SalesInvoiceDirect/cmbPurchaseNo');
        const data = await res.json();

        const ddl = $('#ddlPurchaseNo');

        ddl.empty();
        ddl.append('<option value="">--- Select Purchase No ---</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}"> ${item.value} - ${item.text}  </option>`);
        });

        // Initialize searchable dropdown
        ddl.select2({ placeholder: '--- Select Purchase No ---', allowClear: true, width: '100%' });

    } catch (error) {
        console.error("Error loading Purchase No:", error);
    }
}

async function cmbDocStatus() {
    try {
        const res = await fetch('/SalesInvoiceDirect/cmbDocStatus');
        if (!res.ok) {
            throw new Error(`HTTP error! Status: ${res.status}`);
        }

        const data = await res.json();
        const ddl = $('#ddlDocStatus');
        ddl.empty();
        ddl.append('<option value="">--- Select Doc Status ---</option>');
        data.forEach(item => {
            const option = `<option value="${item.value}">${item.text}</option>`;
            ddl.append(option);
    
        });

    } catch (error) {
        console.error("Error loading  DocStatus:", error);
    }
}

async function cmbPaymentTerm() {
    try {
        const res = await fetch('/SalesInvoiceDirect/cmbPaymentTerm');
        if (!res.ok) {
            throw new Error(`HTTP error! Status: ${res.status}`);
        }

        const data = await res.json();
        const ddl = $('#ddlPaymentTerm');
        ddl.empty();
        ddl.append('<option value="">--- Select Payment Term ---</option>');
        data.forEach(item => {
            const option = `<option value="${item.value}">${item.text}</option>`;
            ddl.append(option);
    
        });

    } catch (error) {
        console.error("Error loading  Payment Term:", error);
    }
}

async function cmbProductName() {
    try {
        const res = await fetch('/SalesInvoiceDirect/cmbProductName');

        if (!res.ok) {
            throw new Error(`HTTP error! Status: ${res.status} `);
        }
        const data = await res.json();
        console.log("cmbProductName:", data);
        if (!Array.isArray(data)) {
            throw new Error("cmbProductName response is not an array");
        }

        ProductList = data.map(x =>
            `<option value="${x.code}">${x.itemname}</option>`
        ).join('');


        return ProductList;

    } catch (error) {
        console.error("Error loading ItemName:", error);
        throw error;
    }
}

async function cmbTransportName() {
    try {
        const res = await fetch('/SalesInvoiceDirect/cmbTransportName');

        if (!res.ok) {
            throw new Error(`HTTP error! Status: ${res.status}`);
        }
        const data = await res.json();
        const ddl = $('#ddlTransport');
        ddl.empty();
        ddl.append('<option value="">--- Select Transport ---</option>');
        data.forEach(item => {
            const option = `<option value="${item.value}">${item.text}</option>`;
            ddl.append(option);
    
        });

    } catch (error) {
        console.error("Error loading City:", error);
    }
}

async function GetDoNo(DoNo, DOType) {
    try {
        const res = await $.ajax({
            url: '/SalesInvoiceDirect/GETDatabyDoNO',
            type: 'GET',
            data: {
                DoNo: DoNo,
                DOType: DOType
            }
        });

        const data = res;
        console.log("DO Data:", data);

        if (!data)
        {
            toastr.warning("DO data not found.");
            return;
        }

        const val = value =>
            value != null && typeof value !== 'object' ? value : '';

        // Load address dropdowns first.
        await Promise.all([
            cmbPartyAddress(val(data.bilL_CODE)),
            cmbConsigneeAddress(val(data.shiP_CODE))
        ]);

        // Party Details
        $('#ddlPartyName').val(val(data.bilL_CODE)).trigger('change');
        $('#TxtAddressL1').val(val(data.bilL_ADD1));
        $('#TxtAddressL2').val(val(data.bilL_ADD2));
        $('#TxtAddressL3').val(val(data.bilL_ADD3));
        $('#NumGSTNoL').val(val(data.bilL_GST));
        $('#ddlStation').val(val(data.bilL_CITY)).trigger('change');
        $('#NumPincode').val(val(data.bilL_PINCODE));

        // Consignee Details
        $('#ddlConsignee').val(val(data.shiP_CODE)).trigger('change');
        $('#TxtSupplyAddressL1').val(val(data.shiP_ADD1));
        $('#TxtSupplyAddressL2').val(val(data.shiP_ADD2));
        $('#TxtSupplyAddressL3').val(val(data.shiP_ADD3));
        $('#NumGSTNo').val(val(data.shiP_GST));
        $('#ddlSupplyStation').val(val(data.shiP_CITY)).trigger('change');
        $('#NumSupplyPIN').val(val(data.shiP_PINCODE));

        // Other Details
        $('#ddlTaxType').val(val(data.taX_CODE)).trigger('change');
        $('#ddlProductType').val(val(data.iteM_TYPE)).trigger('change');
        $('#ddlSupplyType').val(val(data.supply_Type)).trigger('change');
        $('#ddlTransactionType').val(val(data.traN_TYPE)).trigger('change');

        $('#NumOtherTotalAmount').val(val(data.amount));
        $('#NumOtherTotalNos').val(val(data.toT_NOS));
        $('#NumOtherNetAmount').val(val(data.namount));
        $('#TxtTruckNo').val(val(data.vehiclE_NO));
        $('#TxtTransRemarks').val(val(data.remarks));
        $('#NumOtherDiscount2').val(val(data.disC_AMT));
        $('#NumOtherCashDiscount2').val(val(data.cdisC_AMT));

        return data;
    }
    catch (error)
    {
        console.error("GetDoNo error:", error);
        toastr.error("Failed to load DO details.");
    }
}
function SetFormReadOnly(isReadOnly = true)
{
    $('#SalesInvoiceForm') .find('input, textarea') .prop('readonly', isReadOnly);
    $('#SalesInvoiceForm') .find('select, input[type="checkbox"], input[type="radio"]') .prop('disabled', isReadOnly);
    $('#SalesInvoiceForm') .find('button') .not('#btn-save') .prop('disabled', isReadOnly);
    $('#btn-save').prop('disabled', isReadOnly);
}



async function TransitReport()
{
    if (!rowId)
    {
        showToast(`Please save the data before printing the report.`, { type: "info" });
        return;
    }

    let V_NO = $('#NumInvoiceNo').val();
    let V_TYPE = $('#ddlDocumentType').val();
    let V_Date = $('#DtDocumentDate').val();
    let FrtAmt = $('#NumFreightAmount').val();
    let TdsAmt = $('#NumTDSFreight2').val();
    const netAmount = parseFloat($('#NumOtherNetAmount').val()) || 0;
    var reportName = "";
    var exportPrint = false;
    var RPTNAME = "";

    const validation = await $.ajax({
        url: '/SalesInvoiceDirect/PrintValidation',
        type: 'POST',
        contentType: 'application/json; charset=utf-8',
        data: JSON.stringify({
            V_NO: parseInt(V_NO) || null,
            V_TYPE: V_TYPE,
            V_Date: V_Date,
            FrtAmt: FrtAmt,
            TdsAmt: TdsAmt
        })
    });

    if (validation.success == "false")
    {
        toastr.warning(validation.message);
    }
    console.log("validation", validation);
          

    if (exportPrint == false)
     {
         if (globalVars.CompCode == "2" || globalVars.CompCode == "5")
         {
             if (V_TYPE == "SAJI")
             {
                 reportName = "INVOICE_JobworkSale";
             }
             else if (globalVars.CompCode == "5")
             {
                 reportName = "INVOICE_GST1SALASARQR_DETAIL";
             }
             else {
                 reportName = "INVOICE_GST1PLPLQR_DETAIL";
             }
         }
         else if (globalVars.CompCode == "4")
         {
             if (V_TYPE == "SACH" || V_TYPE == "SAJI")
             {
                 if (V_TYPE == "SAJI") {
                     reportName = "INVOICE_JobworkSale";
                 }
                 else
                 {
                     reportName = "INVOICE_ChallanPEPL";
                 }
             }

             else if (globalVars.CompCode > "4")
             {
                 reportName = "INVOICE_GST1PEPLQR_DETAIL";
             }

             else
             {
                 reportName = "INVOICE_GST1PEPL";
             }
         }

         else
         {
             if (V_TYPE == "SACH" || V_TYPE == "SAJI")
             {
                 if (V_TYPE == "SAJI")
                 {
                     reportName = "INVOICE_JobworkSale";
                 }
                 else
                 {
                     reportName = "INVOICE_ChallanSale";
                 }

             }
             else if (globalVars.CompCode == "3")
             {
                 const date = new Date(V_Date);

                 if (date > new Date(2022, 9, 5))
                 {
                     reportName = "INVOICE_GST1QR";
                 } else
                 {
                     reportName = "INVOICE_GST1";
                 }
             }

             else if (globalVars.CompCode == "7")
             {
                 if (globalVars.CompCode > "4")
                 {
                 }
                 else
                 {
                     reportName = "INVOICE_GST1KQR";
                 }
             }
             else if (globalVars.CompCode == "8")
             {
                 if (globalVars.FYearCode >= "6")
                 {

                     if (V_TYPE == "SATD")
                     {
                         reportName = "INVOICEQR_SCTPL_SASI";
                     }
                     else
                     {
                         reportName = "INVOICE_GST1QR_SCTPL";
                     }
                 }
             }

             else
             {
                 reportName = "INVOICE_GST1QR";
             }
         }
     }

    if (V_TYPE == "SAGT" || V_TYPE == "SATD" || V_TYPE == "SAST")
     {
         RPTNAME = "TAX INVOICE";
     }

    var formula =
        "{SALE1.V_TYPE} = '" + V_TYPE + "'" +
        " and {SALE1.V_NO} = " + V_NO + "" +
        " and {SALE1.COMP_CODE} = " + globalVars.CompCode + "" +
        " and {SALE1.YEAR_CODE} = " + globalVars.FYearCode + "" +
        " and {SALE1.BRANCH_CODE} = " + globalVars.BranchCode + "";

    var payload = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: database,
        Parameters: {
            comp_name: globalVars.CompanyName || "",
            comp_add1: globalVars.Address1 || "",
            comp_add2: globalVars.Address2 || "",
            comp_phone: "Mobile : " + (globalVars.Phone || ""),
            PAN: "PAN NO.   : " + (globalVars.PAN || ""),
            EMAIL: "Email   : " + (globalVars.Email || ""),
            Website: "Web     : " + (globalVars.website || ""),
            GST: "GST NO.   : " + (globalVars.GST || ""),
            RPTNAME: RPTNAME
        }
    };

    if (globalVars.CompCode != "3")
    {

        if (globalVars.CompCode != "8")
        {
            payload.Parameters.comp_name1 = "An ISO 9001:2015 Certified Company";
        }

        payload.Parameters.Comp_reg = "Reg.Office :" + (globalVars.RegAdd1 || "") + ", " + (globalVars.RegAdd2 || "") + "  CIN :" + (globalVars.CINNO || "");
    }


    const INWORD = numberToWordsCurrency(netAmount, "Rs.", "PAISE");

    payload.Parameters.INWORD = INWORD;

    var now = new Date();
    var timestamp =
        String(now.getDate()).padStart(2, '0') +
        String(now.getMonth() + 1).padStart(2, '0') +
        String(now.getFullYear()).slice(-2) + "_" +
        String(now.getHours()).padStart(2, '0') +
        String(now.getMinutes()).padStart(2, '0') +
        String(now.getSeconds()).padStart(2, '0');

    $.ajax({
        url: 'http://localhost:24085/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(payload),
        contentType: "application/json",
        xhrFields: { responseType: 'blob' },

        success: function (response)
        {
            var file = new Blob([response], { type: 'application/pdf' });
            var fileName = `${reportName}_${timestamp}.pdf`;
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

async function SendMail() {
    try {

        if (!rowId)
        {
            showToast(`Please save the data before printing the report.`, { type: "info" });
            return;
        }

        let PartyCode = $('#ddlPartyName').val();
        const vno = parseInt($('#NumInvoiceNo').val()) || 0;
        const v_type = $('#ddlDocumentType').val() || '';

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
            url: '/SalesInvoiceDirect/SendMail',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false
        });


        if (mailRes.success == true) {
            toastr.success(mailRes.message || "Mail sent successfully.");
        }
        else {
            toastr.error(mailRes.message || "Failed to send mail.");
        }
        return mailRes;

    } catch (error) {
        console.error("Error:", error);
        toastr.error("An error occurred while sending the mail.");
    }
}


async function GetTransitReportFile() {

    if (!rowId) {
        showToast(`Please save the data before printing the report.`, { type: "info" });
        return;
    }

    let V_NO = $('#NumInvoiceNo').val();
    let V_TYPE = $('#ddlDocumentType').val();
    let V_Date = $('#DtDocumentDate').val();
    let FrtAmt = $('#NumFreightAmount').val();
    let TdsAmt = $('#NumTDSFreight2').val();
    const netAmount = parseFloat($('#NumOtherNetAmount').val()) || 0;
    var reportName = "";
    var exportPrint = false;
    var RPTNAME = "";

    const validation = await $.ajax({
        url: '/SalesInvoiceDirect/PrintValidation',
        type: 'POST',
        contentType: 'application/json; charset=utf-8',
        data: JSON.stringify({
            V_NO: parseInt(V_NO) || null,
            V_TYPE: V_TYPE,
            V_Date: V_Date,
            FrtAmt: FrtAmt,
            TdsAmt: TdsAmt
        })
    });

    if (validation.success == "false")
    {
        toastr.warning(validation.message);
    }
    console.log("validation", validation);


    if (exportPrint == false) {
        if (globalVars.CompCode == "2" || globalVars.CompCode == "5")
        {
            if (V_TYPE == "SAJI")
            {
                reportName = "INVOICE_JobworkSale";
            }
            else if (globalVars.CompCode == "5")
            {
                reportName = "INVOICE_GST1SALASARQR_DETAIL";
            }
            else {
                reportName = "INVOICE_GST1PLPLQR_DETAIL";
            }
        }
        else if (globalVars.CompCode == "4")
        {
            if (V_TYPE == "SACH" || V_TYPE == "SAJI") {
                if (V_TYPE == "SAJI") {
                    reportName = "INVOICE_JobworkSale";
                }
                else {
                    reportName = "INVOICE_ChallanPEPL";
                }
            }

            else if (globalVars.CompCode > "4")
            {
                reportName = "INVOICE_GST1PEPLQR_DETAIL";
            }

            else
            {
                reportName = "INVOICE_GST1PEPL";
            }
        }

        else
        {
            if (V_TYPE == "SACH" || V_TYPE == "SAJI")
            {
                if (V_TYPE == "SAJI")
                {
                    reportName = "INVOICE_JobworkSale";
                }
                else
                {
                    reportName = "INVOICE_ChallanSale";
                }

            }
            else if (globalVars.CompCode == "3")
            {
                const date = new Date(V_Date);

                if (date > new Date(2022, 9, 5))
                {
                    reportName = "INVOICE_GST1QR";
                } else
                {
                    reportName = "INVOICE_GST1";
                }
            }

            else if (globalVars.CompCode == "7")
            {
                if (globalVars.CompCode > "4")
                {
                }
                else {
                    reportName = "INVOICE_GST1KQR";
                }
            }
            else if (globalVars.CompCode == "8") {
                if (globalVars.FYearCode >= "6") {

                    if (V_TYPE == "SATD") {
                        reportName = "INVOICEQR_SCTPL_SASI";
                    }
                    else {
                        reportName = "INVOICE_GST1QR_SCTPL";
                    }
                }
            }

            else {
                reportName = "INVOICE_GST1QR";
            }
        }
    }

    if (V_TYPE == "SAGT" || V_TYPE == "SATD" || V_TYPE == "SAST") {
        RPTNAME = "TAX INVOICE";
    }


    var formula =
        "{SALE1.V_TYPE} = '" + V_TYPE + "'" +
        " and {SALE1.V_NO} = " + V_NO + "" +
        " and {SALE1.COMP_CODE} = " + globalVars.CompCode + "" +
        " and {SALE1.YEAR_CODE} = " + globalVars.FYearCode + "" +
        " and {SALE1.BRANCH_CODE} = " + globalVars.BranchCode + "";

    var payload = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: database,
        Parameters: {
            comp_name: globalVars.CompanyName || "",
            comp_add1: globalVars.Address1 || "",
            comp_add2: globalVars.Address2 || "",
            comp_phone: "Mobile : " + (globalVars.Phone || ""),
            PAN: "PAN NO.   : " + (globalVars.PAN || ""),
            EMAIL: "Email   : " + (globalVars.Email || ""),
            Website: "Web     : " + (globalVars.website || ""),
            GST: "GST NO.   : " + (globalVars.GST || ""),
            RPTNAME: RPTNAME
        }
    };

    if (globalVars.CompCode != "3") {

        if (globalVars.CompCode != "8")
        {
            payload.Parameters.comp_name1 = "An ISO 9001:2015 Certified Company";
        }

        payload.Parameters.Comp_reg = "Reg.Office :" + (globalVars.RegAdd1 || "") + ", " + (globalVars.RegAdd2 || "") + "  CIN :" + (globalVars.CINNO || "");
    }


    const INWORD = numberToWordsCurrency(netAmount, "Rs.", "PAISE");

    payload.Parameters.INWORD = INWORD;


    // Generate report
    const pdfBlob = await $.ajax({
        url: 'http://localhost:24085/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(payload),
        contentType: "application/json",
        xhrFields: { responseType: 'blob' }
    });

    const file = new Blob([pdfBlob], { type: "application/pdf" });
    const now = new Date();
    const timestamp = String(now.getDate()).padStart(2, '0') +
        String(now.getMonth() + 1).padStart(2, '0') +
        String(now.getFullYear()).slice(-2) + "_" +
        String(now.getHours()).padStart(2, '0') +
        String(now.getMinutes()).padStart(2, '0') +
        String(now.getSeconds()).padStart(2, '0');

    const fileName = `SalesInvoiceDirect_${V_NO}_${timestamp}.pdf`;

    return { file, fileName };
}
