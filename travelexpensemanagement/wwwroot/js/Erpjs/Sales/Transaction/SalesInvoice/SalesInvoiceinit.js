

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
        let selectedVType = $('#ddlInvType').val();
        await GetVNo(selectedVType, "SALE1");
    }

    $('#ddlPartyName').on('change', function () {
        selectedPartyData();
        let partycode = $('#ddlPartyName').val();
         DDlPackNo();
    });

});