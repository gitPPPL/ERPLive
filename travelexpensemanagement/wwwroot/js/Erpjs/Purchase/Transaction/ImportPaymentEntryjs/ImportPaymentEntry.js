let poList = [];
let itemList = [];
let countryList = [];
let PartyList = [];
let PortOfDispatch = [];
let DestinationPort = [];

$(document).ready(async function () {

    SetCurrentDate();

    await wireEvents();
    await GetDocTypeDDl();
    await BindAllDropdown();

    if ($("#tblImportPaymentEntry tbody tr").length === 0) {
        addNewRow();
    }

    await LoadItemMaster();
    await LoadCountryMaster();
    await LoadPartyMaster();
    await LoadPortOfDispatch();
    await LoadDestinationPort();

    //===========================
    // Save and Update Data
    //===========================
    $('#btnSave').on('click', async function () {

        const header = {

            //========Main Tab==================
            V_TYPE: $('#ddldoctype').val(),
            V_NO: $('#Numdocno').val(),
            V_DATE: $('#Dtdocdate').val(),
            PARTY_CODE: $('#hdnSupplierCode').val() || null,
            DOC_EVEDENCE: $('#Txtdocevidence').val(),
            ECB_PURPOSE: $('#ddlnatureproject').val(),
            PAY_TYPE: $('#ddlpaytype').val(),
            IMPORT_CAT: $('#ddlimport').val(),
            ITEM_CAT: $('#ddlimport').val(),
            IMPORT_REMIT: $('#ddlimportremitt').val(),
            CURRENCY: $('#ddlcurrency option:selected').text(),
            FOREIGN_BANKCHARGE: $('#ddlforeignbankch').val(),
            INTRATE_APPL: $('#ddlintrateappl').val(),
            REMARKS: $('#txtremarks').val(),
            TOT_AMT: $('#Numnetamount').val() || 0,
           
            //========Bank Detail Tab===========
            BANK_CODE: $('#ddlourbank').val(),

            BENI_BANK: $('#ddlbeneficiarybank option:selected').text(),
            BENI_ACTNO: $('#Numbeneficiaryacno').val(),
            BENI_SWIFT: $('#Txtbeneficiaryshift').val(),
            BENI_ABA: $('#Txtbeneficiaryaba').val(),
            BENI_ROUT: $('#Txtbeneficiaryrouting').val(),
            BENI_SC: $('#NumbeneficiaryCode').val(),
            BENI_BANKADD: $('#Txtbeneficiarybankaddress').val(),

            CORR_BANK: $('#ddlcorrespondancebank').val(),
            CORR_ACTNO: $('#Numcorrespondanceacno').val(),
            CORR_SWIFT: $('#Txtcorrespondanceshift').val(),
            CORR_ABA: $('#Txtcorrespondanceaba').val(),
            CORR_ROUT: $('#Txtcorrespondancerouting').val(),
            CORR_SC: $('#NumcorrespondanceCode').val(),
            CORR_BANKADD: $('#Txtcorrespondancebankaddress').val(),

            //========Customer Declaration tab===============
            SPFC_BANK: $('#ddlSPFCBank').val(),
            SPFC_BANKNAME: $('#ddlSPFCBank option:selected').text(),
            CD_BILLREFNO: $('#txtbillreferenceno').val(),
            CD_CCY: $('#txtCCY').val(),
            CD_AMTREMITT: $('#Numamountremitted').val() || 0,
            CDFEMA_NC: $('#Chknotcoveredunderprohibited').is(':checked'),
            CDFEMA_RES: $('#Chkreceivedforimport').is(':checked'),
            CD_ATTCH1: $('#cbCDAttach1').is(':checked'),
            CD_ATTCH2: $('#cbCDAttach2').is(':checked'),
            CD_ATTCH3: $('#cbCDAttach3').is(':checked'),
            CD_ATTCH4: $('#cbCDAttach4').is(':checked'),
            CD_ATTCH5: $('#cbCDAttach5').is(':checked'),
            CD_ATTCH6: $('#cbCDAttach6').is(':checked'),
            CD_ATTCH7: $('#cbCDAttach7').is(':checked'),
            CD_ATTCH8: $('#cbCDAttach8').is(':checked'),
            CD_ATTCH9: $('#cbCDAttach9').is(':checked'),
            OTHDOC_DETAILS: $('#txtotherdocumentdetails').val(),

            //============Foam A2 Tab========================
            A2_ISSUEDRAFT: $('#Chkissuedraft').is(':checked'),
            A2_FEREFFECT: $('#Chkeffectforeignexchange').is(':checked'),
            A2_BENIFICIARY: $('#hdnBeneficiaryCode').val(),
            A2_ACTNO: $('#NumAccountno').val(),
            A2_NAMEADD: $('#Txtnamebankaddress').val(),
            A2_ITFOR: $('#txtA2_3').val(),
            A2_FCNFOR: $('#txtA2_4').val(),
            A2_AMOUNT: $('#Numamount').val() || 0,
            A2_LRS: $('#ddlLRS').val(),
            A2_PC: $('#Txtpurposecode').val(),
            A2_DESC: $('#TxtDes').val(),
            A2_ISSUETRAVELLER: $('#Chkissuetravellerscheque').is(':checked'),
            A2_FCN: $('#Chkissueforeigncurrencynotes').is(':checked'),

            //==========Part B Info tab================
            ECB_LENDER: $('#hdnLenderCode').val(),
            ECB_NAMEADD: $('#textaddresslender').val(),

            ECB_NATURE1: $('#Chksuppliercredit').is(':checked'),
            ECB_NATURE2: $('#Chkbuyercredit').is(':checked'),
            ECB_NATURE3: $('#Chksyndicatedloan').is(':checked'),
            ECB_NATURE4: $('#Chkexportcredit').is(':checked'),
            ECB_NATURE5: $('#Chkloanforeigncollaboration').is(':checked'),
            ECB_NATURE6: $('#Chkfloatingratenotes').is(':checked'),
            ECB_NATURE7: $('#Chkfixedratebonds').is(':checked'),
            ECB_NATURE8: $('#Chklinecredit').is(':checked'),
            ECB_NATURE9: $('#ChkCommercialbankloan').is(':checked'),
            ECB_NATURE10: $('#ChkOthers').is(':checked'),

            ECB_ROI: $('#txtrateinterest').val(),
            ECB_UPFRONTFEE: $('#txtupfrontfree').val(),
            ECB_MGMTFEE: $('#Txtmanagementfree').val(),
            ECB_OTHCH: $('#Txtothercharges').val(),
            ECB_ALLINCOST: $('#txtallincost').val(),
            ECB_COMMITMENTFEE: $('#txtcommitmentfree').val(),
            ECB_ROPI: $('#Txtratepenalinterest').val(),
            ECB_PERIOD: $('#Txtperiodecb').val(),
            ECB_CALLPUT: $('#txtDetailscallput').val(),
            ECB_GRACE: $('#txtGraceMoratorium').val(),
            ECB_REPAYTERM: $('#ddlrepaymentterms').val(),
            ECB_AVGMATURITY: $('#Txtaveragematurity').val(),
            ECB_NATUREOFSEC: $('#Txtnaturesecurity').val(),

            //============Part-C & D ====================
            PCD_DDMONTH: $('#Dtmonthyeardraw').val() || null,
            PCD_DDAMT: $('#NumAmountdraw').val() || 0,

            PCD_RPMONTH: $('#Dtmonthyearrepayment').val() || null,
            PCD_RPAMT: $('#NumAmountrepayment').val() || 0,

            PCD_IPMONTH: $('#Dtmonthyearinterest').val() || null,
            PCD_IPAMT: $('#NumAmountinterest').val() || 0,

            PCD_NAMELOC: $('#txtnamelocationproject').val(),
            PCD_TOTALCOST: $('#txttotalcostproject').val(),
            PCD_PERCOST: $('#txttotalecbproject').val(),
            PCD_PIBANKAPPL: $('#ddlappraisedfinancial').val(),

            PCD_IS1: $('#Chkpower').is(':checked'),
            PCD_IS2: $('#Chktelecommunication').is(':checked'),
            PCD_IS3: $('#Chkrailways').is(':checked'),
            PCD_IS4: $('#Chkroadsbridges').is(':checked'),
            PCD_IS5: $('#Chkports').is(':checked'),
            PCD_IS6: $('#Chkindustrialparks').is(':checked'),
            PCD_IS7: $('#Chkurbaninfrastructure').is(':checked'),

            PCD_REQSA: $('#ddlclearancestaturity').val(),
            PCD_AUTHORITY: $('#txtnameauthority').val(),
            //PCD_CLNO: $('#txtclearanceno').val(),
            PCD_CLDATE: $('#chkClearanceDate').is(':checked') ? $('#DtClearanceDate').val() : null,
            CLEARANCE_NO: $('#txtclearanceno').val(),

        }

        // ---------------------------------------
        // FOOTER TABLE DATA
        // ---------------------------------------

        const footerDetails = [];

        $('#tblImportPaymentEntry tbody tr').each(function () {

            const row = $(this);

            const poDDL = row.find('.ddlPoNo option:selected');
            const shippingDDL = row.find('.shippingcompany option:selected');
            const podDDL = row.find('.portdispatch option:selected');
            const destinationDDL = row.find('.destinationport option:selected');
            const itemDDL = row.find('.itemname option:selected');
            const poValue = row.find('.ddlPoNo').val() || '';

            const poType = poValue.substring(0, 4);
            const poNo = poValue.substring(4);

            const footer = {

                PO_TYPE: poType || null,
                PO_NO: poNo || null,
                PO_DATE: row.find('.podate').val() || null,
                INV_NO: row.find('.invoiceno').val() || null,
                INV_DATE: row.find('.invoicedate').val() || null,
                AMOUNT: row.find('.amount').val() || 0,
                QTY: row.find('.quantity').val() || 0,
                ITEM_CODE: row.find('.itemname').val() || null,
                ITEM_NAME: itemDDL.text().trim() || null,
                ITEM_DESC: row.find('.itemdesc').val() || null,
                HSN_CODE: row.find('.hsncode').val() || null,
                COUNTRY_ORIGIN: row.find('.country').val() || null,
                SHIPMENT_MODE: row.find('.shipmentmode').val() || null,
                SHIPMENT_DATE: row.find('.shipmentdate').val() || null,
                EXPECTED_DOD: row.find('.dispatchdate').val() || null,
                SHIPCOMP_CODE: row.find('.shippingcompany').val() || null,
                SHIPPING_COMP: shippingDDL.text().trim() || null,
                POD_CODE: row.find('.portdispatch').val() || null,
                POD: podDDL.text().trim() || null,
                DEST_PORTCODE: row.find('.destinationport').val() || null,
                DEST_PORT: destinationDDL.text().trim() || null,
                BL_NO: row.find('.blno').val() || null,
                BE_NO: row.find('.beno').val() || null,
                BE_DATE: row.find('.bedate').val() || null,
                BE_CCYNO: row.find('.beccy').val() || null,
                BE_AMT: row.find('.beamount').val() || 0,
                BE_UTIAMT: row.find('.beutilized').val() || 0,
                FOB_VALUE: row.find('.fobvalue').val() || 0,
                AD_CODE: null,
                PORT_CODE: null
            };

            footerDetails.push(footer);
        });

        const saveData = {
            Header: header,
            FooterDetails: footerDetails
        };

        try
        {
            const response = await fetch('/ImportPaymentEntry/SaveImportPaymentEntry', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(saveData)
            });

            const result = await response.json();

            if (!response.ok) {
                throw new Error(result.message || 'Unable to save Import Payment Entry.');
            }

            if (result.success || result.statusCode === 200) {

                showToast(result.message ||"Data Saved Successfully", { type: "success" });
            }
            else
            {
                showToast(result.message ||"Error while saving", { type: "error" });
            }
        }
        catch (error)
        {
            console.error('Save Import Payment Entry Error:', error);
        }
        
    });

});

