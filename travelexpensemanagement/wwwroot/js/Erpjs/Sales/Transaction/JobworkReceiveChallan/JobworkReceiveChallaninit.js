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
let SaudaRateList = [];
let TaxTypeList = '';
let TaxPercentageData = [];
let ProductList = '';
let ProductDataList = [];

$(document).ready(async function ()
{
    SetFYDate('DtDocumentDate', LoginDate);
    checkPermissionForEntryPage(controllerName);
    await LoadDropdown();
          
     AddRow();
    if (rowId)
    {
        await LoadData();
        vtype = $('#ddlDocumentType').val();
        vNo = $('#NumDocumentNo').val();
        checkApprovalStatus(vtype, rowId, 'SALE1');

        $('#tblJobworkReceiveChallan tbody tr').each(function () {
            const $row = $(this);
            const itemCode = $.trim($row.find('.ddlProductName').val() || '');
            if (itemCode !== '')
            {          
                CalculateRow($row);
            }
        });

        if (mode == 'view')
        {
            SetFormReadOnly();
        }

    }
    else
    {     
        vtype = $('#ddlDocumentType').val();
        await GetVNo(vtype, "SALE1");
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

    $('#ddlTaxType').on('change', function () {

        const selectedCode = $(this).val();

        const selectedTax = TaxPercentageData.find(
            item => String(item.code) === String(selectedCode)
        );

        console.log("Selected Code:", selectedCode);
        console.log("Selected Tax Data:", selectedTax);

        if (!selectedTax) {
            $('#tblSalesInvoice tbody tr').each(function () {

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

            // Recalculate row if required
            if (typeof CalculateRow === 'function') {
                CalculateRow($row);
            }

        });

    });

    $("#btn-save").click(async function (e) {
        e.preventDefault();

        if (!validateRequiredField('#ddlDocumentType', 'Please select a Voucher Type')) return;
        if (!validateRequiredField('#NumDocumentNo', 'Please Fill Voucher No')) return;
        if (!validateRequiredField('#DtDocumentDate', 'Please select a Voucher Date.')) return;
        if (!validateRequiredField('#ddlPartyName', 'Please select a Party Name.')) return;

        const isValid = await checkValidDate();
        if (isValid === false)
        {
            return;
        }

 

        const DOC_ID = $.trim($('#TxtCode').val()) || "";
        const V_TYPE = $.trim($('#ddlDocumentType').val()) || "";
        const V_NO = parseInt($.trim($('#NumDocumentNo').val()), 10) || 0;



        if (V_TYPE != "SAJR")
        {
            if (!validateRequiredField('#ddlReferenceNo', 'Please Select Reference No')) return;
        }



        // Validate quantity when Item Name is selected
        let isQtyValid = true;

        $('#tblJobworkReceiveChallan tbody tr').each(function () {
            const $row = $(this);
            const itemName = $.trim($row.find('.ddlProductName').val() || '');
            const qty = $.trim($row.find('.TxtNetQty').val());
            // Check quantity only if Item Name is selected
            if (itemName !== '' && (qty === '' || isNaN(Number(qty)) || Number(qty) <= 0)) {
                toastr.warning('Please enter a valid quantity for the selected Item Name.');
                $row.find('.TxtNos').focus();
                isQtyValid = false;
                return false; // Stop checking rows
            }
        });
        if (!isQtyValid)
        {
            return;
        }

        let V_DATE = null;
        if ($.trim($('#DtDocumentDate').val()))
        {
            V_DATE = formatDate($('#DtDocumentDate').val());
        }

        const BILL_NAME = $.trim($('#ddlPartyName option:selected').text()) || "";
        const BILL_CODE = parseInt($.trim($('#ddlPartyName').val()), 10) || 0;
        const BILL_ADDRESSID = parseInt($.trim($('#ddladdressL1').val()), 10) || 0;
        const BILL_ADD1 = $.trim($('#TxtAddressL1').val()) || "";
        const BILL_ADD2 = $.trim($('#TxtAddressL2').val()) || "";
        const BILL_ADD3 = $.trim($('#TxtAddressL3').val()) || "";
        const BILL_CITY = parseInt($.trim($('#ddlStation').val()), 10) || 0;
        const BILL_CITYName = $.trim($('#ddlStation option:selected').text()) || "";
        const BILL_PINCODE = $.trim($('#NumPincode').val()) || "";

        const SHIP_CODE = parseInt($.trim($('#ddlConsignee').val()), 10) || 0;
        const SHIP_NAME = SHIP_CODE ? $.trim($('#ddlConsignee option:selected').text()) : "";
        const SHIP_ADD1 = $.trim($('#TxtTransactionAddressL1').val()) || "";
        const SHIP_ADD2 = $.trim($('#TxtTransactionAddressL2').val()) || "";
        const SHIP_ADD3 = $.trim($('#TxtTransactionAddressL3').val()) || "";
        const SHIP_ADDRESSID = parseInt($.trim($('#ddlTransactionaddressL1').val()), 10) || 0;
        const SHIP_CITY = parseInt($.trim($('#TxtTransactionStation').val()), 10) || 0;
        const SHIP_CITYNAME = SHIP_CITY ? $.trim($('#TxtTransactionStation option:selected').text()) : "";
        const SHIP_PINCODE = $.trim($('#NumTransactionPIN').val()) || "";

        const TAX_CODE = parseInt($.trim($('#ddlTaxType').val()), 10) || 0;
        const PACK_NO = parseInt($.trim($('#ddlPackNo').val()), 10) || 0;
        const PACK_TYPE = PACK_NO ? $.trim($('#ddlPackNo option:selected').text()).split('-').pop().trim() : "";
        const ITEM_TYPE = $.trim($('#ddlProductionType').val()) || "";

        const AMOUNT = ($.trim($('#NumOtherTotalAmount').val())) || 0;
        const PACK_PER = ($.trim($('#NumOtherPacking1').val())) || 0;
        const PACK_AMT = ($.trim($('#NumOtherPacking2').val())) || 0;

        const CGST_PER = ($.trim($('#NumOtherCGST1').val())) || 0;
        const CGST_AMT = ($.trim($('#NumOtherCGST2').val())) || 0;
        const SGST_PER = ($.trim($('#NumOtherSGST1').val())) || 0;
        const SGST_AMT = ($.trim($('#NumOtherSGST2').val())) || 0;
        const IGST_PER = ($.trim($('#NumOtherIGST1').val())) || 0;
        const IGST_AMT = ($.trim($('#NumOtherIGST2').val())) || 0;
        const CESS_PER = ($.trim($('#NumOtherCESS1').val())) || 0;
        const CESS_AMT = ($.trim($('#NumOtherCESS2').val())) || 0;

        const LOAD_AC = $.trim($('#ddlLoadParty').val()) || "";
        const LOAD_PER = ($.trim($('#NumLoadAmount1').val())) || 0;
        const LOAD_AMT = ($.trim($('#NumLoadAmount2').val())) || 0;
        const LOAD_REM = $.trim($('#TxtRemarks1').val()) || "";

        const WB_AMT = ($.trim($('#NumWBAmount').val())) || 0;
        const WB_AC = $.trim($('#ddlWBParty').val()) || "";
        const WB_NO = parseInt($.trim($('#ddlWBNo').val()), 10) || 0;
        const WB_TYPE = WB_NO ? $.trim($('#ddlWBNo option:selected').text()).split('-').pop().trim() : "";

        const TOT_NOS = parseInt($.trim($('#NumOtherTotalNos').val()), 10) || 0;
        const FRT_AMT = ($.trim($('#NumConsFreight').val())) || 0;
        const ROUND_OFF = ($.trim($('#NumOtherRoundOff').val())) || 0;
        const NAMOUNT = ($.trim($('#NumOtherNetAmount').val())) || 0;

        const INSU_PER = ($.trim($('#NumInsurance1').val())) || 0;
        const INSU_AMT = ($.trim($('#NumInsurance2').val())) || 0;

        const TCS_PER = ($.trim($('#NumOtherTCS1').val())) || 0;
        const TCS_AMT = ($.trim($('#NumOtherTCS2').val())) || 0;
        const TDS_PER = ($.trim($('#NumTDSFreight1').val())) || 0;
        const TDS_AMT = ($.trim($('#NumTDSFreight2').val())) || 0;

        const WB_REM = $.trim($('#TxtRemarks2').val()) || "";
        const TOT_GROSS = ($.trim($('#NumGrossQty').val())) || 0;
        const TOT_NET = ($.trim($('#NumNetQty').val())) || 0;

        const GR_NO = $.trim($('#NumGRNo').val()) || "";
        let GR_DATE = null;

        if ($('#chkGRDate').is(':checked') && $.trim($('#DtGRdate').val())) {
            GR_DATE = formatDate($('#DtGRdate').val());
        }

        const VEHICLE_NO = $.trim($('#NumTruckNo').val()) || "";
        const TRANSPORT_CODE = parseInt($.trim($('#ddlTransport').val()), 10) || 0;
        const TRANSPORT_NAME = TRANSPORT_CODE ? $.trim($('#ddlTransport option:selected').text()) : "";
        const DRIVER_NAME = $.trim($('#txtDriverName').val()) || "";
        const DRIVER_NO = $.trim($('#NumMobileNo').val()) || "";
        const WAYBILL_NO = $.trim($('#NumWayBillNo').val()) || "";
        const REMARK = $.trim($('#TxtRemarks3').val()) || "";
        const WB_QTY = ($.trim($('#NumWBQty').val())) || 0;

        const DISC_PER = ($.trim($('#NumOtherDiscount1').val())) || 0;
        const DISC_AMT = ($.trim($('#NumOtherDiscount2').val())) || 0;
        const FRT_TOPAY = ($.trim($('#NumPayFreight').val())) || 0;

        const AGENT_CODE = parseInt($.trim($('#ddlSaleThrough').val()), 10) || 0;
        const AGENT_Name = AGENT_CODE ? $.trim($('#ddlSaleThrough option:selected').text()) : "";

        const FORM_CODE = parseInt($.trim($('#ddlFormType').val()), 10) || 0;
        const SAUDA_NO = parseInt($.trim($('#ddlSaudaNo').val()), 10) || 0;
        const SAUDA_TYPE = SAUDA_NO ? $.trim($('#ddlSaudaNo option:selected').text()).split('-').pop().trim() : "";
        const SAUDA_RATE = $.trim($('#NumSaudaRate').val()) || 0;   

        const CAL_ONPCS = $('#ChkPCS').is(':checked') ? 1 : 0;
        const INSU_DETAIL = $.trim($('#ddlNatureJW').val()) || "";
        const TRAN_TYPE = $.trim($('#ddlTransactionThrough').val()) || "";
        const SUPPLY_TYPE = $.trim($('#ddlSupplyType').val()) || "";
        const STATUS = parseInt($.trim($('#ddlDocStatus').val()), 10) || 0;

        const action = (!rowId || rowId.trim() === "") ? "INSERT" : "UPDATE";
        const BUYER_ORDNO = $.trim($('#NumPartyChallanNo').val()) || "";
        const GATE_NO = parseInt($.trim($('#ddlGateNo').val()), 10) || 0;
        const GATE_TYPE = GATE_NO ? $.trim($('#ddlGateNo option:selected').text()).split('-').pop().trim() : "";

        const REF_NO = parseInt($.trim($('#ddlReferenceNo').val()), 10) || 0;
        const REF_TYPE = REF_NO ? $.trim($('#ddlReferenceNo option:selected').text()).split('-').pop().trim() : "";

        let REF_DATE = null;
        if ($.trim($('#DtReferenceDate').val()))
        {
            REF_DATE = formatDate($('#DtReferenceDate').val());
        }

        const shipment_type = $.trim($('#ddlJobType').val()) || "";

        if (!validateRequiredField('#ddlTaxType', 'Please select Valid Tax Type')) return;

        const Header = {       
            DOC_ID: DOC_ID,
            FRT_TOPAY: FRT_TOPAY ,
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
            SHIP_CODE: SHIP_CODE,
            SHIP_NAME: SHIP_NAME,
            SHIP_ADD1: SHIP_ADD1,
            SHIP_ADD2: SHIP_ADD2,
            SHIP_ADD3: SHIP_ADD3,
            SHIP_ADDRESSID: SHIP_ADDRESSID,
            SHIP_CITY: SHIP_CITY,
            SHIP_CITYNAME: SHIP_CITYNAME,
            SHIP_PINCODE: SHIP_PINCODE,
            TAX_CODE: TAX_CODE,
            PACK_NO: PACK_NO,
            PACK_TYPE: PACK_TYPE,
            ITEM_TYPE: ITEM_TYPE,
            AMOUNT: AMOUNT,
            PACK_PER: PACK_PER,
            PACK_AMT: PACK_AMT,
            CGST_PER: CGST_PER,
            CGST_AMT: CGST_AMT,
            SGST_PER: SGST_PER,
            SGST_AMT: SGST_AMT,
            IGST_PER: IGST_PER,
            IGST_AMT: IGST_AMT,
            CESS_PER: CESS_PER,
            CESS_AMT: CESS_AMT,
            LOAD_AC: LOAD_AC,
            LOAD_PER: LOAD_PER,
            LOAD_AMT: LOAD_AMT,
            LOAD_REM: LOAD_REM,
            WB_AMT: WB_AMT,
            WB_AC: WB_AC,
            WB_NO: WB_NO,
            WB_TYPE: WB_TYPE,
            WB_REM: WB_REM,
            WB_QTY: WB_QTY,
            BUYER_ORDNO: BUYER_ORDNO,
            TOT_NOS: TOT_NOS,
            TOT_GROSS: TOT_GROSS,
            TOT_NET: TOT_NET,
            FRT_AMT: FRT_AMT,
            ROUND_OFF: ROUND_OFF,
            NAMOUNT: NAMOUNT,
            INSU_PER: INSU_PER,
            INSU_AMT: INSU_AMT,
            INSU_DETAIL: INSU_DETAIL,
            TCS_PER: TCS_PER,
            TCS_AMT: TCS_AMT,
            TDS_PER: TDS_PER,
            TDS_AMT: TDS_AMT,
            GR_NO: GR_NO,
            GR_DATE: GR_DATE,
            VEHICLE_NO: VEHICLE_NO,
            TRANSPORT_CODE: TRANSPORT_CODE,
            TRANSPORT_NAME: TRANSPORT_NAME,
            DRIVER_NAME: DRIVER_NAME,
            DRIVER_NO: DRIVER_NO,
            WAYBILL_NO: WAYBILL_NO,
            REMARK: REMARK,
            DISC_PER: DISC_PER,
            DISC_AMT: DISC_AMT,
            AGENT_CODE: AGENT_CODE,
            AGENT_Name: AGENT_Name,
            FORM_CODE: FORM_CODE,
            SAUDA_NO: SAUDA_NO,
            SAUDA_TYPE: SAUDA_TYPE,
            SAUDA_RATE: SAUDA_RATE,
            CAL_ONPCS: CAL_ONPCS,
            GATE_TYPE: GATE_TYPE,
            GATE_NO: GATE_NO,
            REF_TYPE: REF_TYPE,
            REF_NO: REF_NO,
            REF_DATE: REF_DATE,
            shipment_type: shipment_type,
            TRAN_TYPE: TRAN_TYPE,
            SUPPLY_TYPE: SUPPLY_TYPE,
            STATUS: STATUS,
            action: action
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
            url: '/JobworkReceiveChallan/SavedData',
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
                            `/JobworkReceiveChallan/Index?id=${Header.DOC_ID}&mode=view`;
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

    $('#NumOtherPacking1').on('change', function () {
        const packingPer = parseFloat($(this).val()) || 0;
        if (packingPer > 0) {
            $('#tblJobworkReceiveChallan tbody tr').each(function () {
                const $row = $(this);
                const itemCode = $.trim($row.find('.ddlProductName').val() || '');
                if (itemCode !== '') {
                    $row.find('.TxtPacKPer').val(packingPer);
                    CalculateRow($row);
                }
            });
        }
    });

    $('#NumOtherCESS1').on('change', function () {
        const CessPer = parseFloat($(this).val()) || 0;
        if (CessPer > 0) {
            $('#tblJobworkReceiveChallan tbody tr').each(function () {
                const $row = $(this);
                const itemCode = $.trim($row.find('.ddlProductName').val() || '');
                if (itemCode !== '') {
                    $row.find('.TxtCessPer').val(CessPer);
                    CalculateRow($row);
                }
            });
        }
    });

    $('#NumOtherDiscount1').on('change', function () {
        const DiscPer = parseFloat($(this).val()) || 0;
        if (DiscPer > 0) {
            $('#tblJobworkReceiveChallan tbody tr').each(function () {
                const $row = $(this);
                const itemCode = $.trim($row.find('.ddlProductName').val() || '');
                if (itemCode !== '') {
                    $row.find('.TxtDisPer').val(DiscPer);
                    CalculateRow($row);
                }
            });
        }
    });

    $('#NumOtherTCS1').on('change', function () {
        const TCSPer = parseFloat($(this).val()) || 0;
        if (TCSPer > 0) {
            $('#tblJobworkReceiveChallan tbody tr').each(function () {
                const $row = $(this);
                const itemCode = $.trim($row.find('.ddlProductName').val() || '');
                if (itemCode !== '') {                
                    CalculateRow($row);
                }
            });
        }
    });

    $('#NumOtherCGST1').on('change', function () {
        const CGSTPer = parseFloat($(this).val()) || 0;
        if (CGSTPer > 0) {
            $('#tblJobworkReceiveChallan tbody tr').each(function () {
                const $row = $(this);
                const itemCode = $.trim($row.find('.ddlProductName').val() || '');
                if (itemCode !== '') {
                    $row.find('.TxtCgstper').val(CGSTPer);
                    CalculateRow($row);
                }
            });
        }
    });

    $('#NumOtherSGST1').on('change', function () {
        const SGSTPer = parseFloat($(this).val()) || 0;
        if (SGSTPer > 0) {
            $('#tblJobworkReceiveChallan tbody tr').each(function () {
                const $row = $(this);
                const itemCode = $.trim($row.find('.ddlProductName').val() || '');
                if (itemCode !== '') {
                    $row.find('.TxtSgstPer').val(SGSTPer);
                    CalculateRow($row);
                }
            });
        }
    });

    $('#NumOtherIGST1').on('change', function () {
        const IGSTPer = parseFloat($(this).val()) || 0;
        if (IGSTPer > 0) {
            $('#tblJobworkReceiveChallan tbody tr').each(function () {
                const $row = $(this);
                const itemCode = $.trim($row.find('.ddlProductName').val() || '');
                if (itemCode !== '') {
                    $row.find('.TxtIGSTPer').val(IGSTPer);
                    CalculateRow($row);
                }
            });
        }
    });

    $('#button_print').on('click', async function () {
        TransitReport();
    });

    $('#button_mail').on('click', async function () {
        SendMail();
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