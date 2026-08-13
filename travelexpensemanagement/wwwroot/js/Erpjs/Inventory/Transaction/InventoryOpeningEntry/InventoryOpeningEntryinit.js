const urlParams = new URLSearchParams(window.location.search);
const rowId = urlParams.get('id');
const mode = urlParams.get('mode');
const isReadOnly = (mode === 'view');


var globalVars = window.globalVariables || {};
var database = window.database || "";
let PubUserLevel = globalVars.UserLevel;
let CompCode = globalVars.CompCode;
let LoginDate = globalVars.LoginDate;
var controllerName = window.location.pathname.split('/')[1];

let ItemNameList = "";
let unitnameList = "";
let ItemmakeList = "";
let ItemDeptList = "";


$(document).ready(async function ()
{

    try {
        SetFYDate('DtDocDate', LoginDate);
        await LoadDropDown();
        AddRow();
        if (rowId == null) {
            let v_type = $('#ddlDocType').val();

            if (v_type) {
                await GetVNo(v_type);
            }
        }

    }
    catch (error)
    {
        console.error("An error occurred:", error);
    }

    $('#ddlDocType').on('change', async function () {

        let v_type = $(this).val();

        if (v_type) {
            await GetVNo(v_type);
        }

    });

    $(document).on('input', '.TxtQty, .TxtRate', function () {

        let row = $(this).closest('tr');

        let qty = parseFloat(row.find('.TxtQty').val()) || 0;
        let rate = parseFloat(row.find('.TxtRate').val()) || 0;

        row.find('.TxtAmount').val(qty * rate);
    });

    $(document).on('change', '.ddlItemname', async function () {
        let row = $(this).closest('tr');
        let ItemCode = parseFloat($(this).val()) || 0;
        if (!ItemCode) {
            row.find('.ddlUnit').val('');
            return;
        }
        try {
            const res = await $.ajax({
                url: 'InventoryOpeningEntry/GetDataByItemcode',
                type: 'POST',
                data: { ItemCode: ItemCode }
            });

            console.log("res", res);

            if (res.status && res.data)
            {
                let unitCode = res.data.unit_code;
                row.find('.ddlUnit').val(unitCode);
                row.find('.ddlUnit').trigger('change');
            }
            else
            {
                row.find('.ddlUnit').val('');
            }

        }
        catch (error)
        {
            console.error("Error fetching item data:", error);
        }
    });




    $('#btn_save').on('Click', async function () {

        let V_TYPE = $('#ddlDocType').val();
        let V_NO = $('#NumDocno').val();
        let V_DATE = $('#DtDocDate').val();
        let REMARKS = $('#TxtRemarks').val();
        

        let inventoryData = GetInventoryOpeningData();

        console.log(inventoryData);
          


    });


});