async function wireEvents() {

    $("#ddldoctype").trigger("focus");

    $("#ddldoctype").on("focus", function () {
        $(this).prop("disabled", true);
    });

    //===========================
    // Get Our Bank Details 
    //===========================
    $('#ddlourbank').on('change', async function () {

        const bankCode = $(this).val();

        if (!bankCode) {
            $('#txtOurActNo').val('');
            $('#txtOurSwift').val('');
            $('#txtADCode').val('');
            return;
        }

        const response = await fetch(`/ImportPaymentEntry/GetOurBankDetails?bankCode=${bankCode}`);
        const data = await response.json();
        console.log("Bank details fetched:", data);
        $('#Numacno').val(data.accountNo);
        $('#Txtourshift').val(data.swiftCode);
        $('#Txtadcode').val(data.adCode);
    });

    //================================
    // Get Beneficiary Bank Details
    //================================
    $('#ddlbeneficiarybank').on('change', async function () {

        const bankCode = $(this).val();
        const vType = $('#ddldoctype').val();

        try {

            const response = await fetch(`/ImportPaymentEntry/GetBeneficiaryBankDetails?bankCode=${bankCode}&vType=${encodeURIComponent(vType)}`);

            if (!response.ok) {
                throw new Error("Failed to load beneficiary bank details.");
            }

            const data = await response.json();

            $('#Numbeneficiaryacno').val(data.accountNo);
            $('#Txtbeneficiaryaba').val(data.aba);
            $('#Txtbeneficiaryrouting').val(data.routingNo);
            $('#NumbeneficiaryCode').val(data.sortCode);
            $('#Txtbeneficiaryshift').val(data.swiftCode);
            $('#Txtbeneficiarybankaddress').val(data.bankAdd);

        }
        catch (error) {
            console.error(error);
        }
    });

    //================================
    // Correspondence Bank Details
    //================================
    $('#ddlcorrespondancebank').on('change', async function () {

        const bankCode = $(this).val();
        const vType = $('#ddldoctype').val();

        const response = await fetch(`/ImportPaymentEntry/GetBeneficiaryBankDetails?bankCode=${bankCode}&vType=${encodeURIComponent(vType)}`);
        const data = await response.json();

        $('#Numcorrespondanceacno').val(data.accountNo);
        $('#Txtcorrespondanceaba').val(data.aba);
        $('#Txtcorrespondancerouting').val(data.routingNo);
        $('#NumcorrespondanceCode').val(data.sortCode);
        $('#Txtcorrespondanceshift').val(data.swiftCode);
        $('#Txtcorrespondancebankaddress').val(data.bankAdd);
    });

    //===========================
    // Party Change
    //===========================
    $('#txtSupplierName').on('autocompleteselect', async function (event, ui) {

        console.log("autocompleteselect fired");
        console.log(ui.item);
        const partyCode = ui.item.code;

        if (!partyCode) return;

        await GetPartyDetails(partyCode);
        await FillPartyDetailsInPartB();
        await LoadPODropdown(partyCode);

    });

    //========================================
    //  Add and Delete Row(Footer Table)
    //========================================
    $('#tblImportPaymentEntry').on('click', '.add', function () {
        addNewRow();
    });

    $('#tblImportPaymentEntry').on('click', '.delete', function () {
        const totalRows = $('#tblImportPaymentEntry tbody tr').length;

        if (totalRows === 1) {
            showToast("At least one row is required.", { type: "warning" });
            return;
        }

        $(this).closest('tr').remove();
    });

    //==============================================
    // Fill Data On Change Of PO No
    //==============================================

    $('#tblImportPaymentEntry').on('change', '.ddlPoNo', function () {

        const row = $(this).closest('tr');
        const po = poList.find(x => x.vNo == $(this).val());
        const itemDDL = row.find('.itemname');

        if (!po) return;

        row.find('.podate').val(formatDate(po.saudaDate));
        row.find('.invoiceno').val(po.supplierInvNo);
        row.find('.invoicedate').val(formatDate(po.supplierInvDate));
        row.find('.amount').val(po.supplierInvAmt);
        row.find('.quantity').val(po.qty);

        itemDDL.val(po.itemCode).trigger('change');

        row.find('.itemdesc').val(po.itemName);

        //row.find('.hsncode').val(po.hsnCode);
        row.find('.country').val(po.originCountry);
        row.find('.shipmentmode').val(po.mode);
        row.find('.dispatchdate').val(formatDate(po.etd));
        row.find('.destinationport').val(po.destinationPort);
        row.find('.blno').val(po.blNo);
        row.find('.bldate').val(formatDate(po.blDate));
        row.find('.beno').val(po.beNo);
        row.find('.bedate').val(formatDate(po.beDate));

    });

    //==============================================
    // Fil HSN CODE Change On ITEM MAST
    //==============================================
    $('#tblImportPaymentEntry').on('change', '.itemname', function () {

        const row = $(this).closest('tr');
        const itemCode = $(this).val();

        const item = itemList.find(x => x.code == itemCode);

        if (!item) {
            row.find('.hsncode').val('');
            return;
        }

        row.find('.hsncode').val(item.hsn);
    });

}

