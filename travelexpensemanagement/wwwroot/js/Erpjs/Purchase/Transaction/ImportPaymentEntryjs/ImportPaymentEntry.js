let poList = [];
let itemList = [];
let countryList = [];
let PartyList = [];
let PortOfDispatch = [];
let DestinationPort = [];

const urlParams = new URLSearchParams(window.location.search);
const id = urlParams.get('docId');
const vtype = urlParams.get('vtype');
let isReadOnly = urlParams.get('readOnly') === 'true';
 
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

    //----------------------
    //   Edit Mode
    //----------------------
    if (id) {
        await LoadEditData(id);
    }
    else {
        if ($("#tblImportPaymentEntry tbody tr").length === 0) {
            addNewRow();
        }
    }

    if (isReadOnly) {
        setFormReadOnly();
    }

    //===========================
    // Save and Update Data
    //===========================
    $('#btnSave').on('click', async function (e) {
        e.preventDefault();

        const isValidDate = await checkValidDate();
        if (!isValidDate) return;

        if (!(await ValidateData())) {
            return;
        }

        const header = {
             
            //========Main Tab==================
            V_TYPE: $('#ddldoctype').val(),
            V_NO: parseInt($('#Numdocno').val()),
            V_DATE: $('#Dtdocdate').val(),
            DOC_ID: id || null,
            PARTY_CODE: parseInt($('#ddlParty').val()) || null,
            DOC_EVEDENCE: $('#Txtdocevidence').val() || null,
            ECB_PURPOSE: $('#ddlnatureproject').val() || null,
            PAY_TYPE: $('#ddlpaytype').val() || null,
            IMPORT_CAT: $('#ddlimport').val() || null,
            ITEM_CAT: $('#ddlimport').val() || null,
            IMPORT_REMIT: $('#ddlimportremitt').val() || null,
            CURRENCY: $('#ddlcurrency option:selected').text() || null,
            FOREIGN_BANKCHARGE: $('#ddlforeignbankch').val() || null,
            INTRATE_APPL: $('#ddlintrateappl').val() || null,
            REMARKS: $('#txtremarks').val() || null,
            TOT_AMT: parseFloat($('#Numnetamount').val()) || 0,
            ROI: 0,
            ROI_PERIOD: null,

            //========Bank Detail Tab===========
            BANK_CODE: parseInt($('#ddlourbank').val()) || 0,

            BENI_BANK: $('#ddlbeneficiarybank option:selected').text() || null,
            BENI_ACTNO: $('#Numbeneficiaryacno').val() || null,
            BENI_SWIFT: $('#Txtbeneficiaryshift').val() || null,
            BENI_ABA: $('#Txtbeneficiaryaba').val() || null,
            BENI_ROUT: $('#Txtbeneficiaryrouting').val() || null,
            BENI_SC: $('#NumbeneficiaryCode').val() || null,
            BENI_BANKADD: $('#Txtbeneficiarybankaddress').val() || null,

            CORR_BANK: $('#ddlcorrespondancebank').val() !== '' ? $('#ddlcorrespondancebank option:selected').text() : null,
            CORR_ACTNO: $('#Numcorrespondanceacno').val() || null,
            CORR_SWIFT: $('#Txtcorrespondanceshift').val() || null,
            CORR_ABA: $('#Txtcorrespondanceaba').val() || null,
            CORR_ROUT: $('#Txtcorrespondancerouting').val() || null,
            CORR_SC: $('#NumcorrespondanceCode').val() || null,
            CORR_BANKADD: $('#Txtcorrespondancebankaddress').val() || null,

            //========Customer Declaration tab===============
            SPFC_BANK: parseInt($('#ddlSPFCBank').val()) || null,
            SPFC_BANKNAME: $('#ddlSPFCBank option:selected').text() || null,
            CD_BILLREFNO: $('#txtbillreferenceno').val() || null,
            CD_CCY: $('#txtCCY').val() || null,
            CD_AMTREMITT: parseFloat($('#Numamountremitted').val()) || 0,
            CDFEMA_NC: $('#Chknotcoveredunderprohibited').is(':checked') ? 1 : 0,
            CDFEMA_RES: $('#Chkreceivedforimport').is(':checked') ? 1: 0 , 
            CD_ATTCH1: $('#cbCDAttach1').is(':checked') ? 1 : 0,
            CD_ATTCH2: $('#cbCDAttach2').is(':checked') ? 1 : 0,
            CD_ATTCH3: $('#cbCDAttach3').is(':checked') ? 1 : 0,
            CD_ATTCH4: $('#cbCDAttach4').is(':checked') ? 1 : 0,
            CD_ATTCH5: $('#cbCDAttach5').is(':checked') ? 1 : 0,
            CD_ATTCH6: $('#cbCDAttach6').is(':checked') ? 1 : 0,
            CD_ATTCH7: $('#cbCDAttach7').is(':checked') ? 1 : 0,
            CD_ATTCH8: $('#cbCDAttach8').is(':checked') ? 1 : 0,
            CD_ATTCH9: $('#cbCDAttach9').is(':checked') ? 1 : 0,
            OTHDOC_DETAILS: $('#txtotherdocumentdetails').val(),
            
            //============Foam A2 Tab========================
            A2_ISSUEDRAFT: $('#Chkissuedraft').is(':checked') ? 1 : 0,
            A2_FEREFFECT: $('#Chkeffectforeignexchange').is(':checked') ? 1 : 0,
            A2_BENIFICIARY: parseInt($('#hdnBeneficiaryCode').val()) || 0,
            A2_ACTNO: $('#NumAccountno').val(),
            A2_NAMEADD: $('#Txtnamebankaddress').val(),
            A2_ITFOR: $('#txtA2_3').val(),
            A2_FCNFOR: $('#txtA2_4').val(),
            A2_AMOUNT: parseFloat($('#Numamount').val()) || 0,
            A2_LRS: $('#ddlLRS').val(),
            A2_PC: $('#Txtpurposecode').val(),
            A2_DESC: $('#TxtDes').val(),
            A2_ISSUETRAVELLER: $('#Chkissuetravellerscheque').is(':checked') ? 1 : 0,
            A2_FCN: $('#Chkissueforeigncurrencynotes').is(':checked') ? 1 : 0,

            //==========Part B Info tab================
            ECB_LENDER: parseInt($('#hdnLenderCode').val()),
            ECB_NAMEADD: $('#textaddresslender').val(),

            ECB_NATURE1: $('#Chksuppliercredit').is(':checked') ? 1 : 0,
            ECB_NATURE2: $('#Chkbuyercredit').is(':checked') ? 1 : 0,
            ECB_NATURE3: $('#Chksyndicatedloan').is(':checked') ? 1 : 0,
            ECB_NATURE4: $('#Chkexportcredit').is(':checked') ? 1 : 0,
            ECB_NATURE5: $('#Chkloanforeigncollaboration').is(':checked') ? 1 : 0,
            ECB_NATURE6: $('#Chkfloatingratenotes').is(':checked') ? 1 : 0,
            ECB_NATURE7: $('#Chkfixedratebonds').is(':checked') ? 1 : 0,
            ECB_NATURE8: $('#Chklinecredit').is(':checked') ? 1 : 0,
            ECB_NATURE9: $('#ChkCommercialbankloan').is(':checked') ? 1 : 0,
            ECB_NATURE10: $('#ChkOthers').is(':checked') ? 1 : 0,

            ECB_ROI: $('#txtrateinterest').val() || null,
            ECB_UPFRONTFEE: parseFloat($('#txtupfrontfree').val()) || null,
            ECB_MGMTFEE: parseFloat($('#Txtmanagementfree').val()) || null,
            ECB_OTHCH: parseFloat($('#Txtothercharges').val()) || null,
            ECB_ALLINCOST: $('#txtallincost').val() || null,
            ECB_COMMITMENTFEE: parseFloat($('#txtcommitmentfree').val()) || null,
            ECB_PERIOD: $('#Txtperiodecb').val() || null,
            ECB_ROPI: parseFloat($('#Txtratepenalinterest').val()) || null,
            ECB_CALLPUT: $('#txtDetailscallput').val() || null,
            ECB_GRACE: $('#txtGrace').val() || null,
            ECB_REPAYTERM: $('#ddlrepaymentterms').val() || null,
            ECB_AVGMATURITY: $('#Txtaveragematurity').val() || null,
            ECB_NATUREOFSEC: $('#Txtnaturesecurity').val() || null,

            //============Part-C & D ====================
            PCD_DDMONTH: $('#Dtmonthyeardraw').val() || null,
            PCD_DDAMT: parseFloat($('#NumAmountdraw').val()) || 0,

            PCD_RPMONTH: $('#Dtmonthyearrepayment').val() || null,
            PCD_RPAMT: parseFloat($('#NumAmountrepayment').val()) || 0,
            
            PCD_IPMONTH: $('#Dtmonthyearinterest').val() || null,
            PCD_IPAMT: parseFloat($('#NumAmountinterest').val()) || 0,

            PCD_NAMELOC: $('#txtnamelocationproject').val() || null,
            PCD_TOTALCOST: parseFloat($('#txttotalcostproject').val()) || 0,
            PCD_PERCOST: parseFloat($('#txttotalecbproject').val()) || null,
            PCD_PIBANKAPPL: $('#ddlappraisedfinancial').val() || null,

            PCD_IS1: $('#Chkpower').is(':checked') ? 1 : 0,
            PCD_IS2: $('#Chktelecommunication').is(':checked') ? 1 : 0,
            PCD_IS3: $('#Chkrailways').is(':checked') ? 1 : 0,
            PCD_IS4: $('#Chkroadsbridges').is(':checked') ? 1 : 0,
            PCD_IS5: $('#Chkports').is(':checked') ? 1 : 0,
            PCD_IS6: $('#Chkindustrialparks').is(':checked') ? 1 : 0,
            PCD_IS7: $('#Chkurbaninfrastructure').is(':checked') ? 1 : 0,

            PCD_REQSA: $('#ddlclearancestaturity').val() || null,
            PCD_AUTHORITY: $('#txtnameauthority').val() || null,
            PCD_CLNO: $('#txtremarks').val() || null,
            PCD_CLDATE: $('#chkClearanceDate').is(':checked') ? $('#DtClearanceDate').val() : null,
            CLEARANCE_NO: $('#txtclearanceno').val() || null,

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
                PO_NO: parseInt(poNo) || null,
                PO_DATE: row.find('.podate').val() || null,

                INV_NO: row.find('.invoiceno').val() || null,
                INV_DATE: row.find('.invoicedate').val() || null,

                AMOUNT: parseFloat(row.find('.amount').val()) || 0,
                QTY: parseFloat(row.find('.quantity').val()) || 0,

                ITEM_CODE: parseInt(row.find('.itemname').val()) || null,
                ITEM_NAME: itemDDL.text().trim() || null,
                ITEM_DESC: row.find('.itemdesc').val() || null,

                HSN_CODE: row.find('.hsncode').val() || null,
                COUNTRY_ORIGIN: row.find('.country').val() || null,
                SHIPMENT_MODE: row.find('.shipmentmode').val() || null,

                SHIPMENT_DATE: row.find('.shipmentdate').val() || null,
                EXPECTED_DOD: row.find('.dispatchdate').val() || null,

                SHIPCOMP_CODE: parseInt(row.find('.shippingcompany').val()) || null,
                SHIPPING_COMP: shippingDDL.text().trim() || null,

                POD_CODE: row.find('.portdispatch').val() || null,
                POD: podDDL.text().trim() || null,
                
                DEST_PORTCODE: row.find('.destinationport').val() || null,
                DEST_PORT: destinationDDL.text().trim() || null,

                BL_NO: row.find('.blno').val() || null,
                BL_DATE: row.find('.bldate').val() || null,
                BE_NO: row.find('.beno').val() || null,
                BE_DATE: row.find('.bedate').val() || null,

                BE_CCYNO: row.find('.beccy').val() || null,

                BE_AMT: parseFloat(row.find('.beamount').val()) || 0,
                BE_UTIAMT: parseFloat(row.find('.beutilized').val()) || 0,
                FOB_VALUE: parseFloat(row.find('.fobvalue').val()) || 0,

                AD_CODE: null,
                PORT_CODE: null
            };

            footerDetails.push(footer);
        });

        const saveData = {
            Header: header,
            Footer: footerDetails
        };

        console.log("Save", saveData);

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

            if (result.success) {
                showToast(result.message, { type: "success" });
                isReadOnly = true;
                setFormReadOnly();
            }
            else {
                showToast(result.message || "Error while saving", { type: "error" });
            }
        }
        catch (error)
        {
            showToast(error.message || 'Unable to save Import Payment Entry.',{ type: "error" });
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
    $('#ddlParty').on('change', async function () {

        const partyCode = $(this).val();

        if (!partyCode) return;

        console.log("Selected Party Code:", partyCode);

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

        calculateNetAmount();
    });

    //==============================================
    // Fill Data On Change Of PO No
    //==============================================
    $('#tblImportPaymentEntry').on('change', '.ddlPoNo', function () {

        const row = $(this).closest('tr');
        const po = poList.find(x =>
            `${x.vType}${x.vNo}` == $(this).val()
        );
        const itemDDL = row.find('.itemname');

        if (!po) return;

        row.find('.podate').val(formatDate(po.saudaDate));
        row.find('.invoiceno').val(po.supplierInvNo);
        row.find('.invoicedate').val(formatDate(po.supplierInvDate));
        row.find('.amount').val(po.supplierInvAmt);
        row.find('.quantity').val(po.qty);

        itemDDL.val(po.itemCode).trigger('change');

        row.find('.country').val(String(po.originCountry || '')).trigger('change');
        row.find('.shipmentmode').val(po.mode);
        row.find('.dispatchdate').val(formatDate(po.etd));
        row.find('.destinationport').val(po.destinationPort);
        row.find('.blno').val(po.blNo);
        row.find('.bldate').val(formatDate(po.blDate));
        row.find('.beno').val(po.beNo);
        row.find('.bedate').val(formatDate(po.beDate));

        calculateNetAmount();

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
    
    //==========================================
    // Select Current Date on Checkbox Tick
    //==========================================
    $('#chkClearanceDate').on('change', function () {

        if (this.checked) {
            const today = new Date().toISOString().split('T')[0];
            $('#DtClearanceDate').val(today);
        } else {
            $('#DtClearanceDate').val('');
        }

    });
}

function SetCurrentDate() {

    const today = new Date().toISOString().split("T")[0];
    $("#Dtdocdate").val(today);

}

async function BindAllDropdown() {
    await Promise.all([

        //bindDropdownNew('ImportPaymentEntry', 'SupplierName', '#txtSupplierName', '-- Select Supplier --'),

        bindDropdown('ImportPaymentEntry', 'PartyDetail', '#ddlParty', '-- Select Party --', null, null, false, null, true),

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

        if (id) {

            if (vtype) {
                ddl.val(vtype);
            }
            $('#ddldoctype').prop('disabled', true);
        }
        else {
            await GetVNo();
        }

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

    const partyCode = $('#ddlParty').val();
   
    if (!partyCode) return;

    try {

        const response = await fetch(`/ImportPaymentEntry/GetPartyDetailsForPartB?partyCode=${partyCode}`);

        if (!response.ok) {
            throw new Error("Failed to load Party Details.");
        }

        const data = await response.json();
        $('#ddlLender').val(data.name);
        $('#textaddresslender').val(data.address);

    }
    catch (error) {
        console.error(error);
    }
}

async function LoadEditData(docId) {

    try {

        if (!docId) {
            console.warn("DOC_ID is missing.");
            return;
        }

        const response = await fetch(
            `/ImportPaymentEntry/LoadEditData?docId=${encodeURIComponent(docId)}`
        );

        if (!response.ok) {
            throw new Error("Failed to load edit data.");
        }

        const result = await response.json();

        if (!result.success) {

            showToast(
                result.message || "Unable to load edit data.",
                { type: "error" }
            );

            return;
        }

        // -------------
        // HEADER
        // -------------

        if (result.header) {
            await BindEditHeader(result.header);
        }

        // --------------
        // FOOTER
        // --------------

        if (result.footer && result.footer.length > 0) {

            $('#tblImportPaymentEntry tbody').empty();

            for (const footerRow of result.footer) {

                addNewRow();

                await BindEditFooterRow(
                    $('#tblImportPaymentEntry tbody tr:last'),
                    footerRow
                );
            }

        }
        else {

            if ($("#tblImportPaymentEntry tbody tr").length === 0) {
                addNewRow();
            }
        }

        calculateNetAmount();

    }
    catch (error) {

        console.error("LoadEditData Error:", error);

        showToast("Failed to load edit data.",{ type: "error" });
    }
}

async function BindEditHeader(header) {

    console.log("Edit Header:", header);

    // =====================
    // Main Tab
    // ====================

    $('#ddldoctype').val(header.V_TYPE);
    $('#Numdocno').val(header.V_NO);

    $('#Dtdocdate').val(
        formatDateForInput(header.V_DATE)
    );

    $('#ddlParty').val(header.PARTY_CODE).trigger('change');

    $('#Txtdocevidence').val(header.DOC_EVEDENCE);
    $('#ddlnatureproject').val(header.ECB_PURPOSE);
    $('#ddlpaytype').val(header.PAY_TYPE);
    $('#ddlimport').val(header.IMPORT_CAT);
    $('#ddlimportremitt').val(header.IMPORT_REMIT);
    setDropdownByText('#ddlcurrency', header.CURRENCY);
    $('#ddlforeignbankch').val(header.FOREIGN_BANKCHARGE);
    $('#ddlintrateappl').val(header.INTRATE_APPL);

    $('#txtremarks').val(header.REMARKS);

    $('#Numnetamount').val(
        header.TOT_AMT ?? 0
    );

    if (header.PARTY_CODE) {

        $('#ddlParty').val(header.PARTY_CODE);

        await GetPartyDetails(header.PARTY_CODE);

        await FillPartyDetailsInPartB();

        await LoadPODropdown(header.PARTY_CODE);
    }

    // ==========================
    // Bank Detail Tab
    // ==========================

    $('#ddlourbank').val(header.BANK_CODE);

    setDropdownByText('#ddlbeneficiarybank', header.BENI_BANK);
    $('#Numbeneficiaryacno').val(header.BENI_ACTNO);
    $('#Txtbeneficiaryshift').val(header.BENI_SWIFT);
    $('#Txtbeneficiaryaba').val(header.BENI_ABA);
    $('#Txtbeneficiaryrouting').val(header.BENI_ROUT);
    $('#NumbeneficiaryCode').val(header.BENI_SC);
    $('#Txtbeneficiarybankaddress').val(header.BENI_BANKADD);

    setDropdownByText('#ddlcorrespondancebank', header.CORR_BANK);
    $('#Numcorrespondanceacno').val(header.CORR_ACTNO);
    $('#Txtcorrespondanceshift').val(header.CORR_SWIFT);
    $('#Txtcorrespondanceaba').val(header.CORR_ABA);
    $('#Txtcorrespondancerouting').val(header.CORR_ROUT);
    $('#NumcorrespondanceCode').val(header.CORR_SC);
    $('#Txtcorrespondancebankaddress').val(header.CORR_BANKADD);

    // ============================
    // Customer Declaration
    // ============================

    $('#ddlSPFCBank').val(header.SPFC_BANK).trigger('change');

    $('#txtbillreferenceno').val(header.CD_BILLREFNO);
    $('#txtCCY').val(header.CD_CCY);

    $('#Numamountremitted').val(
        header.CD_AMTREMITT ?? 0
    );

    $('#Chknotcoveredunderprohibited').prop('checked', Number(header.CDFEMA_NC) === 1);
    $('#Chkreceivedforimport').prop('checked', Number(header.CDFEMA_RES) === 1);
    $('#cbCDAttach1').prop('checked', Number(header.CD_ATTCH1) === 1);
    $('#cbCDAttach2').prop('checked', Number(header.CD_ATTCH2) === 1);
    $('#cbCDAttach3').prop('checked', Number(header.CD_ATTCH3) === 1);
    $('#cbCDAttach4').prop('checked', Number(header.CD_ATTCH4) === 1);
    $('#cbCDAttach5').prop('checked', Number(header.CD_ATTCH5) === 1);
    $('#cbCDAttach6').prop('checked', Number(header.CD_ATTCH6) === 1);
    $('#cbCDAttach7').prop('checked', Number(header.CD_ATTCH7) === 1);
    $('#cbCDAttach8').prop('checked', Number(header.CD_ATTCH8) === 1);
    $('#cbCDAttach9').prop('checked', Number(header.CD_ATTCH9) === 1);
    $('#txtotherdocumentdetails').val(
        header.OTHDOC_DETAILS
    );

    // ===================
    // Form A2
    // ==================

    $('#Chkissuedraft').prop('checked', Number(header.A2_ISSUEDRAFT) === 1);
    $('#Chkeffectforeignexchange').prop('checked', Number(header.A2_FEREFFECT) === 1);
    $('#hdnBeneficiaryCode').val(header.A2_BENIFICIARY);
    $('#NumAccountno').val(header.A2_ACTNO);
    $('#Txtnamebankaddress').val(header.A2_NAMEADD);
    $('#txtA2_3').val(header.A2_ITFOR);
    $('#txtA2_4').val(header.A2_FCNFOR);
    $('#Numamount').val(header.A2_AMOUNT);
    $('#ddlLRS').val(header.A2_LRS);
    $('#Txtpurposecode').val(header.A2_PC);
    $('#TxtDes').val(header.A2_DESC);
    $('#Chkissuetravellerscheque').prop('checked', Number(header.A2_ISSUETRAVELLER) === 1);
    $('#Chkissueforeigncurrencynotes').prop('checked', Number(header.A2_FCN) === 1);

    // =====================
    // Part B
    // =====================

    $('#hdnLenderCode').val(header.ECB_LENDER);
    $('#textaddresslender').val(header.ECB_NAMEADD);
    $('#Chksuppliercredit').prop('checked', Number(header.ECB_NATURE1) === 1);
    $('#Chkbuyercredit').prop('checked', Number(header.ECB_NATURE2) === 1);
    $('#Chksyndicatedloan').prop('checked', Number(header.ECB_NATURE3) === 1);
    $('#Chkexportcredit').prop('checked', Number(header.ECB_NATURE4) === 1);
    $('#Chkloanforeigncollaboration').prop('checked', Number(header.ECB_NATURE5) === 1);
    $('#Chkfloatingratenotes').prop('checked', Number(header.ECB_NATURE6) === 1);
    $('#Chkfixedratebonds').prop('checked', Number(header.ECB_NATURE7) === 1);
    $('#Chklinecredit').prop('checked', Number(header.ECB_NATURE8) === 1);
    $('#ChkCommercialbankloan').prop('checked', Number(header.ECB_NATURE9) === 1);
    $('#ChkOthers').prop('checked', Number(header.ECB_NATURE10) === 1);
    $('#txtrateinterest').val(header.ECB_ROI);
    $('#txtupfrontfree').val(header.ECB_UPFRONTFEE);
    $('#Txtmanagementfree').val(header.ECB_MGMTFEE);
    $('#Txtothercharges').val(header.ECB_OTHCH);
    $('#txtallincost').val(header.ECB_ALLINCOST);
    $('#Txtperiodecb').val(header.ECB_PERIOD);
    $('#Txtratepenalinterest').val(header.ECB_ROPI);
    $('#txtDetailscallput').val(header.ECB_CALLPUT);
    $('#txtGrace').val(header.ECB_GRACE);
    $('#ddlrepaymentterms').val(header.ECB_REPAYTERM);
    $('#Txtaveragematurity').val(header.ECB_AVGMATURITY);
    $('#Txtnaturesecurity').val(header.ECB_NATUREOFSEC);
    $('#txtcommitmentfree').val(header.ECB_COMMITMENTFEE);

    // ====================
    // Part C & D
    // ====================

    $('#Dtmonthyeardraw').val(
        formatDateForInput(header.PCD_DDMONTH)
    );

    $('#NumAmountdraw').val(header.PCD_DDAMT);

    $('#Dtmonthyearrepayment').val(
        formatDateForInput(header.PCD_RPMONTH)
    );

    $('#NumAmountrepayment').val(header.PCD_RPAMT);

    $('#Dtmonthyearinterest').val(
        formatDateForInput(header.PCD_IPMONTH)
    );

    $('#NumAmountinterest').val(header.PCD_IPAMT);
    $('#txtnamelocationproject').val(header.PCD_NAMELOC);
    $('#txttotalcostproject').val(header.PCD_TOTALCOST);
    $('#txttotalecbproject').val(header.PCD_PERCOST);
    $('#ddlappraisedfinancial').val(header.PCD_PIBANKAPPL);
    $('#Chkpower').prop('checked', Number(header.PCD_IS1) === 1);
    $('#Chktelecommunication').prop('checked', Number(header.PCD_IS2) === 1);
    $('#Chkrailways').prop('checked', Number(header.PCD_IS3) === 1);
    $('#Chkroadsbridges').prop('checked', Number(header.PCD_IS4) === 1);
    $('#Chkports').prop('checked', Number(header.PCD_IS5) === 1);
    $('#Chkindustrialparks').prop('checked', Number(header.PCD_IS6) === 1);
    $('#Chkurbaninfrastructure').prop('checked', Number(header.PCD_IS7) === 1);
    $('#ddlclearancestaturity').val(header.PCD_REQSA);
    $('#txtnameauthority').val(header.PCD_AUTHORITY);
    $('#txtremarks').val(header.PCD_CLNO);
    $('#DtClearanceDate').val(
        formatDateForInput(header.PCD_CLDATE)
    );
    $('#txtclearanceno').val(header.CLEARANCE_NO);

    // ==========================================
    // Trigger dependent dropdown events
    // ==========================================

    $('#ddlourbank').trigger('change');
    $('#ddlcorrespondancebank').trigger('change');
}

async function BindEditFooterRow(row, footer) {

    console.log("Footer Row:", footer);

    row.find('.code').val(footer.SNO);
    const poValue = `${footer.PO_TYPE}${footer.PO_NO}`;

    row.find('.ddlPoNo').val(poValue).trigger('change');

    row.find('.podate').val(formatDateForInput(footer.PO_DATE));
    row.find('.invoiceno').val(footer.INV_NO);
    row.find('.invoicedate').val(formatDateForInput(footer.INV_DATE));

    row.find('.amount').val(footer.AMOUNT);
    row.find('.quantity').val(footer.QTY);
    row.find('.itemname').val(footer.ITEM_CODE);
    row.find('.itemdesc').val(footer.ITEM_DESC);
    row.find('.hsncode').val(footer.HSN_CODE);

    row.find('.country').val(footer.COUNTRY_ORIGIN);

    row.find('.shipmentmode').val(footer.SHIPMENT_MODE);

    row.find('.shipmentdate').val(formatDateForInput(footer.SHIPMENT_DATE));

    row.find('.dispatchdate').val(formatDateForInput(footer.EXPECTED_DOD));

    row.find('.shippingcompany').val(String(footer.SHIPCOMP_CODE || '')).trigger('change');

    row.find('.portdispatch').val(String(footer.POD_CODE || '')).trigger('change');
    
    row.find('.destinationport').val(String(footer.DEST_PORTCODE || '')).trigger('change');

    row.find('.blno').val(footer.BL_NO);

    row.find('.bldate').val(formatDateForInput(footer.BL_DATE));

    row.find('.beno').val(footer.BE_NO);

    row.find('.bedate').val(formatDateForInput(footer.BE_DATE));

    row.find('.beccy').val(footer.BE_CCYNO);

    row.find('.beamount').val(footer.BE_AMT);

    row.find('.beutilized').val(footer.BE_UTIAMT);

    row.find('.fobvalue').val(footer.FOB_VALUE);
}

//================================================
//     Footer Table
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
                <input type="text" class="erppagetable-control beccy">
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

    const lastRow = tbody.find("tr:last");

    const ddl = lastRow.find(".ddlPoNo");
    const itemDDL = lastRow.find(".itemname");
    const countryDDL = lastRow.find(".country");
    const shippingCompanyDDL = lastRow.find(".shippingcompany");
    const portDispatchDDL = lastRow.find(".portdispatch");
    const destinationPortDDL = lastRow.find(".destinationport");

    // =====================
    // PO
    // =====================

    ddl.empty().append('<option value=""> Select PO No </option>');

    $.each(poList, function (_, item) {

        ddl.append(`
            <option value="${item.vType}${item.vNo}">
                ${item.vType}${item.vNo}
            </option>
        `);

    });

    // =====================
    // Item
    // =====================

    itemDDL.empty().append('<option value=""> Select Item </option>');

    $.each(itemList, function (_, item) {

        itemDDL.append(
            `<option value="${item.code}">${item.name}</option>`
        );

    });

    // =====================
    // Country
    // =====================

    countryDDL.empty().append(
        '<option value=""> Select Country </option>'
    );

    $.each(countryList, function (_, item) {

        countryDDL.append(
            `<option value="${item.value}">${item.text}</option>`
        );

    });

    // =====================
    // Shipping Company
    // =====================

    shippingCompanyDDL.empty().append(
        '<option value=""> Select Shipping Company </option>'
    );

    $.each(PartyList, function (_, item) {

        shippingCompanyDDL.append(
            `<option value="${item.value}">${item.text}</option>`
        );

    });

    // =====================
    // Port Of Dispatch
    // =====================

    portDispatchDDL.empty().append(
        '<option value=""> Select Port Of Dispatch </option>'
    );

    $.each(PortOfDispatch, function (_, item) {

        portDispatchDDL.append(
            `<option value="${item.value}">${item.text}</option>`
        );

    });

    // =====================
    // Destination Port
    // =====================

    destinationPortDDL.empty().append(
        '<option value=""> Select Destination Port </option>'
    );

    $.each(DestinationPort, function (_, item) {

        destinationPortDDL.append(
            `<option value="${item.value}">${item.text}</option>`
        );

    });
}

async function LoadPODropdown(partyCode) {

    try {

        const response = await fetch(`/ImportPaymentEntry/GetItemMaster?partyCode=${partyCode}`);

        if (!response.ok) {
            throw new Error("Failed to load PO.");
        }

        poList = await response.json();

        $('.ddlPoNo').each(function () {

            const ddl = $(this);

            if (ddl.hasClass("select2-hidden-accessible")) {
                ddl.select2('destroy');
            }

            ddl.empty().append('<option value="">Select PO No</option>');

            $.each(poList, function (_, item) {

                ddl.append(`
                    <option value="${item.vType}${item.vNo}">
                        ${item.vType}${item.vNo}
                    </option>
                `);

            });

            ddl.select2({
                width: '100%',
                placeholder: 'Select PO No',
                allowClear: true
            });

            ddl.on('select2:open', function () {
                setTimeout(function () {
                    document.querySelector('.select2-container--open .select2-search__field')?.focus();
                }, 0);
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

        if (ddl.hasClass("select2-hidden-accessible")) {
            ddl.select2('destroy');
        }

        ddl.empty().append('<option value="">Select Item</option>');

        $.each(itemList, function (_, item) {
            ddl.append(`
                <option value="${item.code}">
                    ${item.name}
                </option>
            `);
        });

        // Initialize Select2
        ddl.select2({
            width: '100%',
            placeholder: 'Select Item',
            allowClear: true
        });

        // Auto focus cursor in search box
        ddl.on('select2:open', function () {
            setTimeout(function () {
                $('.select2-container--open .select2-search__field').focus();
            }, 0);
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

        ddl.select2({
            width: '100%',
            placeholder: 'Select Country',
            allowClear: true
        });

        ddl.on('select2:open', function () {
            setTimeout(function () {
                $('.select2-container--open .select2-search__field').focus();
            }, 0);
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

        ddl.select2({
            width: '100%',
            placeholder: 'Select Shipping Company',
            allowClear: true
        });

        ddl.on('select2:open', function () {
            setTimeout(function () {
                $('.select2-container--open .select2-search__field').focus();
            }, 0);
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

        ddl.select2({
            width: '100%',
            placeholder: 'Select Port Dispatch',
            allowClear: true
        });

        ddl.on('select2:open', function () {
            setTimeout(function () {
                $('.select2-container--open .select2-search__field').focus();
            }, 0);
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

        ddl.select2({
            width: '100%',
            placeholder: 'Select Destination Port',
            allowClear: true
        });

        ddl.on('select2:open', function () {
            setTimeout(function () {
                $('.select2-container--open .select2-search__field').focus();
            }, 0);
        });

    });
}

function formatDate(date) {

    if (!date) return "";

    const parts = date.split("/"); 

    return `${parts[2]}-${parts[1]}-${parts[0]}`;
}

function formatDateForInput(date) {

    if (!date) return "";

    const value = String(date);

    if (/^\d{4}-\d{2}-\d{2}/.test(value)) {
        return value.substring(0, 10);
    }

    if (/^\d{2}\/\d{2}\/\d{4}$/.test(value)) {

        const parts = value.split("/");

        return `${parts[2]}-${parts[1]}-${parts[0]}`;
    }

    const parsedDate = new Date(value);

    if (!isNaN(parsedDate.getTime())) {

        return parsedDate.toISOString().split("T")[0];
    }

    return "";
}
              
function calculateNetAmount() {

    let total = 0;

    $('#tblImportPaymentEntry tbody .amount').each(function () {
        total += parseFloat($(this).val()) || 0;
    });
    $('#Numnetamount').val(total);
}

function setDropdownByText(selector, text) {
    const ddl = $(selector);

    const option = ddl.find('option').filter(function () {
        return $.trim($(this).text()).toLowerCase() ===
            $.trim(String(text)).toLowerCase();
    }).first();

    if (option.length) {
        ddl.val(option.val()).trigger('change');
    }
}

function setFormReadOnly() {

    const page = $('.erppage-fieldset');

    page.addClass('erppage-readonly');

    page.find('input, textarea') .prop('readonly', true);

    page.find('input[type="checkbox"], input[type="radio"]').prop('disabled', true);

    page.find('select').prop('disabled', true);

    page.find('select.select2-hidden-accessible').each(function () {

        const select = $(this);

        select.prop('disabled', true);

        select.next('.select2-container').addClass('select2-readonly');
    });

    const table = $('#tblImportPaymentEntry');

    table.find('input').prop('readonly', true);

    table.find('select').prop('disabled', true);

    table.find('.add, .delete').prop('disabled', true);

    $('#btnSave').prop('disabled', true).hide();

}

//=======================
//  Reports
//=======================

async function RequestLetterReport() {

    var reportName = "IMPORT_BANK1";

    var vType = $('#ddldoctype').val();
    var vNo = $('#Numdocno').val();

    if (!vType || !vNo) {
        showToast("V Type and V No are required.", {
            type: "error"
        });
        return;
    }

    var SelForMul =
        "{IMPORT_PAY1.V_TYPE}='" + vType + "'" +
        " AND {IMPORT_PAY1.V_NO}=" + vNo +
        " AND {IMPORT_PAY1.COMP_CODE}=" + window.globalVariables.compCode +
        " AND {IMPORT_PAY1.YEAR_CODE}=" + window.globalVariables.yearCode +
        " AND {IMPORT_PAY1.BRANCH_CODE}=" + window.globalVariables.branchCode;

    var formulaFields = {

        Reportname: reportName,

        selectionFormula: SelForMul,

        Database: window.database.db,

        Parameters: {
            RPTNAME: "REQUEST LETTER FOR PAYMENT",
            comp_name: window.globalVariables.companyName,
            comp_add1: window.globalVariables.add1,
            comp_add2: window.globalVariables.add2,
            reg_add1: "Regd. Office:" + (window.globalVariables.regAdd1 || "") + (window.globalVariables.regAdd2 || ""),
            comp_email: "Email :" + (window.globalVariables.email || ""),
            comp_cin: "CIN:" +(window.globalVariables.cin || "")
        }
    };

    console.log("Request Letter Report Data:", formulaFields);

    var now = new Date();

    var day = String(now.getDate()).padStart(2, '0');
    var month = String(now.getMonth() + 1).padStart(2, '0');
    var year = String(now.getFullYear()).slice(-2);

    var hours = String(now.getHours()).padStart(2, '0');
    var minutes = String(now.getMinutes()).padStart(2, '0');
    var seconds = String(now.getSeconds()).padStart(2, '0');

    var timestamp =`${day}${month}${year}_${hours}${minutes}${seconds}`;

    $.ajax({
        url: 'http://localhost:34089/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(formulaFields),
        contentType: "application/json",
        xhrFields: {
            responseType: 'blob'
        },
        success: function (response) {
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
            console.error('Error generating report:', error);
        }
    });
}

async function PaymentRequestForm() {

    var reportName = "IMPORT_BANK2";

    var vType = $('#ddldoctype').val();
    var vNo = $('#Numdocno').val();

    if (!vType || !vNo) {
        showToast("V Type and V No are required.", {
            type: "error"
        });
        return;
    }

    var SelForMul =
        "{IMPORT_PAY1.V_TYPE}='" + vType + "'" +
        " AND {IMPORT_PAY1.V_NO}=" + vNo +
        " AND {IMPORT_PAY1.COMP_CODE}=" + window.globalVariables.compCode +
        " AND {IMPORT_PAY1.YEAR_CODE}=" + window.globalVariables.yearCode +
        " AND {IMPORT_PAY1.BRANCH_CODE}=" + window.globalVariables.branchCode;

    var formulaFields = {

        Reportname: reportName,

        selectionFormula: SelForMul,

        Database: window.database.db,

        Parameters: {
            RPTNAME: "REQUEST LETTER FOR PAYMENT",
            comp_name: window.globalVariables.companyName,
            comp_add1: window.globalVariables.add1,
            comp_add2: window.globalVariables.add2,
            reg_add1: "Regd. Office:" + (window.globalVariables.regAdd1 || "") + (window.globalVariables.regAdd2 || ""),
            comp_email: "Email :" + (window.globalVariables.email || ""),
            comp_cin: "CIN:" + (window.globalVariables.cin || "")
        }
    };

    console.log("Request Letter Report Data:", formulaFields);

    var now = new Date();

    var day = String(now.getDate()).padStart(2, '0');
    var month = String(now.getMonth() + 1).padStart(2, '0');
    var year = String(now.getFullYear()).slice(-2);

    var hours = String(now.getHours()).padStart(2, '0');
    var minutes = String(now.getMinutes()).padStart(2, '0');
    var seconds = String(now.getSeconds()).padStart(2, '0');

    var timestamp = `${day}${month}${year}_${hours}${minutes}${seconds}`;

    $.ajax({
        url: 'http://localhost:34089/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(formulaFields),
        contentType: "application/json",
        xhrFields: {
            responseType: 'blob'
        },
        success: function (response) {
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
            console.error('Error generating report:', error);
        }
    });
}

async function SupplierCredit() {

    var reportName = "IMPORT_BANK3";

    var vType = $('#ddldoctype').val();
    var vNo = $('#Numdocno').val();

    if (!vType || !vNo) {
        showToast("V Type and V No are required.", {
            type: "error"
        });
        return;
    }

    var SelForMul =
        "{IMPORT_PAY1.V_TYPE}='" + vType + "'" +
        " AND {IMPORT_PAY1.V_NO}=" + vNo +
        " AND {IMPORT_PAY1.COMP_CODE}=" + window.globalVariables.compCode +
        " AND {IMPORT_PAY1.YEAR_CODE}=" + window.globalVariables.yearCode +
        " AND {IMPORT_PAY1.BRANCH_CODE}=" + window.globalVariables.branchCode;

    var formulaFields = {

        Reportname: reportName,

        selectionFormula: SelForMul,

        Database: window.database.db,

        Parameters: {
            RPTNAME: "REQUEST LETTER FOR PAYMENT",
            comp_name: window.globalVariables.companyName,
            comp_add1: window.globalVariables.add1,
            comp_add2: window.globalVariables.add2,
            reg_add1: "Regd. Office:" + (window.globalVariables.regAdd1 || "") + (window.globalVariables.regAdd2 || ""),
            comp_email: "Email :" + (window.globalVariables.email || ""),
            comp_cin: "CIN:" + (window.globalVariables.cin || "")
        }
    };

    console.log("Request Letter Report Data:", formulaFields);

    var now = new Date();

    var day = String(now.getDate()).padStart(2, '0');
    var month = String(now.getMonth() + 1).padStart(2, '0');
    var year = String(now.getFullYear()).slice(-2);

    var hours = String(now.getHours()).padStart(2, '0');
    var minutes = String(now.getMinutes()).padStart(2, '0');
    var seconds = String(now.getSeconds()).padStart(2, '0');

    var timestamp = `${day}${month}${year}_${hours}${minutes}${seconds}`;

    $.ajax({
        url: 'http://localhost:34089/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(formulaFields),
        contentType: "application/json",
        xhrFields: {
            responseType: 'blob'
        },
        success: function (response) {
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
            console.error('Error generating report:', error);
        }
    });
}

async function FormA2() {

    var reportName = "IMPORT_BANK4_A2";

    var vType = $('#ddldoctype').val();
    var vNo = $('#Numdocno').val();

    if (!vType || !vNo) {
        showToast("V Type and V No are required.", {
            type: "error"
        });
        return;
    }

    var SelForMul =
        "{IMPORT_PAY1.V_TYPE}='" + vType + "'" +
        " AND {IMPORT_PAY1.V_NO}=" + vNo +
        " AND {IMPORT_PAY1.COMP_CODE}=" + window.globalVariables.compCode +
        " AND {IMPORT_PAY1.YEAR_CODE}=" + window.globalVariables.yearCode +
        " AND {IMPORT_PAY1.BRANCH_CODE}=" + window.globalVariables.branchCode;

    var formulaFields = {

        Reportname: reportName,

        selectionFormula: SelForMul,

        Database: window.database.db,

        Parameters: {
            RPTNAME: "REQUEST LETTER FOR PAYMENT",
            comp_name: window.globalVariables.companyName,
            comp_add1: window.globalVariables.add1,
            comp_add2: window.globalVariables.add2,
            reg_add1: "Regd. Office:" + (window.globalVariables.regAdd1 || "") + (window.globalVariables.regAdd2 || ""),
            comp_email: "Email :" + (window.globalVariables.email || ""),
            comp_cin: "CIN:" + (window.globalVariables.cin || "")
        }
    };

    console.log("Request Letter Report Data:", formulaFields);

    var now = new Date();

    var day = String(now.getDate()).padStart(2, '0');
    var month = String(now.getMonth() + 1).padStart(2, '0');
    var year = String(now.getFullYear()).slice(-2);

    var hours = String(now.getHours()).padStart(2, '0');
    var minutes = String(now.getMinutes()).padStart(2, '0');
    var seconds = String(now.getSeconds()).padStart(2, '0');

    var timestamp = `${day}${month}${year}_${hours}${minutes}${seconds}`;

    $.ajax({
        url: 'http://localhost:34089/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(formulaFields),
        contentType: "application/json",
        xhrFields: {
            responseType: 'blob'
        },
        success: function (response) {
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
            console.error('Error generating report:', error);
        }
    });
}

async function PartB() {

    var reportName = "IMPORT_BANK5_ECB";

    var vType = $('#ddldoctype').val();
    var vNo = $('#Numdocno').val();

    if (!vType || !vNo) {
        showToast("V Type and V No are required.", {
            type: "error"
        });
        return;
    }

    var SelForMul =
        "{IMPORT_PAY1.V_TYPE}='" + vType + "'" +
        " AND {IMPORT_PAY1.V_NO}=" + vNo +
        " AND {IMPORT_PAY1.COMP_CODE}=" + window.globalVariables.compCode +
        " AND {IMPORT_PAY1.YEAR_CODE}=" + window.globalVariables.yearCode +
        " AND {IMPORT_PAY1.BRANCH_CODE}=" + window.globalVariables.branchCode;

    var formulaFields = {

        Reportname: reportName,

        selectionFormula: SelForMul,

        Database: window.database.db,

        Parameters: {
            RPTNAME: "REQUEST LETTER FOR PAYMENT",
            comp_name: window.globalVariables.companyName,
            comp_add1: window.globalVariables.add1,
            comp_add2: window.globalVariables.add2,
            reg_add1: "Regd. Office:" + (window.globalVariables.regAdd1 || "") + (window.globalVariables.regAdd2 || ""),
            comp_email: "Email :" + (window.globalVariables.email || ""),
            comp_cin: "CIN:" + (window.globalVariables.cin || "")
        }
    };

    console.log("Request Letter Report Data:", formulaFields);

    var now = new Date();

    var day = String(now.getDate()).padStart(2, '0');
    var month = String(now.getMonth() + 1).padStart(2, '0');
    var year = String(now.getFullYear()).slice(-2);

    var hours = String(now.getHours()).padStart(2, '0');
    var minutes = String(now.getMinutes()).padStart(2, '0');
    var seconds = String(now.getSeconds()).padStart(2, '0');

    var timestamp = `${day}${month}${year}_${hours}${minutes}${seconds}`;

    $.ajax({
        url: 'http://localhost:34089/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(formulaFields),
        contentType: "application/json",
        xhrFields: {
            responseType: 'blob'
        },
        success: function (response) {
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
            console.error('Error generating report:', error);
        }
    });
}

async function PaymentEcb() {

    var reportName = "IMPORT_BANK6_ECB";

    var vType = $('#ddldoctype').val();
    var vNo = $('#Numdocno').val();

    if (!vType || !vNo) {
        showToast("V Type and V No are required.", {
            type: "error"
        });
        return;
    }

    var SelForMul =
        "{IMPORT_PAY1.V_TYPE}='" + vType + "'" +
        " AND {IMPORT_PAY1.V_NO}=" + vNo +
        " AND {IMPORT_PAY1.COMP_CODE}=" + window.globalVariables.compCode +
        " AND {IMPORT_PAY1.YEAR_CODE}=" + window.globalVariables.yearCode +
        " AND {IMPORT_PAY1.BRANCH_CODE}=" + window.globalVariables.branchCode;

    var formulaFields = {

        Reportname: reportName,

        selectionFormula: SelForMul,

        Database: window.database.db,

        Parameters: {
            RPTNAME: "REQUEST LETTER FOR PAYMENT",
            comp_name: window.globalVariables.companyName,
            comp_add1: window.globalVariables.add1,
            comp_add2: window.globalVariables.add2,
            reg_add1: "Regd. Office:" + (window.globalVariables.regAdd1 || "") + (window.globalVariables.regAdd2 || ""),
            comp_email: "Email :" + (window.globalVariables.email || ""),
            comp_cin: "CIN:" + (window.globalVariables.cin || "")
        }
    };

    console.log("Request Letter Report Data:", formulaFields);

    var now = new Date();

    var day = String(now.getDate()).padStart(2, '0');
    var month = String(now.getMonth() + 1).padStart(2, '0');
    var year = String(now.getFullYear()).slice(-2);

    var hours = String(now.getHours()).padStart(2, '0');
    var minutes = String(now.getMinutes()).padStart(2, '0');
    var seconds = String(now.getSeconds()).padStart(2, '0');

    var timestamp = `${day}${month}${year}_${hours}${minutes}${seconds}`;

    $.ajax({
        url: 'http://localhost:34089/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(formulaFields),
        contentType: "application/json",
        xhrFields: {
            responseType: 'blob'
        },
        success: function (response) {
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
            console.error('Error generating report:', error);
        }
    });
}

async function BuyerCredit1() {

    var reportName = "IMPORT_BANK7_LC";

    var vType = $('#ddldoctype').val();
    var vNo = $('#Numdocno').val();

    if (!vType || !vNo) {
        showToast("V Type and V No are required.", {
            type: "error"
        });
        return;
    }

    var SelForMul =
        "{IMPORT_PAY1.V_TYPE}='" + vType + "'" +
        " AND {IMPORT_PAY1.V_NO}=" + vNo +
        " AND {IMPORT_PAY1.COMP_CODE}=" + window.globalVariables.compCode +
        " AND {IMPORT_PAY1.YEAR_CODE}=" + window.globalVariables.yearCode +
        " AND {IMPORT_PAY1.BRANCH_CODE}=" + window.globalVariables.branchCode;

    var formulaFields = {

        Reportname: reportName,

        selectionFormula: SelForMul,

        Database: window.database.db,

        Parameters: {
            RPTNAME: "REQUEST LETTER FOR PAYMENT",
            comp_name: window.globalVariables.companyName,
            comp_add1: window.globalVariables.add1,
            comp_add2: window.globalVariables.add2,
            reg_add1: "Regd. Office:" + (window.globalVariables.regAdd1 || "") + (window.globalVariables.regAdd2 || ""),
            comp_email: "Email :" + (window.globalVariables.email || ""),
            comp_cin: "CIN:" + (window.globalVariables.cin || "")
        }
    };

    console.log("Request Letter Report Data:", formulaFields);

    var now = new Date();

    var day = String(now.getDate()).padStart(2, '0');
    var month = String(now.getMonth() + 1).padStart(2, '0');
    var year = String(now.getFullYear()).slice(-2);

    var hours = String(now.getHours()).padStart(2, '0');
    var minutes = String(now.getMinutes()).padStart(2, '0');
    var seconds = String(now.getSeconds()).padStart(2, '0');

    var timestamp = `${day}${month}${year}_${hours}${minutes}${seconds}`;

    $.ajax({
        url: 'http://localhost:34089/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(formulaFields),
        contentType: "application/json",
        xhrFields: {
            responseType: 'blob'
        },
        success: function (response) {
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
            console.error('Error generating report:', error);
        }
    });
}

async function BuyerCredit2() {

    var reportName = "IMPORT_BANK8_BC";

    var vType = $('#ddldoctype').val();
    var vNo = $('#Numdocno').val();

    if (!vType || !vNo) {
        showToast("V Type and V No are required.", {
            type: "error"
        });
        return;
    }

    var SelForMul =
        "{IMPORT_PAY1.V_TYPE}='" + vType + "'" +
        " AND {IMPORT_PAY1.V_NO}=" + vNo +
        " AND {IMPORT_PAY1.COMP_CODE}=" + window.globalVariables.compCode +
        " AND {IMPORT_PAY1.YEAR_CODE}=" + window.globalVariables.yearCode +
        " AND {IMPORT_PAY1.BRANCH_CODE}=" + window.globalVariables.branchCode;

    var formulaFields = {

        Reportname: reportName,

        selectionFormula: SelForMul,

        Database: window.database.db,

        Parameters: {
            RPTNAME: "REQUEST LETTER FOR PAYMENT",
            comp_name: window.globalVariables.companyName,
            comp_add1: window.globalVariables.add1,
            comp_add2: window.globalVariables.add2,
            reg_add1: "Regd. Office:" + (window.globalVariables.regAdd1 || "") + (window.globalVariables.regAdd2 || ""),
            comp_email: "Email :" + (window.globalVariables.email || ""),
            comp_cin: "CIN:" + (window.globalVariables.cin || "")
        }
    };

    console.log("Request Letter Report Data:", formulaFields);

    var now = new Date();

    var day = String(now.getDate()).padStart(2, '0');
    var month = String(now.getMonth() + 1).padStart(2, '0');
    var year = String(now.getFullYear()).slice(-2);

    var hours = String(now.getHours()).padStart(2, '0');
    var minutes = String(now.getMinutes()).padStart(2, '0');
    var seconds = String(now.getSeconds()).padStart(2, '0');

    var timestamp = `${day}${month}${year}_${hours}${minutes}${seconds}`;

    $.ajax({
        url: 'http://localhost:34089/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(formulaFields),
        contentType: "application/json",
        xhrFields: {
            responseType: 'blob'
        },
        success: function (response) {
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
            console.error('Error generating report:', error);
        }
    });
}

async function FormDeclaration() {

    var reportName = "IMPORT_BANK_FORM1";

    var vType = $('#ddldoctype').val();
    var vNo = $('#Numdocno').val();

    if (!vType || !vNo) {
        showToast("V Type and V No are required.", {
            type: "error"
        });
        return;
    }

    var SelForMul =
        "{IMPORT_PAY1.V_TYPE}='" + vType + "'" +
        " AND {IMPORT_PAY1.V_NO}=" + vNo +
        " AND {IMPORT_PAY1.COMP_CODE}=" + window.globalVariables.compCode +
        " AND {IMPORT_PAY1.YEAR_CODE}=" + window.globalVariables.yearCode +
        " AND {IMPORT_PAY1.BRANCH_CODE}=" + window.globalVariables.branchCode;

    var formulaFields = {

        Reportname: reportName,

        selectionFormula: SelForMul,

        Database: window.database.db,

        Parameters: {
            RPTNAME: "REQUEST LETTER FOR PAYMENT",
            comp_name: window.globalVariables.companyName,
            comp_add1: window.globalVariables.add1,
            comp_add2: window.globalVariables.add2,
            reg_add1: "Regd. Office:" + (window.globalVariables.regAdd1 || "") + (window.globalVariables.regAdd2 || ""),
            comp_email: "Email :" + (window.globalVariables.email || ""),
            comp_cin: "CIN:" + (window.globalVariables.cin || "")
        }
    };

    console.log("Request Letter Report Data:", formulaFields);

    var now = new Date();

    var day = String(now.getDate()).padStart(2, '0');
    var month = String(now.getMonth() + 1).padStart(2, '0');
    var year = String(now.getFullYear()).slice(-2);

    var hours = String(now.getHours()).padStart(2, '0');
    var minutes = String(now.getMinutes()).padStart(2, '0');
    var seconds = String(now.getSeconds()).padStart(2, '0');

    var timestamp = `${day}${month}${year}_${hours}${minutes}${seconds}`;

    $.ajax({
        url: 'http://localhost:34089/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(formulaFields),
        contentType: "application/json",
        xhrFields: {
            responseType: 'blob'
        },
        success: function (response) {
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
            console.error('Error generating report:', error);
        }
    });
}

async function CustomDeclaration() {

    var reportName = "IMPORT_BANK_DECL";

    var vType = $('#ddldoctype').val();
    var vNo = $('#Numdocno').val();

    if (!vType || !vNo) {
        showToast("V Type and V No are required.", {
            type: "error"
        });
        return;
    }

    var SelForMul =
        "{IMPORT_PAY1.V_TYPE}='" + vType + "'" +
        " AND {IMPORT_PAY1.V_NO}=" + vNo +
        " AND {IMPORT_PAY1.COMP_CODE}=" + window.globalVariables.compCode +
        " AND {IMPORT_PAY1.YEAR_CODE}=" + window.globalVariables.yearCode +
        " AND {IMPORT_PAY1.BRANCH_CODE}=" + window.globalVariables.branchCode;

    var formulaFields = {

        Reportname: reportName,

        selectionFormula: SelForMul,

        Database: window.database.db,

        Parameters: {
            RPTNAME: "REQUEST LETTER FOR PAYMENT",
            comp_name: window.globalVariables.companyName,
            comp_add1: window.globalVariables.add1,
            comp_add2: window.globalVariables.add2,
            reg_add1: "Regd. Office:" + (window.globalVariables.regAdd1 || "") + (window.globalVariables.regAdd2 || ""),
            comp_email: "Email :" + (window.globalVariables.email || ""),
            comp_cin: "CIN:" + (window.globalVariables.cin || "")
        }
    };

    console.log("Request Letter Report Data:", formulaFields);

    var now = new Date();

    var day = String(now.getDate()).padStart(2, '0');
    var month = String(now.getMonth() + 1).padStart(2, '0');
    var year = String(now.getFullYear()).slice(-2);

    var hours = String(now.getHours()).padStart(2, '0');
    var minutes = String(now.getMinutes()).padStart(2, '0');
    var seconds = String(now.getSeconds()).padStart(2, '0');

    var timestamp = `${day}${month}${year}_${hours}${minutes}${seconds}`;

    $.ajax({
        url: 'http://localhost:34089/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(formulaFields),
        contentType: "application/json",
        xhrFields: {
            responseType: 'blob'
        },
        success: function (response) {
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
            console.error('Error generating report:', error);
        }
    });
}

//======================
// Foam Validation
//======================
async function ValidateData() {

    if (!validateRequiredField('#ddldoctype', 'Document Type')) return;
    if (!validateRequiredField('#ddlParty', 'Party Name')) return;
    if (!validateRequiredField('#ddlourbank', 'Our Bank')) return;
    if (!validateRequiredField('#ddlbeneficiarybank', 'Beneficiary Bank')) return;
    if (!validateRequiredField('#Numdocno', 'Document Number')) return;
    if (!validateRequiredField('#ddlimport', 'Import for')) return;
    if (!validateRequiredField('#ddlimportremitt', 'Import Remit')) return;
    if (!validateRequiredField('#ddlpaytype', 'Pay type')) return;
    if (!validateRequiredField('#ddlforeignbankch', 'Foreign bank charge')) return;
    if (!validateRequiredField('#ddlintrateappl', 'International rate applicable')) return;

    // =========================
    // ROI / Period
    // Only when Interest Applicable = YES
    // =========================

    if (String($('#ddlintrateappl').val()).trim().toUpperCase() === 'YES')
    {
        if (!validateRequiredField('#txtrateinterest', 'Rate of interest')) {
            return false;
        }

        if (!validateRequiredField('#Txtperiodecb', 'Period')) {
            return false;
        }
    }

    // =========================
    // Footer Validation
    // =========================

    let validRowCount = 0;
    let isValid = true;

    const rows = $('#tblImportPaymentEntry tbody tr');

    rows.each(function () {

        if (!isValid) {
            return false;
        }

        const row = $(this);

        const poValue = row.find('.ddlPoNo').val();

        if (!poValue) {
            return;
        }

        validRowCount++;

        if (!row.find('.ddlPoNo').val()) {
            showToast("PO/Sauda No. is Mandatory Field.", { type: "error" });
            row.find('.ddlPoNo').focus();
            isValid = false;
            return false;
        }

        if (!row.find('.podate').val()) {
            showToast("PO/Sauda Date is Mandatory Field.", { type: "error" });
            row.find('.podate').focus();
            isValid = false;
            return false;
        }

        if (!row.find('.invoiceno').val()) {
            showToast("Invoice No. is Mandatory Field.", { type: "error" });
            row.find('.invoiceno').focus();
            isValid = false;
            return false;
        }

        if (!row.find('.invoicedate').val()) {
            showToast("Invoice Date is Mandatory Field.", { type: "error" });
            row.find('.invoicedate').focus();
            isValid = false;
            return false;
        }

        if ((parseFloat(row.find('.amount').val()) || 0) === 0) {
            showToast("Amount must not be left blank.", { type: "error" });
            row.find('.amount').focus();
            isValid = false;
            return false;
        }

        if ((parseFloat(row.find('.quantity').val()) || 0) === 0) {
            showToast("Invoice Quantity is Mandatory Field.", { type: "error" });
            row.find('.quantity').focus();
            isValid = false;
            return false;
        }

        if (!row.find('.itemname').val()) {
            showToast("Invoice Item Name is Mandatory Field.", { type: "error" });
            row.find('.itemname').focus();
            isValid = false;
            return false;
        }

        if (!row.find('.hsncode').val()) {
            showToast("Item HSN Code is Mandatory Field.", { type: "error" });
            row.find('.hsncode').focus();
            isValid = false;
            return false;
        }

        if (!row.find('.country').val()) {
            showToast("Country of Origin is Mandatory Field.", { type: "error" });
            row.find('.country').focus();
            isValid = false;
            return false;
        }
    });

    if (!isValid) {
        return false;
    }

    if (validRowCount === 0) {
        showToast("No Record in grid to save.", { type: "error" });
        return false;
    }

    return true;

}

//==========================
// Doc Date Validation
//==========================
async function checkValidDate() {

    const data = {
        vdate: $("#Dtdocdate").val(),
        vtype: $("#ddldoctype").val(),
        vno: $("#Numdocno").val()
    };

    try {

        const response = await fetch('/ItemMarketRate/CheckValidDate', {
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
        console.error(error);
        showToast("Date validation failed", { type: "error" });
        return false;
    }
}

