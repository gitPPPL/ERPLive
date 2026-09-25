

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
       await LoadData();
    }
    else
    {
        let selectedVType = $('#ddlInvType').val();
        await GetVNo(selectedVType, "SALE1");
    }

    $('#ddlPartyName').on('change', function () {
        selectedPartyData();
        let partycode = $('#ddlPartyName').val();
        cmbPartyAddress(partycode);

    });


    $('#ddlConsignee').on('change', function () {
        selectedConsigneePartyData();
        let partycode = $('#ddlConsignee').val();
        cmbConsigneeAddress(partycode);
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
        let selectedProduct = ProductDataList.find(x =>  String(x.code) === String(selectedCode));
        if (!selectedProduct)
        {
            $row.find('.HsnCode').val('');
            return;
        }

        console.log("selectedProduct", selectedProduct);
        $row.find('.HsnCode').val(selectedProduct.hsN_CODE ?? '');
        $row.find('.TxtPackWeight').val(selectedProduct.packing_Wt ?? '');
        $row.find('.ID').val(selectedCode ?? '');
    });

    $("#btn_save").click(function (e) {
        e.preventDefault();

        if (!validateRequiredField('#ddlInvType', 'Please select a Voucher Type')) return;
        if (!validateRequiredField('#NumSerialNo', 'Please Fill Voucher No')) return;
        if (!validateRequiredField('#DtDate', 'Please select a Voucher Date.')) return;
        if (!validateRequiredField('#ddlPartyName', 'Please select a Party Name.')) return;      

        const DOC_ID = $.trim($('#CODE').val());
        const V_TYPE = $.trim($('#ddlInvType').val());
        const V_NO = parseInt($.trim($('#NumSerialNo').val())) || 0;
        const V_DATE = formatDate($('#DtDate').val());
        const SUPPLY_TYPE = $.trim($('#ddlSupplyType').val());
        const IMPORT_CURRENCY = $.trim($('#ddlCurrency option:selected').text()) || "";
        const BILL_CODE = parseInt($.trim($('#ddlPartyName').val())) || 0;
 
        const BILL_NAME = $.trim($('#ddlPartyName option:selected').text()) || "";
        const BILL_ADDRESSID = parseInt($.trim($('#ddladdressl1').val())) || 0;
        const BILL_ADD1 = $.trim($('#txtaddressL1').val());
        const BILL_ADD2 = $.trim($('#txtaddressL2').val());
        const BILL_ADD3 = $.trim($('#txtaddressL3').val());
        const BILL_CITY = parseInt($.trim($('#ddlStationl').val())) || 0;
        const BILL_CITYName = $.trim($('#ddlStationl option:selected').text()) || "";
        const BILL_STATE = parseInt($.trim($('#ddlState').val())) || 0;
        const BILL_STATENAME = $.trim($('#ddlState option:selected').text()) || "";
        const BILL_PINCODE = $.trim($('#NumPincode').val());
        const BILL_COUNTRY = $.trim($('#ddlCountry').val());
        const BILL_COUNTRYNAME = $.trim($('#ddlCountry option:selected').text()) || "";
        const BILL_GST = $.trim($('#TxtGSTSa').val());
        const AGENT_CODE = parseInt($.trim($('#ddlSalesThrough').val())) || 0;
        const AGENT_Name = $.trim($('#ddlSalesThrough option:selected').text()) || "";
        const SHIP_CODE = parseInt($.trim($('#ddlConsignee').val())) || 0;
        const SHIP_NAME = $.trim($('#ddlConsignee option:selected').text()) || "";
        const SHIP_ADD1 = $.trim($('#txtaddressL1Sa').val());
        const SHIP_ADD2 = $.trim($('#txtaddressL2Sa').val());
        const SHIP_ADD3 = $.trim($('#txtaddressL3Sa').val());
        const SHIP_ADDRESSID = parseInt($.trim($('#ddlShipAddress').val())) || 0;
        const SHIP_CITY = parseInt($.trim($('#ddlStationSa').val())) || 0;
        const SHIP_CITYNAME = $.trim($('#ddlStationSa option:selected').text()) || "";
        const SHIP_PINCODE = $.trim($('#NumPincodeSa').val());
        const SHIP_COUNTRY = parseInt($.trim($('#ddlCountrySa').val())) || 0;
        const SHIP_COUNTRYNAME = $.trim($('#ddlCountrySa option:selected').text()) || "";
        const SHIP_GST = $.trim($('#TxtGSTSa').val());
        const ITEM_TYPE = $.trim($('#ddlProdType').val());
        const GR_NO = $.trim($('#TxtARNNo').val());

        let ARN_DATE = null;

        if ($('#chkARNdate').is(':checked')) {
            ARN_DATE = formatDate($('#DtARNdate').val());
        }


        const VEHICLE_NO = $.trim($('#txtModeofTransport').val());
        const TRANSPORT_CODE = parseInt($.trim($('#ddlTransport').val())) || 0;
        const TRANSPORT_NAME = $.trim($('#ddlTransport option:selected').text()) || "";
        const PORT_LOADING = $.trim($('#TxtPortLoading').val());
        const PORT_DISCHARGE = $.trim($('#TxtPortDisch').val());
        const INCOTERM = $.trim($('#ddlIncoterm').val());
        const SHIPMENT_TYPE = $.trim($('#ddlShipment').val());
        const MODEOF_PAYMENT = $.trim($('#TxtModepayment').val());
        const CONTAINER_SIZE = $.trim($('#ddlContainerSize').val());
        const BUYER_ORDNO = $.trim($('#TxtBuyerorderno').val());
        const AMOUNT = parseFloat($.trim($('#Numtotalamount').val())) || 0;
        const PACK_AMT = parseFloat($.trim($('#NumPacking2').val())) || 0;
        const DISC_AMT = parseFloat($.trim($('#NumDiscount2').val())) || 0;
        const FRT_AMT = parseFloat($.trim($('#NumFreight').val())) || 0;  
        const CGST_AMT = parseFloat($.trim($('#NumCGST2').val())) || 0;
        const SGST_AMT = parseFloat($.trim($('#NumSGST2').val())) || 0;
        const IGST_AMT = parseFloat($.trim($('#NumIGST2').val())) || 0;
        const TOT_NOS = parseInt($.trim($('#NumTotalNos').val())) || 0;  
        const CESS_AMT = parseFloat($.trim($('#NumCESS2').val())) || 0;
        const TCS_PER = parseFloat($.trim($('#NumTCS1').val())) || 0;
        const TCS_AMT = parseFloat($.trim($('#NumTCS2').val())) || 0;
        const ROUND_OFF = parseFloat($.trim($('#NumRoundOff').val())) || 0;
        const NAMOUNT = parseFloat($.trim($('#NumNetAmount').val())) || 0;
        const STATUS = parseInt($.trim($('#ddlDocStatus').val())) || 0;
        const TOT_GROSS = parseFloat($.trim($('#NumGrossQty').val())) || 0;
        const TOT_NET = parseFloat($.trim($('#NumNetQty').val())) || 0;
        const SOLD_BY = parseInt($.trim($('#ddlSoldBy').val())) || 0;
        const FINAL_DEST = $.trim($('#txtPriceValidity').val());
        const PAY_TERM = parseInt($.trim($('#ddlPaymentTerm').val())) || 0;
        const DELIVERY_TERMS = $.trim($('#txtTransportation').val());
        const WB_NO = parseInt($.trim($('#NumWeighmentQty1').val())) || 0;
        const WB_QTY = parseFloat($.trim($('#NumWeighmentQty2').val())) || 0;
        const INSU_DETAIL = $.trim($('#txtPackaging').val());
        const REMARK = $.trim($('#txtRemarks').val());
        const DEL_SCH = $.trim($('#txtDeliverySchedule').val());
        const action = (!rowId || rowId.trim() === '') ? 'INSERT' : 'UPDATE';

        if (BILL_COUNTRYNAME == 'INDIA' && SUPPLY_TYPE == 'EXPORT') {
            toastr.warning("It seems you are selecting out of India shipment so please slect 'Export' in shipment type also fill Export details.");
            return;
        }

        if (BILL_COUNTRY > 1 && SUPPLY_TYPE == "LOCAL")
        {
            toastr.warning("Please check, Customer Country is India but you select shipment type => Export.");          
            $('#ddlSupplyType').select2('open');         

            return;
        }

        const Header = {
            DOC_ID: DOC_ID,
            V_NO: V_NO,
            V_TYPE: V_TYPE,
            V_DATE: V_DATE,
            SUPPLY_TYPE: SUPPLY_TYPE,
            IMPORT_CURRENCY: IMPORT_CURRENCY,
            BILL_CODE: BILL_CODE,
            BILL_NAME: BILL_NAME,
            BILL_ADDRESSID: BILL_ADDRESSID,
            BILL_ADD1: BILL_ADD1,
            BILL_ADD2: BILL_ADD2,
            BILL_ADD3: BILL_ADD3,
            BILL_CITY: BILL_CITY,
            BILL_CITYName: BILL_CITYName,
            BILL_STATE: BILL_STATE,
            BILL_STATENAME: BILL_STATENAME,
            BILL_PINCODE: BILL_PINCODE,
            BILL_COUNTRY: BILL_COUNTRY,
            BILL_COUNTRYNAME: BILL_COUNTRYNAME,
            BILL_GST: BILL_GST,
            AGENT_CODE: AGENT_CODE,
            AGENT_Name: AGENT_Name,
            SHIP_CODE: SHIP_CODE,
            SHIP_NAME: SHIP_NAME,
            SHIP_ADD1: SHIP_ADD1,
            SHIP_ADD2: SHIP_ADD2,
            SHIP_ADD3: SHIP_ADD3,
            SHIP_ADDRESSID: SHIP_ADDRESSID,
            SHIP_CITY: SHIP_CITY,
            SHIP_CITYNAME: SHIP_CITYNAME,
            SHIP_PINCODE: SHIP_PINCODE,
            SHIP_COUNTRY: SHIP_COUNTRY,
            SHIP_COUNTRYNAME: SHIP_COUNTRYNAME,
            SHIP_GST: SHIP_GST,
            ITEM_TYPE: ITEM_TYPE,
            GR_NO: GR_NO,
            GR_DATE : ARN_DATE,
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
            PACK_AMT: PACK_AMT,
            DISC_AMT: DISC_AMT,
            FRT_AMT: FRT_AMT,
            CGST_AMT: CGST_AMT,
            SGST_AMT: SGST_AMT,
            IGST_AMT: IGST_AMT,
            TOT_NOS: TOT_NOS,
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
            WB_NO: WB_NO,
            WB_QTY: WB_QTY,
            INSU_DETAIL: INSU_DETAIL,
            DEL_SCH: DEL_SCH,
            REMARK: REMARK,
            action: action
        };

        const model = {
            Header: Header,
            Details: CollectDetailRows()
        };

        if (!ValidateDetailTable()) {
            return;
        }
             
        $("#btn-saves").prop("disabled", true);

        $.ajax({
            url: '/SalesProformaInvoice/SavedData',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(model),
            success: function (response)
            {
                console.log("Response", response);
                if (response.status == "Success") {
                    toastr.success("Saved successfully!");   

                    setTimeout(() => { window.location.href = `/SalesProformaInvoice/Index?id=${Header.DOC_ID}&mode=view`;  }, 1000);          
                }
                else if (response.status == "Validation")
                {
                    toastr.warning(response.message);
                }
                else {
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

    $('#NumTCS1').on('change input', function () {

        let subtotal = parseFloat($('#NumSubTotal').val()) || 0;
        let tcgst = parseFloat($('#NumCGST2').val()) || 0;
        let tsgst = parseFloat($('#NumSGST2').val()) || 0;
        let tigst = parseFloat($('#NumIGST2').val()) || 0;
        let cess = parseFloat($('#NumCESS2').val()) || 0;
        let tcsper = parseFloat($('#NumTCS1').val()) || 0;
        // Taxable + GST + Cess
        let subTotalAmount = subtotal + tcgst + tsgst + tigst + cess;
        // TCS
        let tcsamount = subTotalAmount * tcsper * 0.01;
        // Net Amount before rounding
        let netamount = subTotalAmount + tcsamount;
        // Rounded Net Amount
        let roundedAmount = Math.round(netamount);
        // Round Off
        let roundoff = roundedAmount - netamount;

        $('#NumTCS2').val(tcsamount.toFixed(2));
        $('#NumRoundOff').val(roundoff.toFixed(2));
        $('#NumNetAmount').val(roundedAmount.toFixed(2));
    });

    $('#ddladdressl1').change(function () {
        let PartyCode = $('#ddlPartyName').val();
        let AddressId = $('#ddladdressl1').val();
        AddressPartyData(PartyCode, AddressId);
    });

    $('#ddladdressl1Sa').change(function () {
        let PartyCode = $('#ddlConsignee').val();
        let AddressId = $('#ddladdressl1Sa').val();
        AddressConsigneeData(PartyCode, AddressId);
    });

    $('#button_mail').on('click', async function () {
        alert("hh")
        await SendMail();

    });

});