function SetCurrentDate() {

    const today = new Date().toISOString().split("T")[0];
    $("#Dtdocdate").val(today);

}

async function BindAllDropdown() {
    await Promise.all([

        bindDropdownNew('ImportPaymentEntry', 'SupplierName', '#txtSupplierName', '-- Select Supplier --'),

        bindDropdownNew('ImportPaymentEntry', 'SupplierName', '#ddlBeeficiaryname', '-- Select Beneficiary --', '#hdnBeneficiaryCode'),

        bindDropdownNew('ImportPaymentEntry', 'SupplierName', '#ddlLender', '-- Select Supplier/Lender --', '#hdnLenderCode'),

        bindDropdown('ImportPaymentEntry', 'OurBank', '#ddlourbank', '-- Select Bank Type --' , null, null, false, null, true),

        bindDropdown('ImportPaymentEntry', 'Bank', '#ddlbeneficiarybank', '-- Select Beneficiary Bank --', null, null, false, null, true),

        bindDropdown('ImportPaymentEntry', 'Currency', '#ddlcurrency', '-- Select Currency --', null, null, false, null, true),

        bindDropdown('ImportPaymentEntry', 'Bank', '#ddlcorrespondancebank', '-- Select Correspondance bank --', null, null, false, null, true),

        bindDropdown('ImportPaymentEntry', 'Bank', '#ddlSPFCBank', '-- Select SPFC Bank --', null, null, false, null, true),

    ]);
}

