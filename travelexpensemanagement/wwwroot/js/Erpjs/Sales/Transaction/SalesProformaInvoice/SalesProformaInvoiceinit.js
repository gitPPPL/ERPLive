
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








});