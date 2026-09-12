
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

    SetFYDate('DtDate', LoginDate);
    SetFYDate('DtARNdate', LoginDate);
    await LoadDropdown();

    AddRow();

    if (rowId)
    {

    }
    else
    {
        let selectedVType = $('#ddlInvType').val();
        await GetVNo(selectedVType, "SALE1");
    }

    $('#ddlPartyName').on('change', function () {
        selectedPartyData();
    });

    $("#chksameaboveaddress").change(function () {
        if ($(this).is(":checked"))
        {
            $('#ddlConsignee').val($('#ddlPartyName').val());
            $('#txtaddressL1Sa').val($('#txtaddressL1').val());
            $('#txtaddressL2Sa').val($('#txtaddressL2').val());
            $('#txtaddressL3Sa').val($('#txtaddressL3').val());
            $('#ddlStationSa').val($('#ddlStationl').val());
            $('#NumPincodeSa').val($('#NumPincode').val());
            $('#ddlCountrySa').val($('#ddlCountry').val());
            $('#TxtGSTSa').val($('#TxtGST').val());
        }
        else
        {
            $('#ddlConsignee').val('');
            $('#txtaddressL1Sa').val('');
            $('#txtaddressL1Sa').val('');
            $('#txtaddressL2Sa').val('');
            $('#txtaddressL3Sa').val('');
            $('#ddlStationSa').val('');
            $('#NumPincodeSa').val('');
            $('#ddlCountrySa').val('');
            $('#TxtGSTSa').val('');
        }
    });

    $('#ddlTaxType').on('change', function () {

        const selectedCode = $(this).val();

        const selectedTax = TaxPercentageData.find(
            item => item.code == selectedCode
        );

        $('#NumCGST1').val(selectedTax.cgsT_PER);
        $('#NumSGST1').val(selectedTax.sgsT_PER);
        $('#NumIGST1').val(selectedTax.igsT_PER);
        $('#NumCESS1').val(selectedTax.otH_PER);

        console.log("Selected Code:", selectedCode);
        console.log("Selected Tax Data:", selectedTax);
    });


    $(document).on('change', '.ddlProductName', function () {

        let $row = $(this).closest('tr');

        let selectedCode = $(this).val();

        let selectedProduct = ProductDataList.find(x =>
            String(x.code) === String(selectedCode)
        );

        if (!selectedProduct) {
            $row.find('.HsnCode').val('');
            return;
        }

        // Product details → selected row
        $row.find('.HsnCode').val(selectedProduct.hsN_CODE ?? '');


    });



    $("#btn_save").click(function (e) {
        e.preventDefault();

        const DOC_ID = $.trim($('#TxtCode').val());

        const V_TYPE = $.trim($('#ddlInvType').val());
        const V_NO = parseInt($.trim($('#NumSerialNo').val())) || 0;
        const V_DATE = formatDate($("#DtDate").val());

        const SUPPLY_TYPE = $.trim($('#ddlSupplyType').val());
        const CURRENCY = $.trim($('#ddlCurrency').val());

        const BILL_CODE = parseInt($.trim($('#ddlPartyName').val())) || 0;
        const BILL_ADD1 = $.trim($('#txtaddressL1').val());
        const BILL_ADD2 = $.trim($('#txtaddressL2').val());
        const BILL_ADD3 = $.trim($('#txtaddressL3').val());
        const BILL_CITY = parseInt($.trim($('#ddlStationl').val())) || 0;
        const BILL_PINCODE = $.trim($('#NumPincode').val());
        const BILL_COUNTRY = $.trim($('#ddlCountry').val());
        const BILL_GST = $.trim($('#TxtGSTSa').val());

        const AGENT_CODE = parseInt($.trim($('#ddlSalesThrough').val())) || 0;

        const SHIP_CODE = parseInt($.trim($('#ddlConsignee').val())) || 0;
        const SHIP_NAME = $.trim($('#ddlConsignee option:selected').text()) || "";
        const SHIP_ADD1 = $.trim($('#txtaddressL1Sa').val());
        const SHIP_ADD2 = $.trim($('#txtaddressL2Sa').val());
        const SHIP_ADD3 = $.trim($('#txtaddressL3Sa').val());
        const SHIP_CITY = parseInt($.trim($('#ddlStationSa').val())) || 0;
        const SHIP_PINCODE = $.trim($('#NumPincodeSa').val());
        const SHIP_COUNTRY = $.trim($('#ddlCountrySa').val());
        const SHIP_GST = $.trim($('#TxtGSTSa').val());

        const TAX_CODE = parseInt($.trim($('#ddlTaxType').val())) || 0;

        const ITEM_TYPE = $.trim($('#ddlProdType').val());

        const GR_NO = $.trim($('#TxtARNNo').val());
        const GR_DATE = formatDate($("#DtARNdate").val());

        const VEHICLE_NO = $.trim($('#txtModeofTransport').val());

        const TRANSPORT_CODE = parseInt($.trim($('#ddlTransport').val())) || 0;
        const TRANSPORT_NAME = $.trim($('#ddlTransport option:selected').text()) || "";

        const PORT_LOADING = $.trim($('#TxtPortLoading').val());
        const PORT_DISCHARGE = $.trim($('#TxtPortLoading').val());

        const INCOTERM = $.trim($('#ddlIncoterm').val());
        const SHIPMENT_TYPE = $.trim($('#ddlShipment').val());
        const MODEOF_PAYMENT = $.trim($('#TxtModepayment').val());
        const CONTAINER_SIZE = $.trim($('#ddlContainerSize').val());

        const BUYER_ORDNO = $.trim($('#TxtBuyerorderno').val());


        // Decimal fields
        const AMOUNT = parseFloat($.trim($('#Numtotalamount').val())) || 0;

        const PACK_PER = parseFloat($.trim($('#NumPacking1').val())) || 0;
        const PACK_AMT = parseFloat($.trim($('#NumPacking1').val())) || 0;

        const DISC_PER = parseFloat($.trim($('#NumDiscount1').val())) || 0;
        const DISC_AMT = parseFloat($.trim($('#NumDiscount2').val())) || 0;

        const FRT_AMT = parseFloat($.trim($('#NumFreight').val())) || 0;

        const CGST_PER = parseFloat($.trim($('#NumCGST1').val())) || 0;
        const CGST_AMT = parseFloat($.trim($('#NumCGST2').val())) || 0;

        const SGST_PER = parseFloat($.trim($('#NumSGST1').val())) || 0;
        const SGST_AMT = parseFloat($.trim($('#NumSGST2').val())) || 0;

        const IGST_PER = parseFloat($.trim($('#NumIGST1').val())) || 0;
        const IGST_AMT = parseFloat($.trim($('#NumIGST2').val())) || 0;

        const CESS_PER = parseFloat($.trim($('#NumCESS1').val())) || 0;
        const CESS_AMT = parseFloat($.trim($('#NumCESS2').val())) || 0;

        const TCS_PER = parseFloat($.trim($('#NumTCS1').val())) || 0;
        const TCS_AMT = parseFloat($.trim($('#NumTCS2').val())) || 0;

        const ROUND_OFF = parseFloat($.trim($('#NumRoundOff').val())) || 0;
        const NAMOUNT = parseFloat($.trim($('#NumNetAmount').val())) || 0;

        const TOT_GROSS = parseFloat($.trim($('#NumGrossQty').val())) || 0;
        const TOT_NET = parseFloat($.trim($('#NumNetQty').val())) || 0;


        // Integer fields
        const TOT_NOS = parseInt($.trim($('#NumTotalNos').val())) || 0;

        const STATUS = parseInt($.trim($('#ddlDocStatus').val())) || 0;

        const SOLD_BY = parseInt($.trim($('#ddlSoldBy').val())) || 0;

        const PAY_TERM = parseInt($.trim($('#ddlPaymentTerm').val())) || 0;


        // String fields
        const FINAL_DEST = $.trim($('#txtPriceValidity').val());
        const DELIVERY_TERMS = $.trim($('#txtTransportation').val());
        const LUT_DETAIL = $.trim($('#NumWeighmentQty1').val());
        const FINAL_DEST_COUNTRY = $.trim($('#NumWeighmentQty2').val());
        const INSU_DETAIL = $.trim($('#txtPackaging').val());
        const DEL_SCH = $.trim($('#txtDeliverySchedule').val());


        // Action
        const action = (!DOC_ID || DOC_ID.trim() === '') ? 'INSERT' : 'UPDATE';


        const Header = {
            DOC_ID: DOC_ID,

            V_TYPE: V_TYPE,
            V_NO: V_NO,
            V_DATE: V_DATE,

            SUPPLY_TYPE: SUPPLY_TYPE,
            CURRENCY: CURRENCY,

            BILL_CODE: BILL_CODE,
            BILL_ADD1: BILL_ADD1,
            BILL_ADD2: BILL_ADD2,
            BILL_ADD3: BILL_ADD3,
            BILL_CITY: BILL_CITY,
            BILL_PINCODE: BILL_PINCODE,
            BILL_COUNTRY: BILL_COUNTRY,
            BILL_GST: BILL_GST,

            AGENT_CODE: AGENT_CODE,

            SHIP_CODE: SHIP_CODE,
            SHIP_NAME: SHIP_NAME,
            SHIP_ADD1: SHIP_ADD1,
            SHIP_ADD2: SHIP_ADD2,
            SHIP_ADD3: SHIP_ADD3,
            SHIP_CITY: SHIP_CITY,
            SHIP_PINCODE: SHIP_PINCODE,
            SHIP_COUNTRY: SHIP_COUNTRY,
            SHIP_GST: SHIP_GST,

            TAX_CODE: TAX_CODE,
            ITEM_TYPE: ITEM_TYPE,

            GR_NO: GR_NO,
            GR_DATE: GR_DATE,

            VEHICLE_NO: VEHICLE_NO,

            TRANSPORT_CODE: TRANSPORT_CODE,
            TRANSPORT_NAME: TRANSPORT_NAME,

            PORT_LOADING: PORT_LOADING,
            PORT_DISCHARGE: PORT_DISCHARGE,

            INCOTERM: INCOTERM,
            SHIPMENT_TYPE: SHIPMENT_TYPE,
            MODEOF_PAYMENT: MODEOF_PAYMENT,
            CONTAINER_SIZE: CONTAINER_SIZE,

            BUYER_ORDNO: BUYER_ORDNO,

            AMOUNT: AMOUNT,

            PACK_PER: PACK_PER,
            PACK_AMT: PACK_AMT,

            DISC_PER: DISC_PER,
            DISC_AMT: DISC_AMT,

            FRT_AMT: FRT_AMT,

            CGST_PER: CGST_PER,
            CGST_AMT: CGST_AMT,

            SGST_PER: SGST_PER,
            SGST_AMT: SGST_AMT,

            IGST_PER: IGST_PER,
            IGST_AMT: IGST_AMT,

            TOT_NOS: TOT_NOS,

            CESS_PER: CESS_PER,
            CESS_AMT: CESS_AMT,

            TCS_PER: TCS_PER,
            TCS_AMT: TCS_AMT,

            ROUND_OFF: ROUND_OFF,
            NAMOUNT: NAMOUNT,

            STATUS: STATUS,

            TOT_GROSS: TOT_GROSS,
            TOT_NET: TOT_NET,

            SOLD_BY: SOLD_BY,

            FINAL_DEST: FINAL_DEST,
            PAY_TERM: PAY_TERM,
            DELIVERY_TERMS: DELIVERY_TERMS,

            LUT_DETAIL: LUT_DETAIL,
            FINAL_DEST_COUNTRY: FINAL_DEST_COUNTRY,
            INSU_DETAIL: INSU_DETAIL,

            DEL_SCH: DEL_SCH,

            action: action
        };


        const model = {
            Header: Header,
            Details: CollectDetailRows()
        };

    
    
     
        $("#btn-saves").prop("disabled", true);

        $.ajax({
            url: '/SalesProformaInvoice/SavedData',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(model),
            success: function (response) {
                if (response.success) {
                    toastr.success("Saved successfully!");
                    setTimeout(() => window.location.href = '/FlakesQCEntryList/Index', 1000);
                } else {
                    toastr.error(response.message || "Save failed.");
                }
            },
            error: function (xhr) {
                let errorMessage = "Something went wrong.";
                if (xhr.status === 400) {
                    errorMessage = "Bad Request: " + xhr.responseText;
                } else if (xhr.status === 500) {
                    errorMessage = "Server error: " + xhr.responseText;
                } else {
                    errorMessage = "Unexpected error: " + xhr.statusText;
                }
                toastr.error("Error: " + errorMessage);
            },
            complete: function () {
                $("#btn-saves").prop("disabled", false);
            }
        });
    });




 








});