async function GetDocTypeDDl() {

    try {

        const res = await fetch("/ImportPaymentEntry/DocType", {
            method: "GET",
        });

        const data = await res.json();

        const ddl = $("#ddldoctype");
        ddl.empty();

        $.each(data, function (i, item) {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });

        await GetVNo();

    }catch (error) {
        showToast("Doc Type Load Failed", { type: "error" });
        console.error("Error fetching DocType dropdown data:", error);
    }

}

async function GetVNo() {

    try {
        const vType = $('#ddldoctype').val();
        if (!vType) {
            console.warn("vType is empty");
            return;
        }
        const res = await fetch(`/ImportPaymentEntry/GenerateVNo?vType=${encodeURIComponent(vType)}`);

        if (!res.ok) {
            throw new Error("Network response was not ok");
        }
        const data = await res.json();
        if (data.v_NO) {
            $('#Numdocno').val(data.v_NO);
            const docId = vType + data.v_NO;
        } else {
            console.warn("V_NO not found in response");
        }

    } catch (e) {
        console.error("Error in GetVNo:", e);
    }
}

async function GetPartyDetails(partyCode) {

    try {

        const response = await fetch(`/ImportPaymentEntry/GetPartyDetails?partyCode=${partyCode}`);

        if (!response.ok) {
            throw new Error("Failed to load party details.");
        }

        const data = await response.json();

        console.log("Party Details :", data);

        //==================================
        // Part B
        //==================================
        $('#ddlLender').val(data.ecbLenderCode);
        $('#hdnLenderCode').val(data.ecbLenderCode);
        $('#txtECBLenderAdd').val(data.ecbAddress);

        //==================================
        // Beneficiary
        //==================================
        $('#ddlBeeficiaryname').val(data.beneficiaryName);
        $('#hdnBeneficiaryCode').val(data.beneficiaryCode);
        $('#txtBeneficiaryActNo').val(data.beneficiaryActNo);
        $('#txtBeneficiaryBankAdd').val(data.beneficiaryBankAddress);

        //==================================
        // Last Import
        //==================================
        $('#ddlImportFor').val(data.importCategory);
        $('#ddlImportRemit').val(data.importRemit);
        $('#ddlPayType').val(data.payType);
        $('#ddlForeignBankCharge').val(data.foreignBankCharge);
        $('#ddlInterestApplicable').val(data.interestApplicable);
        $('#txtROI').val(data.roi);
        $('#txtROIPeriod').val(data.roiPeriod);
          
        //==================================
        // Bank Details
        //==================================
        $('#ddlbeneficiarybank').val(data.beneficiaryBankCode).trigger('change');
        $('#txtBeneficiarySwift').val(data.beneficiarySwift);
        $('#txtBeneficiaryActNo').val(data.beneficiaryAccount);
        $('#ddlcorrespondancebank').val(data.corrBankCode).trigger('change');
        $('#txtCorrSwift').val(data.corrSwift);
        $('#txtCorrActNo').val(data.corrAccount);

    }
    catch (error) {

        console.error("Error loading party details:", error);
        showToast("Failed to load party details.", { type: "error" });
        
    }
}

