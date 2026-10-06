

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