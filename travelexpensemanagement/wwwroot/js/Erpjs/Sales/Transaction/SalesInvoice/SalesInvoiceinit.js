

const urlParams = new URLSearchParams(window.location.search);
const rowId = urlParams.get('id');
const vtype = urlParams.get('VType');
const $tbody = $("#tblSalesProformaInvoice tbody");
const form = $('#SalesProformaInvoiceform');
const mode = urlParams.get('mode');
const isReadOnly = (mode === 'view');
var globalVars = window.globalVariables || {};
var database = window.database || "";
let PubUserLevel = globalVars.UserLevel;
let CompCode = globalVars.CompCode;
let LoginDate = globalVars.LoginDate;
var controllerName = window.location.pathname.split('/')[1];
let partyData = [];

let TaxTypeList = '';
let TaxPercentageData = [];
let ProductList = '';
let ProductDataList = [];

$(document).ready(async function () {

    SetFYDate('DtDocumentDate', LoginDate);

    await LoadDropdown();

    AddRow();
    if (rowId)
    {
        await LoadData();
    }
    else
    {
        let selectedVType = $('#ddlDocumentType').val();
        await GetVNo(selectedVType, "SALE1");
    }

    $('#ddlPartyName').on('change',async function () {
        selectedPartyData();
        let partycode = $('#ddlPartyName').val();
        await Promise.all([
            DDlPackNo(partycode),
            cmbPartyAddress(partycode)
        ]);
    });


    $('#ddlConsignee').on('change', async function ()
    {  
        selectedConsigneeData();
        let partycode = $('#ddlConsignee').val();
        cmbConsigneeAddress(partycode);       
    });



    $("#btn-save").click(function (e) {
        e.preventDefault();

        if (!validateRequiredField('#ddlDocumentType', 'Please select a Voucher Type')) return;
        if (!validateRequiredField('#NumInvoiceNo', 'Please Fill Voucher No')) return;
        if (!validateRequiredField('#DtDocumentDate', 'Please select a Voucher Date.')) return;
        if (!validateRequiredField('#ddlPartyName', 'Please select a Party Name.')) return;

        // =========================================================
        // HEADER FIELDS
        // =========================================================

        let DoType = $.trim($('#ddlDONo option:selected').text()).split('-').pop().trim();
        let Do_NO = $.trim($('#ddlDONo').val()) || "";
        
        let DOC_ID = $.trim($('#TxtCode').val()) || "";
        let V_TYPE = $.trim($('#ddlDocumentType').val()) || "";       
        let V_NO = parseInt($.trim($('#NumInvoiceNo').val()),  10) || 0;
        let V_DATE = null;

        if ($('#DtDocumentDate').val())
        {
            V_DATE = formatDate($('#DtDocumentDate').val());
        }
            

        let BILL_NAME =  $.trim($('#ddlPartyName option:selected').text()) || "";
        let BILL_CODE = parseInt($.trim($('#ddlPartyName').val()), 10) || 0;
        let BILL_ADDRESSID = parseInt($.trim($('#ddladdressL1').val()), 10) || 0;
        let BILL_ADD1 = $.trim($('#TxtAddressL1').val()) || "";
        let BILL_ADD2 =  $.trim($('#TxtAddressL2').val()) || "";
        let BILL_ADD3 = $.trim($('#TxtAddressL3').val()) || "";
        let BILL_CITY = parseInt($.trim($('#ddlStation').val()), 10) || 0;
        let BILL_CITYName = $.trim($('#ddlStation option:selected').text()) || "";
        let BILL_PINCODE = $.trim($('#NumPincode').val()) || "";
        let BILL_GST = $.trim($('#NumGSTNoL').val()) || "";
 
        let AGENT_CODE =  parseInt($.trim($('#ddlSalesThrough').val()), 10) || 0;

        let AGENT_Name = "";

        if (AGENT_CODE) {
            AGENT_Name = $.trim($('#ddlSalesThrough option:selected').text()) || "";
        }

        let SUPPLY_TYPE =  $.trim($('#ddlSupplyType').val()) || "";

        let SHIP_CODE = parseInt($.trim($('#ddlConsignee').val()), 10) || 0;

        let SHIP_NAME = "";

        if (SHIP_CODE) {
            SHIP_NAME = $.trim($('#ddlConsignee option:selected').text()) || "";
        }

        let SHIP_ADD1 =  $.trim($('#TxtSupplyAddressL1').val()) || "";
        let SHIP_ADD2 = $.trim($('#TxtSupplyAddressL2').val()) || "";
        let SHIP_ADD3 = $.trim($('#TxtSupplyAddressL3').val()) || "";
        let SHIP_ADDRESSID = parseInt($.trim($('#ddlsupplyaddressL1').val()), 10) || 0;
        let SHIP_CITY = parseInt($.trim($('#ddlSupplyStation').val()), 10) || 0;

        let SHIP_CITYNAME = "";

        if (SHIP_CITY) {
            SHIP_CITYNAME = $.trim($('#ddlSupplyStation option:selected').text()) || "";
        }

        let SHIP_PINCODE = $.trim($('#NumSupplyPIN').val()) || "";
        let SHIP_GST = $.trim($('#NumGSTNo').val()) || "";
        let FORM_CODE = parseInt($.trim($('#ddlFormType').val()), 10) || 0;
        let TRAN_TYPE = $.trim($('#ddlTransactionType').val()) || "";
        let TAX_CODE =  parseInt($.trim($('#ddlTaxType').val()), 10) || 0;
        let PACK_NO = parseInt($.trim($('#ddlPackNo').val()), 10) || 0;

        let PACK_TYPE = "";

        if (PACK_NO) {
            PACK_TYPE = $.trim($('#ddlPackNo option:selected').text()).split('-').pop().trim();
        }
        let PACK_PER =  parseFloat($.trim($('#NumOtherPacking1').val())) || 0;
        let PACK_AMT = parseFloat($.trim($('#NumOtherPacking2').val())) || 0;

        let SAUDA_NO = parseInt($.trim($('#ddlSaudaNo').val()), 10) || 0;

        let SAUDA_TYPE = $.trim($('#ddlSaudaNo option:selected').text()).split('-').pop().trim();

        let SAUDA_RATE = parseFloat($.trim($('#txt_saudarate').val())) || 0;
        let ISSUE_NO =  parseInt($.trim($('#ddlIssueNo').val()), 10) || 0;

        let ISSUE_TYPE = $.trim($('#ddlIssueNo option:selected').text()).split('-').pop().trim();

        let GODOWN_CODE =  $.trim($('#ddlGodown').val()) || "";

        let REMARK = $.trim($('#TxtTransRemarks').val()) || "";

        let ITEM_TYPE =$.trim($('#ddlProductType').val()) || "";

        let WB_NO = parseInt($.trim($('#ddlWBNo').val()), 10) || 0;

        let WB_TYPE = $.trim($('#ddlWBNo option:selected').text()).split('-').pop().trim();

        let WB_QTY = parseFloat($.trim($('#NumWbQty').val())) || 0;

        let WB_AMT = parseFloat($.trim($('#txt_WBamt').val())) || 0;

        let WB_REM = $.trim($('#txt_wbRemark').val()) || "";

        let WB_AC = $.trim($('#ddl_WBParty').val()) || "";

        let AMOUNT = parseFloat($.trim($('#NumOtherTotalAmount').val())) || 0;

        let DISC_PER =  parseFloat($.trim($('#NumOtherDiscount1').val())) || 0;

        let DISC_AMT = parseFloat($.trim($('#NumOtherDiscount2').val())) || 0;

        let CDISC_PER = parseFloat($.trim($('#NumOtherCashDiscount1').val())) || 0;

        let CDISC_AMT = parseFloat($.trim($('#NumOtherCashDiscount2').val())) || 0;

        let CGST_PER = parseFloat($.trim($('#NumOtherCGST1').val())) || 0;

        let CGST_AMT =  parseFloat($.trim($('#NumOtherCGST2').val())) || 0;

        let SGST_PER = parseFloat($.trim($('#NumOtherSGST1').val())) || 0;

        let SGST_AMT = parseFloat($.trim($('#NumOtherSGST2').val())) || 0;

        let IGST_PER = parseFloat($.trim($('#NumOtherIGST1').val())) || 0;

        let IGST_AMT = parseFloat($.trim($('#NumOtherIGST2').val())) || 0;

        let CESS_PER =  parseFloat($.trim($('#NumOtherCESS1').val())) || 0;

        let CESS_AMT = parseFloat($.trim($('#NumOtherCESS2').val())) || 0;

        let TCS_PER = parseFloat($.trim($('#NumOtherTCS1').val())) || 0;

        let TCS_AMT = parseFloat($.trim($('#NumOtherTCS2').val())) || 0;

        let ROUND_OFF = parseFloat($.trim($('#NumOtherRoundOff').val())) || 0;

        let TOT_NOS =  parseInt($.trim($('#NumOtherTotalNos').val()), 10) || 0;

        let TOT_GROSS =  parseFloat($.trim($('#NumGrossQty').val())) || 0;

        let TOT_NET = parseFloat($.trim($('#NumNetQty').val())) || 0;

        let TRANSPORT_CODE =  parseInt($.trim($('#ddlTransport').val()), 10) || 0;

        let TRANSPORT_NAME = "";

        if (TRANSPORT_CODE) {
            TRANSPORT_NAME = $.trim($('#ddlTransport option:selected').text()) || "";
        }

        let GR_NO = $.trim($('#NumGRNo').val()) || "";

        let GR_DATE = null;

        if ($('#chkGRDate').is(':checked') && $('#DtGRDate').val()) {
            GR_DATE = formatDate($('#DtGRDate').val());
        }

        let VEHICLE_NO = $.trim($('#TxtTruckNo').val()) || "";
        let DRIVER_NAME = $.trim($('#TxtDriverName').val()) || "";
        let DRIVER_NO = $.trim($('#NumDriverMob').val()) || "";
        let INSU_PER =  parseFloat($.trim($('#NumInsurance1').val())) || 0;
        let INSU_AMT =  parseFloat($.trim($('#NumInsurance2').val())) || 0;
        let INSU_DETAIL =  $.trim($('#txt_InsuranceDe').val()) || "";
        let TDS_PER = parseFloat($.trim($('#NumTDSFreight1').val())) || 0;
        let TDS_AMT = parseFloat($.trim($('#NumTDSFreight2').val())) || 0;
        let FRT_TAXPER = parseFloat($.trim($('#NumTaxFreight1').val())) || 0;
        let FRT_TAXAMT =  parseFloat($.trim($('#NumTaxFreight2').val())) || 0;
        let FRT_AMT = parseFloat($.trim($('#NumFreightAmount').val())) || 0;
        let FRT_TOPAY = parseFloat($.trim($('#txt_ToPayFrt').val())) || 0;
        let TPT_DISTANCE = parseInt($.trim($('#NumDistance').val()), 10) || 0;
        let TPT_MODE = parseInt($.trim($('#ddlMode').val()), 10) || 0;
        let PAY_TERM = parseInt($.trim($('#ddlPaymentTerm').val()), 10) || 0;
        let PAYMENT_TERM = $.trim($('#ddlPaymentTerm option:selected').text()) || "";
        let DELIVERY_TERMS = $.trim($('#TxtDeliveryTerm').val()) || "";
        let WAYBILL_NO = $.trim($('#txt_WayBillNo').val()) || "";
        let LOAD_AC =  $.trim($('#ddl_LoadParty').val()) || "";
        let LOAD_PER =  parseFloat($.trim($('#txt_loadPer').val())) || 0;
        let LOAD_AMT = parseFloat($.trim($('#txt_LoadAmt').val())) || 0;
        let LOAD_REM =  $.trim($('#txt_ldRemark').val()) || "";
        let NAMOUNT = parseFloat($.trim($('#NumOtherNetAmount').val())) || 0;
        let EXRATE =  parseFloat($.trim($('#txxt_ExRate').val())) || 0;
        let CURRENCY = $.trim($('#ddl_currency').val()) || "";
        let DEFECTIVE_GOODS = document.getElementById("ChkDefectiveGoods").checked ? "1"  : "0";
        let CAL_ONPCS = document.getElementById("ChkPCS").checked ? 1 : 0;
        let PRINT_DETAIL = document.getElementById("ChkDetail").checked ? "1"  : "0";
        let BUYER_ORDNO = "";

        let PLACE_RECEIPT =  $.trim($('#txt_receiptat').val()) || "";
        let PORT_LOADING = $.trim($('#txxt_pol').val()) || "";
        let PORT_DISCHARGE = $.trim($('#txxt_POD').val()) || "";
        let PORT_CODE =  $.trim($('#txxt_PortCode').val()) || "";
        let FINAL_DEST = $.trim($('#txxt_finalDestination').val()) || "";
        let FINAL_DEST_COUNTRY =  $.trim($('#txxt_FDCountry').val()) || "";
        let SB_NO = $.trim($('#txxt_ShipBillNo').val()) || "";
        let SB_DATE = null;

        if ($('#DtDocumentDate').val()) {
            SB_DATE = formatDate($('#DtDocumentDate').val());
        }

        let FOB_VALUE =   parseFloat($.trim($('#txxt_FobValue').val())) || 0;
        let FOB_FRT =  parseFloat($.trim($('#txt_FOBFRT').val())) || 0;
        let FOB_INSU = parseFloat($.trim($('#txxt_ForIssu').val())) || 0;
        let FOB_OTHER = parseFloat($.trim($('#txxt_FobOther').val())) || 0;
        let LUT_NO = $.trim($('#txt_LutNo').val()) || "";

        let LUT_DETAIL = "";

        // DateTime?
        let LUT_DATE = null;

        if ($('#dt_LutDate').val()) {
            LUT_DATE =
                formatDate($('#dt_LutDate').val());
        }

        let INCOTERM = "";

        let BILLOF_LADING = $.trim($('#txt_BillOfLanding').val()) || "";
        let LC_NO = $.trim($('#ddl_licNo').val()) || "";
        let action = (!rowId || rowId.trim() === '')  ? "INSERT" : "UPDATE";
        let STATUS = parseInt($.trim($('#ddlDocStatus').val()), 10) || 0;
        const Header = {
            DoType,
            Do_NO,
            DOC_ID,
            V_TYPE,
            V_NO,
            V_DATE,
            BILL_NAME,
            BILL_CODE,
            BILL_ADDRESSID,
            BILL_ADD1,
            BILL_ADD2,
            BILL_ADD3,
            BILL_CITY,
            BILL_CITYName,
            BILL_PINCODE,
            BILL_GST,
            AGENT_CODE,
            AGENT_Name,
            SUPPLY_TYPE,
            SHIP_CODE,
            SHIP_NAME,
            SHIP_ADD1,
            SHIP_ADD2,
            SHIP_ADD3,
            SHIP_ADDRESSID,
            SHIP_CITY,
            SHIP_CITYNAME,
            SHIP_PINCODE,
            SHIP_GST,
            FORM_CODE,
            TRAN_TYPE,
            TAX_CODE,
            PACK_NO,
            PACK_TYPE,
            PACK_PER,
            PACK_AMT,
            SAUDA_NO,
            SAUDA_TYPE,
            SAUDA_RATE,
            ISSUE_NO,
            ISSUE_TYPE,
            GODOWN_CODE,
            REMARK,
            ITEM_TYPE,
            WB_NO,
            WB_TYPE,
            WB_QTY,
            WB_AMT,
            WB_REM,
            WB_AC,
            AMOUNT,
            DISC_PER,
            DISC_AMT,
            CDISC_PER,
            CDISC_AMT,
            CGST_PER,
            CGST_AMT,
            SGST_PER,
            SGST_AMT,
            IGST_PER,
            IGST_AMT,
            CESS_PER,
            CESS_AMT,
            TCS_PER,
            TCS_AMT,
            ROUND_OFF,
            TOT_NOS,
            TOT_GROSS,
            TOT_NET,
            TRANSPORT_CODE,
            TRANSPORT_NAME,
            GR_NO,
            GR_DATE,
            VEHICLE_NO,
            DRIVER_NAME,
            DRIVER_NO,
            INSU_PER,
            INSU_AMT,
            INSU_DETAIL,
            TDS_PER,
            TDS_AMT,
            FRT_TAXPER,
            FRT_TAXAMT,
            FRT_AMT,
            FRT_TOPAY,
            TPT_DISTANCE,
            TPT_MODE,
            PAY_TERM,
            PAYMENT_TERM,
            DELIVERY_TERMS,
            WAYBILL_NO,
            LOAD_AC,
            LOAD_PER,
            LOAD_AMT,
            LOAD_REM,
            NAMOUNT,
            EXRATE,
            CURRENCY,
            DEFECTIVE_GOODS,
            CAL_ONPCS,
            PRINT_DETAIL,
            BUYER_ORDNO,
            PLACE_RECEIPT,
            PORT_LOADING,
            PORT_DISCHARGE,
            PORT_CODE,
            FINAL_DEST,
            FINAL_DEST_COUNTRY,
            SB_NO,
            SB_DATE,
            FOB_VALUE,
            FOB_FRT,
            FOB_INSU,
            FOB_OTHER,
            LUT_NO,
            LUT_DETAIL,
            LUT_DATE,
            INCOTERM,
            BILLOF_LADING,
            LC_NO,
            action,
            STATUS
        };

        const model = {  Header: Header,
            Details: GetSalesInvoiceDetails()
        };
          

        console.log("HEADER:", Header);
        console.log("DETAILS:", model.Details);
        console.log(  "FINAL JSON:", JSON.stringify(model, null, 2) );

        $("#btn-saves").prop("disabled", true);

        $.ajax({
            url: '/SalesInvoice/SavedData',
            type: 'POST',

            contentType: 'application/json; charset=utf-8',
            dataType: 'json',

            data: JSON.stringify(model),

            success: function (response) {

                console.log("Response:", response);

                if (response.status === "Success") {

                    toastr.success("Saved successfully!");

                    setTimeout(function () {

                        window.location.href =
                            `/salesInvoice/Index?id=${Header.DOC_ID}&mode=view`;

                    }, 1000);
                }
                else if (response.status === "Validation") {

                    toastr.warning(response.message);
                }
                else {

                    toastr.error(
                        response.message || "Save failed."
                    );
                }
            },

            error: function (xhr) {

                console.log("HTTP Status:", xhr.status);
                console.log("Response:", xhr.responseText);

                let errorMessage = "Something went wrong.";

                if (xhr.status === 400) {

                    errorMessage =
                        "Bad Request: " + xhr.responseText;
                }
                else if (xhr.status === 500) {

                    errorMessage =
                        "Server error: " + xhr.responseText;
                }
                else {

                    errorMessage =
                        "Unexpected error: " + xhr.statusText;
                }

                toastr.error("Error: " + errorMessage);
            },

            complete: function () {

                $("#btn-saves").prop("disabled", false);
            }
        });
    });

});