async function FillPartyDetailsInPartB() {

    const partyCode = $('#hdnSupplierCode').val();

    if (!partyCode) return;

    try {

        const response = await fetch(`/ImportPaymentEntry/GetPartyDetailsForPartB?partyCode=${partyCode}`);

        if (!response.ok) {
            throw new Error("Failed to load Party Details.");
        }

        const data = await response.json();
        console.log("FillPartyDetailsInPartB", data);
        $('#ddlLender').val(data.name);
        $('#textaddresslender').val(data.address);

    }
    catch (error) {
        console.error(error);
    }
}

//================================================
//      Footer Table
//================================================

function addNewRow() {

    const tbody = $("#tblImportPaymentEntry tbody");

    const row = `
        <tr>

            <td class="hidden-col">
                <input type="hidden" class="erppagetable-control code">
            </td>

            <td> <select class="erppagetable-control ddlPoNo">
                </select>
            </td>

            <td><input type="date" class="erppagetable-control podate"></td>

            <td><input type="text" class="erppagetable-control invoiceno"></td>

            <td><input type="date" class="erppagetable-control invoicedate"></td>

            <td><input type="number" class="erppagetable-control amount text-end"></td>

            <td><input type="number" class="erppagetable-control quantity text-end"></td>

            <td>
                <select class="erppagetable-control itemname">
                    <option value=""> Select Item </option>
                </select>
            </td>

            <td><input type="text" class="erppagetable-control itemdesc"></td>
                
            <td><input type="text" class="erppagetable-control hsncode"></td>
                                          
            <td>
                <select class="erppagetable-control country">
                    <option value=""> Select Country </option>
                </select>
            </td>

            <td>
                <select class="erppagetable-control shipmentmode">
                    <option value=""> Select ShipmentMode </option>
                    <option value="AIR">Air</option>
                    <option value="SEA">Sea</option>
                    <option value="POST">Post</option>
                    <option value="RAIL">Rail</option>
                    <option value="ROAD">Road</option>
                </select>
            </td>

            <td><input type="date" class="erppagetable-control shipmentdate"></td>
                                         
            <td><input type="date" class="erppagetable-control dispatchdate"></td>
                                          
            <td>
                 <select class="erppagetable-control shippingcompany">
                        <option value=""> Select ShippingCompany </option>
                 </select>
            </td>
                                         
            <td>
                 <select class="erppagetable-control portdispatch">
                        <option value=""> Select portdispatch </option>
                 </select>
            </td>
                                         
            <td>
                  <select class="erppagetable-control destinationport">
                        <option value=""> Select Destination Port </option>
                 </select>
            </td>
                                         
            <td><input type="text" class="erppagetable-control blno"></td>
                                       
            <td><input type="date" class="erppagetable-control bldate"></td>
                                        
            <td><input type="text" class="erppagetable-control beno"></td>

            <td><input type="date" class="erppagetable-control bedate"></td>

            <td>
                <select class="erppagetable-control beccy">
                    <option value=""> Select </option>
                </select>
            </td>
            
            <td><input type="number" class="erppagetable-control beamount text-end"></td>
                                            
            <td><input type="number" class="erppagetable-control beutilized text-end"></td>
                                            
            <td><input type="number" class="erppagetable-control fobvalue text-end"></td>

            <td class="action-col">
                <div class="action-wrap">
                     <button class="act-btn add" title="Add Row"><i class="fa fa-plus"></i></button>
                     <button class="act-btn delete"><i class="fa fa-trash"></i></button>
                </div>
            </td>
            
        </tr>
    `;

    tbody.append(row);

    const ddl = tbody.find("tr:last .ddlPoNo");
    const itemDDL = tbody.find("tr:last .itemname");
    const countryDDL = tbody.find("tr:last .country");
    const PartyDDL = tbody.find("tr:last .shippingcompany");
    const PortOfDispatch = tbody.find("tr:last .portdispatch");
    const destinationPort = tbody.find("tr:last .destinationport");

    ddl.append('<option value=""> Select PO No </option>');
        $.each(poList, function (_, item) {
            ddl.append(`
            <option value="${item.vNo}">
                ${item.vType}${item.vNo}
            </option>
        `);

    });

    itemDDL.empty().append('<option value=""> Select Item </option>');

    $.each(itemList, function (_, item) {
        itemDDL.append(`<option value="${item.code}">${item.name}</option>`);
    });

    countryDDL.empty().append('<option value=""> Select Country </option>');

    $.each(countryList, function (_, item) {
        countryDDL.append(`<option value="${item.value}">${item.text}</option>`);
    });

    PartyDDL.empty().append('<option value=""> Select Party </option>');

    $.each(PartyList, function (_, item) {
        PartyDDL.append(`<option value="${item.value}">${item.text}</option>`);
    });

    PortOfDispatch.empty().append('<option value=""> Select Port Of Dispatch </option>');

    $.each(PortOfDispatch, function (_, item) {
        PortOfDispatch.append(`<option value="${item.value}">${item.text}</option>`);
    });

    destinationPort.empty().append('<option value=""> Select Destination Port </option>');

    $.each(destinationPort, function (_, item) {
        destinationPort.append(`<option value="${item.value}">${item.text}</option>`);
    });
}

