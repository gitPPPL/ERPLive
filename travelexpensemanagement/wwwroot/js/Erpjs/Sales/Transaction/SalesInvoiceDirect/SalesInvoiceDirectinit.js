

const urlParams = new URLSearchParams(window.location.search);
const rowId = urlParams.get('id');
let vtype = "";
let vNo = "";

const $tbody = $("#tblSalesProformaInvoice tbody");
const form = $('#SalesProformaInvoiceform');
const mode = urlParams.get('mode');
const isReadOnly = (mode === 'view');
var globalVars = window.globalVariables || {};
var database = window.database || "";
var LoadGeneralSetting = window.LoadGeneralSetting || {};
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
        vtype = $('#ddlDocumentType').val();
        vNo = $('#NumInvoiceNo').val();
        checkApprovalStatus(vtype, rowId, 'SALE1');

        $('#tblSalesInvoice tbody tr').each(function () {
            const $row = $(this);
            const itemCode = $.trim($row.find('.ddlProductName').val() || '');
            if (itemCode !== '')
            {          
                CalculateRow($row);
            }
        });


    }
    else
    {     
        vtype = $('#ddlDocumentType').val();
        await GetVNo(vtype, "SALE1");
    }

    $('#ddlPartyName').on('change', async function () {
        selectedPartyData();
        let partycode = $('#ddlPartyName').val();
        await Promise.all([  
            cmbPartyAddress(partycode)
        ]);
    });

    $('#ddlConsignee').on('change', async function () {
        selectedConsigneeData();
        let partycode = $('#ddlConsignee').val();
        cmbConsigneeAddress(partycode);
    });

    $('#ddladdressL1').change(function () {
        let PartyCode = $('#ddlPartyName').val();
        let AddressId = $('#ddladdressL1').val();
        AddressPartyData(PartyCode, AddressId);
    });

    $('#ddlsupplyaddressL1').change(function () {
        let PartyCode = $('#ddlConsignee').val();
        let AddressId = $('#ddlsupplyaddressL1').val();
        AddressConsigneeData(PartyCode, AddressId);
    });

    $('#ddlTaxType').on('change', function () {

        const selectedCode = $(this).val();

        const selectedTax = TaxPercentageData.find(
            item => String(item.code) === String(selectedCode)
        );

        console.log("Selected Code:", selectedCode);
        console.log("Selected Tax Data:", selectedTax);

        if (!selectedTax)
        {
            $('#tblSalesInvoice tbody tr').each(function ()
            {
                const $row = $(this);
                $row.find('.TxtTaxType').val('');
                $row.find('.TxtCgstper').val('0.00');
                $row.find('.TxtSgstPer').val('0.00');
                $row.find('.TxtIGSTPer').val('0.00');
            });
            return;
        }

        const CGST = Number(selectedTax.cgsT_PER || 0).toFixed(2);
        const SGST = Number(selectedTax.sgsT_PER || 0).toFixed(2);
        const IGST = Number(selectedTax.igsT_PER || 0).toFixed(2);

        $('#tblSalesInvoice tbody tr').each(function () {
            const $row = $(this);
            $row.find('.TxtTaxType').val(selectedCode);
            $row.find('.TxtCgstper').val(CGST);
            $row.find('.TxtSgstPer').val(SGST);
            $row.find('.TxtIGSTPer').val(IGST);       

        });

    });



    $("#btn-save").click(async function (e) {
        e.preventDefault();

        if (!validateRequiredField('#ddlDocumentType', 'Please select a Voucher Type')) return;
        if (!validateRequiredField('#NumInvoiceNo', 'Please Fill Voucher No')) return;
        if (!validateRequiredField('#DtDocumentDate', 'Please select a Voucher Date.')) return;
        if (!validateRequiredField('#ddlPartyName', 'Please select a Party Name.')) return;
        if (!validateRequiredField('#ddlFormType', 'Please select a Form Type.')) return;
        if (!validateRequiredField('#TxtDriverName', 'Please Fill  Driver Name.')) return;
        if (!validateRequiredField('#TxtDriverName', 'Please Fill  Driver Name.')) return;
        if (!validateRequiredField('#NumDriverMob', 'Please Fill  Driver Mobile No.')) return;


        // =========================================================
        // HEADER
        // =========================================================


        const isValid = await checkValidDate();
        if (isValid === false) {
            return;
        }



        const DOC_ID = $.trim($('#TxtCode').val()) || "";
        const V_TYPE = $.trim($('#ddlDocumentType').val()) || "";
        let V_DATE = null;
        if ($.trim($('#DtDocumentDate').val())) {
            V_DATE = formatDate($('#DtDocumentDate').val());
        }
        const V_NO = parseInt($.trim($('#NumInvoiceNo').val()), 10) || 0;
        const BILL_CODE = parseInt($.trim($('#ddlPartyName').val()), 10) || 0;
        const BILL_NAME = $.trim($('#ddlPartyName option:selected').text()) || "";
        const BILL_ADD1 = $.trim($('#TxtAddressL1').val()) || "";
        const BILL_ADD2 = $.trim($('#TxtAddressL2').val()) || "";
        const BILL_ADD3 = $.trim($('#TxtAddressL3').val()) || "";
        const BILL_GST = $.trim($('#NumGSTNoL').val()) || "";
        const BILL_PINCODE = $.trim($('#NumPincode').val()) || "";
        const INSUCR_DAYS =  0;            
        const SHIP_CODE = parseInt($.trim($('#ddlConsignee').val()), 10) || 0;
        const SHIP_NAME = SHIP_CODE ? $.trim($('#ddlConsignee option:selected').text()) : "";
        const SHIP_ADD1 = $.trim($('#TxtSupplyAddressL1').val()) || "";
        const SHIP_ADD2 = $.trim($('#TxtSupplyAddressL2').val()) || "";
        const SHIP_ADD3 = $.trim($('#TxtSupplyAddressL3').val()) || "";    
        const SHIP_CITY = parseInt($.trim($('#ddlSupplyStation').val()), 10) || 0;
        const SHIP_GST = $.trim($('#NumGSTNo').val()) || "";
        const SHIP_PINCODE = $.trim($('#NumSupplyPIN').val()) || "";
        const TAX_CODE = parseInt($.trim($('#ddlTaxType').val()), 10) || 0;
        const ITEM_TYPE = $.trim($('#ddlProductType').val()) || "";
        const ISSUE_NO = parseInt($.trim($('#ddlIssueNo').val()), 10) || 0;
        const ISSUE_TYPE = ISSUE_NO ? $.trim($('#ddlIssueNo option:selected').text()).split('-').pop().trim() : "";
        const AMOUNT = getDecimal($.trim($('#NumOtherTotalAmount').val())) || 0;
        const PACK_PER = parseFloat($.trim($('#NumOtherPacking1').val())) || 0;
        const PACK_AMT = getDecimal($.trim($('#NumOtherPacking2').val())) || 0;
        const TCS_PER = parseFloat($.trim($('#NumOtherTCS1').val())) || 0;
        const TCS_AMT = getDecimal($.trim($('#NumOtherTCS2').val())) || 0;
        const CGST_PER = parseFloat($.trim($('#NumOtherCGST1').val())) || 0;
        const CGST_AMT = getDecimal($.trim($('#NumOtherCGST2').val())) || 0;
        const SGST_PER = parseFloat($.trim($('#NumOtherSGST1').val())) || 0;
        const SGST_AMT = getDecimal($.trim($('#NumOtherSGST2').val())) || 0;
        const IGST_PER = parseFloat($.trim($('#NumOtherIGST1').val())) || 0;
        const IGST_AMT = getDecimal($.trim($('#NumOtherIGST2').val())) || 0;
        const CESS_PER = parseFloat($.trim($('#NumOtherCESS1').val())) || 0;
        const CESS_AMT = getDecimal($.trim($('#NumOtherCESS2').val())) || 0;
        const LOAD_PER = parseFloat($.trim($('#txt_loadPer').val())) || 0;
        const LOAD_AMT = getDecimal($.trim($('#txt_LoadAmt').val())) || 0;
        const LOAD_AC = $.trim($('#ddl_LoadParty').val()) || "";
        const LOAD_REM = $.trim($('#txt_ldRemark').val()) || "";
        const WB_AMT = getDecimal($.trim($('#txt_WBamt').val())) || 0;
        const WB_AC = $.trim($('#ddl_WBParty').val()) || "";
        const TOT_NOS = parseInt($.trim($('#NumOtherTotalNos').val()), 10) || 0;
        const FRT_AMT = getDecimal($.trim($('#NumFreightAmount').val())) || 0;
        const ROUND_OFF = getDecimal($.trim($('#NumOtherRoundOff').val())) || 0;
        const NAMOUNT = getDecimal($.trim($('#NumOtherNetAmount').val())) || 0;
        const INSU_PER = parseFloat($.trim($('#NumInsurance1').val())) || 0;
        const INSU_AMT = getDecimal($.trim($('#NumInsurance2').val())) || 0;
        const TDS_PER = parseFloat($.trim($('#NumTDSFreight1').val())) || 0;
        const TDS_AMT = getDecimal($.trim($('#NumTDSFreight2').val())) || 0;
        const FRT_TAXPER = parseFloat($.trim($('#NumTaxFreight1').val())) || 0;
        const FRT_TAXAMT = getDecimal($.trim($('#NumTaxFreight2').val())) || 0;
        const WB_REM = $.trim($('#txt_wbRemark').val()) || "";
        const TOT_GROSS = getDecimal($.trim($('#NumGrossQty').val())) || 0;
        const TOT_NET = getDecimal($.trim($('#NumNetQty').val())) || 0;
        const GR_NO = $.trim($('#NumGRNo').val()) || "";
        let GR_DATE = null;
        if ($('#chkGRDate').is(':checked') && $.trim($('#DtGRDate').val())) {
            GR_DATE = formatDate($('#DtGRDate').val());
        }

        const VEHICLE_NO = $.trim($('#TxtTruckNo').val()) || "";
        const TRANSPORT_CODE = parseInt($.trim($('#ddlTransport').val()), 10) || 0;
        const TRANSPORT_NAME = TRANSPORT_CODE ? $.trim($('#ddlTransport option:selected').text()) : "";
        const DRIVER_NAME = $.trim($('#TxtDriverName').val()) || "";
        const DRIVER_NO = $.trim($('#NumDriverMob').val()) || "";
        const TPT_DISTANCE = parseInt($.trim($('#NumDistance').val()), 10) || 0;
        const TPT_MODE = parseInt($.trim($('#ddlMode').val()), 10) || 0;
        const WAYBILL_NO = $.trim($('#txt_WayBillNo').val()) || "";
        const FRT_TOPAY = parseFloat($.trim($('#txt_ToPayFrt').val())) || 0;
        const REMARK = $.trim($('#TxtTransRemarks').val()) || "";
        const DISC_PER = parseFloat($.trim($('#NumOtherDiscount1').val())) || 0;
        const DISC_AMT = getDecimal($.trim($('#NumOtherDiscount2').val())) || 0;
        const CDISC_PER = parseFloat($.trim($('#NumOtherCashDiscount1').val())) || 0;
        const CDISC_AMT = getDecimal($.trim($('#NumOtherCashDiscount2').val())) || 0;
        const AGENT_CODE = parseInt($.trim($('#ddlSalesThrough').val()), 10) || 0;
        const FORM_CODE = parseInt($.trim($('#ddlFormType').val()), 10) || 0;
        const DELIVERY_TERMS = $.trim($('#TxtDeliveryTerm').val()) || "";
        const SUPPLY_TYPE = $.trim($('#ddlSupplyType').val()) || "";
        const LC_NO = $.trim($('#ddl_licNo').val()) || "";
        const TRAN_TYPE = $.trim($('#ddlTransactionType').val()) || "";
        const SHIPMENT_TYPE = '';
        const STATUS = parseInt($.trim($('#ddlDocStatus').val()), 10) || 0;            
        const action = (!rowId || rowId.trim() === "") ? "INSERT" : "UPDATE";
        // =========================================================


        if (BILL_NAME !== SHIP_NAME && TRAN_TYPE === "Regular") {

            const result = await Swal.fire({
                title: "Confirmation",
                text: "The Billing Party and Shipping Party are different. In this case, the Transaction Type should not be Regular. Do you want to continue?",
                icon: "warning",
                showCancelButton: true,
                confirmButtonText: "Yes",
                cancelButtonText: "No"
            });

            if (!result.isConfirmed) {
                $('#ddlTransactionType').focus();
                return;
            }
        }

        else if (BILL_NAME === SHIP_NAME && TRAN_TYPE === "Regular") {

            const result = await Swal.fire({
                title: "Confirmation",
                text: "The Billing Party and Shipping Party are Same; in this case, the Transaction Type should be Regular. Do you want to Continue ?",
                icon: "warning",
                showCancelButton: true,
                confirmButtonText: "Yes",
                cancelButtonText: "No"
            });

            if (!result.isConfirmed) {
                $('#ddlTransactionType').focus();
                return;
            }
        }


        if (V_TYPE == "SAGT" && (ITEM_TYPE == "Waste" || ITEM_TYPE == "Scrap")) {
            const result = await Swal.fire({
                title: "Confirmation",
                text: "TCS Percentage must be 2% for Scrap and Waste. Do you want to Continue ?",
                icon: "warning",
                showCancelButton: true,
                confirmButtonText: "Yes",
                cancelButtonText: "No"
            });

            if (!result.isConfirmed) {
                return;
            }
        }


        if (GR_NO !== "" && GR_DATE === "") {

            toastr.warning("GR Date is required with GR No.");
            return;
        }

        if (FRT_AMT > 0) {
            if (!validateRequiredField('#ddlTransport', 'Please select Transport')) return;
        }

        if (LOAD_AMT > 0 && LOAD_AC == "") {

            toastr.warning("Please select Loading Party.");
            $('#txt_LoadAmt').focus();
            return;
        }

        if (WB_AMT > 0 && WB_AC == "") {
            toastr.warning("Please select Weighbridge Party.");
            $('#txt_WBamt').focus();
            return;
        }

        if (EXRATE > 0) {
            if (!validateRequiredField('#txxt_pol', 'Please enter POL.')) return;
            if (!validateRequiredField('#txxt_ExRate', '"Please enter POD.')) return;
            if (!validateRequiredField('#txt_receiptat', 'Please enter Place of receipt.')) return;
            if (!validateRequiredField('#txxt_finalDestination', 'Please enter Final destination.')) return;
            if (!validateRequiredField('#txxt_FDCountry', 'Please enter Final destination country.')) return;

        }


        if (V_TYPE == "SAGT" || V_TYPE == "SAJI" || V_TYPE == "JBIS") {

            if (!validateRequiredField('#NumDistance', 'Please enter approx. Distance.')) return;
            if (!validateRequiredField('#ddlMode', 'Please select Mode of Transport.')) return;

        }

        if (!validateRequiredField('#ddlTaxType', 'Please select Valid Tax Type')) return;








        const Header = {
            AMOUNT: AMOUNT,
            AGENT_CODE: AGENT_CODE,
            AGENT_Name: AGENT_Name,
            BANK_CODE: BANK_CODE,
            DoType: DoType,
            Do_NO: Do_NO,
            DOC_ID: DOC_ID,
            V_TYPE: V_TYPE,
            V_NO: V_NO,
            V_DATE: V_DATE,
            BILL_NAME: BILL_NAME,
            BILL_CODE: BILL_CODE,
            BILL_ADDRESSID: BILL_ADDRESSID,
            BILL_ADD1: BILL_ADD1,
            BILL_ADD2: BILL_ADD2,
            BILL_ADD3: BILL_ADD3,
            BILL_CITY: BILL_CITY,
            BILL_CITYName: BILL_CITYName,
            BILL_PINCODE: BILL_PINCODE,
            BILL_GST: BILL_GST,
            AGENT_CODE: AGENT_CODE,
            AGENT_Name: AGENT_Name,
            SUPPLY_TYPE: SUPPLY_TYPE,
            SHIP_CODE: SHIP_CODE,
            SHIP_NAME: SHIP_NAME,
            SHIP_ADD1: SHIP_ADD1,
            SHIP_ADD2: SHIP_ADD2,
            SHIP_ADD3: SHIP_ADD3,
            SHIP_ADDRESSID: SHIP_ADDRESSID,
            SHIP_CITY: SHIP_CITY,
            SHIP_CITYNAME: SHIP_CITYNAME,
            SHIP_PINCODE: SHIP_PINCODE,
            SHIP_GST: SHIP_GST,
            FORM_CODE: FORM_CODE,
            TRAN_TYPE: TRAN_TYPE,
            TAX_CODE: TAX_CODE,
            PACK_NO: PACK_NO,
            PACK_TYPE: PACK_TYPE,
            PACK_PER: PACK_PER,
            PACK_AMT: PACK_AMT,
            SAUDA_NO: SAUDA_NO,
            SAUDA_TYPE: SAUDA_TYPE,
            SAUDA_RATE: SAUDA_RATE,
            ISSUE_NO: ISSUE_NO,
            ISSUE_TYPE: ISSUE_TYPE,
            GODOWN_CODE: GODOWN_CODE,
            ITEM_TYPE: ITEM_TYPE,
            REMARK: REMARK,
            WB_NO: WB_NO,
            WB_TYPE: WB_TYPE,
            WB_QTY: WB_QTY,
            WB_AMT: WB_AMT,
            WB_REM: WB_REM,
            WB_AC: WB_AC,
            DISC_PER: DISC_PER,
            DISC_AMT: DISC_AMT,
            CDISC_PER: CDISC_PER,
            CDISC_AMT: CDISC_AMT,
            CGST_PER: CGST_PER,
            CGST_AMT: CGST_AMT,
            SGST_PER: SGST_PER,
            SGST_AMT: SGST_AMT,
            IGST_PER: IGST_PER,
            IGST_AMT: IGST_AMT,
            CESS_PER: CESS_PER,
            CESS_AMT: CESS_AMT,
            TCS_PER: TCS_PER,
            TCS_AMT: TCS_AMT,
            ROUND_OFF: ROUND_OFF,
            TOT_NOS: TOT_NOS,
            TOT_GROSS: TOT_GROSS,
            TOT_NET: TOT_NET,
            TRANSPORT_CODE: TRANSPORT_CODE,
            TRANSPORT_NAME: TRANSPORT_NAME,
            GR_NO: GR_NO,
            GR_DATE: GR_DATE,
            VEHICLE_NO: VEHICLE_NO,
            DRIVER_NAME: DRIVER_NAME,
            DRIVER_NO: DRIVER_NO,
            INSU_PER: INSU_PER,
            INSU_AMT: INSU_AMT,
            INSU_DETAIL: INSU_DETAIL,
            TDS_PER: TDS_PER,
            TDS_AMT: TDS_AMT,
            FRT_TAXPER: FRT_TAXPER,
            FRT_TAXAMT: FRT_TAXAMT,
            FRT_AMT: FRT_AMT,
            FRT_TOPAY: FRT_TOPAY,
            TPT_DISTANCE: TPT_DISTANCE,
            TPT_MODE: TPT_MODE,
            PAY_TERM: PAY_TERM,
            PAYMENT_TERM: PAYMENT_TERM,
            DELIVERY_TERMS: DELIVERY_TERMS,
            WAYBILL_NO: WAYBILL_NO,
            LOAD_AC: LOAD_AC,
            LOAD_PER: LOAD_PER,
            LOAD_AMT: LOAD_AMT,
            LOAD_REM: LOAD_REM,
            NAMOUNT: NAMOUNT,
            EXRATE: EXRATE,
            CURRENCY: CURRENCY,
            DEFECTIVE_GOODS: DEFECTIVE_GOODS,
            CAL_ONPCS: CAL_ONPCS,
            PRINT_DETAIL: PRINT_DETAIL,
            BUYER_ORDNO: BUYER_ORDNO,
            PLACE_RECEIPT: PLACE_RECEIPT,
            PORT_LOADING: PORT_LOADING,
            PORT_DISCHARGE: PORT_DISCHARGE,
            PORT_CODE: PORT_CODE,
            FINAL_DEST: FINAL_DEST,
            FINAL_DEST_COUNTRY: FINAL_DEST_COUNTRY,
            SB_NO: SB_NO,
            SB_DATE: SB_DATE,
            FOB_VALUE: FOB_VALUE,
            FOB_FRT: FOB_FRT,
            FOB_INSU: FOB_INSU,
            FOB_OTHER: FOB_OTHER,
            LUT_NO: LUT_NO,
            LUT_DETAIL: LUT_DETAIL,
            LUT_DATE: LUT_DATE,
            INCOTERM: INCOTERM,
            BILLOF_LADING: BILLOF_LADING,
            LC_NO: LC_NO,
            action: action,
            STATUS: STATUS,
            LICENCE_TYPE: LICENCE_TYPE,
            LICENCE_NO: LICENCE_NO,
            LICENCE_DATE: LICENCE_DATE
        };

        const Details = GetSalesInvoiceDetails();



        const model = {
            Header: Header,
            Details: Details
        };

        console.log("FINAL MODEL:");
        console.log(JSON.stringify(model, null, 2));
        $("#btn-save").prop("disabled", true);
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
                    errorMessage = "Bad Request: " + xhr.responseText;
                }
                else if (xhr.status === 500) {
                    errorMessage = "Server error: " + xhr.responseText;
                }
                else {
                    errorMessage = "Unexpected error: " + xhr.statusText;
                }

                toastr.error("Error: " + errorMessage);
            },

            complete: function () {
                $("#btn-save").prop("disabled", false);
            }
        });
    });








    //kks

    $(document).on('click', '#btn_Sendapproval', function () {
        var FromName = window.location.pathname.split('/')[1];
        let vNo = $('#NumInvoiceNo').val();
        let vtype = $('#ddlDocumentType').val();


        $.ajax({
            url: '/Approval/CheckPendingUser',
            type: 'POST',
            data: {
                vNo: vNo,
                vType: vtype
            },
            success: function (response) {
                console.log('Response:', response);
                // Pending with another user
                if (response.success === false) {
                    showToast(`Pending With Another User (${response.userCode})`, { type: "warning" });
                    return;
                }
                // Approval_Code = 5
                if (response.approvalCode8 === true)
                {
                    OpenApprovalModal({ DocType: vtype, DocNo: vNo,  TableName: 'SALE1' });
                    return;
                }
                // Approval_Code != 8
                OpenSendForApprovalModal({
                    DocType: vtype, DocNo: vNo, UserCode: null, UserName: null,
                    DocDate: null, TableName: 'SALE1', FromName, FromName
                });

            },
            error: function (xhr, status, error) {
                console.log(error);
                alert('Error while checking approval status.');
            }
        });

    });

    $(document).on('click', '#btn_Approved', function () {
        OpenApprovalModal({ DocType: vtype, DocNo: vNo, TableName: 'SALE1' });
    });

    //kks

});