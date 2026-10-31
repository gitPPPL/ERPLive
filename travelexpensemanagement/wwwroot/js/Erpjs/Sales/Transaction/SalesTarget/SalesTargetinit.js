
const urlParams = new URLSearchParams(window.location.search);
const rowId = urlParams.get('id');
let vtype = "";
let vNo = "";
const mode = urlParams.get('mode');
const isReadOnly = (mode === 'view');
var globalVars = window.globalVariables || {};
var database = window.database || "";
var LoadGeneralSetting = window.LoadGeneralSetting || {};
let PubUserLevel = globalVars.UserLevel;
let CompCode = globalVars.CompCode;
let LoginDate = globalVars.LoginDate;
var controllerName = window.location.pathname.split('/')[1];
let ProductList = '';
let salesThroughList = '';

$(document).ready(async function ()
{
    SetFYDate('DtDocumentDate', LoginDate);
    await LoadDropdown();

    if (rowId) {
        await LoadData();
        if (mode == 'view')
        {
            SetFormReadOnly();
        }
    }
    else
    {

        await GetVNo("STGT", "SALES_TARGET");
    }

    $('#btn_copydata').on('click', async function () {
        const $btn = $(this);

        try {
            const V_date = $('#DtImportfromDate').val();

            if (!V_date) {
                alert('Please select the import from date.');
                return;
            }

            if ($btn.prop('disabled')) return;

            $btn.prop('disabled', true).text('Loading...');

            const res = await $.ajax({
                url: '/SalesTarget/GetCopyData',
                type: 'GET',
                data: { V_date: V_date },
                dataType: 'json'
            });

            console.log('Copy Data:', res);

            if (!Array.isArray(res) || res.length === 0) {
                return;
            }

            const $tbody = $('#tblSalesTaget tbody');

            // Clear existing rows
            $tbody.empty();

            // Add all rows directly
            res.forEach(function (item) {
                AddRow({
                    ID: item.iteM_CODE,
                    Productcode: item.iteM_CODE,
                    LastMonsale: item.qty
                });
            });

        } catch (error) {
            console.error('Error fetching copy data:', error);
            alert('Failed to fetch copy data.');
        } finally {
            $btn.prop('disabled', false).text('Copy Data');
        }
    });

    $("#btn-save").click(async function (e) {
        e.preventDefault();

        if (!validateRequiredField('#NumDocumentNo', 'Please Fill Voucher No')) return;
        if (!validateRequiredField('#DtDocumentDate', 'Please select a Voucher Date.')) return;
        if (!validateRequiredField('#ddlPartyName', 'Please select a Party Name.')) return;
        if (!validateRequiredField('#DtPalningMonthDate', 'Please select a Planning of Month.')) return;
    
        const Details = GetSalesTargetDetails();

        const model =
        {
            Details: Details
        };

        $("#btn-save").prop("disabled", true);
        $.ajax({
            url: '/SalesTarget/SavedData',
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            data: JSON.stringify(model),

            success: function (response)
            {
                console.log("Response:", response);
                if (response.status === "Success")
                {
                    toastr.success("Saved successfully!");
                    setTimeout(function () {  window.location.href = `/SalesTarget/Index?id=${rowId}&mode=view`; }, 1000);
                }
                else if (response.status === "Validation")
                {
                    toastr.warning(response.message);
                }
                else
                {
                    toastr.error( response.message || "Save failed." );
                }
            },
            error: function (xhr) {
                console.log("HTTP Status:", xhr.status);
                console.log("Response:", xhr.responseText);
                let errorMessage = "Something went wrong.";
                if (xhr.status === 400)
                {
                    errorMessage = "Bad Request: " + xhr.responseText;
                }
                else if (xhr.status === 500)
                {
                    errorMessage = "Server error: " + xhr.responseText;
                }
                else
                {
                    errorMessage = "Unexpected error: " + xhr.statusText;
                }

                toastr.error("Error: " + errorMessage);
            },

            complete: function () {
                $("#btn-save").prop("disabled", false);
            }
        });
    });

});