async function LoadPODropdown(partyCode) {

    try {

        const response = await fetch(`/ImportPaymentEntry/GetItemMaster?partyCode=${partyCode}`);

        if (!response.ok) {
            throw new Error("Failed to load PO.");
        }

        poList = await response.json();

        console.log("Po List Dropdown", poList);
        $('.ddlPoNo').each(function () {

            const ddl = $(this);

            ddl.empty().append('<option value=""> Select PO No </option>');

            $.each(poList, function (_, item) {

                ddl.append(`
                    <option value="${item.vNo}">
                        ${item.vType}${item.vNo}
                    </option>
                `);

            });

        });

    } catch (e) {
        console.error(e);
    }
}

async function LoadItemMaster() {

    const response = await fetch('/ImportPaymentEntry/GetRawItemMaster');
    itemList = await response.json();

    $('.itemname').each(function () {

        const ddl = $(this);

        ddl.empty().append('<option value=""> Select Item </option>');

        $.each(itemList, function (_, item) {
            ddl.append(`<option value="${item.code}">${item.name}</option>`);
        });

    });
}

async function LoadCountryMaster() {

    const response = await fetch('/ImportPaymentEntry/GetCountryMast');
    countryList = await response.json();

    $('#tblImportPaymentEntry .country').each(function () {

        const ddl = $(this);

        ddl.empty().append('<option value=""> Select Country </option>');

        $.each(countryList, function (_, item) {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });

    });
}

async function LoadPartyMaster() {

    const response = await fetch('/ImportPaymentEntry/GetPartyMastForFooter');
    PartyList = await response.json();

    $('#tblImportPaymentEntry .shippingcompany').each(function () {

        const ddl = $(this);

        ddl.empty().append('<option value=""> Select Shipping Company </option>');

        $.each(PartyList, function (_, item) {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });

    });
}

async function LoadPortOfDispatch() {

    const response = await fetch('/ImportPaymentEntry/GetPortOfDispatch');
    PortOfDispatch = await response.json();

    $('#tblImportPaymentEntry .portdispatch').each(function () {

        const ddl = $(this);

        ddl.empty().append('<option value=""> Select Port Dispatch </option>');

        $.each(PortOfDispatch, function (_, item) {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });

    });
}

async function LoadDestinationPort() {

    const response = await fetch('/ImportPaymentEntry/GetPortOfDispatch');
    DestinationPort = await response.json();

    $('#tblImportPaymentEntry .destinationport').each(function () {

        const ddl = $(this);
        ddl.empty().append('<option value=""> Select Destination Port </option>');

        $.each(DestinationPort, function (_, item) {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });

    });
}

function formatDate(date) {

    if (!date) return "";

    const parts = date.split("/"); 

    return `${parts[2]}-${parts[1]}-${parts[0]}`;
}